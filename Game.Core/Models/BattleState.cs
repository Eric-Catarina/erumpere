using Game.Core.Config;
using Game.Core.Domain;

namespace Game.Core.Models;

public sealed class BattleState
{
    public required IList<Combatant> Allies { get; init; }
    public required IList<Combatant> Enemies { get; init; }
    public required IDictionary<string, SkillDefinition> SkillsById { get; init; }
    public required CombatBalanceConfig BalanceConfig { get; init; }
    public required double CorruptionValue { get; set; }
    public required int TurnNumber { get; set; }
    public required Guid BattleId { get; init; }

    public IEnumerable<Combatant> GetAllCombatants()
    {
        return Allies.Concat(Enemies);
    }

    public int CorruptionTier => CorruptionTierCalculator.GetTier(CorruptionValue);

    public bool IsFinished =>
        Allies.All(c => c.Health.IsDead) || Enemies.All(c => c.Health.IsDead);

    public Side? Winner
    {
        get
        {
            if (Allies.All(c => c.Health.IsDead)) return Side.Enemies;
            if (Enemies.All(c => c.Health.IsDead)) return Side.Allies;
            return null;
        }
    }
}

public sealed class ChosenAction
{
    public required Combatant Actor { get; init; }
    public required Combatant Target { get; init; }
    public required SkillDefinition Skill { get; init; }
    public required ActionType ActionType { get; init; }
}

public sealed class ResolveActionResult
{
    public required bool IsHit { get; init; }
    public required bool IsCrit { get; init; }
    public required int DamageApplied { get; init; }
}
