using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float speed = 7f;
    [SerializeField] Vector2 minBounds = new Vector2(-8.6f, -4.6f);
    [SerializeField] Vector2 maxBounds = new Vector2(8.6f, 2.6f);
    [SerializeField] GameObject hitboxDot;

    [Header("Hitbox estilo Touhou (en unidades del mundo)")]
    [SerializeField, Tooltip("Radio real con el que te pegan")] float hitboxRadius = 0.06f;
    [SerializeField, Tooltip("Diametro del punto rojo que se ve con foco")] float dotDiameter = 0.16f;

    [Header("Foco (Shift)")]
    [SerializeField, Tooltip("Velocidad con foco = speed x esto")] float focusSlow = 0.3f;
    [SerializeField, Tooltip("Segundos de foco con la barra llena")] float focusMaxTime = 2.5f;
    [SerializeField, Tooltip("Segundos para llenar la barra desde vacia")] float focusRecharge = 3f;
    [SerializeField, Tooltip("Velocidad de las balas con foco (1 = normal)")] float focusBulletSpeed = 0.7f;

    // Las balas multiplican su velocidad por esto (baja suave al usar foco)
    public static float BulletSpeed { get; private set; } = 1f;
    // 0 = sin foco, 1 = foco completo (para efectos de sonido e imagen)
    public static float FocusBlend { get; private set; }

    // Para que las balas sepan donde esta el gato y si usa foco
    public static Vector2 Position { get; private set; }
    public static bool Focusing { get; private set; }
    public static bool Exists { get; private set; }

    Rigidbody2D rb;
    Vector2 input;
    bool focusing;
    float energy = 1f;   // 1 = llena, 0 = vacia
    bool locked;         // se vacio: hay que esperar a que se llene otra vez (cooldown)
    Transform barRoot, barFill;

    SpriteRenderer fillSr;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Exists = false; Focusing = false; Position = Vector2.zero; BulletSpeed = 1f; FocusBlend = 0f; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ApplyHitbox();
        BuildBar();
    }

    void OnEnable() { Exists = true; }
    void OnDisable() { Exists = false; Focusing = false; BulletSpeed = 1f; FocusBlend = 0f; }

    void Update()
    {
        input = new Vector2(Input.GetAxisRaw("Horizontal"),
                            Input.GetAxisRaw("Vertical")).normalized;

        bool wants = Input.GetKey(KeyCode.LeftShift);
        focusing = wants && !locked && energy > 0f;

        if (focusing)
        {
            energy -= Time.deltaTime / focusMaxTime;
            if (energy <= 0f) { energy = 0f; locked = true; }
        }
        else
        {
            energy = Mathf.Min(1f, energy + Time.deltaTime / focusRecharge);
            if (locked && energy >= 1f) locked = false;
        }

        // Como en Touhou: el punto de la hitbox solo se ve con foco (no cambia de tamano)
        if (hitboxDot != null) hitboxDot.SetActive(focusing);
        Focusing = focusing;
        Position = rb.position;
        FocusBlend = Mathf.MoveTowards(FocusBlend, focusing ? 1f : 0f, Time.unscaledDeltaTime * 5f);
        BulletSpeed = Mathf.Lerp(1f, focusBulletSpeed, FocusBlend);
        UpdateBar();
    }

    void FixedUpdate()
    {
        float currentSpeed = focusing ? speed * focusSlow : speed;
        rb.linearVelocity = input * currentSpeed;

        Vector2 p = rb.position;
        p.x = Mathf.Clamp(p.x, minBounds.x, maxBounds.x);
        p.y = Mathf.Clamp(p.y, minBounds.y, maxBounds.y);
        rb.position = p;
    }

    // Ajusta el collider y el punto al tamano real, sin importar la escala del Player
    void ApplyHitbox()
    {
        float s = Mathf.Max(0.0001f, transform.lossyScale.x);
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null) { col.radius = hitboxRadius / s; col.offset = Vector2.zero; }

        if (hitboxDot != null)
        {
            SpriteRenderer dot = hitboxDot.GetComponent<SpriteRenderer>();
            float spriteSize = (dot != null && dot.sprite != null) ? dot.sprite.bounds.size.x : 1f;
            hitboxDot.transform.localPosition = Vector3.zero;
            hitboxDot.transform.localScale = Vector3.one * (dotDiameter / spriteSize / s);
            if (dot != null) dot.sortingOrder = 30;
        }
    }

    // ---------- Barrita de foco debajo del gato ----------

    void BuildBar()
    {
        Texture2D t = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        t.SetPixels(px); t.Apply();
        Sprite white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0f, 0.5f), 4);

        barRoot = new GameObject("FocusBar").transform;
        barRoot.SetParent(transform, false);
        float inv = 1f / Mathf.Max(0.0001f, transform.lossyScale.x);
        barRoot.localScale = new Vector3(inv, inv, 1f);
        barRoot.localPosition = new Vector3(-0.4f * inv, -0.6f * inv, 0f);

        SpriteRenderer bg = new GameObject("Bg").AddComponent<SpriteRenderer>();
        bg.transform.SetParent(barRoot, false);
        bg.transform.localScale = new Vector3(0.8f, 0.08f, 1f);
        bg.sprite = white; bg.color = new Color(0f, 0f, 0f, 0.6f); bg.sortingOrder = 20;

        fillSr = new GameObject("Fill").AddComponent<SpriteRenderer>();
        barFill = fillSr.transform;
        barFill.SetParent(barRoot, false);
        fillSr.sprite = white; fillSr.sortingOrder = 21;
    }

    void UpdateBar()
    {
        barRoot.gameObject.SetActive(energy < 1f);
        barFill.localScale = new Vector3(0.8f * energy, 0.08f, 1f);
        fillSr.color = locked ? new Color(1f, 0.3f, 0.37f) : new Color(0.25f, 0.88f, 0.94f);
    }
}
