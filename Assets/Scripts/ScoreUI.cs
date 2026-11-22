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
        manager.OnRoundStarted += Rebuild;
        manager.OnProgressChanged += Refresh;
        Rebuild();
    }

    void OnDestroy()
    {
        if (manager == null) return;
        manager.OnRoundStarted -= Rebuild;
        manager.OnProgressChanged -= Refresh;
    }

    void Rebuild()
    {
        Clear(bossRow);
        Clear(playerRow);
        playerSlots.Clear();

        foreach (NoteType note in manager.BossScore)
        {
            Image boss = Instantiate(slotPrefab, bossRow);
            boss.color = Bullet.GetColor(note);

            Image mine = Instantiate(slotPrefab, playerRow);
            mine.color = empty;
            playerSlots.Add(mine);
        }
        Refresh();
    }

    void Clear(Transform row)
    {
        foreach (Transform child in row)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
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