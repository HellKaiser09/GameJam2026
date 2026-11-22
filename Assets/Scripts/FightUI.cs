using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Ponlo en el Canvas. Construye todo el HUD de la pelea (como el mockup "Nivel Luna"):
//  - Izquierda: partitura del jefe que se rompe (sin recuadro)
//  - Centro: fila JEFE (notas que desafina) y fila TU (lo que llevas); la siguiente late con el beat
//  - Derecha: ronda y barra de desafinacion
//  - Abajo izquierda: pescados
//  - Pantalla de victoria / game over con boton Reintentar
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
    [SerializeField] Color barLow = new Color(0.36f, 0.89f, 0.42f);
    [SerializeField] Color barHigh = new Color(1f, 0.30f, 0.37f);
    [SerializeField] Color textColor = new Color(1f, 0.96f, 0.86f);
    [SerializeField] Color labelColor = new Color(0.79f, 0.72f, 1f);
    [SerializeField] Color panelColor = new Color(0.05f, 0.03f, 0.15f, 0.72f);
    [SerializeField] Color nextColor = new Color(1f, 0.88f, 0.30f);

    [Header("Ayuda")]
    [SerializeField, Tooltip("Hace brillar las balas de la nota que necesitas y la muestra en la casilla que sigue")]
    bool showNeededNote = true;

    const float SlotSize = 40f, SlotGap = 8f, IconSize = 28f;

    // Elementos construidos
    Image staff;
    Text roundText;
    RectTransform barFill;
    Image barFillImg;
    Image[] fish;
    RectTransform bossRow, playerRow;
    readonly List<Image> bossSlots = new List<Image>();
    readonly List<Image> playerSlots = new List<Image>();
    readonly List<Image> playerIcons = new List<Image>();
    GameObject endPanel;
    Text endTitle, endSub;
    Image endBg;

    // Efectos de la partitura
    RectTransform staffRt, hudRoot;
    Vector2 staffHome;
    float staffShake, staffFlash, staffPunch;
    bool staffBusy;
    int shownStaff = -1;
    Sprite shardSprite;
    static readonly Color shardColor = new Color(0.9f, 0.88f, 0.97f);
    int shownLives = -1;
    Font font, retroFont;
    RectTransform scorePanel, fishPanel;
    Image dim;
    Text banner, healText;
    Vector2 staffAnimPos;
    float bannerTime, healTime;
    const float Margin = 24f;

    void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        retroFont = Resources.Load<Font>("Fonts/PressStart2P");
        if (retroFont == null) retroFont = font;

        // Que la UI escale igual en cualquier pantalla (base 1280x720, como el mockup)
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
        }
        Build();
    }

    void OnEnable()
    {
        fight.OnLivesChanged += RefreshLives;
        fight.OnGameOver += ShowLose;
        fight.OnWin += ShowWin;
        fight.OnHealed += Healed;
        score.OnRoundStarted += RebuildRows;
        score.OnProgressChanged += RefreshRows;
        score.OnCorrect += StaffHit;
        score.OnCompleted += StaffBreak;
        if (beatClock != null) beatClock.OnBeat += OnBeat;
    }

    void OnDisable()
    {
        fight.OnLivesChanged -= RefreshLives;
        fight.OnGameOver -= ShowLose;
        fight.OnWin -= ShowWin;
        fight.OnHealed -= Healed;
        score.OnRoundStarted -= RebuildRows;
        score.OnProgressChanged -= RefreshRows;
        score.OnCorrect -= StaffHit;
        score.OnCompleted -= StaffBreak;
        if (beatClock != null) beatClock.OnBeat -= OnBeat;
    }

    void Start() { RebuildRows(); RefreshLives(); }

    void Update()
    {
        // Barra: se llena mientras se acaba el tiempo
        if (fight.TimeMax > 0f)
        {
            float t = 1f - Mathf.Clamp01(fight.TimeLeft / fight.TimeMax);
            barFill.anchorMax = new Vector2(t, 1f);
            barFillImg.color = Color.Lerp(barLow, barHigh, t);
        }

        UpdateStaffFx();
        UpdateTexts();

        // El pez que se perdio regresa a su tamano
        foreach (Image f in fish)
            f.rectTransform.localScale = Vector3.MoveTowards(
                f.rectTransform.localScale, Vector3.one, Time.unscaledDeltaTime * 2f);
    }

    void OnBeat(int beat) { }

    // ---------- Filas de la partitura ----------

    void RebuildRows()
    {
        foreach (Image i in bossSlots) Destroy(i.gameObject);
        foreach (Image i in playerSlots) Destroy(i.gameObject);
        bossSlots.Clear(); playerSlots.Clear(); playerIcons.Clear();

        int n = score.BossScore.Count;
        for (int i = 0; i < n; i++)
        {
            Image b = Slot(bossRow, i, out Image bIcon);
            NoteType note = score.BossScore[i];
            bIcon.sprite = NoteStyle.GetSprite(note);
            bIcon.color = NoteStyle.GetColor(note);
            bossSlots.Add(b);

            Image p = Slot(playerRow, i, out Image pIcon);
            playerSlots.Add(p);
            playerIcons.Add(pIcon);
        }
        roundText.text = "RONDA " + (score.Round + 1) + "/" + score.TotalRounds;

        // El panel se ajusta al numero de notas y queda centrado
        float rowWidth = n * SlotSize + (n - 1) * SlotGap;
        scorePanel.sizeDelta = new Vector2(18 + 70 + rowWidth + 18, scorePanel.sizeDelta.y);
        RefreshRows();
    }

    void RefreshRows()
    {
        for (int i = 0; i < playerSlots.Count; i++)
        {
            bool done = i < score.Progress;
            bool next = i == score.Progress && score.HasExpected;
            Image icon = playerIcons[i];
            icon.enabled = done || (next && showNeededNote);
            if (icon.enabled)
            {
                NoteType note = score.OppositeOf(score.BossScore[i]);
                icon.sprite = NoteStyle.GetSprite(note);
                Color c = NoteStyle.GetColor(note);
                if (!done) c.a = 0.6f; // la que sigue: se ve "fantasma"
                icon.color = c;
            }
            playerSlots[i].color = next ? new Color(nextColor.r, nextColor.g, nextColor.b, 0.45f)
                                 : done ? new Color(0.42f, 1f, 0.54f, 0.22f)
                                 : new Color(1f, 1f, 1f, 0.10f);
        }
        NoteStyle.HighlightOn = showNeededNote;
        NoteStyle.SetNeeded(score.HasExpected, score.Expected);
        RefreshStaff();
    }

    // El dibujo cambia por ronda: entera -> rota -> mas rota ... -> destruida al ganar
    int StaffIndex()
    {
        int last = bossStaffStates.Length - 1;
        if (score.Finished) return last;
        return Mathf.Clamp(Mathf.RoundToInt(score.Round * last / (float)score.TotalRounds), 0, last);
    }

    void RefreshStaff()
    {
        if (bossStaffStates == null || bossStaffStates.Length == 0) return;
        if (staffBusy) return; // la animacion grande cambia el dibujo ella misma
        int idx = StaffIndex();
        if (idx != shownStaff && shownStaff >= 0) StaffCrack(18); // cambio de dibujo: se rompe un pedazo
        shownStaff = idx;
        staff.sprite = bossStaffStates[idx];
    }

    Image Slot(RectTransform row, int i, out Image icon)
    {
        RectTransform rt = Rect("Slot" + i, row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(SlotSize / 2 + i * (SlotSize + SlotGap), 0), new Vector2(SlotSize, SlotSize));
        Image bg = Img(rt, null, new Color(1f, 1f, 1f, 0.10f));
        RectTransform it = Rect("Icon", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(IconSize, IconSize));
        icon = Img(it, null, Color.white);
        icon.preserveAspect = true;
        return bg;
    }

    // ---------- Vidas y final ----------

    void RefreshLives()
    {
        for (int i = 0; i < fish.Length; i++)
        {
            bool alive = i < fight.Lives;
            fish[i].sprite = alive ? fishFull : fishLost;
            fish[i].color = alive ? Color.white : new Color(1f, 1f, 1f, 0.7f);
        }
        if (shownLives > fight.Lives && fight.Lives >= 0 && fight.Lives < fish.Length)
            fish[fight.Lives].rectTransform.localScale = Vector3.one * 1.4f;
        shownLives = fight.Lives;
    }

    void ShowWin() { StartCoroutine(WinSequence()); }

    // La partitura se destruye por completo y cae; al final, la pantalla de victoria
    System.Collections.IEnumerator WinSequence()
    {
        staffBusy = true;
        Vector2 center = new Vector2(640, -300);
        const float big = 2.6f;
        dim.transform.SetAsLastSibling();
        staffRt.SetAsLastSibling();

        float t = 0f;
        while (t < 0.5f) { t += Time.unscaledDeltaTime; yield return null; } // golpe final congelado

        // Va al centro
        t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / 0.35f);
            staffAnimPos = Vector2.Lerp(staffHome, center, k);
            staffRt.localScale = Vector3.one * Mathf.Lerp(1f, big, k);
            dim.color = new Color(0f, 0f, 0f, 0.55f * k);
            yield return null;
        }

        // Se rompe en 3 golpes, uno por beat (0.5 s)
        int last = bossStaffStates.Length - 1;
        for (int i = 3; i >= 1; i--)
        {
            int idx = Mathf.Clamp(last - i + 1, 0, last);
            staff.sprite = bossStaffStates[idx];
            staffFlash = 1f; staffShake = 2.5f + (3 - i); staffPunch = 0.25f;
            Shards(25, shardColor);
            t = 0f;
            while (t < 0.5f) { t += Time.unscaledDeltaTime; yield return null; }
        }

        // Se deshace por completo: desaparece y todos los pedazos caen
        staff.enabled = false;
        for (int i = 0; i < 90; i++) Shards(1, i % 3 == 0 ? nextColor : shardColor);
        t = 0f;
        while (t < 3.4f) // mientras tanto la nave cae y choca (GameFeedback)
        {
            t += Time.unscaledDeltaTime;
            dim.color = new Color(0f, 0f, 0f, 0.55f * (1f - Mathf.Clamp01(t / 2.6f)));
            yield return null;
        }
        dim.color = Color.clear;

        // Pantalla de victoria
        endPanel.SetActive(true);
        endPanel.transform.SetAsLastSibling();
        endTitle.font = retroFont;
        endTitle.fontSize = 56;
        endTitle.text = "¡AFINADO!";
        endSub.text = "PECES: " + fight.Lives + "/3" + (fight.HealUsed ? "   CURACION USADA" : "   CURACION SIN USAR");
        if (winBackground != null) { endBg.sprite = winBackground; endBg.color = Color.white; }
    }

    void ShowLose()
    {
        endPanel.SetActive(true);
        endTitle.font = retroFont;
        endTitle.fontSize = 48;
        endTitle.text = "DESAFINADO...";
        endSub.text = "";
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ---------- La partitura del jefe se rompe ----------

    // Cada nota correcta: golpe, destello y unos pedacitos
    void StaffHit(NoteType note)
    {
        staffShake = 1f; staffFlash = 1f; staffPunch = 0.12f;
        Shards(6, NoteStyle.GetColor(note));
    }

    // Cambio de dibujo: un pedazo grande se cae
    void StaffCrack(int count)
    {
        staffShake = 1.6f; staffFlash = 1f; staffPunch = 0.35f;
        Shards(count, shardColor);
    }

    // Ronda completada: la partitura va al centro, se rompe en grande y regresa
    void StaffBreak()
    {
        if (bossStaffStates == null || bossStaffStates.Length == 0) return;
        if (score.Round + 1 >= score.TotalRounds) return; // la ultima ronda: la destruye la victoria
        StartCoroutine(BreakSequence());
    }

    System.Collections.IEnumerator BreakSequence()
    {
        staffBusy = true;
        bool last = score.Round + 1 >= score.TotalRounds;
        Vector2 center = new Vector2(640, -300);
        const float big = 2.6f;
        dim.transform.SetAsLastSibling();
        staffRt.SetAsLastSibling();
        banner.transform.SetAsLastSibling();

        // 1) Se oscurece el fondo y la partitura viaja al centro creciendo
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / 0.3f);
            staffAnimPos = Vector2.Lerp(staffHome, center, k);
            staffRt.localScale = Vector3.one * Mathf.Lerp(1f, big, k);
            dim.color = new Color(0f, 0f, 0f, 0.55f * k);
            yield return null;
        }
        // 2) Tiembla cada vez mas
        t = 0f;
        while (t < 0.45f) { t += Time.unscaledDeltaTime; staffShake = 0.8f + 3f * t; yield return null; }

        // 3) ¡CRACK! cambia al siguiente dibujo roto y salen muchos pedazos
        int idx = StaffIndex();
        shownStaff = idx;
        staff.sprite = bossStaffStates[idx];
        staffFlash = 1f; staffShake = 3.5f; staffPunch = 0.3f;
        Shards(45, shardColor);
        if (!last) { banner.text = "RONDA " + (score.Round + 1); bannerTime = 1.4f; }

        t = 0f;
        while (t < 0.9f) { t += Time.unscaledDeltaTime; yield return null; }

        // 4) Regresa a su esquina y se aclara el fondo
        t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / 0.35f);
            staffAnimPos = Vector2.Lerp(center, staffHome, k);
            staffRt.localScale = Vector3.one * Mathf.Lerp(big, 1f, k);
            dim.color = new Color(0f, 0f, 0f, 0.55f * (1f - k));
            yield return null;
        }
        staffRt.localScale = Vector3.one;
        dim.color = Color.clear;
        staffBusy = false;
    }

    void UpdateStaffFx()
    {
        float dt = Time.unscaledDeltaTime;
        staffShake = Mathf.MoveTowards(staffShake, 0f, dt * 3f);
        staffFlash = Mathf.MoveTowards(staffFlash, 0f, dt * 3f);
        staffPunch = Mathf.MoveTowards(staffPunch, 0f, dt * 1.2f);

        Vector2 basePos = staffBusy ? staffAnimPos : staffHome;
        staffRt.anchoredPosition = basePos + Random.insideUnitCircle * 8f * staffShake;
        if (!staffBusy) staffRt.localScale = Vector3.one * (1f + staffPunch);
        staff.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.6f), staffFlash);
    }

    // ---------- Curacion unica ----------

    void Healed()
    {
        int i = Mathf.Clamp(fight.Lives - 1, 0, fish.Length - 1);
        fish[i].rectTransform.localScale = Vector3.one * 1.8f;
        healText.text = "+1 PEZ\nUNICA VEZ";
        healTime = 2f;
    }

    void UpdateTexts()
    {
        float dt = Time.unscaledDeltaTime;
        bannerTime = Mathf.Max(0f, bannerTime - dt);
        Color c = banner.color; c.a = Mathf.Clamp01(bannerTime * 2f); banner.color = c;
        float s = bannerTime > 1.2f ? Mathf.Lerp(1f, 1.6f, (bannerTime - 1.2f) / 0.2f) : 1f;
        banner.rectTransform.localScale = new Vector3(s, s, 1f);

        healTime = Mathf.Max(0f, healTime - dt);
        Color h = healText.color; h.a = Mathf.Clamp01(healTime); healText.color = h;
        healText.rectTransform.anchoredPosition = new Vector2(-Margin, 100 + (2f - healTime) * 12f);
    }

    void Shards(int count, Color color)
    {
        if (shardSprite == null) shardSprite = NoteStyle.GetSprite(NoteType.Sol); // cuadrito
        for (int i = 0; i < count; i++)
        {
            RectTransform r = Rect("Shard", hudRoot, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f),
                staffRt.anchoredPosition + Random.insideUnitCircle * 60f * staffRt.localScale.x, Vector2.one * Random.Range(8f, 20f));
            Image img = Img(r, shardSprite, Random.value < 0.6f ? shardColor : color);
            StartCoroutine(Fall(r, img));
        }
    }

    System.Collections.IEnumerator Fall(RectTransform r, Image img)
    {
        Vector2 v = new Vector2(Random.Range(-260f, 260f), Random.Range(120f, 420f));
        float spin = Random.Range(-540f, 540f), t = 0f, life = Random.Range(0.9f, 1.8f);
        Color c = img.color;
        while (t < life)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            v.y -= 1100f * dt;
            r.anchoredPosition += v * dt;
            r.Rotate(0, 0, spin * dt);
            c.a = 1f - t / life; img.color = c;
            yield return null;
        }
        Destroy(r.gameObject);
    }

    // ---------- Construir ----------

    void Build()
    {
        RectTransform root = Rect("FightHUD", transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Vector2 topLeft = new Vector2(0, 1), top = new Vector2(0.5f, 1), topRight = new Vector2(1, 1);

        // Izquierda: partitura del jefe, sin recuadro
        Txt(Rect("StaffLabel", root, topLeft, topLeft, topLeft, new Vector2(Margin, -16), new Vector2(260, 22)),
            "PARTITURA DEL JEFE", 15, TextAnchor.MiddleLeft, labelColor);
        // Las imagenes de la partitura ya vienen recortadas (proporcion 2.63 : 1)
        hudRoot = root;
        staffRt = Rect("BossStaff", root, topLeft, topLeft, new Vector2(0.5f, 0.5f), new Vector2(Margin + 130, -92), new Vector2(260, 99));
        staffHome = staffRt.anchoredPosition;
        staff = Img(staffRt, null, Color.white);
        staff.preserveAspect = true;

        // Centro: filas JEFE y TU
        RectTransform center = Rect("ScorePanel", root, top, top, top, new Vector2(0, -14), new Vector2(482, 136));
        scorePanel = center;
        Img(center, null, panelColor);
        RectTransform bossLine = Rect("BossLine", center, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(0, SlotSize));
        Txt(Rect("Label", bossLine, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(70, 0)),
            "JEFE", 18, TextAnchor.MiddleLeft, new Color(1f, 0.62f, 0.69f));
        bossRow = Rect("BossRow", bossLine, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(96, 0), new Vector2(-110, 0));
        RectTransform playerLine = Rect("PlayerLine", center, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -16 - SlotSize - 16), new Vector2(0, SlotSize));
        Txt(Rect("Label", playerLine, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(70, 0)),
            "TÚ", 18, TextAnchor.MiddleLeft, new Color(0.42f, 1f, 0.54f));
        playerRow = Rect("PlayerRow", playerLine, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(96, 0), new Vector2(-110, 0));

        // Derecha: ronda y barra
        RectTransform right = Rect("RoundPanel", root, topRight, topRight, topRight, new Vector2(-24, -14), new Vector2(260, 136));
        Img(right, null, panelColor);
        roundText = Txt(Rect("Round", right, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-16, -8), new Vector2(-32, 42)),
            "RONDA 1/3", 34, TextAnchor.MiddleRight, textColor);
        Txt(Rect("BarLabel", right, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(-32, 20)),
            "DESAFINACIÓN", 14, TextAnchor.MiddleLeft, labelColor);
        RectTransform bar = Rect("Bar", right, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(-32, 18));
        Img(bar, null, new Color(0f, 0f, 0f, 0.55f));
        barFill = Rect("Fill", bar, Vector2.zero, new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
        barFillImg = Img(barFill, null, barLow);

        // Abajo izquierda: pescados
        // 3 peces abajo a la derecha, centrados en su panel y alineados con el panel de ronda
        const float fishSize = 56f, fishGap = 10f, pad = 12f;
        fishPanel = Rect("Lives", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-Margin, Margin),
            new Vector2(pad * 2 + fishSize * 3 + fishGap * 2, fishSize + pad));
        Img(fishPanel, null, panelColor);
        fish = new Image[3];
        for (int i = 0; i < fish.Length; i++)
        {
            RectTransform f = Rect("Fish" + i, fishPanel, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(pad + fishSize / 2 + i * (fishSize + fishGap), 0), new Vector2(fishSize, fishSize));
            fish[i] = Img(f, fishFull, Color.white);
            fish[i].preserveAspect = true;
        }

        // Texto de la curacion unica (arriba de los peces)
        healText = Txt(Rect("Heal", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-Margin, 100), new Vector2(260, 40)),
            "", 14, TextAnchor.LowerRight, new Color(0.42f, 1f, 0.54f));
        healText.font = retroFont;

        // Oscurecer + "RONDA N" (retro) para cuando se rompe la partitura
        dim = Img(Rect("Dim", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), null, Color.clear);
        banner = Txt(Rect("RoundBanner", root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -470), new Vector2(0, 80)),
            "", 52, TextAnchor.MiddleCenter, nextColor);
        banner.font = retroFont;
        banner.color = new Color(nextColor.r, nextColor.g, nextColor.b, 0f);

        // Pantalla final
        RectTransform panel = Rect("EndPanel", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        endBg = Img(panel, null, new Color(0.03f, 0.02f, 0.08f, 0.85f));
        endTitle = Txt(Rect("Title", panel, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(0, 100)),
            "", 72, TextAnchor.MiddleCenter, textColor);
        endSub = Txt(Rect("Sub", panel, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(0, 30)),
            "", 16, TextAnchor.MiddleCenter, labelColor);
        endSub.font = retroFont;
        RectTransform btn = Rect("Retry", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(240, 60));
        Image btnImg = Img(btn, null, new Color(0.35f, 0.25f, 0.7f));
        btnImg.raycastTarget = true; // para que el boton se pueda clicar
        Button b = btn.gameObject.AddComponent<Button>();
        b.targetGraphic = btnImg;
        b.onClick.AddListener(Restart);
        Txt(Rect("Label", btn, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero),
            "REINTENTAR", 26, TextAnchor.MiddleCenter, textColor);
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

    Text Txt(RectTransform rt, string text, int size, TextAnchor align, Color color)
    {
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = font; t.text = text; t.fontSize = size; t.alignment = align;
        t.color = color; t.fontStyle = FontStyle.Bold; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        Outline o = rt.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0.07f, 0.04f, 0.18f, 1f);
        o.effectDistance = new Vector2(2, -2);
        return t;
    }
}
