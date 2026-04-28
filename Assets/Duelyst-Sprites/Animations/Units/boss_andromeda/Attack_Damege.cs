using UnityEngine;

public class Attack_Damege : MonoBehaviour
{
    public float damage = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Triggered with: " + other.name);
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player hit!");
            other.GetComponent<PlayerController>()?.TakeDamage(damage);
        }
    }
}
