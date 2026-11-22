using UnityEngine;

public class PlayerParry : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] Transform boss;
    [SerializeField] ScoreManager score;
    [SerializeField] BeatClock beatClock;

    [Header("Parry")]
    [SerializeField] KeyCode parryKey = KeyCode.Space;
    [SerializeField] float radius = 1.5f;
    [SerializeField] float returnSpeed = 8f;
    [SerializeField, Tooltip("Beats que deben pasar entre un parry y otro")]
    int beatsBetweenParries = 2;

    [Header("Tiempo que regala cada nota devuelta")]
    [SerializeField] int normalSeconds = 1;
    [SerializeField] int onBeatSeconds = 2;
    [SerializeField, Tooltip("Margen (segundos) para contar como 'a ritmo'")]
    float beatWindow = 0.12f;

    int lastParryBeat = -999;

    void Update()
    {
        if (Input.GetKeyDown(parryKey)) TryParry();
    }

    void TryParry()
    {
        int beat = beatClock.CurrentBeat;
        if (beat - lastParryBeat < beatsBetweenParries) return;
        lastParryBeat = beat;

        bool onBeat = beatClock.DistanceToNearestBeat() <= beatWindow;
        int seconds = onBeat ? onBeatSeconds : normalSeconds;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Bullet")) continue;

            Bullet bullet = hit.GetComponent<Bullet>();
            if (bullet == null || bullet.IsReflected) continue;

            // La nota que necesitas NO se devuelve
            if (!score.Finished && bullet.Note == score.Expected) continue;

            Vector2 dir = boss.position - bullet.transform.position;
            bullet.Reflect(dir, returnSpeed, seconds); // ReflectedDamage = segundos
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
