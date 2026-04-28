using UnityEngine;

public class Player_stats : MonoBehaviour
{
    public float Health = 100f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TakeDamage(float damage)
    {
        Health -= damage;
        Debug.Log("Damege Taken. Current Health : " + Health);
        if (Health <= 0f)
        {
            // Handle Death
        }
    }
}
