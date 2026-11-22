using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] float lifetime = 8f;
    [SerializeField] Vector2 killBounds = new Vector2(9.5f, 5.5f);
    [SerializeField, Tooltip("Tamano del dibujo en unidades (Touhou: chico)")] float bulletSize = 0.3f;

    [Header("La nota que necesitas se aleja un poco del gato")]
    [SerializeField] float fleeDistance = 1.3f;
    [SerializeField] float fleeForce = 4f;
    [SerializeField, Tooltip("Con foco (Shift) se aleja menos: es mas facil atraparla")] float fleeWithFocus = 0.25f;
    [SerializeField] float fleeMaxBoost = 1.25f;

    [Header("Colision (proporcion del dibujo)")]
    [SerializeField, Tooltip("Balas normales: mas chica que el dibujo, como en Touhou")] float hitboxScale = 0.28f;
    [SerializeField, Tooltip("La nota que necesitas: mas grande, facil de atrapar")] float catchScale = 0.6f;

    CircleCollider2D col;
    float born;

    Rigidbody2D rb;
    float baseSpeed;
    Vector2 vel; // velocidad "real"; la fisica usa vel x PlayerMovement.BulletSpeed

    bool hasNote;
    SpriteRenderer sr;
    SpriteRenderer halo;

    public NoteType Note { get; private set; }
    public int Damage { get; private set; }
    public bool IsReflected { get; private set; }
    public int ReflectedDamage { get; private set; }

    public static Color GetColor(NoteType n) { return NoteStyle.GetColor(n); }

    public void Launch(Vector2 direction, float speed)
    {
        rb = GetComponent<Rigidbody2D>();
        baseSpeed = speed;
        vel = direction.normalized * speed;
        rb.linearVelocity = vel * PlayerMovement.BulletSpeed;
    }

    public void Launch(Vector2 direction, float speed, NoteType note)
    {
        Note = note;
        hasNote = true;
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = NoteStyle.GetSprite(note);
        sr.color = NoteStyle.GetColor(note);
        transform.localScale = new Vector3(bulletSize, bulletSize, 1f);
        col = GetComponent<CircleCollider2D>();
        born = Time.time;

        // Brillo para cuando sea la nota que necesitas
        GameObject h = new GameObject("Halo");
        h.transform.SetParent(transform, false);
        h.transform.localScale = new Vector3(2.4f, 2.4f, 1f);
        halo = h.AddComponent<SpriteRenderer>();
        halo.sprite = NoteStyle.Halo;
        halo.sortingLayerID = sr.sortingLayerID;
        halo.sortingOrder = sr.sortingOrder - 1;
        halo.enabled = false;
        Launch(direction, speed);
    }

    public void Launch(Vector2 direction, float speed, NoteType note, int damage)
    {
        Damage = damage;
        Launch(direction, speed, note);
    }

    public void Reflect(Vector2 direction, float speed, int damageToBoss)
    {
        IsReflected = true;
        ReflectedDamage = damageToBoss;
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        vel = direction.normalized * speed;
        rb.linearVelocity = vel * PlayerMovement.BulletSpeed;
    }

    void Start() { Destroy(gameObject, lifetime); }

    void Update()
    {
        if (hasNote)
        {
            // La nota que necesitas brilla y late; las demas se ven normales
            bool need = !IsReflected && NoteStyle.IsNeeded(Note);
            halo.enabled = need;
            if (col != null) col.radius = need ? catchScale : hitboxScale;
            float k = need ? 1.15f : 1f;
            k *= Mathf.Lerp(1.8f, 1f, Mathf.Clamp01((Time.time - born) / 0.12f)); // "pop" al salir con el golpe
            transform.localScale = new Vector3(bulletSize * k, bulletSize * k, 1f);
            if (need)
            {
                Flee();
                Color c = NoteStyle.GetColor(Note);
                c.a = 0.7f;
                halo.color = c;
            }
        }

        Vector2 p = transform.position;
        if (Mathf.Abs(p.x) > killBounds.x || Mathf.Abs(p.y) > killBounds.y)
            Destroy(gameObject);
    }

    void Flee()
    {
        if (!PlayerMovement.Exists || rb == null) return;
        Vector2 away = (Vector2)transform.position - PlayerMovement.Position;
        float d = away.magnitude;
        if (d >= fleeDistance || d < 0.001f) return;

        float strength = fleeForce * (1f - d / fleeDistance);
        if (PlayerMovement.Focusing) strength *= fleeWithFocus;

        Vector2 v = vel + away / d * strength * Time.deltaTime;
        float max = baseSpeed * fleeMaxBoost;
        if (v.magnitude > max) v = v.normalized * max;
        vel = v;
    }

    void FixedUpdate()
    {
        if (rb != null) rb.linearVelocity = vel * PlayerMovement.BulletSpeed;
    }
}
