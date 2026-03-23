using Game.Core.Config;
using Game.Core.Domain;
using Game.Core.Models;

namespace Game.Core.Engine;

public static class BattleFactory
{
    public static BattleState CreateSampleBattle(
        IReadOnlyList<SkillDefinition> skills,
        int allyCount = 2,
        int enemyCount = 4,
        double corruptionValue = 0)
    {
        var skillsById = skills.ToDictionary(s => s.Id, s => s);

        var allies = new List<Combatant>();
        for (var i = 0; i < allyCount; i++)
        {
            allies.Add(CreatePlayer($"ally_{i + 1}", i + 1));
        }

        var enemies = new List<Combatant>();
        for (var i = 0; i < enemyCount; i++)
        {
            enemies.Add(CreateEnemy($"enemy_{i + 1}", i + 1));
        }

        return new BattleState
        {
            Allies = allies,
            Enemies = enemies,
            SkillsById = skillsById,
            CorruptionValue = Math.Clamp(corruptionValue, 0, 100),
            BalanceConfig = CombatBalanceConfig.CreateDefault(),
            TurnNumber = 0,
            BattleId = Guid.NewGuid(),
        };
    }

    private static Combatant CreatePlayer(string id, int rank)
    {
        return new Combatant
        {
            Identity = new IdentityComponent
            {
                Id = id,
                DisplayName = id,
                Faction = Faction.Player,
                Tags = ["Player"],
            },
            Health = new HealthComponent
            {
                CurrentHp = 30,
                MaxHp = 30,
                IsDead = false,
                IsDeathblowPending = false,
            },
            Position = new PositionComponent
            {
                Side = Side.Allies,
                FrontRank = rank,
                Size = 1,
            },
            Stats = new StatsComponent
            {
                Speed = 6,
                Accuracy = 1.0,
                CritChance = 0.05,
            },
            Resistances = new ResistanceComponent
            {
                BurnRes = 0.15,
                BlightRes = 0.15,
                MoveRes = 0.15,
                StunRes = 0.15,
                DeathblowRes = 0.15,
            },
            Tokens = new TokenComponent(),
            Dots = new DotComponent(),
            SkillLoadout = new SkillLoadoutComponent { Skills = { "wulfric_slash" } },
            Progression = new ProgressionComponent { Level = 0, SpentPoints = 0 },
            AI = null,
            ElementAffinity = new ElementAffinityComponent { Element = ElementType.Fire },
        };
    }

    private static Combatant CreateEnemy(string id, int rank)
    {
        return new Combatant
        {
            Identity = new IdentityComponent
            {
                Id = id,
                DisplayName = id,
                Faction = Faction.Enemy,
                Tags = ["Enemy"],
            },
            Health = new HealthComponent
            {
                CurrentHp = 20,
                MaxHp = 20,
                IsDead = false,
                IsDeathblowPending = false,
            },
            Position = new PositionComponent
            {
                Side = Side.Enemies,
                FrontRank = rank,
                Size = 1,
            },
            Stats = new StatsComponent
            {
                Speed = 4,
                Accuracy = 1.0,
                CritChance = 0.03,
            },
            Resistances = new ResistanceComponent
            {
                BurnRes = 0.05,
                BlightRes = 0.05,
                MoveRes = 0.05,
                StunRes = 0.05,
                DeathblowRes = 0.05,
            },
            Tokens = new TokenComponent(),
            Dots = new DotComponent(),
            SkillLoadout = new SkillLoadoutComponent
            {
                Skills = { "spider_bite", "spider_web", "wulfric_slash" },
            },
            Progression = new ProgressionComponent { Level = 0, SpentPoints = 0 },
            AI = new AIComponent { DecisionPolicyId = "KillThenWeighted" },
            ElementAffinity = new ElementAffinityComponent { Element = ElementType.Anomaly },
        };
    }
}
