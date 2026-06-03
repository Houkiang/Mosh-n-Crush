# Combat System Refactor Plan

## Background

This project already supports a playable combat loop:

- player movement and survival
- enemy spawning and waves
- melee and projectile weapons
- upgrades and level-up choices
- score, drops, and pooling

The current architecture is good enough for adding more content of the same type, but it will become increasingly hard to maintain when adding deeper combat systems such as:

- critical hits
- physical / magical / true damage
- magic resistance
- buffs and debuffs
- on-hit effects
- elemental attacks
- more complex enemy attack patterns

The core issue is that damage and combat logic are still distributed across multiple scripts in a direct, hard-coded way.

## Current Architecture Assessment

### What is already good

- ScriptableObject-based data entry points already exist for weapons, enemies, waves, and upgrades.
- Core responsibilities are partially separated into managers and gameplay components.
- Object pooling and event-driven kill handling already exist.
- The project is suitable for iterative refactoring instead of a full rewrite.

### Main limitations

#### 1. Damage input is too simple

Current `IDamageable` only supports:

- `TakeDamage(float amount)`
- `TakeKnockback(...)`

This cannot express:

- who caused the damage
- whether the hit can crit
- damage type
- elemental or status payload
- special rules such as shield absorption, vulnerability, reflection, or immunity

#### 2. Damage calculation is duplicated in targets

Player and enemy both calculate received damage by themselves.

This makes it difficult to add:

- crit multipliers
- magical resistance
- true damage
- resist penetration
- conditional damage bonuses

#### 3. Weapons directly pass final damage numbers

Weapons and projectiles currently compute a number and directly apply it to a target.

That means there is no unified hit context where we can inject:

- critical hit logic
- status application
- source-dependent modifiers
- resist calculations
- hit reactions

#### 4. Stats are still raw fields

Player and enemy stats are stored as separate fields such as:

- strength
- defence
- cooldownReduction
- health

This does not scale well when adding:

- magic power
- magic resistance
- crit rate
- crit damage
- status power
- status resistance

#### 5. Upgrade system will grow into switch-heavy logic

Current stat and weapon upgrades are enum-based. This is fine early on, but it becomes fragile when one upgrade needs multiple effects.

#### 6. Weapon identification is fragile

Weapons are currently matched by `gameObject.name == weaponName`.

This is risky for:

- weapon evolution
- multiple instances of the same weapon type
- renamed prefabs
- variants and branching upgrades

## Refactor Goals

The refactor should achieve the following:

1. Introduce a unified combat resolution flow.
2. Support multiple damage types.
3. Support critical hits and future combat modifiers.
4. Introduce a scalable stat system.
5. Introduce a reusable buff / debuff system.
6. Make upgrades effect-driven instead of branch-driven.
7. Make weapons and enemies easier to extend with new attack styles.

## Refactor Strategy

Do not rewrite everything at once.

Use staged migration:

1. keep current gameplay running
2. add new systems beside old ones
3. migrate one attack path at a time
4. remove legacy interfaces only after the new path is stable

## Phase 1: Unified Damage Model

### Goal

Replace raw `float damage` flow with a structured hit payload.

### New files

- `Assets/Scripts/Combat/DamageType.cs`
- `Assets/Scripts/Combat/DamageContext.cs`
- `Assets/Scripts/Combat/DamageResult.cs`
- `Assets/Scripts/Combat/ICombatant.cs`
- `Assets/Scripts/Combat/CombatResolver.cs`

### Suggested structures

```csharp
public enum DamageType
{
    Physical,
    Magical,
    True
}
```

```csharp
public struct DamageContext
{
    public GameObject Source;
    public GameObject Target;
    public float BaseDamage;
    public DamageType DamageType;
    public bool CanCrit;
    public float CritRate;
    public float CritMultiplier;
    public float KnockbackForce;
    public float KnockbackDuration;
}
```

```csharp
public struct DamageResult
{
    public float FinalDamage;
    public bool IsCritical;
    public bool WasBlocked;
    public bool TargetDied;
}
```

### Required code changes

- keep existing `IDamageable` temporarily
- add a new receive-damage entry point to combat entities
- move final damage calculation into `CombatResolver`
- migrate these attack paths first:
  - `NormalSword`
  - `PenetratingProjectile`
  - `EnemyProjectile`
  - `EnemyContactDamage`

### Migration steps

1. Add `DamageType`, `DamageContext`, `DamageResult`.
2. Add `CombatResolver`.
3. Add a new method such as `ReceiveDamage(DamageContext context)` to player and enemy.
4. Update one weapon path at a time.
5. Verify old gameplay remains stable.
6. Remove direct float-based damage only after all attacks are migrated.

### Definition of done

- all main attacks use `DamageContext`
- player and enemy no longer duplicate final damage formula logic
- critical hit and damage type can be added without changing every weapon script

## Phase 2: Unified Stat System

### Goal

Replace scattered combat-related fields with a structured stat container.

### New files

- `Assets/Scripts/Stats/StatType.cs`
- `Assets/Scripts/Stats/StatBlock.cs`
- `Assets/Scripts/Stats/StatModifier.cs`
- `Assets/Scripts/Stats/StatCollection.cs`

### Suggested initial stat types

- `MaxHealth`
- `PhysicalAttack`
- `MagicPower`
- `PhysicalDefense`
- `MagicResistance`
- `CritRate`
- `CritDamage`
- `CooldownReduction`
- `MoveSpeed`
- `AttackSpeed`
- `HealingPower`
- `StatusPower`
- `StatusResistance`

### Suggested responsibilities

- `StatBlock`: base values
- `StatModifier`: flat or percent modifiers
- `StatCollection`: resolves current effective stat values

### Required code changes

- player combat stats migrate into `StatCollection`
- enemy runtime stats migrate into `StatCollection`
- weapon damage retrieval reads stats from collection instead of raw fields
- future upgrades modify stats through modifiers instead of direct field mutation

### Migration steps

1. Create stat model.
2. Introduce `StatCollection` on player.
3. Introduce `StatCollection` on enemy.
4. Replace reads first.
5. Replace writes second.
6. Remove legacy raw-field mutation methods after migration is complete.

### Definition of done

- adding a new combat stat no longer requires editing many unrelated classes
- player and enemy use the same stat access pattern

## Phase 3: Buff / Debuff System

### Goal

Introduce reusable runtime status effects such as burning, poison, slow, shield, and vulnerability.

### New files

- `Assets/Scripts/StatusEffects/StatusEffectType.cs`
- `Assets/Scripts/StatusEffects/StatusEffectDataSO.cs`
- `Assets/Scripts/StatusEffects/StatusEffectInstance.cs`
- `Assets/Scripts/StatusEffects/StatusController.cs`

### Suggested effect capabilities

- duration
- stackability
- max stack
- refresh mode
- tick interval
- stat modifiers
- periodic damage
- on-apply and on-expire behavior

### Recommended first wave of effects

- Burning
- Poison
- Slow
- Vulnerable
- Shield
- Haste

### Required code changes

- add `StatusController` to player and enemy
- allow `DamageContext` or attack payloads to include effect applications
- make effect ticking independent from weapon implementation

### Definition of done

- one new status effect can be added without modifying each weapon script
- both player and enemy can receive timed combat effects through the same path

## Phase 4: Weapon System Upgrade

### Goal

Make weapons easier to extend without duplicating entire attack pipelines.

### Keep

- `WeaponBase` remains the root abstraction

### Add gradually

- target selection strategy
- attack pattern behavior
- projectile payload
- on-hit effect payload

### Practical approach

Do not over-engineer early.

First split weapon behavior into:

1. attack timing and aiming
2. hit payload construction
3. hit result application

### Required code changes

- replace weapon name matching with direct `WeaponDataSO` identity or a stable `weaponId`
- change `activeWeapons` into a map keyed by weapon data
- allow one weapon to own effect metadata beyond plain damage

### Definition of done

- adding a new weapon mostly means creating a new behavior or payload, not copying and editing an old script

## Phase 5: Upgrade System Refactor

### Goal

Replace enum-branch upgrade application with composable upgrade effects.

### New files

- `Assets/Scripts/Upgrades/UpgradeEffectSO.cs`
- `Assets/Scripts/Upgrades/Effects/ModifyStatEffectSO.cs`
- `Assets/Scripts/Upgrades/Effects/ModifyWeaponStatEffectSO.cs`
- `Assets/Scripts/Upgrades/Effects/AddStatusOnHitEffectSO.cs`

### Benefits

A single upgrade can do more than one thing:

- increase crit rate
- increase magical damage
- add burning on hit
- increase poison duration

### Required code changes

- `UpgradeDataSO` should hold a list of effects instead of one hard-coded enum route
- `UpgradeManager` still controls selection, but not the concrete effect logic

### Definition of done

- new upgrades can be authored mainly by data, not by adding new switch cases

## Phase 6: Enemy Attack and Ability Expansion

### Goal

Make enemy combat behavior scalable.

### New files

- `Assets/Scripts/Enemy/Attack/EnemyAttackBase.cs`
- `Assets/Scripts/Enemy/Attack/MeleeEnemyAttack.cs`
- `Assets/Scripts/Enemy/Attack/ProjectileEnemyAttack.cs`
- `Assets/Scripts/Enemy/Attack/AreaEnemyAttack.cs`
- `Assets/Scripts/Enemy/EnemyPassiveController.cs`

### Result

This will make it easier to build:

- melee enemies
- caster enemies
- suicide enemies
- summoners
- elites with debuffs
- bosses with multiple attack phases

## Recommended Implementation Order

1. Phase 1: unified damage model
2. Phase 2: unified stat system
3. Fix weapon identification in `WeaponManager`
4. Phase 3: buff / debuff system
5. Phase 4: weapon payload and on-hit model
6. Phase 5: effect-driven upgrades
7. Phase 6: enemy attack modularization

## Minimum Viable Refactor

If time is limited, prioritize these first:

- `DamageType`
- `DamageContext`
- `CombatResolver`
- `StatCollection`

These four items already unlock:

- critical hits
- magic damage
- magic resistance
- true damage
- cleaner future upgrades

## Development Checklist

### Phase 1

- [ ] Add `DamageType`
- [ ] Add `DamageContext`
- [ ] Add `DamageResult`
- [ ] Add `CombatResolver`
- [ ] Add new receive-damage entry point to player
- [ ] Add new receive-damage entry point to enemy
- [ ] Migrate sword damage path
- [ ] Migrate player projectile path
- [ ] Migrate enemy projectile path
- [ ] Migrate enemy contact damage path

### Phase 2

- [ ] Add `StatType`
- [ ] Add `StatModifier`
- [ ] Add `StatCollection`
- [ ] Migrate player combat stats
- [ ] Migrate enemy combat stats
- [ ] Read weapon scaling from stat collection

### Phase 3

- [ ] Add `StatusEffectDataSO`
- [ ] Add `StatusController`
- [ ] Support timed status instances
- [ ] Add Burning
- [ ] Add Slow
- [ ] Add Shield

### Phase 4

- [ ] Replace weapon name matching
- [ ] Add weapon identity mapping
- [ ] Separate payload construction from hit resolution

### Phase 5

- [ ] Add effect-based upgrade model
- [ ] Migrate one stat upgrade as proof of concept
- [ ] Migrate one weapon upgrade as proof of concept

### Phase 6

- [ ] Extract enemy attack base
- [ ] Migrate ranged enemy
- [ ] Add one new modular enemy attack type

## Risks and Notes

- Do not migrate all weapons and enemies in one pass.
- Keep current gameplay playable after every phase.
- Avoid making all systems generic too early.
- Prefer introducing one clean path first, then migrating old logic gradually.
- Test every migrated attack source independently.

## Final Success Criteria

The refactor is successful when:

- adding a new damage type does not require changing every attack script
- adding a new stat does not require editing many core entity classes
- adding a new buff does not require changing every weapon
- upgrades can apply multiple effects cleanly
- new weapons and enemies can reuse the same combat resolution flow

