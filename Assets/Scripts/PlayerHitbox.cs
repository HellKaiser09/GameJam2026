using System;
using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    public static event Action<NoteType> OnNoteAbsorbed;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Bullet")) return;

        Bullet bullet = other.GetComponent<Bullet>();
        if (bullet != null && bullet.IsReflected) return; // NUEVO: ignora balas devueltas

        if (bullet != null)
        {
            Debug.Log("Nota absorbida: " + bullet.Note);
            OnNoteAbsorbed?.Invoke(bullet.Note);
        }
        Destroy(other.gameObject);
    }
}
