using System;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("Partitura del jefe")]
    [SerializeField] List<NoteType> bossScore =
        new List<NoteType> { NoteType.Do, NoteType.Mi, NoteType.Re, NoteType.Fa };

    [Header("Opuesto de cada nota (en orden: Do, Re, Mi, Fa)")]
    [SerializeField] NoteType[] opposites =
        { NoteType.Fa, NoteType.Mi, NoteType.Re, NoteType.Do };

    [Header("Al equivocarte")]
    [SerializeField] bool resetOnMistake = true;

    int progress;

        public IReadOnlyList<NoteType> BossScore => bossScore;
    public int Progress => progress;

    public NoteType OppositeOf(NoteType n) { return opposites[(int)n]; }

    public event Action OnProgressChanged;
    public event Action OnMistake;
    public event Action OnCompleted;

    void OnEnable() { PlayerHitbox.OnNoteAbsorbed += HandleNote; }
    void OnDisable() { PlayerHitbox.OnNoteAbsorbed -= HandleNote; }

    void HandleNote(NoteType note)
    {
        NoteType expected = opposites[(int)bossScore[progress]];

        if (note == expected)
        {
            progress++;
            Debug.Log("Bien: " + note + " (" + progress + "/" + bossScore.Count + ")");
            OnProgressChanged?.Invoke();

            if (progress >= bossScore.Count)
            {
                Debug.Log("¡Partitura completada!");
                progress = 0;
                OnCompleted?.Invoke();
                OnProgressChanged?.Invoke();
            }
        }
        else
        {
            Debug.Log("Nota equivocada: " + note + " (esperaba " + expected + ")");
            OnMistake?.Invoke();
            if (resetOnMistake)
            {
                progress = 0;
                OnProgressChanged?.Invoke();
            }
        }
    }
}