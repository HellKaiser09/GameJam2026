using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    public int power;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Bullet"))
        {
            power++;
            Debug.Log("Poder: " + power);
            Destroy(other.gameObject);
        }
    }
}