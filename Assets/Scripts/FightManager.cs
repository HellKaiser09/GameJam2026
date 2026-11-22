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

    void OnEnable()
    {
        score.OnMistake += LoseLife;
        score.OnRoundStarted += ResetTimer;
        score.OnAllRoundsCompleted += Win;
    }

    void OnDisable()
    {
        score.OnMistake -= LoseLife;
        score.OnRoundStarted -= ResetTimer;
        score.OnAllRoundsCompleted -= Win;
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
        if (Over) return;

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
