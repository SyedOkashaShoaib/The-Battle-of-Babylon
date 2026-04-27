using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
public class Player_Combat : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;
    private PlayerController playerController;

    [SerializeField] private int currentAttackPhase = 0;
    [SerializeField] private float timeSinceLastStroke = 0f;
    [SerializeField] private float maxComboDelay = 0.8f;

    [SerializeField] private float lungeForce = 0f;
    [SerializeField] private float lungeDuration = 0.15f;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
    }


    void Update()
    {
        if (currentAttackPhase != 0)
        {
            timeSinceLastStroke += Time.deltaTime;
            if (timeSinceLastStroke > maxComboDelay)
            {
                ResetCombo();
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            timeSinceLastStroke = 0;
            ExecuteAttack();
        }
    }
    private void ExecuteAttack()
    {
        playerController.isAttacking = true;
        currentAttackPhase += 1;
        if (currentAttackPhase > 3)
        {
            currentAttackPhase = 1;

        }
        anim.SetInteger("AttackPhase", currentAttackPhase);
        anim.SetTrigger("Attack");
        StartCoroutine(AttackLunge());
    }

    private IEnumerator AttackLunge()
    {
        rb.linearVelocity = new Vector2(transform.localScale.x * lungeForce, rb.linearVelocity.y);
        yield return new WaitForSeconds(lungeDuration);

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }
    public void FinishAttackState()
    {
        playerController.isAttacking = false;
        anim.SetInteger("AttackPhase", 0);
    }

    public void ResetCombo()
    {
        currentAttackPhase = 0;
        timeSinceLastStroke = 0f;
        anim.SetInteger("AttackPhase", 0);
        playerController.isAttacking = false;
    }
}
    

