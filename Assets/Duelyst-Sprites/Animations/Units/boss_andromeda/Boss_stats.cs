using UnityEngine;

public enum BossPhase
{
    Normal, Aggressive, Rage
}


public class Boss_stats : MonoBehaviour
{
    
    public BossPhase currentPhase = BossPhase.Normal;
    public float movespeed = 10f;
    public float currentHP = 100f;
    public bool queenisDead = false;
    [SerializeField] Animator anim;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
        public void TakeDamage(float damage)
    {
        currentHP -= damage;
        Debug.Log("Damege Taken by boss. Current Health : " + currentHP);
        if (currentHP <= 0f)
        {
            anim.SetTrigger("Ondie");
            queenisDead = true;
            
        }
    }
}
