using UnityEngine;

public class BossAttack : MonoBehaviour
{
    enum State { Phase1, Phase2, Dead }

    const int NoteCount = 4;

    [Header("Referencias")]
    [SerializeField] BeatClock beatClock;
    [SerializeField] BossHealth health;
    [SerializeField] Bullet bulletPrefab;
    [SerializeField] Transform player;

    [Header("Daño")]
    [SerializeField] int baseDamage = 15;

    [Header("Círculo")]
    [SerializeField] int circleCount = 12;
    [SerializeField] float circleSpeed = 3f;
    [SerializeField] int beatsPerCircleP1 = 2;
    [SerializeField] int beatsPerCircleP2 = 1;

    [Header("Ráfaga dirigida")]
    [SerializeField] float burstSpeed = 6f;
    [SerializeField] float burstSpread = 10f;

    State state = State.Phase1;

    void OnEnable()
    {
        beatClock.OnBeat += HandleBeat;
        health.OnPhaseTwo += GoPhaseTwo;
        health.OnDeath += GoDead;
    }

    void OnDisable()
    {
        beatClock.OnBeat -= HandleBeat;
        health.OnPhaseTwo -= GoPhaseTwo;
        health.OnDeath -= GoDead;
    }

    void GoPhaseTwo() { state = State.Phase2; }
    void GoDead() { state = State.Dead; }

    int CurrentDamage => state == State.Phase2 ? baseDamage * 2 : baseDamage;

    void HandleBeat(int beat)
    {
        if (state == State.Dead) return;

        int beatsPerCircle = state == State.Phase2 ? beatsPerCircleP2 : beatsPerCircleP1;

        if (beat % beatsPerCircle == 0) FireCircle();
        if (beat % 4 == 3) FireBurst(beat);
    }

    void FireCircle()
    {
        for (int i = 0; i < circleCount; i++)
        {
            float angle = i * 360f / circleCount * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Spawn(dir, circleSpeed, (NoteType)(i % NoteCount));
        }
    }

    void FireBurst(int beat)
    {
        NoteType note = (NoteType)((beat / 4) % NoteCount);
        Vector2 toPlayer = (player.position - transform.position).normalized;
        for (int i = -1; i <= 1; i++)
            Spawn(Quaternion.Euler(0, 0, i * burstSpread) * toPlayer, burstSpeed, note);
    }

    void Spawn(Vector2 dir, float speed, NoteType note)
    {
        Bullet b = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        b.Launch(dir, speed, note, CurrentDamage);
    }
}
