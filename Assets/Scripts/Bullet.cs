using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] float lifetime = 8f;
    [SerializeField] Vector2 killBounds = new Vector2(9.5f, 5.5f);

    public NoteType Note { get; private set; }

    static readonly Color[] noteColors =
    {
        Color.red, Color.blue, Color.green, Color.yellow,
        new Color(1f, 0.5f, 0f), Color.cyan, Color.magenta,
        new Color(0.8f, 0.8f, 0.8f)
    };

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

    void Update()
    {
        Vector2 p = transform.position;
        if (Mathf.Abs(p.x) > killBounds.x || Mathf.Abs(p.y) > killBounds.y)
            Destroy(gameObject);
    }
}