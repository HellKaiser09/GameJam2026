using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Ponlo en el objeto Canvas. Construye solo la UI de la pelea:
// vida del jefe (partitura que se rompe), barra de desafinacion,
// ronda, pescados del gato y pantallas de victoria / game over.
public class FightUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] FightManager fight;
    [SerializeField] ScoreManager score;
    [SerializeField] BeatClock beatClock;

    [Header("Sprites")]
    [SerializeField] Sprite fishFull;           // pez
    [SerializeField] Sprite fishLost;           // hueso
    [SerializeField] Sprite[] bossStaffStates;  // parti1 ... parti5 (entera -> rota)
    [SerializeField] Sprite winBackground;      // final (opcional)

    [Header("Colores")]
    [SerializeField] Color barLow = new Color(0.30f, 0.85f, 0.45f);
    [SerializeField] Color barHigh = new Color(0.95f, 0.25f, 0.30f);
    [SerializeField] Color textColor = new Color(1f, 0.95f, 0.75f);

    [Header("Tamanos")]
    [SerializeField] float fishSize = 64f;
    [SerializeField] float beatPulse = 0.08f;

    Image[] fish;
    Image staff;
    RectTransform barFill;
    Image barFillImg;
    Text roundText;
    GameObject endPanel;
    Text endTitle;
    Image endBg;
    RectTransform pulseA, pulseB;
    float pulse;
    int shownLives = -1;
    static Font font;

    void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Build();
    }

    void OnEnable()
    {
        fight.OnLivesChanged += RefreshLives;
        fight.OnGameOver += ShowLose;
        fight.OnWin += ShowWin;
        score.OnProgressChanged += RefreshBoss;
        score.OnRoundStarted += RefreshBoss;
        if (beatClock != null) beatClock.OnBeat += OnBeat;
    }

    void OnDisable()
    {
        fight.OnLivesChanged -= RefreshLives;
        fight.OnGameOver -= ShowLose;
        fight.OnWin -= ShowWin;
        score.OnProgressChanged -= RefreshBoss;
        score.OnRoundStarted -= RefreshBoss;
        if (beatClock != null) beatClock.OnBeat -= OnBeat;
    }

    void Start() { RefreshBoss(); RefreshLives(); }

    void Update()
    {
        // Barra: se llena mientras se acaba el tiempo
        if (fight.TimeMax > 0f)
        {
            float t = 1f - Mathf.Clamp01(fight.TimeLeft / fight.TimeMax);
            barFill.anchorMax = new Vector2(t, 1f);
            barFillImg.color = Color.Lerp(barLow, barHigh, t);
        }

        // Pulso al ritmo
        pulse = Mathf.MoveTowards(pulse, 0f, Time.unscaledDeltaTime * 0.6f);
        float s = 1f + pulse;
        pulseA.localScale = new Vector3(s, s, 1f);
        pulseB.localScale = new Vector3(s, s, 1f);
    }

    void OnBeat(int beat) { if (!fight.Over) pulse = beatPulse; }

    // ---------- Actualizar ----------

    void RefreshLives()
    {
        for (int i = 0; i < fish.Length; i++)
        {
            bool alive = i < fight.Lives;
            fish[i].sprite = alive ? fishFull : fishLost;
            fish[i].color = alive ? Color.white : new Color(1f, 1f, 1f, 0.6f);
        }
        // Pequeno salto del pez que se perdio
        if (shownLives > fight.Lives && fight.Lives >= 0 && fight.Lives < fish.Length)
            fish[fight.Lives].rectTransform.localScale = Vector3.one * 1.4f;
        shownLives = fight.Lives;
    }

    void RefreshBoss()
    {
        roundText.text = "RONDA " + (score.Round + 1) + "/" + score.TotalRounds;

        if (bossStaffStates == null || bossStaffStates.Length == 0) return;
        float done = score.Finished ? 1f
            : (score.Round + (float)score.Progress / score.PoolSize) / score.TotalRounds;
        int idx = Mathf.Clamp(Mathf.FloorToInt(done * (bossStaffStates.Length - 1) + 0.001f),
                              0, bossStaffStates.Length - 1);
        staff.sprite = bossStaffStates[idx];
    }

    void LateUpdate()
    {
        // el pez que se perdio regresa a su tamano
        foreach (Image f in fish)
            f.rectTransform.localScale = Vector3.MoveTowards(
                f.rectTransform.localScale, Vector3.one, Time.unscaledDeltaTime * 2f);
    }

    void ShowWin()
    {
        endPanel.SetActive(true);
        endTitle.text = "¡AFINADO!";
        if (winBackground != null) { endBg.sprite = winBackground; endBg.color = Color.white; }
    }

    void ShowLose()
    {
        endPanel.SetActive(true);
        endTitle.text = "DESAFINADO...";
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ---------- Construir ----------

    void Build()
    {
        RectTransform root = Rect("FightHUD", transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // Vida del jefe: partitura arriba a la izquierda
        RectTransform staffRt = Rect("BossStaff", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(-80, 60), new Vector2(420, 236));
        staff = Img(staffRt, null, Color.white);
        staff.preserveAspect = true;
        pulseA = staffRt;

        // Ronda y barra arriba a la derecha
        RectTransform right = Rect("RoundAndBar", root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -16), new Vector2(340, 70));
        pulseB = right;
        roundText = Txt(Rect("Round", right, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 30)), "RONDA 1/3", 26, TextAnchor.MiddleRight);
        Txt(Rect("BarLabel", right, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(0, 18)), "DESAFINACIÓN", 14, TextAnchor.MiddleLeft);
        RectTransform bar = Rect("Bar", right, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 16));
        Img(bar, null, new Color(0f, 0f, 0f, 0.55f));
        barFill = Rect("Fill", bar, Vector2.zero, new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
        barFillImg = Img(barFill, null, barLow);

        // Pescados abajo a la izquierda
        fish = new Image[3];
        for (int i = 0; i < fish.Length; i++)
        {
            RectTransform f = Rect("Fish" + i, root, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f),
                new Vector2(24 + fishSize / 2 + i * (fishSize + 8), 20 + fishSize / 2), new Vector2(fishSize, fishSize));
            fish[i] = Img(f, fishFull, Color.white);
            fish[i].preserveAspect = true;
        }

        // Pantalla final
        RectTransform panel = Rect("EndPanel", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        endBg = Img(panel, null, new Color(0.03f, 0.02f, 0.08f, 0.85f));
        endTitle = Txt(Rect("Title", panel, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(0, 100)), "", 72, TextAnchor.MiddleCenter);

        RectTransform btn = Rect("Retry", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(240, 60));
        Image btnImg = Img(btn, null, new Color(0.35f, 0.25f, 0.7f));
        Button b = btn.gameObject.AddComponent<Button>();
        btnImg.raycastTarget = true; // para que el boton se pueda clicar
        b.targetGraphic = btnImg;
        b.onClick.AddListener(Restart);
        Txt(Rect("Label", btn, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), "REINTENTAR", 26, TextAnchor.MiddleCenter);
        endPanel = panel.gameObject;
        endPanel.SetActive(false);
    }

    RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    Image Img(RectTransform rt, Sprite s, Color c)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = s; img.color = c; img.raycastTarget = false;
        return img;
    }

    Text Txt(RectTransform rt, string text, int size, TextAnchor align)
    {
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = font; t.text = text; t.fontSize = size; t.alignment = align;
        t.color = textColor; t.fontStyle = FontStyle.Bold; t.raycastTarget = false;
        Outline o = rt.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0.1f, 0.05f, 0.2f, 1f);
        o.effectDistance = new Vector2(2, -2);
        return t;
    }
}
