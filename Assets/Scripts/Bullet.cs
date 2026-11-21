using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] float lifetime = 8f;

    public void Launch(Vector2 direction, float speed)
    {
        GetComponent<Rigidbody2D>().linearVelocity = direction.normalized * speed;
    }

    void Start() { Destroy(gameObject, lifetime); }
}