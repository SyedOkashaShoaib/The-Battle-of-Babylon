using UnityEngine;

public class Attack_effect : MonoBehaviour
{
    public GameObject E03_65;
    public Transform AttackPoint;

    public Boss_stats boss;

    public void Attack()
    {
        Debug.Log("Attack started");

        if (E03_65 == null)
        {
            Debug.LogError("E03_65 is NOT assigned!");
        }
        Instantiate(E03_65, AttackPoint.position, AttackPoint.rotation); 
    }
}