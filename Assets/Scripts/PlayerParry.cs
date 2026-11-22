using UnityEngine;

public class PlayerParry : MonoBehaviour
{
    [SerializeField] Transform boss;
    [SerializeField] KeyCode parryKey = KeyCode.Space;
    [SerializeField] float radius = 1.5f;
    [SerializeField] float cooldown = 0.4f;
    [SerializeField] float returnSpeed = 8f;
    [SerializeField] int damageToBoss = 10;

    float nextParryTime;

    void Update()
    {
        if (Input.GetKeyDown(parryKey) && Time.time >= nextParryTime)
        {
            nextParryTime = Time.time + cooldown;
            Parry();
        }
    }

    void Parry()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Bullet")) continue;

            Bullet bullet = hit.GetComponent<Bullet>();
            if (bullet == null || bullet.IsReflected) continue;

            Vector2 dir = boss.position - bullet.transform.position;
            bullet.Reflect(dir, returnSpeed, damageToBoss);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Cyan();
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    static Color Cyan() { return Color.cyan; }
}