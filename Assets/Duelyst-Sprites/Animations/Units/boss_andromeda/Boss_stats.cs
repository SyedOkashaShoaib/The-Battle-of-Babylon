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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
