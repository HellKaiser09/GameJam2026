using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] float lifetime = 8f;

    public NoteType Note { get; private set; }

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

    void Start() { Destroy(gameObject, lifetime); }
}