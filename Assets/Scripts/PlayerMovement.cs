using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float speed = 7f;
    Rigidbody2D rb;
    Vector2 input;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void Update()
    {
        input = new Vector2(Input.GetAxisRaw("Horizontal"),
                            Input.GetAxisRaw("Vertical")).normalized;
    }

    void FixedUpdate() { rb.linearVelocity = input * speed; }
}