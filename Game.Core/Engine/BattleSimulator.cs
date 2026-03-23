using Game.Core.Abstractions;
using Game.Core.Analytics;
using Game.Core.Config;
using Game.Core.Domain;
using Game.Core.Models;

namespace Game.Core.Engine;

public sealed class BattleSimulator
{
    private readonly IRandomSource _random;
    private readonly CombatEventCollector _eventCollector;

    public BattleSimulator(IRandomSource random, CombatEventCollector eventCollector)
    {
        _random = random;
        _eventCollector = eventCollector;
    }

    public BattleState Simulate(BattleState state, int maxTurns = 100)
    {
        Emit(state, BattleEventType.BattleStarted, battleResult: string.Empty);

        while (!state.IsFinished && state.TurnNumber < maxTurns)
        {
            state.TurnNumber++;
            var turnOrder = BuildInitiativeOrder(state);
            foreach (var actor in turnOrder)
            {
                if (state.IsFinished) break;
                if (actor.Health.IsDead) continue;

                Emit(state, BattleEventType.TurnStarted, actorId: actor.Identity.Id, battleResult: string.Empty);
                ResolveDotTick(state, actor);
                if (actor.Health.IsDead || state.IsFinished) continue;
                if (actor.Tokens.ConsumeOne(TokenType.Stun)) continue;

                var action = ChooseAction(state, actor);
                if (action is null) continue;
                ResolveAction(state, action);
            }
        }

        var winner = state.Winner?.ToString() ?? "None";
        Emit(state, BattleEventType.BattleEnded, battleResult: winner);
        return state;
    }

    private List<Combatant> BuildInitiativeOrder(BattleState state)
    {
        return state.GetAllCombatants()
            .Where(c => !c.Health.IsDead)
            .OrderByDescending(c => c.Stats.Speed)
            .ThenBy(_ => _random.Next(0, int.MaxValue))
            .ToList();
    }

    private void ResolveDotTick(BattleState state, Combatant actor)
    {
        if (actor.Dots.ActiveDots.Count == 0) return;
        var active = actor.Dots.ActiveDots.ToList();
        actor.Dots.ActiveDots.Clear();
        foreach (var dot in active)
        {
            var damage = Math.Max(0, dot.Potency);
            if (damage > 0)
            {
                actor.Health.CurrentHp = Math.Max(0, actor.Health.CurrentHp - damage);
                Emit(
                    state,
                    BattleEventType.DotTick,
                    actorId: dot.AppliedById,
                    targetId: actor.Identity.Id,
                    dotType: dot.Type.ToString(),
                    dotAmount: damage,
                    damageAmount: damage);
            }

            dot.RemainingTurns--;
            if (dot.RemainingTurns > 0)
            {
                actor.Dots.ActiveDots.Add(dot);
            }

            if (actor.Health.CurrentHp <= 0 && !actor.Health.IsDead)
            {
                actor.Health.IsDead = true;
                Emit(state, BattleEventType.CombatantDied, targetId: actor.Identity.Id);
                HandleCompaction(state, actor.Position.Side);
                break;
            }
        }
    }

    private ChosenAction? ChooseAction(BattleState state, Combatant actor)
    {
        var enemies = actor.Position.Side == Side.Allies ? state.Enemies : state.Allies;
        var availableTargets = enemies.Where(e => !e.Health.IsDead).ToList();
        if (availableTargets.Count == 0) return null;

        var availableSkills = actor.SkillLoadout.Skills
            .Where(id => state.SkillsById.ContainsKey(id))
            .Select(id => state.SkillsById[id])
            .Where(skill => IsSkillUsable(actor, skill))
            .ToList();

        if (availableSkills.Count == 0) return null;

        SkillDefinition selectedSkill;
        if (actor.AI?.DecisionPolicyId == "KillThenWeighted")
        {
            selectedSkill = ChooseEnemySkill(state, actor, availableTargets, availableSkills);
        }
        else
        {
            selectedSkill = availableSkills[_random.Next(0, availableSkills.Count)];
        }

        var target = SelectTarget(actor, availableTargets, selectedSkill);
        if (target is null) return null;

        return new ChosenAction
        {
            Actor = actor,
            Target = target,
            Skill = selectedSkill,
            ActionType = ActionType.Skill,
        };
    }

    private SkillDefinition ChooseEnemySkill(
        BattleState state,
        Combatant actor,
        IReadOnlyList<Combatant> targets,
        IReadOnlyList<SkillDefinition> skills)
    {
        var lethalSkills = new List<SkillDefinition>();
        foreach (var skill in skills)
        {
            foreach (var target in targets)
            {
                var estimate = EstimateDamage(state, actor, target, skill);
                if (estimate >= target.Health.CurrentHp)
                {
                    lethalSkills.Add(skill);
                    break;
                }
            }
        }

        var skillPool = lethalSkills.Count > 0 ? lethalSkills : skills.ToList();
        var totalWeight = skillPool.Sum(s => Math.Max(1, s.Weight));
        var roll = _random.Next(1, totalWeight + 1);
        var cumulative = 0;
        foreach (var skill in skillPool)
        {
            cumulative += Math.Max(1, skill.Weight);
            if (roll <= cumulative) return skill;
        }

        return skillPool[0];
    }

    private Combatant? SelectTarget(
        Combatant actor,
        IReadOnlyList<Combatant> availableTargets,
        SkillDefinition skill)
    {
        var tauntTargets = availableTargets.Where(t => t.Tokens.GetStacks(TokenType.Taunt) > 0).ToList();
        var candidateTargets = tauntTargets.Count > 0 ? tauntTargets : availableTargets.ToList();

        var visibleTargets = candidateTargets
            .Where(t => t.Tokens.GetStacks(TokenType.Stealth) == 0)
            .ToList();
        if (visibleTargets.Count == 0)
        {
            return null;
        }

        var inRankTargets = visibleTargets
            .Where(t => t.Position.OccupiedRanks.Any(r => skill.AllowedTargetRanks.Contains(r)))
            .ToList();
        if (inRankTargets.Count == 0)
        {
            return null;
        }

        return inRankTargets[_random.Next(0, inRankTargets.Count)];
    }

    private bool IsSkillUsable(Combatant actor, SkillDefinition skill)
    {
        var inRank = actor.Position.OccupiedRanks.Any(r => skill.AllowedCasterRanks.Contains(r));
        if (!inRank) return false;

        if (actor.SkillLoadout.Cooldowns.TryGetValue(skill.Id, out var turns) && turns > 0)
        {
            return false;
        }

        return true;
    }

    private void ResolveAction(BattleState state, ChosenAction action)
    {
        var actor = action.Actor;
        var target = action.Target;
        var skill = action.Skill;
        Emit(
            state,
            BattleEventType.ActionUsed,
            actorId: actor.Identity.Id,
            targetId: target.Identity.Id,
            skillId: skill.Id,
            element: skill.Element);

        var result = ResolveHitAndDamage(state, actor, target, skill);
        Emit(
            state,
            BattleEventType.HitResolved,
            actorId: actor.Identity.Id,
            targetId: target.Identity.Id,
            skillId: skill.Id,
            element: skill.Element,
            isHit: result.IsHit,
            isCrit: result.IsCrit,
            damageAmount: result.DamageApplied);

        if (result.IsHit)
        {
            ApplyEffects(state, actor, target, skill, result);
        }

        if (skill.Cooldown > 0)
        {
            actor.SkillLoadout.Cooldowns[skill.Id] = skill.Cooldown;
        }

        TickCooldowns(actor);
    }

    private ResolveActionResult ResolveHitAndDamage(
        BattleState state,
        Combatant actor,
        Combatant target,
        SkillDefinition skill)
    {
        if (actor.Tokens.GetStacks(TokenType.Blind) > 0)
        {
            actor.Tokens.ConsumeOne(TokenType.Blind);
            var blindMiss = _random.NextDouble() < state.BalanceConfig.BlindMissChance;
            if (blindMiss)
            {
                return new ResolveActionResult { IsHit = false, IsCrit = false, DamageApplied = 0 };
            }
        }

        if (_random.NextDouble() > skill.Accuracy * actor.Stats.Accuracy)
        {
            return new ResolveActionResult { IsHit = false, IsCrit = false, DamageApplied = 0 };
        }

        if (target.Tokens.GetStacks(TokenType.Dodge) > 0)
        {
            var dodged = _random.NextDouble() < state.BalanceConfig.DodgeNegateChance;
            target.Tokens.ConsumeOne(TokenType.Dodge);
            if (dodged)
            {
                return new ResolveActionResult { IsHit = false, IsCrit = false, DamageApplied = 0 };
            }
        }

        var isCrit = _random.NextDouble() < EffectiveCritChance(state, actor, target, skill);
        var damage = _random.Next(skill.BaseDamage.Min, skill.BaseDamage.Max + 1);
        var elementalMultiplier = GetElementalMultiplier(state, actor, target, skill);
        damage = (int)Math.Round(damage * elementalMultiplier);
        if (isCrit)
        {
            damage = (int)Math.Round(damage * 1.5);
        }

        damage = (int)Math.Round(damage * CorruptionDamageMultiplier(state, actor, target));
        damage = ApplyMitigation(state, target, damage);
        target.Health.CurrentHp = Math.Max(0, target.Health.CurrentHp - damage);

        Emit(
            state,
            BattleEventType.DamageApplied,
            actorId: actor.Identity.Id,
            targetId: target.Identity.Id,
            skillId: skill.Id,
            element: skill.Element,
            isHit: true,
            isCrit: isCrit,
            damageAmount: damage);

        if (target.Health.CurrentHp <= 0 && !target.Health.IsDead)
        {
            target.Health.IsDead = true;
            Emit(state, BattleEventType.CombatantDied, targetId: target.Identity.Id);

            if (!isCrit)
            {
                MaybeCreateCorpse(state, target, wasDotKill: false);
            }

            HandleCompaction(state, target.Position.Side);
        }

        return new ResolveActionResult
        {
            IsHit = true,
            IsCrit = isCrit,
            DamageApplied = damage,
        };
    }

    private void ApplyEffects(
        BattleState state,
        Combatant actor,
        Combatant target,
        SkillDefinition skill,
        ResolveActionResult result)
    {
        var effects = skill.EffectsOnHit.ToList();
        if (target.Tokens.GetStacks(TokenType.Combo) > 0)
        {
            effects.AddRange(skill.ComboBonus);
        }

        foreach (var effect in effects)
        {
            if (_random.NextDouble() > effect.Chance) continue;
            switch (effect.Type)
            {
                case EffectType.ApplyToken:
                    if (effect.Token.HasValue)
                    {
                        target.Tokens.Add(effect.Token.Value, Math.Max(1, effect.Stacks));
                        Emit(
                            state,
                            BattleEventType.TokenApplied,
                            actorId: actor.Identity.Id,
                            targetId: target.Identity.Id,
                            skillId: skill.Id,
                            tokenType: effect.Token.Value.ToString(),
                            tokenDelta: Math.Max(1, effect.Stacks));
                    }

                    break;
                case EffectType.ApplyDot:
                    if (effect.Dot.HasValue && EffectPassesResistance(target, effect.Dot.Value, state))
                    {
                        var elementalMultiplier = GetElementalMultiplier(state, actor, target, skill);
                        var potency = (int)Math.Round(Math.Max(1, effect.Potency) * elementalMultiplier);
                        target.Dots.ActiveDots.Add(new DotInstance
                        {
                            Type = effect.Dot.Value,
                            Potency = potency,
                            RemainingTurns = Math.Max(1, effect.Duration),
                            AppliedById = actor.Identity.Id,
                        });
                    }

                    break;
                case EffectType.Push:
                    MoveTarget(state, target, +Math.Abs(effect.Steps));
                    break;
                case EffectType.Pull:
                    MoveTarget(state, target, -Math.Abs(effect.Steps));
                    break;
                case EffectType.HealHp:
                    actor.Health.CurrentHp = Math.Min(actor.Health.MaxHp, actor.Health.CurrentHp + Math.Max(0, effect.Potency));
                    break;
                case EffectType.HealCorruption:
                    state.CorruptionValue = Math.Max(0, state.CorruptionValue - Math.Max(0, effect.Potency));
                    break;
                case EffectType.ApplyStun:
                    if (_random.NextDouble() >= target.Resistances.StunRes)
                    {
                        target.Tokens.Add(TokenType.Stun, Math.Max(1, effect.Stacks));
                        Emit(
                            state,
                            BattleEventType.TokenApplied,
                            actorId: actor.Identity.Id,
                            targetId: target.Identity.Id,
                            skillId: skill.Id,
                            tokenType: TokenType.Stun.ToString(),
                            tokenDelta: Math.Max(1, effect.Stacks));
                    }
                    break;
            }
        }

        if (!string.Equals(skill.SelfMove.Type, "None", StringComparison.OrdinalIgnoreCase) && skill.SelfMove.Steps != 0)
        {
            MoveTarget(state, actor, skill.SelfMove.Steps);
        }
    }

    private int ApplyMitigation(BattleState state, Combatant target, int damage)
    {
        if (target.Tokens.GetStacks(TokenType.BlockPlus) > 0)
        {
            target.Tokens.ConsumeOne(TokenType.BlockPlus);
            damage = (int)Math.Round(damage * state.BalanceConfig.BlockPlusDamageMultiplier);
        }
        else if (target.Tokens.GetStacks(TokenType.Block) > 0)
        {
            target.Tokens.ConsumeOne(TokenType.Block);
            damage = (int)Math.Round(damage * state.BalanceConfig.BlockDamageMultiplier);
        }

        return Math.Max(0, damage);
    }

    private bool EffectPassesResistance(Combatant target, DotType dotType, BattleState state)
    {
        var resistance = dotType switch
        {
            DotType.Burn => target.Resistances.BurnRes,
            DotType.Blight => target.Resistances.BlightRes,
            _ => 0,
        };
        return _random.NextDouble() >= resistance;
    }

    private void MoveTarget(BattleState state, Combatant target, int steps)
    {
        if (steps == 0) return;
        var newFront = Math.Clamp(target.Position.FrontRank + steps, 1, 5 - target.Position.Size);
        target.Position.FrontRank = newFront;
        HandleCompaction(state, target.Position.Side);
    }

    private void HandleCompaction(BattleState state, Side side)
    {
        var roster = side == Side.Allies ? state.Allies : state.Enemies;
        var alive = roster.Where(c => !c.Health.IsDead).OrderBy(c => c.Position.FrontRank).ToList();
        var nextRank = 1;
        foreach (var unit in alive)
        {
            unit.Position.FrontRank = nextRank;
            nextRank += unit.Position.Size;
        }
    }

    private void MaybeCreateCorpse(BattleState state, Combatant defeatedTarget, bool wasDotKill)
    {
        if (wasDotKill) return;
        if (defeatedTarget.Identity.Faction != Faction.Enemy) return;

        var roster = state.Enemies;
        var corpse = new Combatant
        {
            Identity = new IdentityComponent
            {
                Id = $"{defeatedTarget.Identity.Id}_corpse_{Guid.NewGuid():N}",
                DisplayName = $"{defeatedTarget.Identity.DisplayName} Corpse",
                Faction = Faction.Corpse,
                Tags = ["Corpse"],
            },
            Health = new HealthComponent { CurrentHp = 1, MaxHp = 1, IsDead = false, IsDeathblowPending = false },
            Position = new PositionComponent
            {
                Side = Side.Enemies,
                FrontRank = defeatedTarget.Position.FrontRank,
                Size = defeatedTarget.Position.Size,
            },
            Stats = new StatsComponent { Speed = 0, Accuracy = 0, CritChance = 0 },
            Resistances = new ResistanceComponent
            {
                BurnRes = 0,
                BlightRes = 0,
                MoveRes = 0,
                StunRes = 1,
                DeathblowRes = 0,
            },
            Tokens = new TokenComponent(),
            Dots = new DotComponent(),
            SkillLoadout = new SkillLoadoutComponent(),
            Progression = new ProgressionComponent { Level = 0, SpentPoints = 0 },
            AI = null,
            ElementAffinity = new ElementAffinityComponent { Element = ElementType.None },
        };
        roster.Add(corpse);
    }

    private int EstimateDamage(BattleState state, Combatant actor, Combatant target, SkillDefinition skill)
    {
        var average = (skill.BaseDamage.Min + skill.BaseDamage.Max) / 2;
        var elementalMultiplier = GetElementalMultiplier(state, actor, target, skill);
        return (int)Math.Round(average * elementalMultiplier * CorruptionDamageMultiplier(state, actor, target));
    }

    private double CorruptionDamageMultiplier(BattleState state, Combatant actor, Combatant target)
    {
        var tierModifiers = state.BalanceConfig.GetTierModifiers(state.CorruptionTier);
        if (actor.Identity.Faction == Faction.Player)
        {
            return tierModifiers.PlayerDamageDealtMultiplier;
        }

        if (target.Identity.Faction == Faction.Player)
        {
            return tierModifiers.PlayerDamageTakenMultiplier;
        }

        return 1.0;
    }

    private double EffectiveCritChance(BattleState state, Combatant actor, Combatant target, SkillDefinition skill)
    {
        var baseChance = skill.BaseCritChance + actor.Stats.CritChance;
        var tierModifiers = state.BalanceConfig.GetTierModifiers(state.CorruptionTier);

        if (actor.Identity.Faction == Faction.Player)
        {
            baseChance += tierModifiers.PlayerCritBonus;
        }

        if (target.Identity.Faction == Faction.Player)
        {
            baseChance += tierModifiers.EnemyCritBonusAgainstPlayer;
        }

        return Math.Clamp(baseChance, 0, 1);
    }

    private double GetElementalMultiplier(BattleState state, Combatant actor, Combatant target, SkillDefinition skill)
    {
        var attackElement = skill.Element == ElementType.None
            ? actor.ElementAffinity.Element
            : skill.Element;
        var defenseElement = target.ElementAffinity.Element;
        if (ElementTriangle.HasAdvantage(attackElement, defenseElement))
        {
            return state.BalanceConfig.ElementAdvantageMultiplier;
        }

        if (ElementTriangle.HasAdvantage(defenseElement, attackElement))
        {
            return state.BalanceConfig.ElementDisadvantageMultiplier;
        }

        return 1.0;
    }

    private void TickCooldowns(Combatant actor)
    {
        var keys = actor.SkillLoadout.Cooldowns.Keys.ToList();
        foreach (var key in keys)
        {
            actor.SkillLoadout.Cooldowns[key] = Math.Max(0, actor.SkillLoadout.Cooldowns[key] - 1);
        }
    }

    private void Emit(
        BattleState state,
        BattleEventType eventType,
        string actorId = "",
        string targetId = "",
        string skillId = "",
        ElementType element = ElementType.None,
        bool isHit = false,
        bool isCrit = false,
        int damageAmount = 0,
        string dotType = "",
        int dotAmount = 0,
        string tokenType = "",
        int tokenDelta = 0,
        string battleResult = "")
    {
        _eventCollector.Add(new CombatEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            BattleId = state.BattleId.ToString("N"),
            Turn = state.TurnNumber,
            TimestampUtc = DateTime.UtcNow,
            EventType = eventType,
            ActorId = actorId,
            TargetId = targetId,
            SkillId = skillId,
            Element = element,
            IsHit = isHit,
            IsCrit = isCrit,
            DamageAmount = damageAmount,
            DotType = dotType,
            DotAmount = dotAmount,
            TokenType = tokenType,
            TokenDelta = tokenDelta,
            CorruptionValue = state.CorruptionValue,
            CorruptionTier = state.CorruptionTier,
            BattleResult = battleResult,
        });
    }
}
