using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum BossAction
{
    None,
    MeleeBurst,
    OrbitActivate,
    OrbitThrow,
    TeleAoE,
    Chase,      // move toward player
    Retreat,    // move away from player
}

[System.Serializable]
public class AttackSlot
{
    public string id;
    public BossAction action;
    public float cooldown;
    public float timer;
    public float baseDamageValue;   // heuristic value, not actual damege. Actual damege is saved in effect prefab that has the hitbox collider 
    public float idealRange;        // sweetspot for RangeBonus scoring
    public BossPhase[] allowedPhases;
    public bool isOnCooldown => timer < cooldown;

    public void Tick(float dt) { timer += dt; }
    public void Reset() { timer = 0f; }
}

[System.Serializable]
public class PhaseConfig
{
    public BossPhase phase; // different phase stats. Will change dynamically when hp threshold is hit. default values for Normal phase
    public float meleeCooldown = 3f;
    public float orbitCooldown = 16f;
    public float aoeCooldown = 1.75f;
    public float globalCooldown = 0.5f;
    public float pressureBonus = 1.0f;
    public float moveSpeed = 6f;
}

public class Bossai : MonoBehaviour // changing the goofy looking class name breaks the code
{
    [Header("References")]
    public Boss_stats bossStats;
    public Animator anim;
    public Transform attackPoint;
    public OrbManager orbManager;
    public PlayerController playerControl;
    public Rigidbody2D rb;

    [Header("Prefabs")]
    public GameObject meleeBurstPrefab;
    public GameObject aoeZonePrefab;

    [Header("Range Thresholds")]
    
    public float meleeMaxRange = 9f; // Boss prefers melee below this distance
    public float orbitIdealRange = 12f; // Boss prefers orb attack around this distance
    public float retreatThreshold = 3f; // Boss retreats if player is closer than this

    private AttackSlot slotMelee;
    private AttackSlot slotOrbit;
    private AttackSlot slotOrbitThrow;  // might have to remove
    private AttackSlot slotAoE;

    private AttackSlot slotChase;
    private AttackSlot slotRetreat;

    private Dictionary<BossPhase, PhaseConfig> phaseTable; // phase stats. Normal, Aggressive, Rage. I realize now that the third one should've been God of War

    private Transform player; 
    private float globalCooldownTimer = 999f; // uhh this is random. boss will be ready to attack as soon as game starts
    private AttackSlot lastUsedSlot = null; // for weight 
    private bool phaseTransitioning = false; 
    private bool isMoving = false;

    // Melee windup
    private bool meleePending = false; 
    private float meleeDelayTimer = 0f;
    private const float MeleeDelay = 0.7f;

    private Queue<BossAction> intentQueue = new Queue<BossAction>(); // Intent queue — lets combo logic inject the NEXT action without bypassing the CSP/Local Search on subsequent frames

    private bool orbRushPending = false;   // boss rush after orb activate

    void Start()
    {
        Debug.Log("START");
        anim = GetComponent<Animator>();
        orbManager = GetComponent<OrbManager>();
        rb = GetComponent<Rigidbody2D>();

        GameObject p = GameObject.FindGameObjectWithTag("Player"); // for player location
        if (p != null) player = p.transform;

        BuildSlots();
        BuildPhaseTable();
        ApplyPhaseConfig(bossStats.currentPhase);
    }

    void BuildSlots()
    {
        slotMelee = new AttackSlot
        {
            id = "MeleeBurst",
            action = BossAction.MeleeBurst,
            idealRange = 5f,
            baseDamageValue = 25f, // might have to make this lower... she spams it too much. Initial value: 35
            allowedPhases = new[] { BossPhase.Normal, BossPhase.Aggressive, BossPhase.Rage }
        };

        slotOrbit = new AttackSlot
        {
            id = "OrbitActivate",
            action = BossAction.OrbitActivate,
            idealRange = orbitIdealRange,
            baseDamageValue = 40f,
            allowedPhases = new[] { BossPhase.Normal, BossPhase.Aggressive, BossPhase.Rage }
        };

        slotOrbitThrow = new AttackSlot
        {
            id = "OrbitThrow",
            action = BossAction.OrbitThrow,
            idealRange = 8f,        // throw from close range for accuracy
            baseDamageValue = 55f,
            allowedPhases = new[] { BossPhase.Aggressive, BossPhase.Rage }   // Rage + Aggressive only
        };

        slotAoE = new AttackSlot
        {
            id = "TeleAoE",
            action = BossAction.TeleAoE,
            idealRange = 35f,       // prefers long range but fires anywhere
            baseDamageValue = 22f,
            allowedPhases = new[] { BossPhase.Normal, BossPhase.Aggressive, BossPhase.Rage }
        };

        // very short "cooldowns, but 0 heuristic value so it doesnt become a cat and mouse game.
        slotChase = new AttackSlot
        {
            id = "Chase",
            action = BossAction.Chase,
            idealRange = 6f,  
            baseDamageValue = 0f,        //score comes purely from range bonus
            allowedPhases = new[] { BossPhase.Normal, BossPhase.Aggressive, BossPhase.Rage }
        };
        slotChase.timer = 999f; // start ready

        slotRetreat = new AttackSlot
        {
            id = "Retreat",
            action = BossAction.Retreat,
            idealRange = retreatThreshold,
            baseDamageValue = 0f,
            allowedPhases = new[] { BossPhase.Normal, BossPhase.Aggressive, BossPhase.Rage }
        };
        slotRetreat.timer = 999f;
    }

    void BuildPhaseTable()
    {
        Debug.Log("Phase table");
        phaseTable = new Dictionary<BossPhase, PhaseConfig>
        {
            [BossPhase.Normal] = new PhaseConfig
            {
                phase = BossPhase.Normal,
                meleeCooldown = 3f,
                orbitCooldown = 16f,
                aoeCooldown = 1.75f,
                globalCooldown = 0.5f,
                pressureBonus = 1.0f,
                moveSpeed = 4.5f
            },
            [BossPhase.Aggressive] = new PhaseConfig
            {
                phase = BossPhase.Aggressive,
                meleeCooldown = 2.3f,
                orbitCooldown = 13f,
                aoeCooldown = 1.0f,
                globalCooldown = 0.35f,
                pressureBonus = 1.3f,
                moveSpeed = 6f
            },
            [BossPhase.Rage] = new PhaseConfig
            {
                phase = BossPhase.Rage,
                meleeCooldown = 1.6f,
                orbitCooldown = 13f,
                aoeCooldown = 1f,
                globalCooldown = 0.25f,
                pressureBonus = 1.6f,
                moveSpeed = 6.5f // Increasing anymore makes it weird 
            }
        };
    }

    void ApplyPhaseConfig(BossPhase phase)
    {
        var cfg = phaseTable[phase];
        slotMelee.cooldown = cfg.meleeCooldown;
        slotOrbit.cooldown = cfg.orbitCooldown;
        slotOrbitThrow.cooldown = cfg.orbitCooldown + 3f; // throw is rarer than activate
        slotAoE.cooldown = cfg.aoeCooldown;
        slotChase.cooldown = 0.1f; // removing these makes it direction flip spam very wonky. she spins like a coin
        slotRetreat.cooldown = 0.1f;
    }

    void Update()
    {
        if (bossStats.queenisDead)
        {
            
            return;
        }
        if (player == null)
        {
            Debug.Log("player is null");
            return;
        } 

        float dt = Time.deltaTime;
        float distance = Vector2.Distance(transform.position, player.position);

        // advance cd
        TickAll(dt);
        globalCooldownTimer += dt;

        // this is so blash matches animation frames
        if (meleePending)
        {
            StopMoving();
            meleeDelayTimer -= dt;
            if (meleeDelayTimer <= 0f)
            {
                FireMeleeBurst();
                meleePending = false;
            }
            return;
        }

        // boss is chasing toward player after orb activate
        if (orbRushPending)
        {
            HandleOrbRush(distance);
            return;
        }

        //doesn't block movement. need to check this to change phaseconfig
        CheckPhaseTransition();

        // Process intent queue
        if (intentQueue.Count > 0 && globalCooldownTimer >= phaseTable[bossStats.currentPhase].globalCooldown)
        {
            BossAction intent = intentQueue.Dequeue();
            ExecuteAction(intent, distance);
            return;
        }

        //CSP + Local Search. Logic needs improvement
        if (globalCooldownTimer >= phaseTable[bossStats.currentPhase].globalCooldown)
        {
            var legal = GetLegalMoves(distance);
            if (legal.Count > 0)
            {
                AttackSlot chosen = LocalSearchBest(legal, distance);
                ExecuteAction(chosen.action, distance);
            }
        }

        //Passive facing. always face the player
        FacePlayer();
    }

    void TickAll(float dt)
    {
        slotMelee.Tick(dt);
        slotOrbit.Tick(dt);
        slotOrbitThrow.Tick(dt);
        slotAoE.Tick(dt);
        slotChase.Tick(dt);
        slotRetreat.Tick(dt);
    }

List<AttackSlot> GetLegalMoves(float distance)
    {
        var legal = new List<AttackSlot>();

        // All attack + move slots to evaluate
        var allSlots = new[] { slotMelee, slotOrbit, slotOrbitThrow, slotAoE, slotChase, slotRetreat };

        foreach (var slot in allSlots)
        {
            // Constraint 1: Cooldown
            if (slot.isOnCooldown) continue;

            // Constraint 2: Phase gate
            bool phaseOK = System.Array.Exists(slot.allowedPhases, p => p == bossStats.currentPhase);
            if (!phaseOK) continue;

            // Constraint 3: Attack-specific hard gates
            if (slot.action == BossAction.MeleeBurst && distance > meleeMaxRange) continue;
            //if (slot.action == BossAction.OrbitThrow && !orbManager.OrbsAreActive()) continue;
            if (slot.action == BossAction.Retreat && distance > retreatThreshold) continue;
            if (slot.action == BossAction.Chase && distance <= meleeMaxRange) continue;

            // Phase transition — block attacks but allow movement
            if (phaseTransitioning)
            {
                if (slot.action != BossAction.Chase && slot.action != BossAction.Retreat) continue;
            }

            legal.Add(slot);
        }

        return legal;
    }

    AttackSlot LocalSearchBest(List<AttackSlot> legal, float distance)
    {
        float bestScore = float.MinValue;
        AttackSlot bestSlot = legal[0];
        float pressure = phaseTable[bossStats.currentPhase].pressureBonus;

        foreach (var slot in legal)
        {
            float rangeDelta = Mathf.Abs(distance - slot.idealRange); 
            float rangeBonus = Mathf.Max(0f, 25f - rangeDelta * 1.8f); // highest at ideal, otherwise linearly decreases based on gap

            // kills Recency bias
            float recency = (lastUsedSlot == slot) ? 25f : 0f;

            // Movement slots get a situational bonus:
            //   Chase scores higher when far away
            //   Retreat scores higher when too close
            float situational = 0f;
            if (slot.action == BossAction.Chase)
                situational = Mathf.Clamp((distance - meleeMaxRange) * 1.5f, 0f, 20f);
            if (slot.action == BossAction.Retreat)
                situational = Mathf.Clamp((retreatThreshold - distance) * 3f, 0f, 10f);

            // makes the boss more aggressive
            float actionBonus = (slot.action == BossAction.Chase || slot.action == BossAction.Retreat)
                                 ? 0f : 15f;

            float jitter = Random.Range(0f, 11f);

            float score = (slot.baseDamageValue + rangeBonus + situational + actionBonus)
                          * pressure
                          - recency
                          + jitter;

            if (score > bestScore)
            {
                bestScore = score;
                bestSlot = slot;
            }
        }

        return bestSlot;
    }
    void ExecuteAction(BossAction action, float distance)
    {
        // Determine which slot was chosen (for timer reset + recency)
        AttackSlot slot = ActionToSlot(action);
        if (slot != null)
        {
            slot.Reset();
            lastUsedSlot = slot;
        }

        globalCooldownTimer = 0f;

        switch (action)
        {
            case BossAction.MeleeBurst: BeginMelee(); break;
            case BossAction.OrbitActivate: BeginOrbitActivate(); break;
            case BossAction.OrbitThrow: BeginOrbitThrow(); break;
            case BossAction.TeleAoE: BeginTeleAoE(); break;
            case BossAction.Chase: BeginChase(); break;
            case BossAction.Retreat: BeginRetreat(); break;
        }
    }

    AttackSlot ActionToSlot(BossAction a)
    {
        return a switch
        {
            BossAction.MeleeBurst => slotMelee,
            BossAction.OrbitActivate => slotOrbit,
            BossAction.OrbitThrow => slotOrbitThrow,
            BossAction.TeleAoE => slotAoE,
            BossAction.Chase => slotChase,
            BossAction.Retreat => slotRetreat,
            _ => null
        };
    }

    // A1 — Melee Burst
    void BeginMelee()
    {
        StopMoving();
        anim.SetTrigger("Attack");
        meleePending = true;
        meleeDelayTimer = MeleeDelay;
    }

    void FireMeleeBurst()
    {
        Instantiate(meleeBurstPrefab, attackPoint.position, attackPoint.rotation);
    }

    // A2 — Orbit Activate
    // After activating orbs, injects a Chase into the intent queue
    // so the boss rushes the player while orbs surround her.
    // In Rage, also queues an OrbitThrow once she closes in.
    void BeginOrbitActivate()
    {
        anim.SetTrigger("Idle");
        orbManager.Activate();

        // Trigger the rush combo
        orbRushPending = true;
    }

    // A2b — Orbit Throw (Rage only)
    void BeginOrbitThrow()
    {
        StopMoving();
        anim.SetTrigger("Idle");
     //   orbManager.ThrowAll(player.position);
    }

    // A3 — Tele-AoE  (any range)
    void BeginTeleAoE()
    {
        anim.SetTrigger("Tele_AoE");

        Vector3 targetPos = player.position;
        Instantiate(aoeZonePrefab, targetPos, Quaternion.identity);

        if (bossStats.currentPhase == BossPhase.Rage)
        {
            bool facingRight = playerControl.isfacingRight;
            float step1 = facingRight ? 2.5f : -2.5f;
            float step2 = facingRight ? 5.0f : -5.0f;

            Instantiate(aoeZonePrefab, targetPos + new Vector3(step1, 0f, 0f), Quaternion.identity);
            Instantiate(aoeZonePrefab, targetPos + new Vector3(step2, 0f, 0f), Quaternion.identity);
        }
    }

    void BeginChase()
    {
        isMoving = true;
        anim.SetBool("Run", true);
        // Actual velocity applied in FixedUpdate
    }

    void BeginRetreat()
    {
        isMoving = true;
        anim.SetBool("Run", true);
    }

    void StopMoving()
    {
        isMoving = false;
        anim.SetTrigger("StopRun");
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    void FixedUpdate()
    {
        if (player == null || !isMoving || rb == null) return;

        float speed = phaseTable[bossStats.currentPhase].moveSpeed;
        float distance = Vector2.Distance(transform.position, player.position);
        float dir = (player.position.x > transform.position.x) ? 1f : -1f;

        if (lastUsedSlot?.action == BossAction.Retreat)
            dir *= -1f; // move away

        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);

        // Auto-stop movement when reaching desired range
        bool chasing = lastUsedSlot?.action == BossAction.Chase;
        bool retreating = lastUsedSlot?.action == BossAction.Retreat;

        if (chasing && distance <= meleeMaxRange * 0.85f) StopMoving();
        if (retreating && distance >= retreatThreshold * 1.5f) StopMoving();
    }

    // Orbit rush — boss chases player with active orbs, then optionally throws
    void HandleOrbRush(float distance)
    {
        float speed = phaseTable[bossStats.currentPhase].moveSpeed * 1.2f; // slightly faster rush
        float dir = (player.position.x > transform.position.x) ? 1f : -1f;
        anim.SetBool("Run", true);

        if (rb != null)
            rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);

        FacePlayer();

        // Arrived inside melee range
        if (distance <= meleeMaxRange * 0.9f)
        {
            StopMoving();
            orbRushPending = false;

        }
    }

    IEnumerator DelayedOrbThrow(float delay)
    {
        yield return new WaitForSeconds(delay);
        anim.SetTrigger("Idle");
    //    orbManager.ThrowAll(player.position);
        slotOrbitThrow.Reset();
    }

    void CheckPhaseTransition()
    {
        if (phaseTransitioning) return;

        BossPhase newPhase = bossStats.currentPhase;
        float hp = bossStats.currentHP;

        if (hp <= 30f && bossStats.currentPhase != BossPhase.Rage) newPhase = BossPhase.Rage;
        else if (hp <= 65f && bossStats.currentPhase == BossPhase.Normal) newPhase = BossPhase.Aggressive;

        if (newPhase != bossStats.currentPhase)
            StartPhaseTransition(newPhase);
    }

    void StartPhaseTransition(BossPhase newPhase)
    {
        // phaseTransitioning = true;
        bossStats.currentPhase = newPhase;
        ApplyPhaseConfig(newPhase);
        intentQueue.Clear();        // cancel any queued combo on phase shift
        orbRushPending = false;

        // anim.SetTrigger("PhaseShift"); No animation for phase change unfortunately
    }

    public void OnPhaseTransitionComplete()
    {
       phaseTransitioning = false;
       CancelInvoke(nameof(OnPhaseTransitionComplete));
    }

    void FacePlayer()
    {
        if (player == null) return;
        float dir = player.position.x - transform.position.x;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (dir < 0 ? -1f : 1f);
        transform.localScale = s;
    }
}