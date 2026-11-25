using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>
/// The ordered call-and-draw transcripts of the Mimicry witness and learn code, taken before it is split: a creature using an ability
/// near players (the witness record, the analysis credit, the rank warning, the early exits) and the same creature dying (the learn roll
/// of every witnessed technique per player, the database write, the messages, the skill XP and the sound). The real container hands out
/// the Mimicry service; the neighbours it calls are recording stand-ins, the engine records its writes in order and every random draw is
/// written with its bounds. Regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c> only for a deliberate change.
/// </summary>
[TestFixture]
[NonParallelizable]
public class MimicryWitnessTranscriptTests
{
    private const uint NpcOne = 70;
    private const uint NpcTwo = 71;
    private const uint NpcThree = 72;
    private const uint NpcFour = 73;
    private const uint NpcFive = 74;
    private const uint NpcSix = 75;
    private const uint NpcSeven = 76;

    private static readonly FeatType Strike = (FeatType)3001;
    private static readonly FeatType Burst = (FeatType)3002;
    private static readonly FeatType Mastery = (FeatType)3003;
    private static readonly FeatType NpcStrike = (FeatType)3101;
    private static readonly FeatType NpcBurst = (FeatType)3102;
    private static readonly FeatType NpcMastery = (FeatType)3103;
    private static readonly FeatType Unrelated = (FeatType)3199;

    private static readonly Type[] Leaves =
    {
        typeof(IQuickCastService), typeof(IDMPresenceService), typeof(IPlayerFeedbackService), typeof(IGuiService),
        typeof(IAbilityCatalog), typeof(IPerkService), typeof(IStatService), typeof(ISkillXPService), typeof(IStatusEffectService),
        typeof(ICombatPointService)
    };

    private sealed class Scene : IDisposable
    {
        private readonly List<IDisposable> _scopes = new();
        private readonly List<uint> _nearby = new() { Attacker, Defender };
        private int _cursor;

        public Scene()
        {
            Bench = new CombatTranscriptBench(Leaves);
            foreach (var (id, name) in new[] { (NpcOne, "NpcOne"), (NpcTwo, "NpcTwo"), (NpcThree, "NpcThree"), (NpcFour, "NpcFour"), (NpcFive, "NpcFive"), (NpcSix, "NpcSix"), (NpcSeven, "NpcSeven") })
                Bench.NameCreature(id, name);

            _scopes.Add(PlayerPluginRuntime.Override(CombatTranscriptBench.EngineProxy.Wrap<IPlayerPlugin>(new FakePlayerPlugin(), Bench)));
            Bench.Db.OnSet = entity => { if (Recording) Bench.Lines.Add(entity is Player saved
                ? $"DB.Set Player {saved.Id} learned=[{string.Join(",", saved.LearnedTechniques.Keys.OrderBy(feat => (int)feat).Select(feat => (int)feat))}]"
                : $"DB.Set {entity.GetType().Name} {entity.Id}"); };

            Bench.Engine.SetReturnHandler("GetFirstPC", _ =>
            {
                _cursor = 0;
                return _nearby.Count > 0 ? _nearby[0] : 0x7F000000u;
            });
            Bench.Engine.SetReturnHandler("GetNextPC", _ =>
            {
                _cursor++;
                return _cursor < _nearby.Count ? _nearby[_cursor] : 0x7F000000u;
            });
            Bench.Engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or 0x7F000000u or Gone));
            Bench.Engine.SetReturnHandler("GetIsDM", _ => false);
            Bench.Engine.SetReturnHandler("GetIsDead", args => Dead.Contains((uint)args[0]!));
            Bench.Engine.SetReturnHandler("GetCurrentHitPoints", args => Dead.Contains((uint)args[0]!) ? 0 : 100);
            Bench.Engine.SetReturnHandler("GetArea", args => Elsewhere.Contains((uint)args[0]!) ? 901u : 900u);
            Bench.Engine.SetReturnHandler("GetAbilityScore", args => Perception);
            Bench.Engine.SetReturnHandler("GetIsPC", args => (uint)args[0]! is Attacker or Defender);

            var details = new Dictionary<FeatType, AbilityDetail>
            {
                [Strike] = Technique("Strike", 0, NpcStrike),
                [Burst] = Technique("Burst", 10, NpcBurst),
                [Mastery] = Technique("Mastery", 40, NpcMastery)
            };
            Bench.Leaf<IAbilityCatalog>().Script(nameof(IAbilityCatalog.GetAllAbilityDetails), _ => details);
            Bench.Leaf<IPerkService>().Script(nameof(IPerkService.GetPerkLevel), args => Perks.GetValueOrDefault((uint)args[0]!, new Dictionary<PerkType, int>()).GetValueOrDefault((PerkType)args[1]!));
            Bench.Leaf<IDMPresenceService>().Script(nameof(IDMPresenceService.IsControlledByDM), args => DmControlled.Contains((uint)args[0]!));
            Bench.Leaf<ICombatPointService>().Script(nameof(ICombatPointService.HasCombatPoints), _ => HasCombatPoints);
            Bench.Leaf<ISkillXPService>().Script(nameof(ISkillXPService.GiveSkillXP), args => (int)args[2]!);
        }

        public CombatTranscriptBench Bench { get; }
        public HashSet<uint> Dead { get; } = new();
        public HashSet<uint> Elsewhere { get; } = new();
        public HashSet<uint> DmControlled { get; } = new();
        public Dictionary<uint, Dictionary<PerkType, int>> Perks { get; } = new();
        public bool HasCombatPoints { get; set; } = true;
        public bool Recording { get; set; } = true;
        public int Perception { get; set; } = 12;

        /// <summary>The technique catalog (the stay-part).</summary>
        public IMimicryService Techniques => Bench.Get<IMimicryService>();

        /// <summary>The witness and learn code: the service that holds it, so a split changes this one line.</summary>
        public IMimicryService Witness => Bench.Get<IMimicryService>();

        public List<uint> Nearby => _nearby;

        private static AbilityDetail Technique(string name, int requiredRank, FeatType source) => new()
        {
            Name = name,
            IsMimicryTechnique = true,
            MimicrySkillRequirement = requiredRank,
            MimicrySlotCost = 1,
            MimicrySourceFeat = source,
            ImpactAction = (_, _, _, _) => { }
        };

        public void SetPerk(uint player, PerkType perk, int level)
        {
            if (!Perks.TryGetValue(player, out var levels))
                Perks[player] = levels = new Dictionary<PerkType, int>();
            levels[perk] = level;
        }

        public void SetRank(uint player, int rank)
        {
            var record = Bench.Db.Get<Player>("uuid-" + Bench.Name(player));
            record.Skills[SkillType.Mimicry] = new PlayerSkill { Rank = rank };
            Setup(record);
        }

        public void SetLearned(uint player, params FeatType[] feats)
        {
            var record = Bench.Db.Get<Player>("uuid-" + Bench.Name(player));
            record.LearnedTechniques.Clear();
            foreach (var feat in feats)
                record.LearnedTechniques[feat] = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Setup(record);
        }

        // A write the scenario makes to set the stage is not part of the transcript.
        private void Setup(Player record)
        {
            Recording = false;
            Bench.Db.Set(record);
            Recording = true;
        }

        public void Dispose()
        {
            for (var index = _scopes.Count - 1; index >= 0; index--)
                _scopes[index].Dispose();
            Bench.Dispose();
        }
    }

    [Test]
    public void TheWitnessRecordProducesTheCapturedTranscript()
    {
        using var scene = new Scene();
        var bench = scene.Bench;
        var mimicry = scene.Witness;
        scene.Techniques.CacheData();
        bench.Lines.Clear();

        bench.Step("a creature uses an ability no technique maps to", () => mimicry.OnCreatureAbilityUsed(NpcOne, Unrelated));
        bench.Step("a player uses a mapped ability", () => mimicry.OnCreatureAbilityUsed(Attacker, NpcStrike));
        bench.Step("an invalid creature uses a mapped ability", () => mimicry.OnCreatureAbilityUsed(Gone, NpcStrike));
        bench.Step("no player is near", () =>
        {
            scene.Nearby.Clear();
            mimicry.OnCreatureAbilityUsed(NpcOne, NpcStrike);
            scene.Nearby.AddRange(new[] { Attacker, Defender });
        });
        bench.Step("players near, neither has the Combat Analyzer perk", () => mimicry.OnCreatureAbilityUsed(NpcOne, NpcStrike));
        bench.Step("both have the perk, one is controlled by a DM", () =>
        {
            scene.SetPerk(Attacker, PerkType.CombatAnalyzer, 1);
            scene.SetPerk(Defender, PerkType.CombatAnalyzer, 1);
            scene.DmControlled.Add(Defender);
            mimicry.OnCreatureAbilityUsed(NpcOne, NpcStrike);
            scene.DmControlled.Clear();
        });
        bench.Step("the same technique again: no second record", () => mimicry.OnCreatureAbilityUsed(NpcOne, NpcStrike));
        bench.Step("a second technique of the same creature, both players", () => mimicry.OnCreatureAbilityUsed(NpcOne, NpcBurst));
        bench.Step("a technique above the rank of both players", () => mimicry.OnCreatureAbilityUsed(NpcOne, NpcMastery));
        bench.Step("a technique the first player already learned", () =>
        {
            scene.SetLearned(Attacker, Strike);
            mimicry.OnCreatureAbilityUsed(NpcTwo, NpcStrike);
            scene.SetLearned(Attacker);
        });
        bench.Step("no combat points: the witness is recorded and no analysis credit", () =>
        {
            scene.HasCombatPoints = false;
            mimicry.OnCreatureAbilityUsed(NpcThree, NpcBurst);
            scene.HasCombatPoints = true;
        });
        bench.Step("a player at a lower rank is told the pattern is out of reach", () =>
        {
            scene.SetRank(Defender, 5);
            mimicry.OnCreatureAbilityUsed(NpcFour, NpcBurst);
            scene.SetRank(Defender, 20);
        });

        AssertGolden("SWLOR.Game.Server.Tests/Fixtures/CompanionTranscripts/MimicryWitness.txt", bench.Text);
    }

    [Test]
    public void TheLearnRollProducesTheCapturedTranscript()
    {
        using var scene = new Scene();
        var bench = scene.Bench;
        var mimicry = scene.Witness;
        scene.Techniques.CacheData();
        scene.SetPerk(Attacker, PerkType.CombatAnalyzer, 1);
        scene.SetPerk(Defender, PerkType.CombatAnalyzer, 1);
        scene.SetPerk(Attacker, PerkType.PatternRecognition, 5);
        bench.Lines.Clear();

        void Witness(uint npc)
        {
            // Each creature starts from players who have learned nothing.
            scene.SetLearned(Attacker);
            scene.SetLearned(Defender);
            bench.Engine.SetReturn("OBJECT_SELF", npc);
            mimicry.OnCreatureAbilityUsed(npc, NpcStrike);
            mimicry.OnCreatureAbilityUsed(npc, NpcBurst);
            mimicry.OnCreatureAbilityUsed(npc, NpcMastery);
        }

        bench.Step("a creature with no witnesses dies", () =>
        {
            bench.Engine.SetReturn("OBJECT_SELF", NpcOne);
            mimicry.OnCreatureDeath();
        });
        Witness(NpcTwo);
        bench.Step("a witnessed creature dies near both players: a roll for every technique each can learn", () =>
        {
            bench.Engine.SetReturn("OBJECT_SELF", NpcTwo);
            mimicry.OnCreatureDeath();
        });
        scene.Perception = 30;
        Witness(NpcThree);
        bench.Step("the same again with the perception of both players high", () =>
        {
            bench.Engine.SetReturn("OBJECT_SELF", NpcThree);
            mimicry.OnCreatureDeath();
            scene.Perception = 12;
        });
        Witness(NpcFour);
        scene.SetRank(Attacker, 50);
        scene.SetRank(Defender, 50);
        bench.Step("a creature dies after the rank of both players rose past the third technique", () =>
        {
            bench.Engine.SetReturn("OBJECT_SELF", NpcFour);
            mimicry.OnCreatureDeath();
            scene.SetRank(Attacker, 20);
            scene.SetRank(Defender, 20);
        });
        Witness(NpcFive);
        scene.SetLearned(Attacker, Strike);
        scene.SetLearned(Defender, Strike, Burst);
        bench.Step("each player already knows some of the techniques", () =>
        {
            bench.Engine.SetReturn("OBJECT_SELF", NpcFive);
            mimicry.OnCreatureDeath();
        });
        Witness(NpcSix);
        bench.Step("a dead player and a player in another area learn nothing", () =>
        {
            scene.Dead.Add(Attacker);
            scene.Elsewhere.Add(Defender);
            bench.Engine.SetReturn("OBJECT_SELF", NpcSix);
            mimicry.OnCreatureDeath();
            scene.Dead.Clear();
            scene.Elsewhere.Clear();
        });
        Witness(NpcSeven);
        bench.Step("the witness entry is cleared even when nobody is near", () =>
        {
            scene.Nearby.Clear();
            bench.Engine.SetReturn("OBJECT_SELF", NpcSeven);
            mimicry.OnCreatureDeath();
            mimicry.OnCreatureDeath();
            scene.Nearby.AddRange(new[] { Attacker, Defender });
        });

        AssertGolden("SWLOR.Game.Server.Tests/Fixtures/CompanionTranscripts/MimicryLearn.txt", bench.Text);
    }
}
