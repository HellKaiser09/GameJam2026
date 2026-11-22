using System;
using UnityEngine;

// Vidas del gato (pescados) + barra de desafinacion (tiempo limite)
public class FightManager : MonoBehaviour
{
    [SerializeField] ScoreManager score;
    [SerializeField] int playerLives = 3;
    [Header("Segundos de la barra en cada ronda")]
    [SerializeField] float[] timePerRound = { 30f, 35f, 40f };

    public int Lives { get; private set; }
    public float TimeLeft { get; private set; }
    public float TimeMax { get; private set; }
    public bool Over { get; private set; }
    public bool Won { get; private set; }

    public event Action OnLivesChanged;
    public event Action OnGameOver;
    public event Action OnWin;
    public event Action<float> OnTimeAdded;
    public event Action OnHealed;          // la curacion unica se uso

    [Header("Curacion unica (Irrepetible)")]
    [SerializeField, Tooltip("Al pasar de ronda recuperas 1 pez, pero solo UNA vez en toda la partida")]
    bool oneTimeHeal = true;
    public bool HealUsed { get; private set; }

    // Mientras sea true la barra de desafinacion no corre (pausa entre rondas)
    public bool Paused { get; set; }

    void OnEnable()
    {
        score.OnMistake += LoseLife;
        score.OnRoundStarted += ResetTimer;
        score.OnAllRoundsCompleted += Win;
        score.OnCompleted += TryHeal;
    }

    void OnDisable()
    {
        score.OnMistake -= LoseLife;
        score.OnRoundStarted -= ResetTimer;
        score.OnAllRoundsCompleted -= Win;
        score.OnCompleted -= TryHeal;
    }

    void Start()
    {
        Time.timeScale = 1f;
        Lives = playerLives;
        ResetTimer();
        OnLivesChanged?.Invoke();
    }

    void Update()
    {
        if (Over || Paused || RetroIntro.Playing) return; // la barra no corre durante la intro

        TimeLeft -= Time.deltaTime;
        if (TimeLeft <= 0f)
        {
            Debug.Log("Se acabo el tiempo: pierdes una vida");
            LoseLife();
            TimeLeft = TimeMax;
        }
    }

    void ResetTimer()
    {
        int i = Mathf.Min(score.Round, timePerRound.Length - 1);
        TimeMax = timePerRound[i];
        TimeLeft = TimeMax;
    }

    // Lo llama el jefe cuando le llega una nota devuelta con el parry
    public void AddTime(float seconds)
    {
        if (Over) return;
        TimeLeft = Mathf.Min(TimeMax, TimeLeft + seconds);
        OnTimeAdded?.Invoke(seconds);
        Debug.Log("+" + seconds + " s | tiempo: " + TimeLeft.ToString("0.0"));
    }

    void LoseLife()
    {
        if (Over) return;
        Lives--;
        OnLivesChanged?.Invoke();
        Debug.Log("Vidas: " + Lives);

        if (Lives <= 0)
        {
            Over = true;
            Time.timeScale = 0f;
            Debug.Log("GAME OVER");
            OnGameOver?.Invoke();
        }
    }

    // Si te falta algun pez y no la has usado: +1 pez. Si estas completo, se guarda para despues.
    void TryHeal()
    {
        if (!oneTimeHeal || HealUsed || Over || Lives >= playerLives) return;
        if (score.Round + 1 >= score.TotalRounds) return; // la ultima ronda ya es la victoria
        HealUsed = true;
        Lives++;
        Debug.Log("Curacion unica: +1 pez");
        OnLivesChanged?.Invoke();
        OnHealed?.Invoke();
    }

    void Win()
    {
        if (Over) return;
        Over = true;
        Won = true;
        Time.timeScale = 0f;
        Debug.Log("VICTORIA");
        OnWin?.Invoke();
    }
}
