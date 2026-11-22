using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Tutorial jugando, con un guia que te habla en una caja de dialogo retro.
// Va en SU PROPIA escena (Scene_Tutorial, copia de Scene_Game). Ponlo en el Canvas.
// Cada paso espera a que lo hagas:
//  1) Moverse   2) Atrapar la que brilla   3) Esquivar las que no brillan
//  4) Parry (ESPACIO)   5) Foco (SHIFT)
// Al terminar (o con SALTAR / Enter) la pantalla se funde a negro y carga la pelea.
public class Tutorial : MonoBehaviour
{
    public static bool Active { get; private set; }

    [Header("Referencias")]
    [SerializeField] ScoreManager score;
    [SerializeField] BeatClock beatClock;
    [SerializeField] Bullet bulletPrefab;
    [SerializeField] Transform player;
    [SerializeField] Transform boss;

    [Header("Guia")]
    [SerializeField, Tooltip("Retrato de Mei Mei con la boca cerrada. Opcional")] Sprite guidePortrait;
    [SerializeField, Tooltip("Retrato de Mei Mei hablando (boca abierta). Opcional: si esta, alterna al hablar")] Sprite guidePortraitTalk;
    [SerializeField] string guideName = "MEI MEI";
    [SerializeField, Tooltip("Letras por segundo")] float typeSpeed = 40f;

    [Header("Al terminar")]
    [SerializeField, Tooltip("Escena que se carga al terminar o saltar el tutorial")] string nextScene = "Scene_Game";

    static readonly Color gold = new Color(1f, 0.88f, 0.3f);
    static readonly Color cream = new Color(1f, 0.96f, 0.86f);
    static readonly Color keyColor = new Color(0.96f, 0.94f, 1f);
    static readonly Color ink = new Color(0.07f, 0.04f, 0.18f);
    static readonly Color green = new Color(0.42f, 1f, 0.54f);
    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    Font font;
    RectTransform root, moveKeys, arrow, spaceKey, shiftKey, check, nextMark;
    Image fade;
    Text dialog;
    Image face;
    RectTransform faceRt;
    bool talking;
    Camera cam;
    AudioSource blipSource;
    Coroutine typing;
    bool caught, reflected, typed, leaving;
    readonly List<Bullet> spawned = new List<Bullet>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Active = false; }

    void Awake()
    {
        Active = true;
        font = Resources.Load<Font>("Fonts/PressStart2P");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cam = Camera.main;
        blipSource = gameObject.AddComponent<AudioSource>();
        blipSource.clip = MakeBlip();
        blipSource.volume = 0.25f;
        blipSource.playOnAwake = false;
        Build();
    }

    void OnEnable()
    {
        score.OnCorrect += OnCorrect;
        PlayerParry.OnParry += OnParry;
    }

    void OnDisable()
    {
        score.OnCorrect -= OnCorrect;
        PlayerParry.OnParry -= OnParry;
        Active = false;
    }

    void Start()
    {
        root.SetAsLastSibling();
        StartCoroutine(Run());
    }

    void Update()
    {
        if (Active && !leaving && Input.GetKeyDown(KeyCode.Return)) Skip();
        AnimateFace();
        if (nextMark.gameObject.activeSelf)
            nextMark.anchoredPosition = new Vector2(nextMark.anchoredPosition.x, 20f + Mathf.Sin(Time.unscaledTime * 8f) * 3f);
    }

    // Mei Mei rebota y mueve la boca mientras habla
    void AnimateFace()
    {
        if (faceRt == null) return;
        float t = Time.unscaledTime;
        if (talking)
        {
            faceRt.anchoredPosition = new Vector2(0, Mathf.Abs(Mathf.Sin(t * 14f)) * 4f);
            faceRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 7f) * 4f);
            if (guidePortraitTalk != null) face.sprite = Mathf.Repeat(t, 0.16f) < 0.08f ? guidePortraitTalk : guidePortrait;
        }
        else
        {
            faceRt.anchoredPosition = Vector2.Lerp(faceRt.anchoredPosition, Vector2.zero, 0.2f);
            faceRt.localRotation = Quaternion.identity;
            face.sprite = guidePortrait;
        }
    }

    void OnCorrect(NoteType n) { caught = true; }
    void OnParry(Vector2 p, float r, bool beat, int count) { if (count > 0) reflected = true; }

    // ---------- El guion ----------

    IEnumerator Run()
    {
        yield return Fade(1f, 0f, 0.7f);
        yield return SayAndWait("¡Miau! Soy Mei Mei. Ese ratón de la nave está desafinando TODA la luna...", 1.2f);
        yield return SayAndWait("Me duelen las orejitas. ¡Ayúdame, porfa!", 1.0f);

        Say("A ver, muévete con las flechas. Así, despacito.");
        yield return StepMove();

        Say("¿Ves la que brilla? Está desafinada. ¡Atrápala, atrápala!");
        yield return StepCatch();
        yield return SayAndWait("¡NYAAA! ¿Viste? Cada nota que afinas le rompe su partitura.", 1.2f);

        Say("Las que NO brillan pican feo. ¡Esquívalas!");
        yield return StepDodge();
        yield return SayAndWait("Si te pegan pierdes un pescado... y solo tienes tres. Cuídalos.", 1.0f);

        Say("Si una se acerca, dale ESPACIO y regrésasela. ¡Toma!");
        yield return StepParry();
        yield return SayAndWait("¡Le diste! Eso calma la barra de desafinación. Purr...", 1.0f);

        Say("¿Son muchas? Mantén SHIFT y todo va más lento.");
        yield return StepFocus();
        yield return SayAndWait("Pero el foco cansa, ¿eh? Yo me cansé solo de verte.", 1.0f);

        ClearBullets();
        check.gameObject.SetActive(true);
        StartCoroutine(PopCheck());
        yield return SayAndWait("¡Listo! Ahora sí... ¡a afinar esa nave! Miau.", 1.2f);

        yield return Leave(0.9f);
    }

    // ---------- Los pasos (solo la mecanica) ----------

    IEnumerator StepMove()
    {
        Show(moveKeys);
        float moved = 0f, t = 0f;
        while (moved < 0.8f)
        {
            t += Time.deltaTime;
            if (PlayerMovement.Moving) moved += Time.deltaTime;
            Follow(moveKeys, player.position + new Vector3(-1.6f, 0.5f, 0f));
            Pulse(moveKeys, t);
            yield return null;
        }
        yield return Hide(moveKeys);
    }

    IEnumerator StepCatch()
    {
        caught = false;
        float t = 0f;
        while (!caught)
        {
            Bullet b = Spawn(score.Expected, boss.position, AimAt(boss.position), 1.6f);
            Show(arrow);
            while (b != null && !caught)
            {
                t += Time.deltaTime;
                Follow(arrow, b.transform.position + new Vector3(0, 0.55f + Mathf.Sin(t * 6f) * 0.12f, 0));
                Pulse(arrow, t);
                yield return null;
            }
            arrow.gameObject.SetActive(false);
            if (!caught) yield return Wait(0.4f);
        }
    }

    IEnumerator StepDodge()
    {
        float t = 0f, next = 0f;
        while (t < 5f)
        {
            t += Time.deltaTime;
            if (t >= next)
            {
                next = t + 0.9f;
                Vector2 dir = AimAt(boss.position);
                NoteType n = WrongNote();
                for (int i = -1; i <= 1; i++)
                    Spawn(n, boss.position, Quaternion.Euler(0, 0, i * 14f) * dir, 2.6f);
            }
            yield return null;
        }
        yield return Wait(0.8f);
    }

    IEnumerator StepParry()
    {
        reflected = false;
        float t = 0f;
        while (!reflected)
        {
            Bullet b = Spawn(WrongNote(), boss.position, AimAt(boss.position), 1.5f);
            while (b != null && !reflected)
            {
                t += Time.deltaTime;
                bool near = Vector2.Distance(b.transform.position, player.position) < 3f;
                spaceKey.gameObject.SetActive(near);
                Follow(spaceKey, player.position + new Vector3(0, 1.1f, 0));
                Pulse(spaceKey, t);
                yield return null;
            }
            spaceKey.gameObject.SetActive(false);
            if (!reflected) yield return Wait(0.4f);
        }
        yield return Wait(0.5f);
    }

    IEnumerator StepFocus()
    {
        float focused = 0f, t = 0f, next = 0f;
        Show(shiftKey);
        while (focused < 1.2f)
        {
            t += Time.deltaTime;
            if (PlayerMovement.Focusing) focused += Time.deltaTime;
            if (t >= next)
            {
                next = t + 1.3f;
                NoteType n = WrongNote();
                for (int i = 0; i < 16; i++)
                {
                    float a = (i * 22.5f + t * 40f) * Mathf.Deg2Rad;
                    Spawn(n, boss.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 2.2f);
                }
            }
            Follow(shiftKey, player.position + new Vector3(0, 1.1f, 0));
            Pulse(shiftKey, t);
            yield return null;
        }
        yield return Hide(shiftKey);
        yield return Wait(0.4f);
    }

    IEnumerator PopCheck()
    {
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float s = t < 0.2f ? Mathf.Lerp(0.3f, 1.3f, t / 0.2f) : Mathf.Lerp(1.3f, 1f, (t - 0.2f) * 3f);
            check.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }

    // ---------- Dialogo ----------

    // Empieza a escribir el texto (no espera)
    void Say(string text)
    {
        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(Type(text));
    }

    // Escribe el texto y espera un poco para que se lea
    IEnumerator SayAndWait(string text, float extra)
    {
        Say(text);
        while (!typed) yield return null;
        yield return Wait(extra);
    }

    IEnumerator Type(string text)
    {
        typed = false;
        talking = true;
        nextMark.gameObject.SetActive(false);
        dialog.text = "";
        float shown = 0f;
        int last = 0;
        while (last < text.Length)
        {
            shown += Time.deltaTime * typeSpeed;
            int n = Mathf.Min(text.Length, Mathf.FloorToInt(shown));
            if (n != last)
            {
                // "bip" retro cada 2 letras (no en espacios)
                if (n / 3 != last / 3 && text[n - 1] != ' ')
                {
                    blipSource.pitch = Random.Range(0.9f, 1.15f);
                    blipSource.Play();
                }
                last = n;
                dialog.text = text.Substring(0, n);
            }
            yield return null;
        }
        typed = true;
        talking = false;
        nextMark.gameObject.SetActive(true);
    }

    // ---------- Salir con fundido ----------

    public void Skip()
    {
        if (!Active || leaving) return;
        StopAllCoroutines();
        StartCoroutine(Leave(0.5f));
    }

    IEnumerator Leave(float time)
    {
        leaving = true;
        yield return Fade(0f, 1f, time);   // la pelea empieza en negro (intro retro), asi no hay corte
        Active = false;
        ClearBullets();
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(nextScene)) { SceneManager.LoadScene(nextScene); yield break; }
        score.RestartRounds();
        Destroy(root.gameObject);
        beatClock.StartSong();
    }

    IEnumerator Fade(float from, float to, float time)
    {
        fade.gameObject.SetActive(true);
        fade.transform.SetAsLastSibling();
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            fade.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        fade.color = new Color(0, 0, 0, to);
        if (to <= 0f) fade.gameObject.SetActive(false);
    }

    // ---------- Ayudantes ----------

    Bullet Spawn(NoteType note, Vector3 from, Vector2 dir, float speed)
    {
        Bullet b = Instantiate(bulletPrefab, from, Quaternion.identity);
        b.Launch(dir, speed, note);
        spawned.Add(b);
        return b;
    }

    void ClearBullets()
    {
        foreach (Bullet b in spawned) if (b != null) Destroy(b.gameObject);
        spawned.Clear();
    }

    Vector2 AimAt(Vector3 from) { return ((Vector2)(player.position - from)).normalized; }

    NoteType WrongNote()
    {
        NoteType need = score.Expected;
        for (int i = 0; i < 20; i++)
        {
            NoteType n = (NoteType)Random.Range(0, score.PoolSize);
            if (n != need) return n;
        }
        return (NoteType)(((int)need + 1) % score.PoolSize);
    }

    IEnumerator Wait(float s) { float t = 0f; while (t < s) { t += Time.deltaTime; yield return null; } }

    void Show(RectTransform r) { r.gameObject.SetActive(true); r.localScale = Vector3.one; }

    IEnumerator Hide(RectTransform r)
    {
        float t = 0f;
        while (t < 0.25f) { t += Time.deltaTime; r.localScale = Vector3.one * (1f - t / 0.25f); yield return null; }
        r.gameObject.SetActive(false);
    }

    // Late; si tardas en hacerlo, late mas fuerte
    void Pulse(RectTransform r, float t)
    {
        float extra = Mathf.Clamp01(t / 6f);
        float s = 1f + (0.06f + 0.12f * extra) * Mathf.Sin(t * (6f + 4f * extra));
        r.localScale = new Vector3(s, s, 1f);
    }

    // Pone un elemento de la UI encima de un punto del mundo
    void Follow(RectTransform r, Vector3 world)
    {
        Vector2 screen = cam.WorldToScreenPoint(world);
        Canvas c = GetComponentInParent<Canvas>();
        Camera uiCam = c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, uiCam, out Vector2 local);
        r.anchoredPosition = local;
    }

    // Voz de Mei Mei hecha con codigo: un "miau" de 8 bits muy cortito.
    // Onda cuadrada que sube y baja de tono (como "mi-au") y se apaga.
    static AudioClip MakeBlip()
    {
        const int rate = 44100;
        int n = rate / 16; // 62 ms
        float[] data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float k = i / (float)n;
            // sube de 700 a 1250 Hz y luego baja a 850 Hz
            float freq = k < 0.4f ? Mathf.Lerp(700f, 1250f, k / 0.4f) : Mathf.Lerp(1250f, 850f, (k - 0.4f) / 0.6f);
            phase += freq / rate;
            float sq = Mathf.Repeat(phase, 1f) < 0.5f ? 1f : -1f;
            float env = Mathf.Clamp01(k * 12f) * (1f - k); // entra rapido, sale suave
            data[i] = sq * 0.25f * env;
        }
        AudioClip c = AudioClip.Create("meow", n, 1, rate, false);
        c.SetData(data, 0);
        return c;
    }

    // ---------- Construir ----------

    void Build()
    {
        root = Rect("Tutorial", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Flechas en cruz
        moveKeys = Rect("MoveKeys", root, Center, Center, Vector2.zero, new Vector2(150, 100));
        ArrowKey(moveKeys, new Vector2(0, 26), 0f);
        ArrowKey(moveKeys, new Vector2(-50, -26), 90f);
        ArrowKey(moveKeys, new Vector2(0, -26), 180f);
        ArrowKey(moveKeys, new Vector2(50, -26), -90f);

        // Flecha dorada que apunta a la figura que brilla
        arrow = Rect("Arrow", root, Center, Center, Vector2.zero, new Vector2(38, 38));
        Image ai = arrow.gameObject.AddComponent<Image>();
        ai.sprite = NoteStyle.GetSprite(NoteType.Re); ai.color = gold; ai.raycastTarget = false;
        arrow.localRotation = Quaternion.Euler(0, 0, 180f);

        spaceKey = Key(root, "ESPACIO", new Vector2(190, 46));
        shiftKey = Key(root, "SHIFT", new Vector2(150, 46));

        // Palomita dibujada con dos barras
        check = Rect("Check", root, Center, Center, new Vector2(0, 60), new Vector2(160, 160));
        Bar(check, new Vector2(-34, -10), new Vector2(60, 22), 45f);
        Bar(check, new Vector2(18, 6), new Vector2(110, 22), -50f);

        // Caja de dialogo retro (arriba al centro, donde no estorba al gato)
        RectTransform box = Rect("DialogBox", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -84), new Vector2(680, 112));
        Image boxBg = box.gameObject.AddComponent<Image>();
        boxBg.color = new Color(0.04f, 0.02f, 0.12f, 0.92f); boxBg.raycastTarget = false;
        Outline bo = box.gameObject.AddComponent<Outline>();
        bo.effectColor = cream; bo.effectDistance = new Vector2(4, -4);
        Outline bo2 = box.gameObject.AddComponent<Outline>();
        bo2.effectColor = ink; bo2.effectDistance = new Vector2(-4, 4);

        float textLeft = 24f;
        if (guidePortrait != null)
        {
            RectTransform pf = Rect("Portrait", box, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(62, 0), new Vector2(88, 88));
            Image pfBg = pf.gameObject.AddComponent<Image>();
            pfBg.color = new Color(0.2f, 0.14f, 0.45f); pfBg.raycastTarget = false;
            RectTransform pi = Rect("Face", pf, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-8, -8));
            faceRt = pi;
            face = pi.gameObject.AddComponent<Image>();
            face.sprite = guidePortrait; face.preserveAspect = true; face.raycastTarget = false;
            textLeft = 122f;
        }

        // Etiqueta con el nombre, sobre la caja
        RectTransform tag = Rect("Name", box, new Vector2(0, 1), new Vector2(0, 1), new Vector2(textLeft + 90, 4), new Vector2(200, 26));
        Image tagBg = tag.gameObject.AddComponent<Image>();
        tagBg.color = gold; tagBg.raycastTarget = false;
        Label(tag, guideName, 11, ink);

        RectTransform tr = Rect("Text", box, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        tr.offsetMin = new Vector2(textLeft, 16); tr.offsetMax = new Vector2(-28, -24);
        dialog = tr.gameObject.AddComponent<Text>();
        dialog.font = font; dialog.fontSize = 14; dialog.color = cream; dialog.lineSpacing = 1.6f;
        dialog.alignment = TextAnchor.UpperLeft; dialog.raycastTarget = false;
        dialog.horizontalOverflow = HorizontalWrapMode.Wrap;

        // Triangulito que indica que termino de hablar
        nextMark = Rect("Next", box, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -36), new Vector2(14, 14));
        Image nm = nextMark.gameObject.AddComponent<Image>();
        nm.sprite = NoteStyle.GetSprite(NoteType.Re); nm.color = gold; nm.raycastTarget = false;
        nextMark.localRotation = Quaternion.Euler(0, 0, 180f);
        nextMark.anchorMin = nextMark.anchorMax = new Vector2(1, 0);
        nextMark.anchoredPosition = new Vector2(-24, 20);

        // Boton SALTAR (abajo a la izquierda)
        RectTransform btn = Rect("Skip", root, Vector2.zero, Vector2.zero, new Vector2(124, 44), new Vector2(200, 42));
        Image bi = btn.gameObject.AddComponent<Image>();
        bi.color = new Color(0.25f, 0.18f, 0.55f, 0.9f);
        Button b = btn.gameObject.AddComponent<Button>();
        b.targetGraphic = bi;
        b.onClick.AddListener(Skip);
        Label(btn, "SALTAR >>", 14, keyColor);

        // Negro para los fundidos
        fade = Rect("Fade", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
        fade.color = Color.black; fade.raycastTarget = false;

        moveKeys.gameObject.SetActive(false);
        arrow.gameObject.SetActive(false);
        spaceKey.gameObject.SetActive(false);
        shiftKey.gameObject.SetActive(false);
        check.gameObject.SetActive(false);
        nextMark.gameObject.SetActive(false);
    }

    RectTransform Key(RectTransform parent, string label, Vector2 size)
    {
        RectTransform k = Rect(label, parent, Center, Center, Vector2.zero, size);
        Image i = k.gameObject.AddComponent<Image>();
        i.color = keyColor; i.raycastTarget = false;
        Outline o = k.gameObject.AddComponent<Outline>();
        o.effectColor = ink; o.effectDistance = new Vector2(4, -4);
        Label(k, label, 16, ink);
        return k;
    }

    void ArrowKey(RectTransform parent, Vector2 pos, float rot)
    {
        RectTransform k = Rect("Key", parent, Center, Center, pos, new Vector2(44, 44));
        Image i = k.gameObject.AddComponent<Image>();
        i.color = keyColor; i.raycastTarget = false;
        Outline o = k.gameObject.AddComponent<Outline>();
        o.effectColor = ink; o.effectDistance = new Vector2(3, -3);
        RectTransform a = Rect("Arrow", k, Center, Center, Vector2.zero, new Vector2(24, 24));
        Image ai = a.gameObject.AddComponent<Image>();
        ai.sprite = NoteStyle.GetSprite(NoteType.Re); ai.color = ink; ai.raycastTarget = false;
        a.localRotation = Quaternion.Euler(0, 0, rot);
    }

    void Bar(RectTransform parent, Vector2 pos, Vector2 size, float rot)
    {
        RectTransform r = Rect("Bar", parent, Center, Center, pos, size);
        Image i = r.gameObject.AddComponent<Image>();
        i.color = green; i.raycastTarget = false;
        Outline o = r.gameObject.AddComponent<Outline>();
        o.effectColor = ink; o.effectDistance = new Vector2(3, -3);
        r.localRotation = Quaternion.Euler(0, 0, rot);
    }

    void Label(RectTransform parent, string s, int size, Color c)
    {
        Text t = Rect("Label", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Text>();
        t.font = font; t.text = s; t.fontSize = size; t.color = c;
        t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
    }

    RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        RectTransform rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = Center;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }
}
