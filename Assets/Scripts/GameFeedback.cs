using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Ponlo en un objeto vacio "Feedback". Hace que cada accion se sienta:
//  - Nota correcta: anillo de su color + nombre de la nota
//  - Error: anillo rojo y el gato parpadea (no pierde otra vida mientras)
//  - Parry: onda cyan; a ritmo, onda dorada + "¡A RITMO!"
//  - Nota devuelta al jefe: "+2 s" sobre el jefe
//  - Ronda completada: borra todas las balas, la musica se frena como cinta, distorsion,
//    el jefe vuela erratico sin disparar y luego todo vuelve al ritmo
//  - Te dan: distorsion de imagen y sonido
//  - Nota correcta: la nota vuela hacia el jefe y el jefe se sacude
public class GameFeedback : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] ScoreManager score;
    [SerializeField] FightManager fight;
    [SerializeField] BeatClock beatClock;
    [SerializeField] Camera cam;
    [SerializeField] Transform player;
    [SerializeField] SpriteRenderer playerVisual;  // PlayerVisual
    [SerializeField] Transform boss;
    [SerializeField] Transform bossVisual;         // BossVisual


    [Header("Pausa entre rondas")]
    [SerializeField] float roundBreakSeconds = 2.6f;

    [Header("Distorsion")]
    [SerializeField] float hitGlitch = 0.6f;
    [SerializeField] float roundGlitch = 1f;

    [Header("Victoria")]
    [SerializeField, Tooltip("Segundo de la cancion desde donde vuelve limpia (16 = el coro)")] float victorySongTime = 16f;

    [Header("Barra casi llena")]
    [SerializeField, Tooltip("Desde que parte de la barra empieza a temblar la camara")] float tensionStart = 0.6f;
    [SerializeField] float tensionShake = 0.09f;

    Volume volume;
    ChromaticAberration chroma;
    LensDistortion lens;
    Vignette vignette;
    ColorAdjustments colorAdj;
    float glitch, parryFlash;
    Color parryColor;
    bool winning;

    static readonly Color gold = new Color(1f, 0.88f, 0.3f);
    static readonly Color cyan = new Color(0.25f, 0.88f, 0.94f);
    static readonly Color red = new Color(1f, 0.3f, 0.37f);
    static readonly Color green = new Color(0.42f, 1f, 0.54f);

    Sprite ring, square;
    SpriteRenderer flash;
    Font font;
    Vector3 camBase, bossVisBase;
    float camSize, shakeTime, shakeAmp, zoomKick, bossShake, bossKick;
    Vector3 bossScaleBase;
    BossAttack attack;
    BossSpin spin;
    SpriteRenderer bossSr;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ring = MakeRing();
        square = MakeSquare();

        // Destello de pantalla completa
        flash = new GameObject("ScreenFlash").AddComponent<SpriteRenderer>();
        flash.transform.SetParent(cam.transform, false);
        flash.transform.localPosition = new Vector3(0, 0, 1f);
        flash.transform.localScale = new Vector3(40f, 25f, 1f);
        flash.sprite = square;
        flash.sortingOrder = 100;
        flash.color = Color.clear;

        // Efectos de imagen (URP): aberracion cromatica + deformacion de lente
        var camData = cam.GetUniversalAdditionalCameraData();
        if (camData != null) camData.renderPostProcessing = true;
        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        chroma = volume.profile.Add<ChromaticAberration>(true);
        lens = volume.profile.Add<LensDistortion>(true);
        vignette = volume.profile.Add<Vignette>(true);
        colorAdj = volume.profile.Add<ColorAdjustments>(true);
        chroma.intensity.value = 0f;
        lens.intensity.value = 0f;
        vignette.intensity.value = 0f;
        vignette.color.value = new Color(0.05f, 0.02f, 0.2f);
        colorAdj.saturation.value = 0f;
    }

    void Start()
    {
        camBase = cam.transform.localPosition;
        camSize = cam.orthographicSize;
        if (bossVisual != null)
        {
            bossVisBase = bossVisual.localPosition;
            bossScaleBase = bossVisual.localScale;
            bossSr = bossVisual.GetComponent<SpriteRenderer>();
            spin = bossVisual.GetComponent<BossSpin>();
        }
    }

    void OnEnable()
    {
        score.OnCorrect += Correct;
        score.OnMistake += Mistake;
        score.OnCompleted += RoundDone;
        fight.OnTimeAdded += TimeAdded;
        fight.OnGameOver += GameOver;
        fight.OnWin += Win;
        PlayerParry.OnParry += Parry;
        if (boss != null) attack = boss.GetComponent<BossAttack>();
        if (attack != null) attack.OnShot += Shot;
    }

    void OnDisable()
    {
        score.OnCorrect -= Correct;
        score.OnMistake -= Mistake;
        score.OnCompleted -= RoundDone;
        fight.OnTimeAdded -= TimeAdded;
        fight.OnGameOver -= GameOver;
        fight.OnWin -= Win;
        PlayerParry.OnParry -= Parry;
        if (attack != null) attack.OnShot -= Shot;
    }

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;

        // Sacudida + zoom al ritmo
        Vector3 off = Vector3.zero;
        if (shakeTime > 0f)
        {
            shakeTime -= dt;
            off = (Vector3)(Random.insideUnitCircle * shakeAmp * Mathf.Clamp01(shakeTime * 4f));
        }
        // Distorsion: colores separados, lente deformada y "cortes" de imagen
        glitch = Mathf.MoveTowards(glitch, 0f, dt * 1.6f);
        if (glitch > 0f)
        {
            chroma.intensity.value = Mathf.Clamp01(glitch * (0.7f + 0.3f * Random.value));
            lens.intensity.value = -0.45f * glitch * Mathf.Sin(Time.unscaledTime * 40f);
            if (Random.value < 0.35f) off.x += (Random.value - 0.5f) * 0.4f * glitch;
        }
        else { chroma.intensity.value = 0f; lens.intensity.value = 0f; }

        // Barra de desafinacion casi llena: la camara tiembla cada vez mas
        if (!fight.Over && !fight.Paused && !PauseMenu.Paused && !RetroIntro.Playing && !Tutorial.Active && fight.TimeMax > 0f)
        {
            float fill = 1f - fight.TimeLeft / fight.TimeMax;
            float k = Mathf.Clamp01((fill - tensionStart) / (1f - tensionStart));
            off += (Vector3)(Random.insideUnitCircle * tensionShake * k * k);
        }

        // Foco: todo se siente lento (bordes oscuros, menos color, musica lejana con eco)
        float f = winning ? 0f : PlayerMovement.FocusBlend;
        vignette.intensity.value = 0.38f * f;
        colorAdj.saturation.value = -35f * f;
        if (!winning) beatClock.SetFocus(f);

        if (!winning) cam.transform.localPosition = camBase + off;
        else cam.transform.localPosition = camBase + off + (Vector3)(Random.insideUnitCircle * winShake);
        zoomKick = Mathf.MoveTowards(zoomKick, 0f, dt * 0.4f);
        cam.orthographicSize = camSize * (1f - zoomKick);

        // El jefe se sacude y se pone rojo cuando le llega una nota afinada
        if (bossVisual != null)
        {
            bossShake = Mathf.MoveTowards(bossShake, 0f, dt * 2f);
            bossKick = Mathf.MoveTowards(bossKick, 0f, dt * 6f);
            // Retroceso y "aplastado" solo cuando dispara con el golpe de la musica
            bossVisual.localPosition = bossVisBase + Vector3.up * 0.18f * bossKick
                                     + (Vector3)(Random.insideUnitCircle * 0.15f * bossShake);
            bossVisual.localScale = new Vector3(bossScaleBase.x * (1f + 0.1f * bossKick),
                                                bossScaleBase.y * (1f - 0.1f * bossKick), bossScaleBase.z);
            parryFlash = Mathf.MoveTowards(parryFlash, 0f, dt * 3f);
            if (bossSr != null && !winning)
            {
                Color bc = Color.Lerp(Color.white, new Color(1f, 0.4f, 0.45f), bossShake);
                bossSr.color = Color.Lerp(bc, parryColor, parryFlash);
            }
        }

        // Destello se desvanece
        Color c = flash.color;
        c.a = Mathf.MoveTowards(c.a, 0f, dt * 2.5f);
        flash.color = c;
    }

    // ---------- Reacciones ----------

    void Correct(NoteType note)
    {
        Color c = NoteStyle.GetColor(note);
        Ring(player.position, c, 0.3f, 1.6f, 0.35f);
        Ring(player.position, Color.white, 0.2f, 1.1f, 0.25f);
        FloatText(player.position + Vector3.up * 0.8f, note.ToString(), c, 0.9f);
        if (boss != null) StartCoroutine(FlyToBoss(note));
    }

    // La nota afinada sale del gato y golpea al jefe
    IEnumerator FlyToBoss(NoteType note)
    {
        SpriteRenderer n = new GameObject("FxNote").AddComponent<SpriteRenderer>();
        n.sprite = NoteStyle.GetSprite(note); n.color = NoteStyle.GetColor(note); n.sortingOrder = 41;
        Vector3 a = player.position, ctrl = a + new Vector3(Random.Range(-2f, 2f), 1.5f, 0f);
        float t = 0f, time = 0.35f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = t / time;
            Vector3 b = boss.position;
            Vector3 p = Vector3.Lerp(Vector3.Lerp(a, ctrl, k), Vector3.Lerp(ctrl, b, k), k);
            n.transform.position = new Vector3(p.x, p.y, 0f);
            float s = Mathf.Lerp(0.4f, 0.7f, k);
            n.transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        Destroy(n.gameObject);
        bossShake = 1f;
        Ring(boss.position, NoteStyle.GetColor(note), 0.5f, 3f, 0.35f);
        Ring(boss.position, Color.white, 0.3f, 2f, 0.25f);
    }

    void Mistake()
    {
        glitch = Mathf.Max(glitch, hitGlitch);
        beatClock.Distort(0.75f, 0.45f);
        Ring(player.position, red, 0.3f, 2f, 0.4f);
        if (playerVisual != null && !fight.Over)
        {
            blinkCo = StartCoroutine(Blink(score.MistakeGrace));
            CatAnimator ca = playerVisual.GetComponent<CatAnimator>();
            if (ca != null) ca.PlayHurt(score.MistakeGrace); // dibujo de "herido"
        }
    }

    // Cada disparo cae en un golpe de la cancion: el jefe lo "toca" con un fogonazo
    void Shot(int kind)
    {
        if (boss == null) return;
        Vector3 p = boss.position;
        if (kind == 0) { Ring(p, Color.white, 0.6f, 3.2f, 0.22f); bossKick = 1f; if (spin != null) spin.Spin(); }
        else if (kind == 1) { Ring(p, new Color(1f, 1f, 1f, 0.7f), 0.5f, 1.8f, 0.15f); bossKick = Mathf.Max(bossKick, 0.5f); }
        else { Ring(p, gold, 0.6f, 2.6f, 0.2f); bossKick = Mathf.Max(bossKick, 0.8f); }
    }

    void Parry(Vector2 pos, float radius, bool onBeat, int count)
    {
        Color c = onBeat ? gold : cyan;
        Ring(pos, c, 0.2f, radius * 2f, 0.25f);
        if (onBeat)
        {
            Ring(pos, Color.white, 0.2f, radius * 2.6f, 0.35f);
            FloatText((Vector3)pos + Vector3.up * 1f, "¡A RITMO!", gold, 0.8f);
        }
    }

    // Una nota devuelta con el parry le pega a la nave
    void TimeAdded(float seconds)
    {
        Vector3 p = boss != null ? boss.position : Vector3.zero;
        bool onBeat = seconds >= 2f;
        Color c = onBeat ? gold : cyan;
        Ring(p, c, 0.5f, onBeat ? 3.4f : 2.5f, 0.3f);
        Ring(p, Color.white, 0.3f, 1.6f, 0.2f);
        FloatText(p + Vector3.right * 1.6f, "+" + seconds + " s", green, 0.9f);

        // La nave: se tambalea, se enciende del color del parry y gira
        bossShake = Mathf.Max(bossShake, onBeat ? 0.9f : 0.5f);
        bossKick = 1f;
        parryColor = c; parryFlash = 1f;
        if (spin != null) spin.Spin();
        // La musica: golpe sordo, como si la nota "tapara" la cancion del jefe
        beatClock.Thump(onBeat ? 1f : 0.6f);
        if (onBeat) Shake(0.15f, 0.08f);
    }

    void RoundDone()
    {
        // Limpia la pantalla, como en Touhou al vencer una fase
        int rings = 0;
        foreach (Bullet b in FindObjectsByType<Bullet>())
        {
            if (rings++ < 40) Ring(b.transform.position, Color.white, 0.1f, 0.6f, 0.25f);
            Destroy(b.gameObject);
        }
        Flash(Color.white, 0.5f);
        zoomKick = 0.08f;
        glitch = roundGlitch;
        beatClock.Distort(0.9f, 0.6f);

        // La musica se frena, el jefe vuela mareado y la barra se pausa
        bool last = score.Round + 1 >= score.TotalRounds;
        if (last) return; // la victoria tiene su propia secuencia
        beatClock.TapeStop();
        if (attack != null) attack.Stagger(roundBreakSeconds);
        if (!last) StartCoroutine(ResumeAfterBreak());
    }

    IEnumerator ResumeAfterBreak()
    {
        fight.Paused = true;
        float t = 0f;
        while (t < roundBreakSeconds) { if (!PauseMenu.Paused) t += Time.unscaledDeltaTime; yield return null; }
        while (PauseMenu.Paused) yield return null;
        fight.Paused = false;
        if (!fight.Over) beatClock.Resume();
    }

    // ---------- Victoria: la nave cae ----------

    float winShake;
    Coroutine blinkCo;

    void Win()
    {
        if (boss == null) return;
        winning = true;
        StartCoroutine(WinSequence());
    }

    IEnumerator WinSequence()
    {
        // 0) Golpe final: todo se congela un instante (el tiempo ya esta en pausa)
        Flash(Color.white, 0.8f);
        glitch = 0.8f;
        beatClock.SetMuffle(true);     // la musica del jefe muere ahogada
        if (spin != null) spin.Continuous = true;
        beatClock.OnStep += WinStep;

        Vector3 start = boss.position;
        float t = 0f;
        while (t < 0.5f) { t += Time.unscaledDeltaTime; winShake = 0.05f; yield return null; }

        // 1) Parpadea rojo y tiembla (beats 1-2), 2) cae en arco hacia la luna echando humo
        float fallStart = 1.5f, fallEnd = 4.6f, smoke = 0f;
        t = 0f;
        while (t < fallEnd)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            bool red = beatClock.BeatPhase < 0.5f;
            if (bossSr != null) bossSr.color = red ? new Color(1f, 0.25f, 0.3f) : Color.white;

            Vector3 p = start + (Vector3)(Random.insideUnitCircle * 0.12f);
            if (t > fallStart)
            {
                float k = (t - fallStart) / (fallEnd - fallStart);
                p += new Vector3(3.5f * k, -9f * k * k, 0f) + (Vector3)(Random.insideUnitCircle * 0.12f);
                smoke -= dt;
                if (smoke <= 0f)
                {
                    smoke = 0.07f;
                    Ring(p + (Vector3)Random.insideUnitCircle * 0.3f, new Color(0.6f, 0.6f, 0.7f, 0.7f), 0.3f, 1.1f, 0.5f);
                }
            }
            boss.position = p;
            winShake = 0.03f + 0.04f * Mathf.Clamp01(t / fallEnd);
            yield return null;
        }

        // 3) Choca: explosion grande, destello y la musica se corta en seco
        beatClock.OnStep -= WinStep;
        Vector3 crash = boss.position;
        crash.y = Mathf.Max(crash.y, -4.6f);
        if (bossSr != null) bossSr.enabled = false;
        for (int i = 0; i < 6; i++)
            Ring(crash + (Vector3)Random.insideUnitCircle * 0.6f, i % 2 == 0 ? gold : Color.white, 0.3f, 4f + i, 0.5f + i * 0.08f);
        Flash(Color.white, 1f);
        glitch = 1f;
        Shake(0.6f, 0.35f);
        winShake = 0f;
        beatClock.Cut();

        // 4) Un beat de silencio y la cancion vuelve LIMPIA: ya esta afinada
        t = 0f;
        while (t < 0.9f) { t += Time.unscaledDeltaTime; yield return null; }
        beatClock.PlayCleanFrom(victorySongTime);
    }

    // Mientras cae, pequenas explosiones con cada golpe de bombo
    void WinStep(int step)
    {
        int bar = step / SongChart.StepsPerBar, s = step % SongChart.StepsPerBar;
        if (!SongChart.Kick(bar, s) || boss == null) return;
        Vector3 p = boss.position + (Vector3)(Random.insideUnitCircle * 0.8f);
        Ring(p, Random.value < 0.5f ? gold : red, 0.2f, 1.6f, 0.3f);
        Ring(p, Color.white, 0.1f, 0.9f, 0.2f);
    }

    void GameOver()
    {
        glitch = roundGlitch;
        beatClock.TapeBreak();
        if (player != null) StartCoroutine(CatFalls());
    }

    // Derrota: el gato queda "muerto" (gatomuro3) un instante y cae girando hacia la luna,
    // haciendose chiquito como si se alejara
    IEnumerator CatFalls()
    {
        if (blinkCo != null) StopCoroutine(blinkCo);
        // Esconde todo lo que no sea el dibujo del gato (punto, anillo de parry, barra de foco)
        foreach (SpriteRenderer r in player.GetComponentsInChildren<SpriteRenderer>())
            if (r != playerVisual) r.enabled = false;
        if (playerVisual != null)
        {
            playerVisual.enabled = true;
            CatAnimator ca = playerVisual.GetComponent<CatAnimator>();
            if (ca != null) ca.SetDead();
        }

        Vector3 start = player.position;
        Vector3 baseScale = player.localScale;
        Ring(start, red, 0.3f, 2.5f, 0.4f);

        // 1) Se queda congelado con cara de muerto, temblando un poquito
        float t = 0f;
        while (t < 0.7f)
        {
            t += Time.unscaledDeltaTime;
            if (playerVisual != null) playerVisual.enabled = true;
            player.position = start + (Vector3)(Random.insideUnitCircle * 0.03f);
            yield return null;
        }

        // 2) Cae hacia la luna: gira cada vez mas rapido y se hace chiquito
        Vector3 target = new Vector3(start.x * 0.5f, -1.6f, 0f);
        float time = 1.7f, angle = 0f;
        t = 0f;
        while (t < time)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            float k = Mathf.Clamp01(t / time);
            float ease = k * k;                                  // empieza lento y acelera
            player.position = Vector3.Lerp(start, target, 1f - (1f - k) * (1f - k));
            angle += Mathf.Lerp(90f, 900f, k) * dt;              // gira cada vez mas rapido
            player.rotation = Quaternion.Euler(0, 0, angle);
            player.localScale = baseScale * Mathf.Lerp(1f, 0.02f, ease);
            if (playerVisual != null)
                playerVisual.color = Color.Lerp(Color.white, new Color(0.55f, 0.45f, 0.9f, 0.7f), k);
            yield return null;
        }

        // 3) Se pierde en la luna: un destellito
        if (playerVisual != null) playerVisual.enabled = false;
        Ring(target, new Color(0.85f, 0.8f, 1f, 0.9f), 0.05f, 0.8f, 0.45f);
        Ring(target, Color.white, 0.05f, 0.4f, 0.3f);
    }

    // ---------- Herramientas ----------

    void Flash(Color c, float alpha) { c.a = alpha; flash.color = c; }
    void Shake(float time, float amp) { shakeTime = time; shakeAmp = amp; }

    IEnumerator Blink(float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            playerVisual.enabled = Mathf.Repeat(t, 0.16f) < 0.08f;
            yield return null;
        }
        playerVisual.enabled = true;
    }

    void Ring(Vector3 pos, Color c, float from, float to, float time)
    {
        SpriteRenderer r = new GameObject("FxRing").AddComponent<SpriteRenderer>();
        r.sprite = ring; r.color = c; r.sortingOrder = 40;
        r.transform.position = new Vector3(pos.x, pos.y, 0f);
        StartCoroutine(Grow(r, from, to, time));
    }

    IEnumerator Grow(SpriteRenderer r, float from, float to, float time)
    {
        float t = 0f; Color c = r.color;
        while (t < time && r != null)
        {
            t += Time.unscaledDeltaTime;
            float k = t / time;
            float s = Mathf.Lerp(from, to, 1f - (1f - k) * (1f - k));
            r.transform.localScale = new Vector3(s, s, 1f);
            c.a = 1f - k; r.color = c;
            yield return null;
        }
        if (r != null) Destroy(r.gameObject);
    }

    void FloatText(Vector3 pos, string text, Color c, float time, float size = 1f)
    {
        GameObject go = new GameObject("FxText");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        TextMesh shadow = MakeText(go.transform, text, new Color(0.07f, 0.04f, 0.18f), size, 44);
        shadow.transform.localPosition = new Vector3(0.04f, -0.04f, 0f);
        TextMesh main = MakeText(go.transform, text, c, size, 45);
        StartCoroutine(Rise(go, main, shadow, time));
    }

    TextMesh MakeText(Transform parent, string text, Color c, float size, int order)
    {
        GameObject go = new GameObject("T");
        go.transform.SetParent(parent, false);
        TextMesh tm = go.AddComponent<TextMesh>();
        tm.font = font; tm.text = text; tm.color = c;
        tm.fontSize = 64; tm.characterSize = 0.06f * size;
        tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.material = font.material; mr.sortingOrder = order;
        return tm;
    }

    IEnumerator Rise(GameObject go, TextMesh main, TextMesh shadow, float time)
    {
        float t = 0f; Vector3 start = go.transform.position;
        Color a = main.color, b = shadow.color;
        while (t < time && go != null)
        {
            t += Time.unscaledDeltaTime;
            float k = t / time;
            go.transform.position = start + Vector3.up * (0.6f * k);
            float pop = k < 0.15f ? Mathf.Lerp(0.6f, 1.1f, k / 0.15f) : Mathf.Lerp(1.1f, 1f, (k - 0.15f) * 4f);
            go.transform.localScale = new Vector3(pop, pop, 1f);
            float alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            a.a = alpha; b.a = alpha; main.color = a; shadow.color = b;
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    static Sprite MakeRing()
    {
        const int N = 128;
        Texture2D t = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float d = new Vector2((x + 0.5f) / N - 0.5f, (y + 0.5f) / N - 0.5f).magnitude;
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.44f) / 0.04f);
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
    }

    static Sprite MakeSquare()
    {
        Texture2D t = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        t.SetPixels(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }
}
