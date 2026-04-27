using UnityEditor.Callbacks;
using UnityEngine;

public class PatrolState : State
{
    public PatrolState(Enemy enemy) : base(enemy)
    {
        
    }

    public override void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(5, rb.linearVelocity.y);
    }
}
