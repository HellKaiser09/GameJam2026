using UnityEngine;

public class BossHitbox : MonoBehaviour
{
    [SerializeField] FightManager fight;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Bullet")) return;

        Bullet bullet = other.GetComponent<Bullet>();
        if (bullet == null || !bullet.IsReflected) return;

        fight.AddTime(bullet.ReflectedDamage); 
        Destroy(other.gameObject);
    }
}
