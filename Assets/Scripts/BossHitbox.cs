using UnityEngine;

[RequireComponent(typeof(BossHealth))]
public class BossHitbox : MonoBehaviour
{
    BossHealth health;

    void Awake() { health = GetComponent<BossHealth>(); }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Bullet")) return;

        Bullet bullet = other.GetComponent<Bullet>();
        if (bullet == null || !bullet.IsReflected) return;

        health.TakeDamage(bullet.ReflectedDamage);
        Debug.Log("Vida del jefe: " + health.CurrentHealth);
        Destroy(other.gameObject);
    }
}