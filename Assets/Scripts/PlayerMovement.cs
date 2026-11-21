using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float speed = 7f;
    [SerializeField] float focusMultiplier = 0.4f;
    [SerializeField] Vector2 minBounds = new Vector2(-8f, -4.5f);
    [SerializeField] Vector2 maxBounds = new Vector2(8f, 4.5f);
    [SerializeField] GameObject hitboxDot;

    Rigidbody2D rb;
    Vector2 input;
    bool focusing;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void Update()
    {
        input = new Vector2(Input.GetAxisRaw("Horizontal"),
                            Input.GetAxisRaw("Vertical")).normalized;

        focusing = Input.GetKey(KeyCode.LeftShift);
        if (hitboxDot != null) hitboxDot.SetActive(focusing);
    }

    void FixedUpdate()
    {
        float currentSpeed = focusing ? speed * focusMultiplier : speed;
        rb.linearVelocity = input * currentSpeed;

        Vector2 p = rb.position;
        p.x = Mathf.Clamp(p.x, minBounds.x, maxBounds.x);
        p.y = Mathf.Clamp(p.y, minBounds.y, maxBounds.y);
        rb.position = p;
    }
}