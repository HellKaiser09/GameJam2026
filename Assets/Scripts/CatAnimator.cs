using UnityEngine;

// Ponlo en PlayerVisual. Cambia los dibujos del gato segun lo que pasa:
//   quieto   -> frente (gatop1...gatop9)
//   moviendo -> volar  (gato1, gato2, gato3)
//   herido   -> morido (gatomuro, gatomuro1, gatomuro2) mientras parpadea
//   muerto   -> gatomuro3 (y luego GameFeedback lo hace caer hacia la luna)
// Reemplaza al Animator (se apaga solo).
[RequireComponent(typeof(SpriteRenderer))]
public class CatAnimator : MonoBehaviour
{
    [SerializeField] Sprite[] idleFrames;     // frente
    [SerializeField] Sprite[] flyFrames;      // volar
    [SerializeField] Sprite[] hurtFrames;     // gatomuro, gatomuro1, gatomuro2
    [SerializeField] Sprite deadSprite;       // gatomuro3

    [Header("Cuadros por segundo")]
    [SerializeField] float idleFps = 8f;
    [SerializeField] float flyFps = 10f;
    [SerializeField] float hurtFps = 10f;

    SpriteRenderer sr;
    Sprite[] current;
    float timer;
    int frame;
    float hurtUntil;
    bool dead;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        Animator a = GetComponent<Animator>();
        if (a != null) a.enabled = false;
    }

    // Lo llama GameFeedback al equivocarte
    public void PlayHurt(float seconds) { hurtUntil = Time.unscaledTime + seconds; }

    // Lo llama GameFeedback al perder
    public void SetDead()
    {
        dead = true;
        if (deadSprite != null) sr.sprite = deadSprite;
    }

    void Update()
    {
        if (dead || PauseMenu.Paused) return;

        Sprite[] want; float fps;
        if (Time.unscaledTime < hurtUntil && Has(hurtFrames)) { want = hurtFrames; fps = hurtFps; }
        else if (PlayerMovement.Moving && Has(flyFrames)) { want = flyFrames; fps = flyFps; }
        else { want = idleFrames; fps = idleFps; }
        if (!Has(want)) return;

        if (want != current) { current = want; frame = 0; timer = 0f; } // cambio de animacion

        timer += Time.unscaledDeltaTime;
        float step = 1f / Mathf.Max(1f, fps);
        while (timer >= step) { timer -= step; frame = (frame + 1) % current.Length; }
        sr.sprite = current[frame];
    }

    static bool Has(Sprite[] s) { return s != null && s.Length > 0; }
}
