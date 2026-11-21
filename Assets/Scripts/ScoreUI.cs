using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] ScoreManager manager;
    [SerializeField] Transform bossRow;
    [SerializeField] Transform playerRow;
    [SerializeField] Image slotPrefab;

    readonly List<Image> playerSlots = new List<Image>();
    static readonly Color empty = new Color(0.3f, 0.3f, 0.3f);

    void Start()
    {
        foreach (NoteType note in manager.BossScore)
        {
            Image boss = Instantiate(slotPrefab, bossRow);
            boss.color = Bullet.GetColor(note);

            Image mine = Instantiate(slotPrefab, playerRow);
            mine.color = empty;
            playerSlots.Add(mine);
        }

        manager.OnProgressChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (manager != null) manager.OnProgressChanged -= Refresh;
    }

    void Refresh()
    {
        for (int i = 0; i < playerSlots.Count; i++)
        {
            playerSlots[i].color = i < manager.Progress
                ? Bullet.GetColor(manager.OppositeOf(manager.BossScore[i]))
                : empty;
        }
    }
}