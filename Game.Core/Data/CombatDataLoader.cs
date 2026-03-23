using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Core.Domain;
using Game.Core.Models;

namespace Game.Core.Data;

public static class CombatDataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter() },
    };

    public static IReadOnlyList<SkillDefinition> LoadSkills(string path)
    {
        var json = File.ReadAllText(path);
        var skills = JsonSerializer.Deserialize<List<SkillDefinition>>(json, JsonOptions) ?? [];
        ValidateSkills(skills);
        return skills;
    }

    public static IReadOnlyList<EnemyDefinition> LoadEnemies(string path)
    {
        var json = File.ReadAllText(path);
        var enemies = JsonSerializer.Deserialize<List<EnemyDefinition>>(json, JsonOptions) ?? [];
        ValidateEnemies(enemies);
        return enemies;
    }

    public static IReadOnlyList<CharacterSkillTreesDefinition> LoadSkillTrees(string path)
    {
        var json = File.ReadAllText(path);
        var trees = JsonSerializer.Deserialize<List<CharacterSkillTreesDefinition>>(json, JsonOptions) ?? [];
        ValidateSkillTrees(trees);
        return trees;
    }

    private static void ValidateSkills(IEnumerable<SkillDefinition> skills)
    {
        foreach (var skill in skills)
        {
            if (string.IsNullOrWhiteSpace(skill.Id))
            {
                throw new InvalidDataException("Skill id is required.");
            }

            if (skill.BaseDamage.Min > skill.BaseDamage.Max)
            {
                throw new InvalidDataException($"Skill {skill.Id} has invalid damage range.");
            }
        }
    }

    private static void ValidateEnemies(IEnumerable<EnemyDefinition> enemies)
    {
        foreach (var enemy in enemies)
        {
            if (enemy.Size is < 1 or > 3)
            {
                throw new InvalidDataException($"Enemy {enemy.Id} has invalid size.");
            }
        }
    }

    private static void ValidateSkillTrees(IEnumerable<CharacterSkillTreesDefinition> trees)
    {
        foreach (var character in trees)
        {
            foreach (var tree in character.Trees)
            {
                foreach (var tier in tree.Tiers)
                {
                    if (tier.Nodes.Count != 4)
                    {
                        throw new InvalidDataException($"Tree {tree.Element} tier {tier.Tier} must have 4 nodes.");
                    }
                }
            }
        }
    }
}

public static class SampleCombatData
{
    public static IReadOnlyList<SkillDefinition> CreateSkills()
    {
        return
        [
            new SkillDefinition
            {
                Id = "wulfric_slash",
                Name = "Slash",
                Element = ElementType.Fire,
                Type = "Active",
                AllowedCasterRanks = [1, 2, 3, 4],
                AllowedTargetRanks = [1, 2, 3, 4],
                BaseDamage = new DamageRange { Min = 4, Max = 7 },
                BaseCritChance = 0.1,
                Accuracy = 0.9,
                Cooldown = 0,
                EffectsOnHit = [],
            },
            new SkillDefinition
            {
                Id = "spider_web",
                Name = "Web",
                Element = ElementType.Anomaly,
                Type = "Active",
                AllowedCasterRanks = [1, 2, 3, 4],
                AllowedTargetRanks = [1, 2, 3, 4],
                BaseDamage = new DamageRange { Min = 1, Max = 2 },
                BaseCritChance = 0.05,
                Accuracy = 0.85,
                Cooldown = 1,
                EffectsOnHit =
                [
                    new EffectSpec { Type = EffectType.ApplyToken, Token = TokenType.Combo, Stacks = 1, Chance = 1.0 },
                    new EffectSpec { Type = EffectType.ApplyDot, Dot = DotType.Blight, Potency = 2, Duration = 2, Chance = 1.0 },
                ],
            },
            new SkillDefinition
            {
                Id = "spider_bite",
                Name = "Bite",
                Element = ElementType.Fire,
                Type = "Active",
                AllowedCasterRanks = [1, 2, 3, 4],
                AllowedTargetRanks = [1, 2, 3, 4],
                BaseDamage = new DamageRange { Min = 2, Max = 4 },
                BaseCritChance = 0.1,
                Accuracy = 0.9,
                Cooldown = 0,
                EffectsOnHit =
                [
                    new EffectSpec { Type = EffectType.ApplyDot, Dot = DotType.Blight, Potency = 2, Duration = 2, Chance = 1.0 },
                ],
            },
        ];
    }
}
