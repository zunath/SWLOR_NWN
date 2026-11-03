#nullable enable
using SWLOR.Game.Server.Service.AIService;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The AI profile definition the instance tests load through <see cref="TestAIService.Definitions"/>. A test sets
/// <see cref="Build"/> to the profiles it needs (the three the instance requires are added when it leaves them out) and
/// loads them with <c>CacheProfiles()</c>. The tests that use it are not parallelizable, because the builder is shared.
/// </summary>
public sealed class ScriptedProfiles : IAIProfileDefinition
{
    public const string DefaultScript = "ai_default";

    /// <summary>Builds the profiles; null builds <see cref="DefaultProfiles"/>.</summary>
    public static Func<AIProfileBuilder, AIProfileBuilder>? Build { get; set; }

    public Dictionary<AIProfileType, AIProfile> BuildProfiles()
    {
        var builder = new AIProfileBuilder();
        if (Build == null)
            return DefaultProfiles(builder).Build();

        var profiles = Build(builder).Build();
        foreach (var required in new[] { AIProfileType.Generic, AIProfileType.BeastCompanion, AIProfileType.DroidCompanion })
        {
            if (!profiles.ContainsKey(required))
                RequiredProfile(builder, required);
        }

        return builder.Build();
    }

    /// <summary>The three required profiles; the generic one runs a script that scores 10.</summary>
    public static AIProfileBuilder DefaultProfiles(AIProfileBuilder builder)
    {
        builder.Create(AIProfileType.Generic).DecisionThrottle(0f).Script(DefaultScript).Score(10);
        RequiredProfile(builder, AIProfileType.BeastCompanion);
        RequiredProfile(builder, AIProfileType.DroidCompanion);
        return builder;
    }

    private static void RequiredProfile(AIProfileBuilder builder, AIProfileType type)
    {
        builder.Create(type).DecisionThrottle(0f).AttackHighestEnmity().Score(5);
    }
}
