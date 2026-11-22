using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] float lifetime = 8f;

    public NoteType Note { get; private set; }
    public int Damage { get; private set; }
    public bool IsReflected { get; private set; }
    public int ReflectedDamage { get; private set; }

    static readonly Color[] noteColors =
        { Color.red, Color.blue, Color.green, Color.yellow };

    public static Color GetColor(NoteType n) { return noteColors[(int)n]; }

    public void Launch(Vector2 direction, float speed)
    {
        GetComponent<Rigidbody2D>().linearVelocity = direction.normalized * speed;
    }

    public void Launch(Vector2 direction, float speed, NoteType note)
    {
        Note = note;
        GetComponent<SpriteRenderer>().color = noteColors[(int)note];
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
        GetComponent<Rigidbody2D>().linearVelocity = direction.normalized * speed;
    }

    void Start() { Destroy(gameObject, lifetime); }
}
