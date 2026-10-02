using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

[NonParallelizable]
public sealed class WeatherSettlementTests
{
    private static readonly DateTime Start = new(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);
    private Type _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionWeatherHarness()
    {
        var isHazardous = ExtractMethod("SWLOR.Game.Server/Service/Weather.cs", "IsHazardousWeatherArea", 1);
        var damage = ExtractMethod("SWLOR.Game.Server/Service/Weather.cs", "ApplyWeatherDamage", 2);
        var lightning = ExtractMethod("SWLOR.Game.Server/Service/Weather.cs", "Thunderstorm", 2);
        var doWeatherEffects = ExtractMethod("SWLOR.Game.Server/Service/Weather.cs", "DoWeatherEffects", 1);
        var exposure = ExtractMethod("SWLOR.Game.Server/Service/WeatherService/WeatherExposure.cs", "GetDamageDice", 2);
        var getHazard = ExtractMethod("SWLOR.Game.Server/Service/WeatherService/WeatherConditions.cs", "GetHazard", 2);
        var feedback = ExtractMethod("SWLOR.Game.Server/Service/WeatherService/WeatherConditions.cs", "GetFeedback", 5);
        var lightningDamage = ExtractMethod("SWLOR.Game.Server/Service/WeatherService/WeatherConditions.cs", "GetLightningDamage", 2);
        var feedbackText = ExtractClass("SWLOR.Game.Server/Service/WeatherService/WeatherFeedbackText.cs", "WeatherFeedbackText");

        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using static WeatherWorld;

            public enum WeatherHazard { None, Acid, Sand, Snow }
            public enum WeatherStorm { None, Thunder, Sand, Snow }
            public enum Precipitation { Clear, Rain, Snow, Foggy }
            public enum DamageType { Physical, Bludgeoning, Acid, Cold, Electrical }
            public enum CombatDamageType { Physical, Poison, Ice, Electrical }
            public enum ObjectType { Creature = 1, Door = 2, Placeable = 4 }
            public enum Shape { Sphere }
            public enum DurationType { Instant, Temporary }
            public enum VisualEffect { Vfx_Imp_Acid_S, Vfx_Imp_Frost_S, Vfx_Imp_Lightning_M }
            public enum ResistanceType { Mobility }
            public enum StatType { KnockdownImmunity }
            public enum VoiceChat { Pain1 }
            public struct Location { public uint Area; }
            public sealed record Effect(string Kind, int Amount = 0, DamageType DamageType = DamageType.Physical,
                VisualEffect Visual = VisualEffect.Vfx_Imp_Acid_S);
            public sealed class WorldProperty { public PropertyType PropertyType { get; set; } }
            public enum PropertyType { Other, City }
            public sealed class WeatherClimate
            {
                public string StormText = "Thunder weather";
                public string MistText = "Mist";
                public string RainWarmText = "Warm rain";
                public string RainNormalText = "Rain";
                public string SnowText = "Snow";
                public string FreezingText = "Freezing";
                public string ScorchingText = "Scorching";
                public string ColdMildText = "Cold mild";
                public string ColdCloudyText = "Cold clouds";
                public string ColdWindyText = "Cold wind";
                public string WarmMildText = "Warm mild";
                public string WarmCloudyText = "Warm clouds";
                public string WarmWindyText = "Warm wind";
                public string MildNightText = "Mild night";
                public string MildText = "Mild";
                public string CloudyText = "Cloudy";
                public string WindyText = "Windy";
            }
            {{feedbackText}}
            public sealed record WeatherConditions(int Heat, int Humidity, int Wind,
                Precipitation Precipitation, WeatherStorm Storm)
            {
                {{getHazard}}
                {{feedback}}
                {{lightningDamage}}
            }
            public sealed class WeatherExposure
            {
                private DateTime _nextDamageUtc;
                private WeatherHazard _previousHazard;
                {{exposure}}
            }
            public static class Property
            {
                public static string GetPropertyId(uint area) => WeatherWorld.PropertyIds.TryGetValue(area, out var id) ? id : "";
            }
            public static class DB
            {
                public static T Get<T>(string id) where T : class => WeatherWorld.Properties.TryGetValue(id, out var value) ? value as T : null;
            }
            public static class NWScript
            {
                public static Precipitation GetWeather(uint area) => WeatherWorld.NativeWeather.TryGetValue(area, out var value) ? value : Precipitation.Clear;
            }
            public static class CombatDamageTypeExtensions
            {
                public static DamageType GetNWScriptDamageType(this CombatDamageType type) => type switch
                {
                    CombatDamageType.Poison => DamageType.Acid,
                    CombatDamageType.Ice => DamageType.Cold,
                    CombatDamageType.Electrical => DamageType.Electrical,
                    _ => DamageType.Physical
                };
            }
            public static class Resistance
            {
                public static int CalculateResistedTicks(uint target, ResistanceType type, int roll) => 1;
            }
            public static class Stat
            {
                public static int GetStatAdjustment(uint target, StatType stat) => 0;
            }
            public static class WeatherWorld
            {
                public static readonly Dictionary<uint, bool> Weatherable = new();
                public static readonly Dictionary<(uint Area, string Name), bool> Booleans = new();
                public static readonly Dictionary<(uint Area, string Name), int> Integers = new();
                public static readonly Dictionary<uint, string> PropertyIds = new();
                public static readonly Dictionary<string, object> Properties = new();
                public static readonly Dictionary<uint, WeatherConditions> Conditions = new();
                public static readonly Dictionary<uint, Precipitation> NativeWeather = new();
                public static readonly Dictionary<uint, uint> CreatureAreas = new();
                public static readonly Dictionary<uint, bool> PlayerCharacters = new(), Dms = new(), Possessed = new(), Dead = new();
                public static readonly Dictionary<uint, ObjectType> ObjectTypes = new();
                public static readonly Dictionary<uint, List<uint>> Targets = new();
                public static readonly List<(uint Target, Effect Effect, DurationType Duration)> ObjectEffects = new();
                public static readonly List<Effect> LocationEffects = new();
                public static readonly List<(uint Target, string Message)> Messages = new();
                public static int TargetIterations;
                private static int _targetIndex;
                private static uint _targetArea;
                public static bool IsWeatherArea(uint area) => Weatherable.TryGetValue(area, out var value) && value;
                public static bool GetLocalBool(uint area, string name) => Booleans.TryGetValue((area, name), out var value) && value;
                public static int GetLocalInt(uint area, string name) => Integers.TryGetValue((area, name), out var value) ? value : 0;
                public static bool GetIsPC(uint creature) => PlayerCharacters.TryGetValue(creature, out var value) && value;
                public static bool GetIsDM(uint creature) => Dms.TryGetValue(creature, out var value) && value;
                public static bool GetIsDMPossessed(uint creature) => Possessed.TryGetValue(creature, out var value) && value;
                public static bool GetIsDead(uint creature) => Dead.TryGetValue(creature, out var value) && value;
                public static bool GetIsObjectValid(uint target) => ObjectTypes.ContainsKey(target);
                public static uint GetArea(uint creature) => CreatureAreas.TryGetValue(creature, out var area) ? area : 0;
                public static WeatherConditions GetConditions(uint area) => Conditions.TryGetValue(area, out var value) ? value : null;
                public static WeatherClimate GetAreaClimate(uint area) => new();
                public static bool GetIsNight() => false;
                public static void AssignCommand(uint area, Action action) => action();
                public static int d6(int dice = 1) => Math.Max(1, dice) * 6;
                public static int GetProtectedDamage(uint target, int amount, CombatDamageType type) => amount;
                public static Effect EffectDamage(int amount, DamageType type) => new("damage", amount, type);
                public static Effect EffectVisualEffect(VisualEffect visual) => new("visual", Visual: visual);
                public static Effect EffectKnockdown() => new("knockdown");
                public static void ApplyEffectToObject(DurationType duration, Effect effect, uint target, float seconds = 0)
                    => ObjectEffects.Add((target, effect, duration));
                public static void ApplyEffectAtLocation(DurationType duration, Effect effect, Location location)
                    => LocationEffects.Add(effect);
                public static uint GetAreaFromLocation(Location location) => location.Area;
                public static float GetDistanceBetweenLocations(Location a, Location b) => 0;
                public static Location GetLocation(uint target) => new() { Area = CreatureAreas.TryGetValue(target, out var area) ? area : 0 };
                public static uint GetFirstObjectInShape(Shape shape, float range, Location location, bool lineOfSight, ObjectType types)
                {
                    TargetIterations++;
                    _targetArea = location.Area;
                    _targetIndex = 0;
                    return GetTargetAtCursor();
                }
                public static uint GetNextObjectInShape(Shape shape, float range, Location location, bool lineOfSight, ObjectType types)
                {
                    TargetIterations++;
                    _targetIndex++;
                    return GetTargetAtCursor();
                }
                private static uint GetTargetAtCursor() => Targets.TryGetValue(_targetArea, out var targets) && _targetIndex < targets.Count
                    ? targets[_targetIndex] : 0;
                public static ObjectType GetObjectType(uint target) => ObjectTypes.TryGetValue(target, out var value) ? value : ObjectType.Creature;
                public static void SendMessageToPC(uint target, string message) => Messages.Add((target, message));
                public static void PlayVoiceChat(VoiceChat voice, uint target) { }
                public static void Reset()
                {
                    Weatherable.Clear(); Booleans.Clear(); Integers.Clear(); PropertyIds.Clear(); Properties.Clear();
                    Conditions.Clear(); NativeWeather.Clear(); CreatureAreas.Clear(); PlayerCharacters.Clear(); Dms.Clear(); Possessed.Clear();
                    Dead.Clear(); ObjectTypes.Clear(); Targets.Clear(); ObjectEffects.Clear(); LocationEffects.Clear();
                    Messages.Clear(); TargetIterations = 0; _targetIndex = 0; _targetArea = 0;
                }
            }
            public static class WeatherRuntime
            {
                private const string VAR_WEATHER_ACID_RAIN = "VAR_WEATHER_ACID_RAIN";
                private static readonly Dictionary<uint, WeatherExposure> _exposures = new();
                private static bool IsWeatherArea(uint area) => WeatherWorld.IsWeatherArea(area);
                {{isHazardous}}
                {{damage}}
                {{lightning}}
                {{doWeatherEffects}}
                public static bool Classify(uint area) => IsHazardousWeatherArea(area);
                public static void DamagePulse(uint creature, DateTime now) => ApplyWeatherDamage(creature, now);
                public static void WeatherEffects(uint creature) => DoWeatherEffects(creature);
                public static void Strike(uint area, int power) => Thunderstorm(new Location { Area = area }, power);
                public static string SafeFeedback(bool enabled, WeatherConditions conditions, bool acid)
                    => conditions.GetFeedback(new WeatherClimate(), false, acid, Precipitation.Rain, enabled);
                public static void Reset()
                {
                    WeatherWorld.Reset(); _exposures.Clear();
                }
                public static void SetWeatherable(uint area, bool value) => WeatherWorld.Weatherable[area] = value;
                public static void SetSafe(uint area, bool value) => WeatherWorld.Booleans[(area, "WEATHER_HAZARD_SAFE")] = value;
                public static void SetProperty(uint area, string id, PropertyType type)
                {
                    WeatherWorld.PropertyIds[area] = id;
                    WeatherWorld.Properties[id] = new WorldProperty { PropertyType = type };
                }
                public static void RemoveProperty(string id) => WeatherWorld.Properties.Remove(id);
                public static void SetConditions(uint area, WeatherConditions conditions, bool acid)
                {
                    WeatherWorld.Conditions[area] = conditions;
                    WeatherWorld.Integers[(area, "VAR_WEATHER_ACID_RAIN")] = acid ? 1 : 0;
                    WeatherWorld.NativeWeather[area] = Precipitation.Rain;
                }
                public static void SetPlayer(uint id, uint area)
                {
                    WeatherWorld.CreatureAreas[id] = area;
                    WeatherWorld.PlayerCharacters[id] = true;
                    WeatherWorld.ObjectTypes[id] = ObjectType.Creature;
                }
                public static void SetCreatureArea(uint id, uint area) => WeatherWorld.CreatureAreas[id] = area;
                public static void SetTarget(uint id, uint area)
                {
                    WeatherWorld.CreatureAreas[id] = area;
                    WeatherWorld.PlayerCharacters[id] = true;
                    WeatherWorld.ObjectTypes[id] = ObjectType.Creature;
                    if (!WeatherWorld.Targets.ContainsKey(area)) WeatherWorld.Targets[area] = new List<uint>();
                    WeatherWorld.Targets[area].Add(id);
                }
                public static int DamageCount(uint target) => WeatherWorld.ObjectEffects.Count(x => x.Target == target && x.Effect.Kind == "damage");
                public static int KnockdownCount(uint target) => WeatherWorld.ObjectEffects.Count(x => x.Target == target && x.Effect.Kind == "knockdown");
                public static string[] Messages(uint target) => WeatherWorld.Messages.Where(x => x.Target == target).Select(x => x.Message).ToArray();
                public static int LightningVisualCount => WeatherWorld.LocationEffects.Count(x => x.Kind == "visual" && x.Visual == VisualEffect.Vfx_Imp_Lightning_M);
                public static int TargetIterationCount => WeatherWorld.TargetIterations;
            }
            """;

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("WeatherSettlementHarness", [CSharpSyntaxTree.ParseText(source)],
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var emit = compilation.Emit(stream);
        emit.Success.Should().BeTrue(string.Join(Environment.NewLine, emit.Diagnostics));
        _harness = Assembly.Load(stream.ToArray()).GetType("WeatherRuntime")!;
    }

    [SetUp]
    public void ResetHarness() => Call("Reset");

    [Test]
    public void HazardClassification_UsesWeatherabilityAuthoredSafetyAndCityProperty()
    {
        SetWeatherable(1, true);
        SetSafe(1, true);
        Classify(1).Should().BeFalse("an authored safe marker protects a settlement area");

        SetWeatherable(2, true);
        Classify(2).Should().BeTrue("unflagged wilderness remains hazardous");

        SetWeatherable(3, true);
        SetProperty(3, "city-3", "City");
        Classify(3).Should().BeFalse("a live player city property is protected");

        SetWeatherable(4, true);
        SetProperty(4, "house-4", "Other");
        Classify(4).Should().BeTrue("non-city property ownership does not suppress wilderness hazards");

        SetWeatherable(5, false);
        Classify(5).Should().BeFalse("non-weather areas cannot be hazardous");
    }

    [TestCase("Acid")]
    [TestCase("Sand")]
    [TestCase("Snow")]
    public void ProtectedSettlement_AppliesNoAcidSandOrSnowDamage_WhileWildernessStillDoes(string hazard)
    {
        SetWeatherable(1, true);
        SetSafe(1, true);
        SetWeatherable(2, true);
        SetWeatherable(3, true);
        SetProperty(3, "city-3", "City");
        SetPlayer(10, 1);
        SetPlayer(20, 2);
        SetPlayer(30, 3);
        SetConditions(1, HazardConditions(hazard), hazard == "Acid");
        SetConditions(2, HazardConditions(hazard), hazard == "Acid");
        SetConditions(3, HazardConditions(hazard), hazard == "Acid");

        DamagePulse(10, Start);
        DamagePulse(20, Start);
        DamagePulse(30, Start);

        DamageCount(10).Should().Be(0);
        DamageCount(20).Should().Be(1, "the same hazard remains active in wilderness");
        DamageCount(30).Should().Be(0, "a player city remains safe through its property classification");
    }

    [Test]
    public void RemovingLiveCityProperty_ReenablesHazardsOnTheNextPulse()
    {
        SetWeatherable(3, true);
        SetProperty(3, "city-3", "City");
        SetPlayer(30, 3);
        SetConditions(3, HazardConditions("Sand"), false);

        DamagePulse(30, Start);
        DamageCount(30).Should().Be(0);

        Call("RemoveProperty", "city-3");
        DamagePulse(30, Start.AddSeconds(1));
        DamageCount(30).Should().Be(1, "protection follows the active city property lifecycle");
    }

    [Test]
    public void CrossingSafeRegion_DoesNotResetOrAdvanceTheSixSecondDamagePulseClock()
    {
        SetWeatherable(1, true);
        SetWeatherable(2, true);
        SetSafe(2, true);
        SetPlayer(10, 1);
        SetConditions(1, HazardConditions("Sand"), false);
        SetConditions(2, HazardConditions("Sand"), false);

        DamagePulse(10, Start);
        SetCreatureArea(10, 2);
        DamagePulse(10, Start.AddSeconds(2));
        SetCreatureArea(10, 1);
        DamagePulse(10, Start.AddSeconds(3));
        DamageCount(10).Should().Be(1);

        DamagePulse(10, Start.AddSeconds(6));
        DamageCount(10).Should().Be(2, "the first pulse clock remains anchored at the initial hit");
    }

    [Test]
    public void LightningInSafeArea_ShowsWeatherVisualWithoutTargetingDamageOrKnockdown()
    {
        SetWeatherable(1, true);
        SetSafe(1, true);
        SetTarget(10, 1);

        Strike(1, 100);

        LightningVisualCount.Should().Be(1);
        TargetIterationCount.Should().Be(0);
        DamageCount(10).Should().Be(0);
        KnockdownCount(10).Should().Be(0);
    }

    [Test]
    public void LightningInWilderness_StillDamagesAndCanKnockDownTargets()
    {
        SetWeatherable(1, true);
        SetTarget(10, 1);

        Strike(1, 100);

        LightningVisualCount.Should().Be(1);
        TargetIterationCount.Should().BeGreaterThan(0);
        DamageCount(10).Should().Be(1);
        KnockdownCount(10).Should().Be(1);
    }

    [TestCase("AcidRain")]
    [TestCase("SandStorm")]
    [TestCase("SnowStorm")]
    [TestCase("Thunderstorm")]
    public void SafeSettlementFeedback_UsesTheAuthoredHarmlessWeatherDescription(string hazard)
    {
        var feedback = (string)_harness.GetMethod("SafeFeedback")!.Invoke(null,
            [false, HazardConditionsForFeedback(hazard), hazard == "AcidRain"] )!;
        var expectedField = "Sheltered" + hazard;
        var expected = (string)_harness.Assembly.GetType("WeatherFeedbackText")!.GetField(expectedField)!.GetRawConstantValue()!;

        feedback.Should().Be(expected);
    }

    [Test]
    public void AreaEntryInSafeSettlement_SendsHarmlessFeedbackAndAppliesNoDamage()
    {
        SetWeatherable(1, true);
        SetSafe(1, true);
        SetPlayer(10, 1);
        SetConditions(1, HazardConditions("Sand"), false);

        Call("WeatherEffects", 10u);

        var messages = (string[])_harness.GetMethod("Messages")!.Invoke(null, [10u])!;
        var expected = (string)_harness.Assembly.GetType("WeatherFeedbackText")!.GetField("ShelteredSandStorm")!.GetRawConstantValue()!;
        messages.Should().ContainSingle().Which.Should().Be(expected);
        DamageCount(10).Should().Be(0);
    }

    [TestCase("veles_exterior")]
    [TestCase("druz_shalim")]
    [TestCase("hutlar_outpost")]
    [TestCase("moncaladaccitysu")]
    [TestCase("dan_centcolony")]
    [TestCase("dan_colony")]
    [TestCase("dath_tribevill")]
    [TestCase("kash_village")]
    [TestCase("anchor_entreenor")]
    [TestCase("anchor_entreesud")]
    [TestCase("tat_anc_northdis")]
    [TestCase("tat_anc_southdis")]
    [TestCase("tat_tocheemain")]
    [TestCase("pw_ar_kashyk")]
    [TestCase("moseis_dow_ca001")]
    [TestCase("moseis_dow_can")]
    [TestCase("moseis_exit_sud")]
    [TestCase("moseis_lucky")]
    [TestCase("pw_ar_narcatwalk")]
    [TestCase("pw_ar_nardocks")]
    [TestCase("pw_ar_narpromena")]
    [TestCase("pw_ar_narscorpd")]
    [TestCase("pw_ar_narshahub")]
    [TestCase("pw_ar_narslum")]
    [TestCase("pw_ar_nsshipyard")]
    [TestCase("tat_anc_southent")]
    [TestCase("tat_anc_astropor")]
    public void CoreSettlementAreas_AuthorWEATHERHAZARDSAFE(string resref)
    {
        var path = Path.Combine(FindRoot(), "Module", "git", $"{resref}.git.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var variables = document.RootElement.GetProperty("VarTable").GetProperty("value");
        variables.EnumerateArray().Should().Contain(variable =>
            variable.GetProperty("Name").GetProperty("value").GetString() == "WEATHER_HAZARD_SAFE" &&
            variable.GetProperty("Value").GetProperty("value").GetInt32() == 1,
            $"{resref} should carry an authored WEATHER_HAZARD_SAFE local");
    }

    [Test]
    public void AnchorheadRoad_RemainsAnUnflaggedWeatherArea()
    {
        var path = Path.Combine(FindRoot(), "Module", "git", "anchor_road_est.git.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var variables = document.RootElement.GetProperty("VarTable").GetProperty("value");
        variables.EnumerateArray().Should().NotContain(variable =>
            variable.GetProperty("Name").GetProperty("value").GetString() == "WEATHER_HAZARD_SAFE");
    }

    private void Call(string name, params object[] args) => _harness.GetMethod(name)!.Invoke(null, args);
    private void SetWeatherable(uint area, bool value) => Call("SetWeatherable", area, value);
    private void SetSafe(uint area, bool value) => Call("SetSafe", area, value);
    private void SetProperty(uint area, string id, string type) => Call("SetProperty", area, id, Enum.Parse(_harness.Assembly.GetType("PropertyType")!, type));
    private bool Classify(uint area) => (bool)_harness.GetMethod("Classify")!.Invoke(null, [area])!;
    private void SetConditions(uint area, object conditions, bool acid) => Call("SetConditions", area, conditions, acid);
    private void SetPlayer(uint id, uint area) => Call("SetPlayer", id, area);
    private void SetTarget(uint id, uint area) => Call("SetTarget", id, area);
    private void SetCreatureArea(uint id, uint area) => Call("SetCreatureArea", id, area);
    private void DamagePulse(uint creature, DateTime now) => Call("DamagePulse", creature, now);
    private void Strike(uint area, int power) => Call("Strike", area, power);
    private int DamageCount(uint target) => (int)_harness.GetMethod("DamageCount")!.Invoke(null, [target])!;
    private int KnockdownCount(uint target) => (int)_harness.GetMethod("KnockdownCount")!.Invoke(null, [target])!;
    private int LightningVisualCount => (int)_harness.GetProperty("LightningVisualCount")!.GetValue(null)!;
    private int TargetIterationCount => (int)_harness.GetProperty("TargetIterationCount")!.GetValue(null)!;

    private object HazardConditions(string hazard)
    {
        var assembly = _harness.Assembly;
        var precipitation = Enum.Parse(assembly.GetType("Precipitation")!, hazard == "Snow" ? "Snow" : "Rain");
        var storm = Enum.Parse(assembly.GetType("WeatherStorm")!, hazard == "Acid" ? "None" : hazard);
        return Activator.CreateInstance(assembly.GetType("WeatherConditions")!, 6, 8, 6, precipitation, storm)!;
    }

    private object HazardConditionsForFeedback(string hazard)
    {
        var weatherHazard = hazard switch
        {
            "AcidRain" => "Acid",
            "SandStorm" => "Sand",
            "SnowStorm" => "Snow",
            _ => "Thunder"
        };
        return HazardConditions(weatherHazard);
    }

    private static string ExtractMethod(string relativePath, string name, int parameterCount)
    {
        var root = FindRoot();
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var source = File.ReadAllText(path);
        var method = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == name && node.ParameterList.Parameters.Count == parameterCount);
        return method.WithAttributeLists(default).WithoutTrivia().ToFullString();
    }

    private static string ExtractClass(string relativePath, string name)
    {
        var path = Path.Combine(FindRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        var source = File.ReadAllText(path);
        var declaration = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == name);
        return declaration.WithoutTrivia().ToFullString();
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
