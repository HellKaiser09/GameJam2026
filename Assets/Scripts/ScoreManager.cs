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

    readonly List<NoteType> bossScore = new List<NoteType>();
    int progress;
    int round;
    bool finished;

    public IReadOnlyList<NoteType> BossScore => bossScore;
    public int Progress => progress;
    public int Round => round;
    public int TotalRounds => notesPerRound.Length;
    public int PoolSize => notesPerRound[round];
    public bool Finished => finished;
    public NoteType Expected => opposites[(int)bossScore[progress]];
    public NoteType OppositeOf(NoteType n) { return opposites[(int)n]; }

    public event Action OnProgressChanged;
    public event Action OnRoundStarted;
    public event Action OnMistake;
    public event Action OnCompleted;          // al terminar una ronda
    public event Action OnAllRoundsCompleted; // al terminar la última

    void Awake() { StartRound(0); }
    void OnEnable() { PlayerHitbox.OnNoteAbsorbed += HandleNote; }
    void OnDisable() { PlayerHitbox.OnNoteAbsorbed -= HandleNote; }

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
            Debug.Log("Nota equivocada: " + note);
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