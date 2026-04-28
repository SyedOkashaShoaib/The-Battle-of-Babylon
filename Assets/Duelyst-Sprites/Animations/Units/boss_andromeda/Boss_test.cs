using UnityEngine;

public class Boss_test : MonoBehaviour
{
    Animator anim;
    Transform player;
    public Boss_stats boss;
    //public Attack_effect attack_effect;

    public GameObject aoeZonePrefab;
    public GameObject E03_65;    //this is the attack_effect prefab object
    public Transform AttackPoint; // this is the empty child object on boss for the position of attack_effect prefab
    public OrbManager Orbit_mana; // this will actiate the orbit attack

    public float aoeCooldown = 1.75f; // the rate at which explosion is allowet to spawn. Changes based on boss phase
    private float aoeTimer = 0f;

    public float attack_effect_CD = 2f; // The cooldown of aoe that boss does around herself 
    private float attackTimer = 0f;
    private float delayTimer = 0f;          //top introduce slight delay before attack. To match attack animation
    private bool waitingForSpawn = false;

    public float OrbAttackCooldown = 15f;
    private float OrbAttackTimer = 0f;


    public PlayerController playerControl;

    void Start()
    {
        anim = GetComponent<Animator>();
        Orbit_mana = GetComponent<OrbManager>(); // Orbit manager object will handle orbit attack activations

        // Finds player by tag (we’ll set this later)
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    void Update()
    {
        aoeTimer += Time.deltaTime;
        attackTimer += Time.deltaTime;
        OrbAttackTimer += Time.deltaTime;

        if (player == null) return;

        if (boss.currentPhase == BossPhase.Aggressive)
        {
            aoeCooldown = 1f;
        }

        float distance = Vector2.Distance(transform.position, player.position);

       
        if (distance < 8f)
        {
            if(attackTimer > attack_effect_CD && !waitingForSpawn) // start attack animation
            {
                anim.SetTrigger("Attack");
                
                waitingForSpawn = true;
                delayTimer = 0.7f;
                attackTimer = 0f;
            }
        }
        if (waitingForSpawn) // make attack_effect EXXPLOOOOOSION!!
        {
            delayTimer -= Time.deltaTime;

            if (delayTimer <= 0f)
            {
                Instantiate(E03_65, AttackPoint.position, AttackPoint.rotation);
                waitingForSpawn = false;
            }
        }

        if (distance > 20f && distance < 30f)
        {
            if(OrbAttackTimer >= OrbAttackCooldown)
            {
                anim.SetTrigger("Idle");
                Orbit_mana.Activate();
                OrbAttackTimer = 0f;
            }
        }

        else if (distance >= 30)
        {
            anim.SetTrigger("Tele_AoE");
            
            if(aoeTimer >= aoeCooldown)
            {
                Vector3 pos = player.position;
                Instantiate(aoeZonePrefab, pos, Quaternion.identity); // creates a warning explosion animation which in turn calls another animation with damege collider
                if (boss.currentPhase == BossPhase.Rage)
                {
                    Vector3 inFront2 = pos;
                    Vector3 inFront1 = pos;
                    if (playerControl.isfacingRight)
                    {
                        inFront1.x += 5f;
                        inFront2.x += 2.5f;
                    }
                    else
                    {
                        inFront1.x -= 5f;
                        inFront2.x -= 2.5f;
                    }
                    Instantiate(aoeZonePrefab, inFront1, Quaternion.identity);
                    Instantiate(aoeZonePrefab, inFront2, Quaternion.identity);
                }
                aoeTimer = 0f;
            }
        }
    }
}