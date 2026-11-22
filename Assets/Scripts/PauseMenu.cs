using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Ponlo en el Canvas. Esc (o P) pausa: se congela el juego y la musica.
// Botones: Continuar, Reintentar, Salir. Se puede usar con mouse o con flechas + Enter.
public class PauseMenu : MonoBehaviour
{
    public static bool Paused { get; private set; }

    [SerializeField] FightManager fight;
    [SerializeField] BeatClock beatClock;

    static readonly Color cream = new Color(1f, 0.96f, 0.86f);
    static readonly Color gold = new Color(1f, 0.88f, 0.3f);
    static readonly Color lilac = new Color(0.79f, 0.72f, 1f);

    GameObject panel;
    Button first;
    Font font;
    bool stoppedMusic;
    float prevTimeScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Paused = false; }

    void Awake()
    {
        font = Resources.Load<Font>("Fonts/PressStart2P");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Build();
    }

    void OnDisable() { Paused = false; }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (Paused) Resume();
            else if (!fight.Over) Pause();   // no se pausa en victoria / derrota
        }
    }

    public void Pause()
    {
        Paused = true;
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        // Si la musica ya estaba parada (pausa entre rondas) no la tocamos
        stoppedMusic = !beatClock.IsStopped;
        if (stoppedMusic) beatClock.PauseNow();
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    public void Resume()
    {
        Paused = false;
        Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;
        if (stoppedMusic) beatClock.Resume();
        panel.SetActive(false);
    }

    void Restart()
    {
        Paused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- Construir ----------

    void Build()
    {
        RectTransform p = Rect("PauseMenu", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image bg = p.gameObject.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.02f, 0.1f, 0.82f);
        panel = p.gameObject;

        Txt(Rect("Title", p, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 190), new Vector2(0, 80)), "PAUSA", 56, gold);

        first = MakeButton(p, "CONTINUAR", new Vector2(0, 60), Resume);
        MakeButton(p, "REINTENTAR", new Vector2(0, -20), Restart);
        MakeButton(p, "SALIR", new Vector2(0, -100), Quit);

        Txt(Rect("Help", p, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 70), new Vector2(0, 60)),
            "FLECHAS/WASD: MOVER   SHIFT: FOCO   ESPACIO: PARRY\nATRAPA LA MISMA FORMA DEL OTRO COLOR", 12, lilac);

        panel.SetActive(false);
    }

    Button MakeButton(RectTransform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        RectTransform r = Rect(label, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(340, 60));
        Image img = r.gameObject.AddComponent<Image>();
        img.color = Color.white;
        Button b = r.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        ColorBlock cb = b.colors;
        cb.normalColor = new Color(0.25f, 0.18f, 0.55f);
        cb.highlightedColor = new Color(0.45f, 0.32f, 0.85f);
        cb.selectedColor = new Color(0.45f, 0.32f, 0.85f);
        cb.pressedColor = gold;
        b.colors = cb;
        b.onClick.AddListener(action);
        Txt(Rect("Label", r, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), label, 22, cream);
        return b;
    }

    RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        RectTransform rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    Text Txt(RectTransform rt, string s, int size, Color c)
    {
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = font; t.text = s; t.fontSize = size; t.color = c;
        t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.lineSpacing = 1.6f;
        Shadow sh = rt.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0.07f, 0.04f, 0.18f, 1f);
        sh.effectDistance = new Vector2(3, -3);
        return t;
    }
}
