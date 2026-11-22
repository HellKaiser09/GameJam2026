using System;
using UnityEngine;

public class PlayerParry : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] Transform boss;
    [SerializeField] ScoreManager score;
    [SerializeField] BeatClock beatClock;

    [Header("Parry")]
    [SerializeField] KeyCode parryKey = KeyCode.Space;
    [SerializeField] float radius = 1.5f;
    [SerializeField] float returnSpeed = 8f;
    [SerializeField, Tooltip("Beats que deben pasar entre un parry y otro")]
    int beatsBetweenParries = 2;

    [Header("Tiempo que regala cada nota devuelta")]
    [SerializeField] int normalSeconds = 1;
    [SerializeField] int onBeatSeconds = 2;
    [SerializeField, Tooltip("Margen (segundos) para contar como 'a ritmo'")]
    float beatWindow = 0.12f;

    int lastParryBeat = -999;

    [Header("Indicador del parry")]
    [SerializeField] Color readyColor = new Color(0.25f, 0.88f, 0.94f);

    SpriteRenderer rangeRing;   // tu alcance: se ve si el parry esta listo

    public bool Ready => beatClock.CurrentBeat - lastParryBeat >= beatsBetweenParries;

    // (posicion, radio, a ritmo, cuantas balas devolvio)
    public static event Action<Vector2, float, bool, int> OnParry;

    void Awake()
    {
        Sprite ring = MakeRing();
        rangeRing = NewRing("ParryRange", ring, 14);
    }

    void Update()
    {
        if (Input.GetKeyDown(parryKey)) TryParry();
        UpdateIndicator();
    }

    void UpdateIndicator()
    {
        // Anillo quieto con tu alcance: se ve si el parry esta listo, desaparece en cooldown
        float baseScale = radius / 0.44f / Mathf.Max(0.0001f, transform.lossyScale.x);
        bool ready = Ready && beatClock.CurrentBeat >= 0;
        rangeRing.transform.localScale = new Vector3(baseScale, baseScale, 1f);
        Color c = readyColor;
        c.a = ready ? 0.35f : 0f;
        rangeRing.color = c;
    }

    SpriteRenderer NewRing(string name, Sprite sprite, int order)
    {
        SpriteRenderer r = new GameObject(name).AddComponent<SpriteRenderer>();
        r.transform.SetParent(transform, false);
        r.sprite = sprite;
        r.sortingOrder = order;
        return r;
    }

    static Sprite MakeRing()
    {
        const int N = 128;
        Texture2D t = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float d = new Vector2((x + 0.5f) / N - 0.5f, (y + 0.5f) / N - 0.5f).magnitude;
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.44f) / 0.025f);
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
    }

    void TryParry()
    {
        int beat = beatClock.CurrentBeat;
        if (beat - lastParryBeat < beatsBetweenParries) return;
        lastParryBeat = beat;

        bool onBeat = beatClock.DistanceToNearestBeat() <= beatWindow;
        int seconds = onBeat ? onBeatSeconds : normalSeconds;

        int count = 0;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Bullet")) continue;

            Bullet bullet = hit.GetComponent<Bullet>();
            if (bullet == null || bullet.IsReflected) continue;

            // La nota que necesitas NO se devuelve
            if (!score.Finished && bullet.Note == score.Expected) continue;

            Vector2 dir = boss.position - bullet.transform.position;
            bullet.Reflect(dir, returnSpeed, seconds); // ReflectedDamage = segundos
            count++;
        }
        OnParry?.Invoke(transform.position, radius, onBeat, count);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
