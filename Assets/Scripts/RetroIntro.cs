using UnityEngine;
using UnityEngine.UI;

// Intro tipo juego retro durante los 8 s sin bateria (16 beats), todo al ritmo:
//  beats 0-7   pantalla negra que se abre, franjas de cine, "WARNING" parpadeando y el jefe bajando
//  beats 8-11  logo del juego + "NIVEL 1 - LA LUNA" escribiendose letra por letra
//  beats 12-15 3, 2, 1, ¡AFINA!  (justo cuando entra la bateria empieza la pelea)
// Ponlo en el Canvas (junto a FightUI).
public class RetroIntro : MonoBehaviour
{
    public static bool Playing { get; private set; }

    [Header("Referencias")]
    [SerializeField] BeatClock beatClock;
    [SerializeField] Transform boss;
    [SerializeField] Sprite titleLogo;          // titulo.png (opcional)

    [Header("Textos")]
    [SerializeField] string warningText = "WARNING";
    [SerializeField] string warningSub = "SE ACERCA UNA NAVE DESAFINADA";
    [SerializeField] string stageText = "NIVEL 1 - LA LUNA";
    [SerializeField] string goText = "¡AFINA!";
    [SerializeField] int introBeats = 16;

    static readonly Color red = new Color(1f, 0.18f, 0.3f);
    static readonly Color gold = new Color(1f, 0.88f, 0.3f);
    static readonly Color cream = new Color(1f, 0.96f, 0.86f);

    Font font;
    RectTransform root, barTop, barBottom, banner, logoRt;
    Image black, logo;
    Text warn, warnSub, stage, big;
    RawImage scan;
    CanvasGroup hud;
    Vector3 bossHome;
    BossSpin spin;
    bool done;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Playing = false; }

    void Awake()
    {
        Playing = true;
        font = Resources.Load<Font>("Fonts/PressStart2P");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (boss != null)
        {
            bossHome = boss.position;
            spin = boss.GetComponentInChildren<BossSpin>();
        }
        Build();
    }

    void Start()
    {
        // Esconde el HUD de la pelea hasta el final de la intro
        Transform h = transform.Find("FightHUD");
        if (h != null)
        {
            hud = h.GetComponent<CanvasGroup>();
            if (hud == null) hud = h.gameObject.AddComponent<CanvasGroup>();
            hud.alpha = 0f;
        }
        root.SetAsLastSibling();
    }

    void OnDisable() { Playing = false; }

    void Update()
    {
        if (done) return;

        // Mientras corre el tutorial, la intro espera escondida (y el HUD se ve)
        if (Tutorial.Active)
        {
            root.gameObject.SetActive(false);
            if (hud != null) hud.alpha = 1f;
            return;
        }
        if (!root.gameObject.activeSelf) { root.gameObject.SetActive(true); if (hud != null) hud.alpha = 0f; }

        int beat = beatClock.CurrentBeat;      // -1 antes de que empiece la cancion
        float ph = beatClock.BeatPhase;        // 0 en el beat -> 1
        float t = beat < 0 ? 0f : beat + ph;   // tiempo en beats

        if (t >= introBeats) { Finish(); return; }

        // Negro que se abre en los primeros 2 beats
        black.color = new Color(0, 0, 0, beat < 0 ? 1f : Mathf.Clamp01(1f - t / 2f));

        // Franjas de cine: entran al inicio, salen en el ultimo beat
        float bars = t < 15f ? 1f : 1f - (t - 15f);
        barTop.anchoredPosition = new Vector2(0, (1f - bars) * 80f);
        barBottom.anchoredPosition = new Vector2(0, -(1f - bars) * 80f);

        // El jefe baja desde arriba (beats 2-7) con un vaivén
        if (boss != null)
        {
            float k = Mathf.Clamp01((t - 2f) / 6f);
            k = 1f - (1f - k) * (1f - k);
            Vector3 p = bossHome;
            p.y = Mathf.Lerp(bossHome.y + 6f, bossHome.y, k);
            p.x = bossHome.x + Mathf.Sin(t * Mathf.PI / 2f) * 1.2f * (1f - k);
            boss.position = p;
            if (spin != null) spin.Continuous = t >= 1f && t < 8f; // gira mientras entra
        }

        // WARNING (beats 0-7): parpadea en cada beat, la franja se estira
        bool warnOn = t < 8f;
        banner.gameObject.SetActive(warnOn);
        if (warnOn)
        {
            float open = Mathf.Clamp01(t / 0.5f) * Mathf.Clamp01((8f - t) / 0.5f);
            banner.localScale = new Vector3(1f, open, 1f);
            warn.enabled = ph < 0.6f;  // encendido en el beat, apagado al final
            warnSub.text = Typed(warningSub, (t - 1f) / 4f);
        }

        // Logo + nivel (beats 8-11)
        bool stageOn = t >= 8f && t < 12f;
        logoRt.gameObject.SetActive(stageOn && logo.sprite != null);
        stage.gameObject.SetActive(stageOn);
        if (stageOn)
        {
            float s = Pop(t - 8f);
            logoRt.localScale = new Vector3(s, s, 1f);
            stage.text = Typed(stageText, (t - 9f) / 2f);
        }

        // 3, 2, 1, ¡AFINA! (beats 12-15)
        bool countOn = t >= 12f;
        big.gameObject.SetActive(countOn);
        if (countOn)
        {
            int n = Mathf.FloorToInt(t) - 12;
            big.text = n < 3 ? (3 - n).ToString() : goText;
            big.color = n < 3 ? cream : gold;
            float s = Mathf.Lerp(1.8f, 1f, Mathf.Clamp01(ph * 3f));
            big.rectTransform.localScale = new Vector3(s, s, 1f);
            if (hud != null) hud.alpha = n < 3 ? 0f : ph;
        }
    }

    void Finish()
    {
        done = true;
        Playing = false;
        if (boss != null) boss.position = bossHome;
        if (spin != null) spin.Continuous = false;
        if (hud != null) hud.alpha = 1f;
        Destroy(root.gameObject);
    }

    static float Pop(float t)
    {
        if (t < 0.25f) return Mathf.Lerp(0.2f, 1.2f, t / 0.25f);
        return Mathf.Lerp(1.2f, 1f, Mathf.Clamp01((t - 0.25f) * 3f));
    }

    static string Typed(string s, float k)
    {
        int n = Mathf.Clamp(Mathf.FloorToInt(k * s.Length), 0, s.Length);
        return s.Substring(0, n);
    }

    // ---------- Construir ----------

    void Build()
    {
        root = Rect("RetroIntro", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        black = Img(Rect("Black", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Color.black);

        // Lineas de TV retro
        Texture2D tex = new Texture2D(1, 2) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        tex.SetPixel(0, 0, new Color(0, 0, 0, 0.35f)); tex.SetPixel(0, 1, new Color(0, 0, 0, 0f)); tex.Apply();
        scan = Rect("Scanlines", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
        scan.texture = tex; scan.uvRect = new Rect(0, 0, 1, 720f / 3f); scan.raycastTarget = false;

        // Franjas de cine
        barTop = Rect("BarTop", root, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 80), new Vector2(0.5f, 1));
        Img(barTop, Color.black);
        barBottom = Rect("BarBottom", root, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 80), new Vector2(0.5f, 0));
        Img(barBottom, Color.black);

        // WARNING
        banner = Rect("Warning", root, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 40), new Vector2(0, 150));
        Img(banner, new Color(0.35f, 0f, 0.06f, 0.85f));
        Img(Rect("LineTop", banner, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 8), new Vector2(0.5f, 1)), red);
        Img(Rect("LineBottom", banner, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 8), new Vector2(0.5f, 0)), red);
        warn = Txt(Rect("Text", banner, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 16), new Vector2(0, 70)), warningText, 56, red);
        warnSub = Txt(Rect("Sub", banner, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -42), new Vector2(0, 30)), "", 18, cream);

        // Logo y nivel
        logoRt = Rect("Logo", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(620, 349));
        logo = Img(logoRt, Color.white);
        logo.sprite = titleLogo; logo.preserveAspect = true;
        stage = Txt(Rect("Stage", root, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -150), new Vector2(0, 40)), "", 26, gold);

        // Cuenta regresiva
        big = Txt(Rect("Count", root, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(0, 140)), "", 96, cream);
        big.gameObject.SetActive(false);
        stage.gameObject.SetActive(false);
        logoRt.gameObject.SetActive(false);
    }

    RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Vector2? pivot = null)
    {
        RectTransform rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    Image Img(RectTransform rt, Color c)
    {
        Image i = rt.gameObject.AddComponent<Image>();
        i.color = c; i.raycastTarget = false;
        return i;
    }

    Text Txt(RectTransform rt, string s, int size, Color c)
    {
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = font; t.text = s; t.fontSize = size; t.color = c;
        t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        Shadow sh = rt.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0.07f, 0.04f, 0.18f, 1f);
        sh.effectDistance = new Vector2(4, -4);
        return t;
    }
}
