using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class Player_Combat : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;
    private PlayerController playerController;

    [SerializeField] private float plungeShockWaveRadius = 2.5f;
    [SerializeField] private int plungeDamage = 5;
    [SerializeField] private float plungeVelocity = 25f;
    [SerializeField] private bool isPlunging = false;
    [SerializeField] private int currentAttackPhase = 0;
    [SerializeField] private float timeSinceLastStroke = 0f;
    [SerializeField] private float maxComboDelay = 0.8f;

    [SerializeField] private float lungeForce = 0f;
    [SerializeField] private float lungeDuration = 0.15f;

    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Transform groundCheck;
    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
    }


    void Update()
    {
        if (isPlunging)
        {
            return;
        }
        if (currentAttackPhase != 0)
        {
            timeSinceLastStroke += Time.deltaTime;
            if (timeSinceLastStroke > maxComboDelay)
            {
                ResetCombo();
            }
        }
        if (!playerController.isGrounded && Input.GetAxisRaw("Vertical") < 0f && Input.GetKeyDown(KeyCode.J))
        {
            ResetCombo();
            StartCoroutine(ExecutePlunge());
        }
        else if (Input.GetKeyDown(KeyCode.J))
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
    public void TriggerShockWaveHitBox()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(groundCheck.position, plungeShockWaveRadius, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            Boss_stats healthscript = enemy.GetComponent<Boss_stats>();
            if (healthscript != null)
            {
                healthscript.TakeDamage(plungeDamage);
                Debug.Log("<color=red>You just did a horridly painful PLUNGE ATTACK on the cutest panda ever!!");
            }
        }
    }
    private IEnumerator ExecutePlunge()
    {
        isPlunging = true;
        playerController.isAttacking = true;
        anim.SetBool("IsPlunging", true);
        Debug.Log("Setting is isPlunging parameter to true.");
        rb.linearVelocity = new Vector2(0f, -plungeVelocity);
        yield return new WaitUntil(() => playerController.isGrounded);
        Debug.Log("Dawg");
        // anim.SetBool("IsPlunging", false);
        anim.SetBool("PlungeImpact", true);
        Debug.Log("Plunge Impact bool true");
        rb.linearVelocity = Vector2.zero;
        TriggerShockWaveHitBox();
    }

    public void FinishPlungeState()
    {
        isPlunging = false;
        playerController.isAttacking = false;
        anim.SetBool("IsPlunging", false);
        anim.SetBool("PlungeImpact", false);
    }
    public void TriggerMeleeHitbox()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            Boss_stats healthScript = enemy.GetComponent<Boss_stats>();

            if (healthScript != null)
            {
                // int damage = 0;
                // if (currentAttackPhase == 1)
                // {
                //     damage = 10;

                // }
                // else if (currentAttackPhase == 2)
                // {
                //     damage = 15;
                // }
                // else if (currentAttackPhase == 3)
                // {
                //     damage = 20;
                // }
                healthScript.TakeDamage(3);
            }
        }
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


