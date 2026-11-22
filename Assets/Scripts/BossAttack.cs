using UnityEngine;

public class BossAttack : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] BeatClock beatClock;
    [SerializeField] ScoreManager score;
    [SerializeField] FightManager fight;
    [SerializeField] Bullet bulletPrefab;
    [SerializeField] Transform player;

    [Header("Notas")]
    [SerializeField, Range(0f, 1f)] float expectedChance = 0.3f;

    [Header("Por ronda (1, 2, 3)")]
    [SerializeField] int[] beatsPerCircle = { 2, 2, 1 };
    [SerializeField] float[] circleSpeed = { 3f, 3.5f, 4f };
    [SerializeField] float[] burstSpeed = { 5f, 6f, 7f };

    [Header("Circulo y rafaga")]
    [SerializeField] int circleCount = 12;
    [SerializeField] float burstSpread = 10f;

    void OnEnable() { beatClock.OnBeat += HandleBeat; }
    void OnDisable() { beatClock.OnBeat -= HandleBeat; }

    int R(int[] a) { return a[Mathf.Min(score.Round, a.Length - 1)]; }
    float R(float[] a) { return a[Mathf.Min(score.Round, a.Length - 1)]; }

    void HandleBeat(int beat)
    {
        if (score.Finished || fight.Over) return;

        if (beat % R(beatsPerCircle) == 0) FireCircle();
        if (beat % 4 == 3) FireBurst();
    }

    NoteType PickNote()
    {
        return Random.value < expectedChance
            ? score.Expected
            : (NoteType)Random.Range(0, score.PoolSize);
    }

    void FireCircle()
    {
        for (int i = 0; i < circleCount; i++)
        {
            float angle = i * 360f / circleCount * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Spawn(dir, R(circleSpeed), PickNote());
        }
    }

    void FireBurst()
    {
        NoteType note = PickNote();
        Vector2 toPlayer = (player.position - transform.position).normalized;
        for (int i = -1; i <= 1; i++)
            Spawn(Quaternion.Euler(0, 0, i * burstSpread) * toPlayer, R(burstSpeed), note);
    }

    void Spawn(Vector2 dir, float speed, NoteType note)
    {
        Bullet b = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        b.Launch(dir, speed, note);
    }
}
