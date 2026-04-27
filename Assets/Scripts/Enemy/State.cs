using UnityEngine;

public abstract class State 
{
    public Rigidbody2D rb;
    protected State(Enemy enemy)
    {
        rb = enemy.RB;
    }
    public virtual void Update() {}
    public virtual void Enter() {}
    public virtual void FixedUpdate(){}
    public virtual void Exit() {}
}
 