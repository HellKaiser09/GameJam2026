using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class BossAttack : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] BeatClock beatClock;
    [SerializeField] ScoreManager score;
    [SerializeField] FightManager fight;
    [SerializeField] Bullet bulletPrefab;
    [SerializeField] Transform player;

    [Header("Notas")]
    [SerializeField, Range(0f, 1f)] float neededChance = 0.2f;

    [Header("Por ronda (1, 2, 3)")]
    [SerializeField] int[] ringBullets = { 12, 16, 20 };
    [SerializeField] int[] spiralBullets = { 2, 3, 4 };
    [SerializeField] int[] fanBullets = { 3, 5, 7 };
    [SerializeField] float[] roundSpeed = { 2.6f, 3f, 3.4f };

    [Header("Velocidad segun la musica")]
    [SerializeField] float calmSpeed = 0.9f;
    [SerializeField] float intenseSpeed = 1.25f;
    [SerializeField] float fanExtraSpeed = 1.6f;
    [SerializeField] float fanSpread = 12f;
    [SerializeField] float spiralTurn = 17f;

    [Header("Movimiento del jefe")]
    [SerializeField] float swayAmplitude = 2.5f;
    [SerializeField, Tooltip("Beats que tarda en ir y volver")] float swayBeats = 16f;

    // 0 = anillo (bombo fuerte), 1 = bombo, 2 = caja (abanico)
    public event Action<int> OnShot;

    Vector3 basePos;
    float ringOffset, spiralAngle;
    float swayTime; // empieza a contar al terminar la intro, asi no da un salto

    // Vuelo erratico (entre rondas): no dispara y se mueve como mareado
    float staggerUntil = -1f, recover;
    Vector3 lastErratic;
    BossSpin spin;
    public bool Staggering => Time.time < staggerUntil;

    public void Stagger(float seconds)
    {
        staggerUntil = Time.time + seconds;
        if (spin != null) spin.Continuous = true;
    }

    void OnEnable() { beatClock.OnStep += HandleStep; }
    void OnDisable() { beatClock.OnStep -= HandleStep; }

    void Start()
    {
        basePos = transform.position;
        spin = GetComponentInChildren<BossSpin>();
    }

    int R(int[] a) { return a[Mathf.Clamp(score.Round, 0, a.Length - 1)]; }
    float R(float[] a) { return a[Mathf.Clamp(score.Round, 0, a.Length - 1)]; }
    bool Stopped => score.Finished || fight.Over;

    void Update()
    {
        if (RetroIntro.Playing || Tutorial.Active) return; // en la intro / tutorial el jefe no se mueve solo

        if (Staggering)
        {
            float t = Time.time * 1.8f;
            Vector3 e = basePos + new Vector3((Mathf.PerlinNoise(t, 0.3f) - 0.5f) * 9f,
                                              (Mathf.PerlinNoise(0.7f, t * 1.3f) - 0.5f) * 2.8f, 0f);
            e += (Vector3)(Random.insideUnitCircle * 0.08f);
            transform.position = e;
            lastErratic = e;
            recover = 1f;
            return;
        }
        if (spin != null && recover >= 1f) spin.Continuous = false;
        if (Stopped) return;

        float period = Mathf.Max(0.1f, swayBeats * beatClock.BeatLength);
        Vector3 p = basePos;
        swayTime += Time.deltaTime;
        p.x += Mathf.Sin(swayTime * Mathf.PI * 2f / period) * swayAmplitude;
        // Al terminar el vuelo erratico regresa suave a su vaiven
        recover = Mathf.MoveTowards(recover, 0f, Time.deltaTime * 1.5f);
        transform.position = Vector3.Lerp(p, lastErratic, recover * recover);
    }

    void HandleStep(int step)
    {
        if (Stopped || RetroIntro.Playing || Tutorial.Active || Staggering) return;
        int bar = step / SongChart.StepsPerBar;
        int s = step % SongChart.StepsPerBar;
        int level = SongChart.Level(bar);
        if (level == 0) return; // intro: el jefe espera

        float speed = R(roundSpeed) * (level == 2 ? intenseSpeed : calmSpeed);

        if (SongChart.Snare(bar, s)) { FireFan(speed * fanExtraSpeed); OnShot?.Invoke(2); }

        if (SongChart.Kick(bar, s))
        {
            if (s == 0) { FireRing(speed); OnShot?.Invoke(0); }
            else
            {
                if (level == 2) FireSpiral(speed); else FireAimed(speed * fanExtraSpeed);
                OnShot?.Invoke(1);
            }
        }
    }

    NoteType PickNote()
    {
        return (score.HasExpected && Random.value < neededChance)
            ? score.Expected
            : (NoteType)Random.Range(0, score.PoolSize);
    }

    void FireRing(float speed)
    {
        int n = R(ringBullets);
        for (int i = 0; i < n; i++) Spawn(ringOffset + i * 360f / n, speed, PickNote());
        ringOffset += 360f / n / 2f;
    }

    void FireSpiral(float speed)
    {
        int n = R(spiralBullets);
        for (int i = 0; i < n; i++) Spawn(spiralAngle + i * 360f / n, speed, PickNote());
        spiralAngle += spiralTurn;
    }

    float AngleToPlayer()
    {
        Vector2 to = player.position - transform.position;
        return Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
    }

    void FireAimed(float speed)
    {
        if (player == null) return;
        Spawn(AngleToPlayer(), speed, PickNote());
    }

    void FireFan(float speed)
    {
        if (player == null) return;
        float a = AngleToPlayer();
        int ways = R(fanBullets);
        NoteType note = PickNote(); 
        for (int i = 0; i < ways; i++) Spawn(a + (i - (ways - 1) / 2f) * fanSpread, speed, note);
    }

    void Spawn(float angleDeg, float speed, NoteType note)
    {
        float r = angleDeg * Mathf.Deg2Rad;
        Bullet b = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        b.Launch(new Vector2(Mathf.Cos(r), Mathf.Sin(r)), speed, note);
    }
}
