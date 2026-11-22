using System;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("Notas distintas de la partitura en cada ronda")]
    [SerializeField] int[] notesPerRound = { 4, 6, 8 };

    [Header("Opuesto de cada nota (orden: Do, Re, Mi, Fa, Sol, La, Si, Do2)")]
    [SerializeField] NoteType[] opposites =
    {
        NoteType.Fa, NoteType.Mi, NoteType.Re, NoteType.Do,
        NoteType.La, NoteType.Sol, NoteType.Do2, NoteType.Si
    };

    [Header("Al equivocarte")]
    [SerializeField] bool resetOnMistake = false;
    [SerializeField, Tooltip("Segundos sin perder otra vida despues de un error")] float mistakeGrace = 1.5f;

    readonly List<NoteType> bossScore = new List<NoteType>();
    int progress;
    int round;
    bool finished;
    float graceUntil;
    public bool Invulnerable => Time.time < graceUntil;
    public float MistakeGrace => mistakeGrace;

    public IReadOnlyList<NoteType> BossScore => bossScore;
    public int Progress => progress;
    public int Round => round;
    public int TotalRounds => notesPerRound.Length;
    public int PoolSize => notesPerRound[round];
    public bool Finished => finished;
    // true mientras falte alguna nota en la ronda actual
    public bool HasExpected => !finished && progress < bossScore.Count;
    // Nunca se sale de la lista (aunque la ronda se acabe de completar)
    public NoteType Expected => opposites[(int)bossScore[Mathf.Min(progress, bossScore.Count - 1)]];
    public NoteType OppositeOf(NoteType n) { return opposites[(int)n]; }

    public event Action OnProgressChanged;
    public event Action OnRoundStarted;
    public event Action OnMistake;
    public event Action<NoteType> OnCorrect;  // atrapaste la nota correcta
    public event Action OnCompleted;          // al terminar una ronda
    public event Action OnAllRoundsCompleted; // al terminar la última

    void Awake() { StartRound(0); }
    void OnEnable() { PlayerHitbox.OnNoteAbsorbed += HandleNote; }
    void OnDisable() { PlayerHitbox.OnNoteAbsorbed -= HandleNote; }

    // Empieza todo desde la ronda 1 (al terminar el tutorial)
    public void RestartRounds()
    {
        finished = false;
        graceUntil = 0f;
        StartRound(0);
    }

    void StartRound(int index)
    {
        round = index;
        progress = 0;

        bossScore.Clear();
        for (int i = 0; i < notesPerRound[index]; i++) bossScore.Add((NoteType)i);

        // Mezclar el orden
        for (int i = bossScore.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (bossScore[i], bossScore[j]) = (bossScore[j], bossScore[i]);
        }

        OnRoundStarted?.Invoke();
        OnProgressChanged?.Invoke();
        LogNext();
    }

    void HandleNote(NoteType note)
    {
        if (finished) return;

        if (note == Expected)
        {
            progress++;
            OnCorrect?.Invoke(note);
            OnProgressChanged?.Invoke();

            if (progress >= bossScore.Count)
            {
                Debug.Log("¡Ronda " + (round + 1) + " completada!");
                OnCompleted?.Invoke();

                if (round + 1 >= notesPerRound.Length)
                {
                    finished = true;
                    OnAllRoundsCompleted?.Invoke();
                }
                else StartRound(round + 1);
            }
            else LogNext();
        }
        else
        {
            if (Invulnerable) return; // acabas de perder una vida: no te quita otra
            Debug.Log("Nota equivocada: " + note);
            graceUntil = Time.time + mistakeGrace;
            OnMistake?.Invoke();
            if (resetOnMistake)
            {
                progress = 0;
                OnProgressChanged?.Invoke();
                LogNext();
            }
        }
    }

    // Solo para probar, luego se puede borrar
    void LogNext()
    {
        if (!finished && progress < bossScore.Count)
            Debug.Log("Necesitas: " + Expected);
    }
}