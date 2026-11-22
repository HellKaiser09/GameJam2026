using UnityEngine;

// Ponlo en BossVisual. La nave mira al frente y solo gira:
//  - cuando dispara el anillo (golpe fuerte de la musica)
//  - mientras entra en la intro
// Reemplaza al Animator (se apaga solo).
[RequireComponent(typeof(SpriteRenderer))]
public class BossSpin : MonoBehaviour
{
    [SerializeField, Tooltip("nave, nave2, nave3, nave4 (frente -> lado -> atras -> lado)")]
    Sprite[] frames;
    [SerializeField, Tooltip("Segundos por cuadro al girar")] float frameTime = 0.05f;

    // RetroIntro lo prende mientras el jefe entra
    public bool Continuous { get; set; }

    SpriteRenderer sr;
    int framesLeft, frame;
    float timer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;
        if (frames != null && frames.Length > 0) sr.sprite = frames[0];
    }

    // Da 'turns' vueltas completas y termina mirando al frente
    public void Spin(int turns = 1)
    {
        if (frames == null || frames.Length == 0) return;
        framesLeft = Mathf.Max(framesLeft, turns * frames.Length);
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        if (!Continuous && framesLeft <= 0) { frame = 0; sr.sprite = frames[0]; return; }

        timer += Time.unscaledDeltaTime;
        while (timer >= frameTime)
        {
            timer -= frameTime;
            frame = (frame + 1) % frames.Length;
            if (!Continuous) framesLeft--;
            // Al acabar la intro, termina la vuelta hasta quedar de frente
            if (!Continuous && framesLeft <= 0 && frame != 0) framesLeft = frames.Length - frame;
        }
        sr.sprite = frames[frame];
    }
}
