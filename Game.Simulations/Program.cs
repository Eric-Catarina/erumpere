using Game.Core.Abstractions;
using Game.Core.Analytics;
using Game.Core.Data;
using Game.Core.Engine;
using Game.Core.Models;

var parsed = ArgsParser.Parse(args);
Directory.CreateDirectory(parsed.OutputDirectory);

var skills = string.IsNullOrWhiteSpace(parsed.SkillsPath)
    ? SampleCombatData.CreateSkills()
    : CombatDataLoader.LoadSkills(parsed.SkillsPath);

if (!string.IsNullOrWhiteSpace(parsed.EnemiesPath))
{
    _ = CombatDataLoader.LoadEnemies(parsed.EnemiesPath);
}

if (!string.IsNullOrWhiteSpace(parsed.SkillTreesPath))
{
    _ = CombatDataLoader.LoadSkillTrees(parsed.SkillTreesPath);
}

var allEvents = new List<CombatEvent>();
for (var i = 0; i < parsed.Battles; i++)
{
    var seed = parsed.Seed + i;
    var random = new SeededRandomSource(seed);
    var collector = new CombatEventCollector();
    var simulator = new BattleSimulator(random, collector);
    var battle = BuildRandomizedBattle(skills, random);
    simulator.Simulate(battle, maxTurns: 100);
    allEvents.AddRange(collector.Events);
}

var eventsCsv = CombatAnalyticsExporter.BuildEventsCsv(allEvents);
var aggregates = CombatAnalyticsExporter.BuildAggregates(allEvents);
var aggregatesCsv = CombatAnalyticsExporter.BuildAggregatesCsv(aggregates);
var eventsPath = Path.Combine(parsed.OutputDirectory, "combat_events.csv");
var aggregatesPath = Path.Combine(parsed.OutputDirectory, "combat_aggregates.csv");
File.WriteAllText(eventsPath, eventsCsv);
File.WriteAllText(aggregatesPath, aggregatesCsv);

Console.WriteLine($"Simulations: {parsed.Battles}");
Console.WriteLine($"Events CSV: {eventsPath}");
Console.WriteLine($"Aggregates CSV: {aggregatesPath}");

foreach (var row in aggregates.OrderBy(r => r.EntityId))
{
    Console.WriteLine($"Skill {row.EntityId}: win_rate={row.WinRate:0.###} ({row.Wins}/{row.Matches} matches)");
}

static BattleState BuildRandomizedBattle(IReadOnlyList<SkillDefinition> skills, IRandomSource random)
{
    // Enough allied bodies that wins are common; 3v3 keeps both sides plausible for aggregate stats.
    var battle = BattleFactory.CreateSampleBattle(
        skills,
        allyCount: 3,
        enemyCount: 3,
        corruptionValue: random.Next(0, 101));

    // Light randomization for headless bulk checks.
    foreach (var ally in battle.Allies)
    {
        ally.Progression.Level = random.Next(0, 13);
        ally.Health.CurrentHp = random.Next(20, ally.Health.MaxHp + 1);
    }

    foreach (var enemy in battle.Enemies)
    {
        enemy.Health.CurrentHp = random.Next(10, enemy.Health.MaxHp + 1);
    }

    return battle;
}

internal sealed class ParsedArgs
{
    public required int Battles { get; init; }
    public required int Seed { get; init; }
    public required string OutputDirectory { get; init; }
    public required string SkillsPath { get; init; }
    public required string EnemiesPath { get; init; }
    public required string SkillTreesPath { get; init; }
}

internal static class ArgsParser
{
    public static ParsedArgs Parse(string[] args)
    {
        var battles = 100;
        var seed = 42;
        var output = DefaultSimulationOutputDirectory();
        var skillsPath = string.Empty;
        var enemiesPath = string.Empty;
        var skillTreesPath = string.Empty;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--battles" && i + 1 < args.Length && int.TryParse(args[i + 1], out var parsedBattles))
            {
                battles = parsedBattles;
                i++;
            }
            else if (arg == "--seed" && i + 1 < args.Length && int.TryParse(args[i + 1], out var parsedSeed))
            {
                seed = parsedSeed;
                i++;
            }
            else if (arg == "--out" && i + 1 < args.Length)
            {
                output = args[i + 1];
                i++;
            }
            else if (arg == "--skills" && i + 1 < args.Length)
            {
                skillsPath = args[i + 1];
                i++;
            }
            else if (arg == "--enemies" && i + 1 < args.Length)
            {
                enemiesPath = args[i + 1];
                i++;
            }
            else if (arg == "--skillTrees" && i + 1 < args.Length)
            {
                skillTreesPath = args[i + 1];
                i++;
            }
        }

        return new ParsedArgs
        {
            Battles = Math.Max(1, battles),
            Seed = seed,
            OutputDirectory = output,
            SkillsPath = skillsPath,
            EnemiesPath = enemiesPath,
            SkillTreesPath = skillTreesPath,
        };
    }

    // CSV output folder for headless runs; authoritative game JSON is in Data/.
    private static string DefaultSimulationOutputDirectory()
    {
        var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        return Path.Combine(projectDir, "SimulationOutput");
    }
}
