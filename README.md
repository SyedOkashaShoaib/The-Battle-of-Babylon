# The Battle of Babylon

Welcome to the repository for **The Battle of Babylon**, a 2D action project built in Unity.
Currently, the game is in a premature, foundational state. We are actively laying down the core architecture, state machines, and combat mechanics.

---

## Our Ultimate Goal

We aren't just building a standard hack-and-slash. The final goal of *The Battle of Babylon* is to engineer a highly advanced, **adaptive Boss AI**.

Instead of relying on static, predictable attack loops, the endgame boss is being designed to **learn the player's fighting patterns**. If you spam the same dodge, rely too heavily on plunge attacks, or favor a specific corner of the arena, the boss will recognize your habits, adapt its strategy, and punish your predictability.

---

## The Boss AI

The current boss AI is already well beyond a simple state machine. It uses a multi-layered decision framework built from scratch in C#.

### Action Space

The boss chooses from a discrete set of actions each frame:

| Action | Description |
|---|---|
| `MeleeBurst` | Close-range burst attack with a windup delay synced to animation frames |
| `OrbitActivate` | Summons orbiting projectiles and immediately rushes the player |
| `OrbitThrow` | Throws all active orbs at the player — **Aggressive/Rage only** |
| `TeleAoE` | Teleports an AoE zone onto the player's position; in Rage, fires 3 in a spread matching the player's facing direction |
| `Chase` | Moves toward the player when out of attack range |
| `Retreat` | Backs off when the player is dangerously close |

---

### Constraint Satisfaction (Legal Move Filtering)

Before scoring any action, the AI filters the full action space down to only **legal moves** using hard constraints:

- **Cooldown gate** — each action has its own cooldown timer (`AttackSlot.timer`), which must be satisfied before the action is eligible
- **Phase gate** — actions are whitelisted per phase (`allowedPhases[]`); e.g. `OrbitThrow` is locked until `Aggressive` or `Rage`
- **Range gates** — `MeleeBurst` is blocked beyond `meleeMaxRange`; `Retreat` is blocked unless the player is dangerously close; `Chase` is blocked if already within melee range
- **Phase transition lock** — during a phase transition, only movement actions (`Chase`, `Retreat`) are legal, blocking attacks mid-shift
- **Intent queue priority** — if a queued combo action is waiting, it bypasses the scoring step entirely and executes directly

This ensures the boss never wastes a move or fires an attack in a context where it makes no sense.

---

### Local Search Scoring

Once the legal move set is built, the AI scores every candidate using a weighted heuristic and picks the best — a lightweight **Local Search** step:

```
score = (baseDamageValue + rangeBonus + situationalBonus + actionBonus) × pressureMultiplier − recencyPenalty + jitter
```

Breaking each term down:

| Term | What it does |
|---|---|
| `baseDamageValue` | Base threat weight of the action — `OrbitThrow` (55) outweighs `TeleAoE` (22) outweighs `MeleeBurst` (25) |
| `rangeBonus` | Peaks at `idealRange` for each action, falls off linearly with distance — the boss prefers actions at their natural range |
| `situationalBonus` | `Chase` scores higher the farther the player is; `Retreat` scores higher the closer the player is |
| `actionBonus` | Flat +15 to all attack slots over movement, so the boss stays aggressive rather than playing keep-away |
| `pressureMultiplier` | Scales the entire score by phase — `1.0` Normal → `1.3` Aggressive → `1.6` Rage, making the boss progressively more threat-dense |
| `recencyPenalty` | −25 if this was the last action used, killing repetitive spam |
| `jitter` | Small random noise (0–11) to prevent perfectly deterministic, readable patterns |

---

### Phase System

The boss has three phases that escalate automatically as HP drops:

| Phase | HP Threshold | Melee CD | AoE CD | Global CD | Move Speed | Pressure |
|---|---|---|---|---|---|---|
| `Normal` | > 65 HP | 3.0s | 1.75s | 0.50s | 4.5 | ×1.0 |
| `Aggressive` | ≤ 65 HP | 2.3s | 1.00s | 0.35s | 6.0 | ×1.3 |
| `Rage` | ≤ 30 HP | 1.6s | 1.00s | 0.25s | 6.5 | ×1.6 |

On transition, the intent queue is cleared (no mid-combo phase shifts), cooldowns are re-applied via `ApplyPhaseConfig`, and the scoring weights immediately reflect the new pressure multiplier — the boss becomes a different fighter, not just a faster one.

---

### Combo Injection via Intent Queue

The boss can chain attacks together through an **intent queue** — a `Queue<BossAction>` that lets one action inject the *next* action without skipping the legal-move check on subsequent frames.

The current example: after `OrbitActivate`, the boss immediately queues a `Chase`, rushing the player while the orbs orbit around her. In `Rage`, this can chain further into an `OrbitThrow` once she closes in. This produces emergent combo pressure without hardcoded attack sequences.

---

### Melee Windup Sync

`MeleeBurst` doesn't fire on the same frame it's chosen. A `meleePending` flag + `meleeDelayTimer` (0.7s) holds execution until the attack animation reaches the right frame, then calls `FireMeleeBurst()` to instantiate the hitbox. During this windup the boss is locked in place — intentional, readable, and punishable.

---

## Built With

- **Engine:** Unity (Universal Render Pipeline — 2D)
- **Language:** C#
- **Art Style:** Pixel art (Duelyst-inspired sprites)
- **Boss Decision System:** Constraint Satisfaction + Local Search heuristic scoring

---

## Credits

This game is a collaborative effort, but credit needs to go where it's due.

Massive shoutout to my co-developer **[Rocksalot](https://github.com/Rocksalot)** for all of his work on this ongoing project.

---

## 📄 License

Currently unlicensed — all rights reserved by the contributors.
