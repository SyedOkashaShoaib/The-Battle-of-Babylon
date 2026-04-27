using Unity.VisualScripting;
using UnityEngine;

public class Enemy : MonoBehaviour
{
   public Rigidbody2D RB {get; private set; }
   public StateMachine StateMachine {get; private set; }

   private void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
        StateMachine = new StateMachine(); 
    }
    private void Start()
    {
        StateMachine.Initialize(new PatrolState(this));
    }
    private void Update() => StateMachine.CurrentState?.Update();
    private void FxedUpdate() => StateMachine.CurrentState?.FixedUpdate();

    




}
