using UnityEngine;

public class TestBulletSpawner : MonoBehaviour
{
    [SerializeField] Bullet bulletPrefab;
    [SerializeField] ScoreManager score;
    [SerializeField] int count = 12;
    [SerializeField] float interval = 1f;
    [SerializeField] float speed = 3f;
    [SerializeField, Range(0f, 1f)] float expectedChance = 0.3f;

    void Start() { InvokeRepeating(nameof(Fire), 1f, interval); }

    void Fire()
    {
        if (score.Finished) return;

        for (int i = 0; i < count; i++)
        {
            float angle = i * 360f / count * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            NoteType note = Random.value < expectedChance
                ? score.Expected
                : (NoteType)Random.Range(0, score.PoolSize);

            Bullet b = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            b.Launch(dir, speed, note);
        }
    }
}