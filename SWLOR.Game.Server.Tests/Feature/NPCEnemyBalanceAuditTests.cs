using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class NPCEnemyBalanceAuditTests
{
    private const int RightHandSlot = 16;
    private const int LeftHandSlot = 32;
    private const int CreatureLeftSlot = 16384;
    private const int CreatureWeaponSlot = 32768;
    private const int CreatureBiteSlot = 65536;
    private const int CreatureArmorSlot = 131072;
    private const int ItemPropertyFP = 91;
    private const int ItemPropertyStamina = 92;
    private const int ItemPropertyDMG = 93;
    private const int ItemPropertyDefense = 94;
    private const int ItemPropertyNPCHP = 96;
    private const int ItemPropertyDelay = 98;
    private const int ItemPropertyNPCLevel = 99;
    private const int ItemPropertyAttack = 111;
    private const int ItemPropertyForceAttack = 112;
    private const int ItemPropertyEvasion = 117;
    private const int ItemPropertyResistance = 133;
    private const int CustomTlkOffset = 16777216;
    private const int ResistanceCostTable = 54;
    private const int PhysicalDefenseSubtype = 1;
    private const int ForceDefenseSubtype = 2;

    private static readonly int[] ResistanceSubtypes = { 1, 2, 3, 4, 100, 101, 102, 103 };

    private static readonly ResistanceType[] ResistanceFamilies =
    {
        ResistanceType.Fire,
        ResistanceType.Poison,
        ResistanceType.Electrical,
        ResistanceType.Ice,
        ResistanceType.Mind,
        ResistanceType.Mobility,
        ResistanceType.Trauma,
        ResistanceType.Disruption,
    };

    private static readonly IReadOnlyDictionary<int, ResistanceType> ResistanceThreatFeats = new Dictionary<int, ResistanceType>
    {
        [(int)FeatType.RendingBite] = ResistanceType.Trauma,
        [(int)FeatType.CripplingTalons] = ResistanceType.Trauma,
        [(int)FeatType.PiercingQuills] = ResistanceType.Trauma,
        [(int)FeatType.ToxicSpit] = ResistanceType.Poison,
        [(int)FeatType.ScorchingBreath] = ResistanceType.Fire,
        [(int)FeatType.InfernoBlast] = ResistanceType.Fire,
        [(int)FeatType.SeismicSlam] = ResistanceType.Mobility,
        [(int)FeatType.RupturingQuake] = ResistanceType.Mobility,
        [(int)FeatType.TerrifyingBellow] = ResistanceType.Mind,
        [(int)FeatType.DisorientingScreech] = ResistanceType.Mind,
        [(int)FeatType.MaulingBite] = ResistanceType.Trauma,
        [(int)FeatType.BonecrusherBite] = ResistanceType.Trauma,
        [(int)FeatType.RakingClaws] = ResistanceType.Mobility,
        [(int)FeatType.PouncingStrike] = ResistanceType.Mobility,
        [(int)FeatType.TailSweep] = ResistanceType.Mind,
        [(int)FeatType.GoringCharge] = ResistanceType.Trauma,
        [(int)FeatType.BarbedVolley] = ResistanceType.Trauma,
        [(int)FeatType.VenomSpray] = ResistanceType.Poison,
        [(int)FeatType.ToxicCloud] = ResistanceType.Poison,
        [(int)FeatType.FrostSpit] = ResistanceType.Ice,
        [(int)FeatType.StaticBurst] = ResistanceType.Electrical,
        [(int)FeatType.SavageRoar] = ResistanceType.Mind,
        [(int)FeatType.SonicShriek] = ResistanceType.Mind,
        [(int)FeatType.PrecisionShot] = ResistanceType.Trauma,
        [(int)FeatType.SuppressingShot] = ResistanceType.Mind,
        [(int)FeatType.GrenadeBurst] = ResistanceType.Fire,
        [(int)FeatType.SerratedSlash] = ResistanceType.Trauma,
        [(int)FeatType.BrutalBash] = ResistanceType.Mobility,
        [(int)FeatType.TacticalMark] = ResistanceType.Trauma,
        [(int)FeatType.OverloadShot] = ResistanceType.Electrical,
        [(int)FeatType.ArcPulse] = ResistanceType.Electrical,
        [(int)FeatType.IonBurst] = ResistanceType.Electrical,
        [(int)FeatType.TargetLock] = ResistanceType.Trauma,
        [(int)FeatType.ShrapnelBurst] = ResistanceType.Trauma,
        [(int)FeatType.ForceRend] = ResistanceType.Disruption,
        [(int)FeatType.MindSpike] = ResistanceType.Mind,
        [(int)FeatType.DarkShock] = ResistanceType.Disruption,
        [(int)FeatType.DreadWave] = ResistanceType.Mind,
        [(int)FeatType.GlacialSlime] = ResistanceType.Ice,
        [(int)FeatType.HoarfrostGlob] = ResistanceType.Ice,
        [(int)FeatType.PermafrostRupture] = ResistanceType.Ice,
        [(int)FeatType.RimePounce] = ResistanceType.Ice,
        [(int)FeatType.CryoBile] = ResistanceType.Ice,
        [(int)FeatType.CapacitorSurge] = ResistanceType.Electrical,
        [(int)FeatType.StaticWeb] = ResistanceType.Electrical,
        [(int)FeatType.ForceSunder] = ResistanceType.Disruption,
        [(int)FeatType.NullShock] = ResistanceType.Disruption,
        [(int)FeatType.ButchersCarve] = ResistanceType.Trauma,
        [(int)FeatType.StimCanister] = ResistanceType.Poison,
        [(int)FeatType.BloodFrenzyFlurry] = ResistanceType.Trauma,
        [(int)FeatType.DraavosChallenge] = ResistanceType.Mind,
    };

    private static readonly ExpectedEnemy[] ExpectedAlternateEnemies =
    {
        new("man_ranger_2", "mando_rgr_skin", "npc_mando_rifle", 13, 199, 11, 19, 11, 16, 16, 29, 7, 9, 0, 5, 4, 4, 22, 30),
        new("man_warrior_2", "mando_war_skin", "npc_mando_blade", 14, 203, 11, 16, 20, 11, 16, 21, 27, 5, 7, 4, 3, 7, 18, 23),
        new("v_raivor2", "raivor_skin", "raivor_c_claw", 14, 238, 20, 11, 11, 16, 16, 35, 6, 9, 0, 2, 6, 4, 27, 24),
        new("v_flesheater2", "flesheater_skin", "vellen_claw", 17, 291, 21, 12, 12, 17, 17, 40, 7, 10, 0, 3, 7, 5, 31, 24),
        new("s_app_m", "s_app_hide", "s_app_electro", 24, 363, 14, 20, 25, 14, 20, 32, 42, 9, 11, 6, 7, 11, 27, 24),
        new("ecoterr_2", "ecoter_hide", "npc_eco_rifle", 27, 490, 27, 15, 15, 22, 22, 59, 10, 14, 0, 5, 11, 9, 43, 30),
        new("byysk_guard002", "hu_byyskgua_hide", "vbyyskguardsword", 50, 2441, 42, 24, 24, 34, 34, 152, 26, 24, 2, 11, 21, 19, 77, 23),
    };

    private static readonly ExpectedEnemy[] ExpectedBloodFrenzyEnemies =
    {
        new("bf_scavenger", "bf_scv_skin", "bf_scv_wp", 50, 1085, 40, 22, 22, 32, 32, 101, 18, 22, 0, 10, 19, 17, 81, 23),
        new("bf_pulsedroid", "bf_pulse_skin", "bf_pulse_wp", 50, 977, 22, 40, 22, 32, 32, 88, 22, 22, 0, 13, 17, 17, 78, 30),
        new("bf_duelist", "bf_duel_skin", "bf_duel_wp", 50, 1573, 41, 23, 23, 33, 33, 121, 21, 23, 1, 10, 20, 18, 88, 23),
        new("bf_butcher", "bf_butch_skin", "bf_butch_wp", 50, 2441, 42, 24, 24, 34, 34, 152, 26, 24, 2, 11, 21, 19, 94, 23),
        new("bf_kess", "bf_kess_skin", "bf_kess_wp", 50, 5425, 43, 25, 25, 35, 35, 253, 44, 25, 3, 11, 22, 20, 102, 23),
    };

    private static readonly ExpectedEnemy OldScarExpectedEnemy =
        new("oldscar_kath", "oldscar_k_sk", "oldscar_k_wp", 4, 193, 16, 10, 10, 14, 14, 25, 4, 8, 2, 1, 5, 3, 13, 24);

    private static readonly ExpectedEnemy StormplumeExpectedEnemy =
        new("stormplume", "stormplume_sk", "stormplume_wp", 4, 164, 10, 14, 16, 10, 14, 15, 19, 4, 6, 3, 2, 6, 10, 24);

    private static readonly ExpectedEnemy[] ExpectedNamedRareEliteEnemies =
    {
        new("soot_rusk", "soot_rusk_sk", "soot_rusk_wp", 6, 230, 11, 18, 11, 15, 15, 26, 7, 9, 2, 5, 4, 4, 16, 30),
        new("nara_venn", "nara_venn_sk", "nara_venn_wp", 6, 230, 11, 18, 11, 15, 15, 26, 7, 9, 2, 5, 4, 4, 16, 25),
        new("silkshade", "silkshade_sk", "silkshade_wp", 7, 259, 11, 18, 11, 15, 15, 29, 7, 9, 2, 5, 4, 4, 17, 24),
        new("mossback", "mossback_sk", "mossback_wp", 12, 461, 21, 13, 13, 18, 18, 47, 8, 11, 2, 3, 8, 6, 27, 24),
        new("tarn_kyric", "tarn_kyric_sk", "tarn_kyric_wp", 14, 483, 13, 22, 13, 18, 18, 46, 11, 11, 2, 6, 6, 6, 29, 22),
        new("varo_skeld", "varo_skeld_sk", "varo_skeld_wp", 14, 483, 13, 22, 13, 18, 18, 46, 11, 11, 2, 6, 6, 6, 29, 30),
        new("harrek_voss", "harrek_voss_sk", "harrek_voss_wp", 14, 483, 13, 22, 13, 18, 18, 46, 11, 11, 2, 6, 6, 6, 29, 23),
        new("greyspine", "greyspine_sk", "greyspine_wp", 12, 461, 21, 13, 13, 18, 18, 47, 8, 11, 2, 3, 8, 6, 27, 24),
        new("maw_ghal", "maw_ghal_sk", "maw_ghal_wp", 17, 557, 14, 19, 23, 14, 19, 37, 48, 8, 10, 6, 6, 10, 27, 24),
        new("redtail_kor", "redtail_kor_sk", "redtail_kor_wp", 14, 536, 22, 13, 13, 18, 18, 52, 9, 11, 2, 3, 8, 6, 31, 24),
        new("shardeye", "shardeye_sk", "shardeye_wp", 10, 350, 12, 20, 12, 16, 16, 36, 9, 10, 2, 6, 5, 5, 22, 24),
        new("rootcoil", "rootcoil_sk", "rootcoil_wp", 12, 461, 21, 13, 13, 18, 18, 47, 8, 11, 2, 3, 8, 6, 27, 24),
        new("mirevein", "mirevein_sk", "mirevein_wp", 12, 461, 21, 13, 13, 18, 18, 47, 8, 11, 2, 3, 8, 6, 27, 24),
        new("vrix7", "vrix7_sk", "vrix7_wp", 50, 2197, 24, 42, 24, 34, 34, 132, 33, 24, 2, 14, 19, 19, 89, 30),
        new("ashwing", "ashwing_sk", "ashwing_wp", 2, 114, 10, 13, 16, 10, 13, 12, 15, 3, 5, 3, 1, 5, 7, 24),
    };

    private static readonly IReadOnlyDictionary<ResistanceType, int> OldScarExpectedResistances = new Dictionary<ResistanceType, int>
    {
        [ResistanceType.Fire] = -10,
        [ResistanceType.Poison] = 4,
        [ResistanceType.Electrical] = 2,
        [ResistanceType.Ice] = 2,
        [ResistanceType.Mind] = -15,
        [ResistanceType.Mobility] = 5,
        [ResistanceType.Trauma] = 6,
        [ResistanceType.Disruption] = -10,
    };

    private static readonly IReadOnlyDictionary<ResistanceType, int> StormplumeExpectedResistances = new Dictionary<ResistanceType, int>
    {
        [ResistanceType.Fire] = -10,
        [ResistanceType.Poison] = 4,
        [ResistanceType.Electrical] = 2,
        [ResistanceType.Ice] = 2,
        [ResistanceType.Mind] = -15,
        [ResistanceType.Mobility] = 5,
        [ResistanceType.Trauma] = 6,
        [ResistanceType.Disruption] = -10,
    };

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<ResistanceType, int>> ExpectedNamedRareEliteResistances =
        new Dictionary<string, IReadOnlyDictionary<ResistanceType, int>>
        {
            ["soot_rusk"] = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = 3, [ResistanceType.Poison] = -5, [ResistanceType.Electrical] = 3, [ResistanceType.Ice] = 3, [ResistanceType.Mind] = -5, [ResistanceType.Mobility] = 3, [ResistanceType.Trauma] = 4, [ResistanceType.Disruption] = 3 },
            ["nara_venn"] = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = 3, [ResistanceType.Poison] = -5, [ResistanceType.Electrical] = 3, [ResistanceType.Ice] = 3, [ResistanceType.Mind] = -5, [ResistanceType.Mobility] = 3, [ResistanceType.Trauma] = 4, [ResistanceType.Disruption] = 3 },
            ["silkshade"] = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = -10, [ResistanceType.Poison] = 5, [ResistanceType.Electrical] = 3, [ResistanceType.Ice] = 3, [ResistanceType.Mind] = -15, [ResistanceType.Mobility] = 6, [ResistanceType.Trauma] = 7, [ResistanceType.Disruption] = -10 },
            ["mossback"] = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = -10, [ResistanceType.Poison] = 6, [ResistanceType.Electrical] = 4, [ResistanceType.Ice] = 4, [ResistanceType.Mind] = -15, [ResistanceType.Mobility] = 7, [ResistanceType.Trauma] = 8, [ResistanceType.Disruption] = -10 },
            ["tarn_kyric"] = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = 4, [ResistanceType.Poison] = -5, [ResistanceType.Electrical] = 4, [ResistanceType.Ice] = 4, [ResistanceType.Mind] = -5, [ResistanceType.Mobility] = 4, [ResistanceType.Trauma] = 5, [ResistanceType.Disruption] = 4 },
            ["varo_skeld"] = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = 4, [ResistanceType.Poison] = -5, [ResistanceType.Electrical] = 4, [ResistanceType.Ice] = 4, [ResistanceType.Mind] = -5, [ResistanceType.Mobility] = 4, [ResistanceType.Trauma] = 5, [ResistanceType.Disruption] = 4 },
            ["harrek_voss"] = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = 4, [ResistanceType.Poison] = -5, [ResistanceType.Electrical] = 4, [ResistanceType.Ice] = 4, [ResistanceType.Mind] = -5, [ResistanceType.Mobility] = 4, [ResistanceType.Trauma] = 5, [ResistanceType.Disruption] = 4 },
            ["greyspine"] = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = -10, [ResistanceType.Poison] = 6, [ResistanceType.Electrical] = 4, [ResistanceType.Ice] = 4, [ResistanceType.Mind] = -15, [ResistanceType.Mobility] = 7, [ResistanceType.Trauma] = 8, [ResistanceType.Disruption] = -10 },
            ["maw_ghal"] = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = -10, [ResistanceType.Poison] = 9, [ResistanceType.Electrical] = 5, [ResistanceType.Ice] = -10, [ResistanceType.Mind] = 11, [ResistanceType.Mobility] = 7, [ResistanceType.Trauma] = 9, [ResistanceType.Disruption] = 7 },
            ["redtail_kor"] = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = -10, [ResistanceType.Poison] = 6, [ResistanceType.Electrical] = 4, [ResistanceType.Ice] = 4, [ResistanceType.Mind] = -15, [ResistanceType.Mobility] = 7, [ResistanceType.Trauma] = 8, [ResistanceType.Disruption] = -10 },
            ["shardeye"] = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = -10, [ResistanceType.Poison] = 5, [ResistanceType.Electrical] = 3, [ResistanceType.Ice] = 3, [ResistanceType.Mind] = -15, [ResistanceType.Mobility] = 6, [ResistanceType.Trauma] = 7, [ResistanceType.Disruption] = -10 },
            ["rootcoil"] = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = -10, [ResistanceType.Poison] = 8, [ResistanceType.Electrical] = 4, [ResistanceType.Ice] = -10, [ResistanceType.Mind] = 10, [ResistanceType.Mobility] = 6, [ResistanceType.Trauma] = 8, [ResistanceType.Disruption] = 6 },
            ["mirevein"] = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = -10, [ResistanceType.Poison] = 8, [ResistanceType.Electrical] = 4, [ResistanceType.Ice] = -10, [ResistanceType.Mind] = 10, [ResistanceType.Mobility] = 6, [ResistanceType.Trauma] = 8, [ResistanceType.Disruption] = 6 },
            ["vrix7"] = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = 12, [ResistanceType.Poison] = 19, [ResistanceType.Electrical] = -20, [ResistanceType.Ice] = 12, [ResistanceType.Mind] = 19, [ResistanceType.Mobility] = 12, [ResistanceType.Trauma] = 13, [ResistanceType.Disruption] = -15 },
            ["ashwing"] = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = -10, [ResistanceType.Poison] = 6, [ResistanceType.Electrical] = 2, [ResistanceType.Ice] = -10, [ResistanceType.Mind] = 8, [ResistanceType.Mobility] = 4, [ResistanceType.Trauma] = 6, [ResistanceType.Disruption] = 4 },
        };

    private static readonly IReadOnlyDictionary<string, FeatType[]> ExpectedBloodFrenzyAbilityPackages = new Dictionary<string, FeatType[]>
    {
        ["bf_scavenger"] = new[] { FeatType.RakingClaws, FeatType.RendingBite },
        ["bf_pulsedroid"] = new[] { FeatType.SuppressingShot, FeatType.PrecisionShot },
        ["bf_duelist"] = new[] { FeatType.PouncingStrike, FeatType.RendingBite, FeatType.TailSweep },
        ["bf_butcher"] = new[] { FeatType.ButchersCarve, FeatType.StimCanister, FeatType.BloodFrenzyFlurry, FeatType.BrutalBash },
        ["bf_kess"] = new[] { FeatType.BloodFrenzyFlurry, FeatType.DraavosChallenge, FeatType.StimCanister, FeatType.SerratedSlash, FeatType.BrutalBash, FeatType.TacticalMark },
    };

    private static readonly IReadOnlyDictionary<string, FeatType[]> ExpectedNamedRareEliteAbilityPackages = new Dictionary<string, FeatType[]>
    {
        ["soot_rusk"] = new[] { FeatType.TacticalMark, FeatType.PrecisionShot, FeatType.PiercingQuills, FeatType.GrenadeBurst },
        ["nara_venn"] = new[] { FeatType.TacticalMark, FeatType.PrecisionShot, FeatType.PiercingQuills, FeatType.GrenadeBurst },
        ["silkshade"] = new[] { FeatType.TacticalMark, FeatType.PrecisionShot, FeatType.PiercingQuills, FeatType.GrenadeBurst },
        ["mossback"] = new[] { FeatType.PouncingStrike, FeatType.MaulingBite, FeatType.TailSweep, FeatType.TerrifyingBellow },
        ["tarn_kyric"] = new[] { FeatType.TacticalMark, FeatType.PrecisionShot, FeatType.PiercingQuills, FeatType.GrenadeBurst },
        ["varo_skeld"] = new[] { FeatType.TacticalMark, FeatType.PrecisionShot, FeatType.PiercingQuills, FeatType.GrenadeBurst },
        ["harrek_voss"] = new[] { FeatType.TacticalMark, FeatType.PrecisionShot, FeatType.PiercingQuills, FeatType.GrenadeBurst },
        ["greyspine"] = new[] { FeatType.PouncingStrike, FeatType.MaulingBite, FeatType.TailSweep, FeatType.TerrifyingBellow },
        ["maw_ghal"] = new[] { FeatType.SonicShriek, FeatType.DisorientingScreech, FeatType.TacticalMark, FeatType.CripplingTalons },
        ["redtail_kor"] = new[] { FeatType.PouncingStrike, FeatType.MaulingBite, FeatType.TailSweep, FeatType.TerrifyingBellow },
        ["shardeye"] = new[] { FeatType.TacticalMark, FeatType.PrecisionShot, FeatType.PiercingQuills, FeatType.GrenadeBurst },
        ["rootcoil"] = new[] { FeatType.PouncingStrike, FeatType.MaulingBite, FeatType.TailSweep, FeatType.TerrifyingBellow },
        ["mirevein"] = new[] { FeatType.PouncingStrike, FeatType.MaulingBite, FeatType.TailSweep, FeatType.TerrifyingBellow },
        ["vrix7"] = new[] { FeatType.TacticalMark, FeatType.PrecisionShot, FeatType.PiercingQuills, FeatType.GrenadeBurst },
        ["ashwing"] = new[] { FeatType.SonicShriek, FeatType.DisorientingScreech, FeatType.TacticalMark, FeatType.CripplingTalons },
    };

    private static readonly IReadOnlyDictionary<string, string> ExpectedDroidEnemySkins = new Dictionary<string, string>
    {
        ["bf_pulsedroid"] = "bf_pulse_skin",
        ["malfunctioningse"] = "malfsecdroid_sk",
        ["malfunctioningsp"] = "malfspiddroi_sk",
        ["malsecdroid"] = "patroldroid_sk",
        ["malspiderdroid"] = "probedroid_sk",
        ["nar_cmd_droid"] = "nar_droid_sk",
        ["nar_rogue_droid"] = "nar_droid_sk",
        ["nar_scavenger"] = "cz220_droid_hide",
        ["vkorrdundroidhvy"] = "imphvydrone_sk",
        ["vkorrdunwarform"] = "impwarform_sk",
        ["vsithbot1"] = "impobsunit_sk",
        ["vsithbot2"] = "imppatrol_sk",
        ["vsithbot3"] = "impturret_sk",
        ["vsithbot4"] = "impcombot_sk",
        ["vsithbot5"] = "impwarform2_sk",
    };

    private static readonly int[] BloodFrenzyPackageFeatIds = ResistanceThreatFeats
        .Keys
        .Append((int)FeatType.ChitinGuard)
        .Distinct()
        .ToArray();

    private static readonly ExpectedDualWieldDamage[] ExpectedDualWieldDamageTotals =
    {
        new("s_app", 38),
        new("byysk_warrior", 43),
        new("vdathguard", 66),
        new("vkorrdunmarauder", 67),
        new("byysk_champion", 75),
        new("vnpcswar3", 59),
    };

    private static readonly ExpectedRuntimeWeaponDamage[] ExpectedRestoredFastCadenceNormalDamage =
    {
        new("vdathtribal", "kwitribal_wp", 67),
        new("vnpcssorc4", "sithsorc4_wp", 69),
        new("qion_hive_tunnel", "qiontunneler_wp", 31),
        new("qion_hive_tunnel", "qiontunneler_wp2", 30),
        new("vkorrdun1sword", "sithguardmel_wp", 85),
        new("vdathchirodac", "chirodactyl_wp", 41),
        new("vdathchirodac", "chirodactyl_wp2", 40),
        new("korr_wraid", "wraid_wp", 35),
        new("ww_kinrath", "wwkinrath_wp", 15),
    };

    [Test]
    public void KorribanTemples_KeepFrogBossOutOfAmbientSpawnTable()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "SpawnDefinition",
            "KorribanSpawnDefinition.cs"));

        // This source substring check is intentionally pragmatic but fragile. It avoids adding an AST parser
        // for one spawn-table invariant, but method reordering, renaming FrogBoss, or changing the surrounding
        // source structure can break the ambient-table extraction even when runtime behavior is still correct.
        var tableStart = source.IndexOf("_builder.Create(\"KORRIBAN_TEMPLES\"", StringComparison.Ordinal);
        var bossStart = source.IndexOf("private void FrogBoss()", StringComparison.Ordinal);

        tableStart.Should().BeGreaterThanOrEqualTo(0);
        bossStart.Should().BeGreaterThan(tableStart);

        var ambientTable = source[tableStart..bossStart];
        ambientTable.Should().NotContain("\"frogboss\"");

        source.Should().Contain("_builder.Create(\"FrogBoss\", \"Alchemized Frog Boss\")");
        source.Should().Contain(".AddSpawn(ObjectType.Creature, \"frogboss\")");
        source.Should().Contain(".RespawnDelay(120)");
    }

    [Test]
    public void SpawnedAlternateEnemies_HaveCombatUpgradeStats()
    {
        var root = FindRepositoryRoot();

        foreach (var expected in ExpectedAlternateEnemies)
        {
            using var utc = ReadJson(root, "Module", "utc", $"{expected.Resref}.utc.json");
            using var skin = ReadJson(root, "Module", "uti", $"{expected.SkinResref}.uti.json");
            using var weapon = ReadJson(root, "Module", "uti", $"{expected.WeaponResref}.uti.json");

            AssertCreatureHitPoints(utc.RootElement, expected);
            AssertCreatureAttributes(utc.RootElement, expected);
            AssertSkinCombatStats(skin.RootElement, expected);
            AssertWeaponStats(weapon.RootElement, expected);
        }
    }

    [Test]
    public void BloodFrenzyEnemies_HaveBibleGuideStats()
    {
        var root = FindRepositoryRoot();

        foreach (var expected in ExpectedBloodFrenzyEnemies)
        {
            using var utc = ReadJson(root, "Module", "utc", $"{expected.Resref}.utc.json");
            using var skin = ReadJson(root, "Module", "uti", $"{expected.SkinResref}.uti.json");
            using var weapon = ReadJson(root, "Module", "uti", $"{expected.WeaponResref}.uti.json");

            GetEquippedResref(utc.RootElement, RightHandSlot).Should().Be(expected.WeaponResref, $"{expected.Resref} should use its dedicated Blood Frenzy weapon stats");
            GetEquippedResref(utc.RootElement, CreatureArmorSlot).Should().Be(expected.SkinResref, $"{expected.Resref} should use its dedicated Blood Frenzy skin stats");
            GetEquippedResref(utc.RootElement, LeftHandSlot).Should().BeNullOrEmpty($"{expected.Resref} should not inherit shield stats outside the Bible guide");

            AssertCreatureHitPoints(utc.RootElement, expected);
            AssertCreatureAttributes(utc.RootElement, expected);
            AssertSkinCombatStats(skin.RootElement, expected);
            AssertWeaponStats(weapon.RootElement, expected);
        }
    }

    [Test]
    public void OldScar_UsesWildlandsEliteBibleStats()
    {
        var root = FindRepositoryRoot();
        using var utc = ReadJson(root, "Module", "utc", "oldscar_kath.utc.json");
        using var skin = ReadJson(root, "Module", "uti", "oldscar_k_sk.uti.json");
        using var weapon = ReadJson(root, "Module", "uti", "oldscar_k_wp.uti.json");

        GetString(utc.RootElement, "Tag").Should().Be(OldScarExpectedEnemy.Resref);
        GetString(utc.RootElement, "TemplateResRef").Should().Be(OldScarExpectedEnemy.Resref);
        GetEquippedResref(utc.RootElement, CreatureWeaponSlot).Should().Be(OldScarExpectedEnemy.WeaponResref);
        GetEquippedResref(utc.RootElement, CreatureArmorSlot).Should().Be(OldScarExpectedEnemy.SkinResref);
        GetJsonLocalInt(utc.RootElement, "QUEST_NPC_GROUP_ID")
            .Should()
            .Be((int)NPCGroupType.Viscara_WildlandKathHounds, "Old Scar should count as a Kath Hound for existing Wildlands quests");

        AssertCreatureHitPoints(utc.RootElement, OldScarExpectedEnemy);
        AssertCreatureAttributes(utc.RootElement, OldScarExpectedEnemy);
        AssertSkinCombatStats(skin.RootElement, OldScarExpectedEnemy);
        AssertWeaponStats(weapon.RootElement, OldScarExpectedEnemy);

        foreach (var (resistanceType, expectedValue) in OldScarExpectedResistances)
        {
            var rawCostValue = GetItemPropertyCost(skin.RootElement, ItemPropertyResistance, (int)resistanceType);
            rawCostValue.Should().NotBeNull($"Old Scar should define {resistanceType} resistance from the World NPCs Bible row");
            Resistance.DecodeItemPropertyCostTableValue(rawCostValue!.Value)
                .Should()
                .Be(expectedValue, $"{resistanceType} should match Old Scar's level 4 Elite Beast package");
        }
    }

    [Test]
    public void Stormplume_UsesWildlandsEliteBibleStats()
    {
        var root = FindRepositoryRoot();
        using var utc = ReadJson(root, "Module", "utc", "stormplume.utc.json");
        using var skin = ReadJson(root, "Module", "uti", "stormplume_sk.uti.json");
        using var weapon = ReadJson(root, "Module", "uti", "stormplume_wp.uti.json");

        GetString(utc.RootElement, "Tag").Should().Be(StormplumeExpectedEnemy.Resref);
        GetString(utc.RootElement, "TemplateResRef").Should().Be(StormplumeExpectedEnemy.Resref);
        GetEquippedResref(utc.RootElement, CreatureWeaponSlot).Should().Be(StormplumeExpectedEnemy.WeaponResref);
        GetEquippedResref(utc.RootElement, CreatureArmorSlot).Should().Be(StormplumeExpectedEnemy.SkinResref);
        GetJsonLocalInt(utc.RootElement, "QUEST_NPC_GROUP_ID")
            .Should()
            .Be((int)NPCGroupType.Viscara_WildlandsWarocas, "Stormplume should count as a Warocas for existing Wildlands hunter tasks");

        AssertCreatureHitPoints(utc.RootElement, StormplumeExpectedEnemy);
        AssertCreatureAttributes(utc.RootElement, StormplumeExpectedEnemy);
        AssertSkinCombatStats(skin.RootElement, StormplumeExpectedEnemy);
        AssertWeaponStats(weapon.RootElement, StormplumeExpectedEnemy);

        foreach (var (resistanceType, expectedValue) in StormplumeExpectedResistances)
        {
            var rawCostValue = GetItemPropertyCost(skin.RootElement, ItemPropertyResistance, (int)resistanceType);
            rawCostValue.Should().NotBeNull($"Stormplume should define {resistanceType} resistance from the World NPCs Bible row");
            Resistance.DecodeItemPropertyCostTableValue(rawCostValue!.Value)
                .Should()
                .Be(expectedValue, $"{resistanceType} should match Stormplume's level 4 Elite Beast package");
        }
    }

    [Test]
    public void ViscaraNamedRareElites_UseBibleStats()
    {
        var root = FindRepositoryRoot();

        foreach (var expected in ExpectedNamedRareEliteEnemies)
        {
            using var utc = ReadJson(root, "Module", "utc", $"{expected.Resref}.utc.json");
            using var skin = ReadJson(root, "Module", "uti", $"{expected.SkinResref}.uti.json");
            using var weapon = ReadJson(root, "Module", "uti", $"{expected.WeaponResref}.uti.json");

            GetString(utc.RootElement, "Tag").Should().Be(expected.Resref);
            GetString(utc.RootElement, "TemplateResRef").Should().Be(expected.Resref);
            GetEquippedWeaponResrefs(utc.RootElement).Should().Contain(expected.WeaponResref);
            GetEquippedResref(utc.RootElement, CreatureArmorSlot).Should().Be(expected.SkinResref);

            AssertCreatureHitPoints(utc.RootElement, expected);
            AssertCreatureAttributes(utc.RootElement, expected);
            AssertSkinCombatStats(skin.RootElement, expected);
            AssertWeaponStats(weapon.RootElement, expected);

            foreach (var (resistanceType, expectedValue) in ExpectedNamedRareEliteResistances[expected.Resref])
            {
                var rawCostValue = GetItemPropertyCost(skin.RootElement, ItemPropertyResistance, (int)resistanceType);
                rawCostValue.Should().NotBeNull($"{expected.Resref} should define {resistanceType} resistance from the World NPCs Bible row");
                Resistance.DecodeItemPropertyCostTableValue(rawCostValue!.Value)
                    .Should()
                    .Be(expectedValue, $"{resistanceType} should match {expected.Resref}'s named rare elite Bible package");
            }
        }
    }

    [Test]
    public void BloodFrenzyEnemies_UseBibleAbilityPackages()
    {
        var root = FindRepositoryRoot();

        foreach (var expected in ExpectedBloodFrenzyAbilityPackages)
        {
            using var utc = ReadJson(root, "Module", "utc", $"{expected.Key}.utc.json");
            var expectedFeatIds = expected.Value.Select(feat => (int)feat).ToArray();

            var creatureFeats = GetCreatureFeats(utc.RootElement);
            creatureFeats.Should().Contain(expectedFeatIds, $"{expected.Key} should have every feat from its Blood Frenzy Bible ability package");
            creatureFeats
                .Intersect(BloodFrenzyPackageFeatIds)
                .Should()
                .BeEquivalentTo(expectedFeatIds, $"{expected.Key} should use its Blood Frenzy Bible ability package");
        }
    }

    [Test]
    public void OldScar_UsesEliteMeleeAbilityPackage()
    {
        var root = FindRepositoryRoot();
        using var utc = ReadJson(root, "Module", "utc", "oldscar_kath.utc.json");
        var expectedFeatIds = new[]
        {
            (int)FeatType.PouncingStrike,
            (int)FeatType.MaulingBite,
            (int)FeatType.TailSweep,
            (int)FeatType.TerrifyingBellow,
        };

        GetCreatureFeats(utc.RootElement)
            .Should()
            .Contain(expectedFeatIds, "Old Scar should use the Elite Melee package from the World NPCs Bible");
        GetCreatureFeats(utc.RootElement)
            .Intersect(ResistanceThreatFeats.Keys)
            .Should()
            .BeEquivalentTo(expectedFeatIds, "Old Scar should not inherit the normal Kath Hound package");
    }

    [Test]
    public void Stormplume_UsesEliteControllerAbilityPackage()
    {
        var root = FindRepositoryRoot();
        using var utc = ReadJson(root, "Module", "utc", "stormplume.utc.json");
        var expectedFeatIds = new[]
        {
            (int)FeatType.SonicShriek,
            (int)FeatType.DisorientingScreech,
            (int)FeatType.TacticalMark,
            (int)FeatType.CripplingTalons,
        };

        GetCreatureFeats(utc.RootElement)
            .Should()
            .Contain(expectedFeatIds, "Stormplume should use the Elite Controller package from the World NPCs Bible");
        GetCreatureFeats(utc.RootElement)
            .Intersect(ResistanceThreatFeats.Keys)
            .Should()
            .BeEquivalentTo(expectedFeatIds, "Stormplume should use the authored Elite Controller package without extra resistance-pressure abilities");
    }

    [Test]
    public void ViscaraNamedRareElites_UseAuthoredEliteAbilityPackages()
    {
        var root = FindRepositoryRoot();

        foreach (var expected in ExpectedNamedRareEliteAbilityPackages)
        {
            using var utc = ReadJson(root, "Module", "utc", $"{expected.Key}.utc.json");
            var expectedFeatIds = expected.Value.Select(feat => (int)feat).ToArray();

            GetCreatureFeats(utc.RootElement)
                .Should()
                .Contain(expectedFeatIds, $"{expected.Key} should include every feat from its elite ability package");
            GetCreatureFeats(utc.RootElement)
                .Intersect(ResistanceThreatFeats.Keys)
                .Should()
                .BeEquivalentTo(expectedFeatIds, $"{expected.Key} should not inherit extra resistance-pressure abilities from the base creature");
        }
    }

    [Test]
    public void KessDraavo_UsesBloodFrenzyCapstone()
    {
        var root = FindRepositoryRoot();
        using var utc = ReadJson(root, "Module", "utc", "bf_kess.utc.json");

        GetCreatureFeats(utc.RootElement)
            .Should()
            .Contain((int)FeatType.BloodFrenzyTrait, "Kess Draavo must visibly use the Blood Frenzy capstone");
        GetJsonLocalInt(utc.RootElement, $"PERK_LEVEL_{(int)PerkType.BloodFrenzy}")
            .Should()
            .Be(1, "NPC perk stat bonuses use the generic PERK_LEVEL variable path");
    }

    [Test]
    public void KessDraavo_UsesDocumentedTraumaResistanceOverride()
    {
        var root = FindRepositoryRoot();
        using var skin = ReadJson(root, "Module", "uti", "bf_kess_skin.uti.json");

        GetItemPropertyCost(skin.RootElement, ItemPropertyResistance, (int)ResistanceType.Trauma)
            .Should()
            .Be(100, "Kess Draavo's World NPCs Bible row documents a Trauma Res=100 stat override");
    }

    [Test]
    public void WorldNpcsBible_CalculatesResistanceAdjustmentsWithHandEntryColumns()
    {
        var root = FindRepositoryRoot();
        using var archive = ZipFile.OpenRead(Path.Combine(
            root.FullName,
            "design",
            "bible",
            "SWLOR Design Bible - Combat Upgrade.xlsx"));
        var worksheet = ReadWorksheetByName(archive, "World NPCs");
        var weaponDelays = ReadWorksheetByName(archive, "World NPC Weapon Delays");
        var sharedStrings = ReadSharedStrings(archive);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        GetWorkbookCellText(worksheet, sharedStrings, "AE1").Should().Be("Fire Res Adj");
        GetWorkbookCellText(worksheet, sharedStrings, "AF1").Should().Be("Poison Res Adj");
        GetWorkbookCellText(worksheet, sharedStrings, "AK1").Should().Be("Trauma Res Adj");
        GetWorkbookCellText(worksheet, sharedStrings, "AL1").Should().Be("Disruption Res Adj");
        GetWorkbookCellText(worksheet, sharedStrings, "AM1").Should().Be("Skill Override");
        GetWorkbookCellNumber(worksheet, sharedStrings, "AC206").Should().Be(100m);
        GetWorkbookCellNumber(worksheet, sharedStrings, "AF206").Should().Be(-5m);
        GetWorkbookCellNumber(worksheet, sharedStrings, "AI206").Should().Be(-5m);
        GetWorkbookCellNumber(worksheet, sharedStrings, "AK206").Should().Be(87m);
        GetWorkbookCellText(worksheet, sharedStrings, "AP206")
            .Should()
            .Be("Blood Frenzy, Blood Frenzy Flurry, Draavo's Challenge, Stim Canister, Serrated Slash, Brutal Bash, Tactical Mark");
        GetWorkbookCellText(worksheet, sharedStrings, "A207").Should().Be("Viscara");
        GetWorkbookCellText(worksheet, sharedStrings, "B207").Should().Be("Old Scar");
        GetWorkbookCellText(worksheet, sharedStrings, "C207").Should().Be("oldscar_kath");
        GetWorkbookCellNumber(worksheet, sharedStrings, "D207").Should().Be(4m);
        GetWorkbookCellText(worksheet, sharedStrings, "E207").Should().Be("Elite");
        GetWorkbookCellText(worksheet, sharedStrings, "F207").Should().Be("Melee");
        GetWorkbookCellText(worksheet, sharedStrings, "G207").Should().Be("Beast");
        GetWorkbookCellText(worksheet, sharedStrings, "H207").Should().Be("None");
        GetWorkbookCellNumber(worksheet, sharedStrings, "AE207").Should().Be(-10m);
        GetWorkbookCellNumber(worksheet, sharedStrings, "AI207").Should().Be(-15m);
        GetWorkbookCellNumber(worksheet, sharedStrings, "AL207").Should().Be(-10m);
        GetWorkbookCellText(worksheet, sharedStrings, "AP207")
            .Should()
            .Be("Pouncing Strike, Mauling Bite, Tail Sweep, Terrifying Bellow");
        GetWorkbookCellText(worksheet, sharedStrings, "A208").Should().Be("Viscara");
        GetWorkbookCellText(worksheet, sharedStrings, "B208").Should().Be("Stormplume");
        GetWorkbookCellText(worksheet, sharedStrings, "C208").Should().Be("stormplume");
        GetWorkbookCellNumber(worksheet, sharedStrings, "D208").Should().Be(4m);
        GetWorkbookCellText(worksheet, sharedStrings, "E208").Should().Be("Elite");
        GetWorkbookCellText(worksheet, sharedStrings, "F208").Should().Be("Controller");
        GetWorkbookCellText(worksheet, sharedStrings, "G208").Should().Be("Beast");
        GetWorkbookCellText(worksheet, sharedStrings, "H208").Should().Be("None");
        GetWorkbookCellNumber(worksheet, sharedStrings, "AE208").Should().Be(-10m);
        GetWorkbookCellNumber(worksheet, sharedStrings, "AI208").Should().Be(-15m);
        GetWorkbookCellNumber(worksheet, sharedStrings, "AL208").Should().Be(-10m);
        GetWorkbookCellText(worksheet, sharedStrings, "AP208")
            .Should()
            .Be("Sonic Shriek, Disorienting Screech, Tactical Mark, Crippling Talons");
        GetWorkbookCellText(worksheet, sharedStrings, "A209").Should().Be("Viscara");
        GetWorkbookCellText(worksheet, sharedStrings, "B209").Should().Be("Sootline Rusk");
        GetWorkbookCellText(worksheet, sharedStrings, "C209").Should().Be("soot_rusk");
        GetWorkbookCellNumber(worksheet, sharedStrings, "D209").Should().Be(6m);
        GetWorkbookCellText(worksheet, sharedStrings, "E209").Should().Be("Elite");
        GetWorkbookCellText(worksheet, sharedStrings, "F209").Should().Be("Ranged");
        GetWorkbookCellText(worksheet, sharedStrings, "G209").Should().Be("Humanoid");
        GetWorkbookCellText(worksheet, sharedStrings, "AP209")
            .Should()
            .Be("Tactical Mark, Precision Shot, Piercing Quills, Grenade Burst");
        GetWorkbookCellText(worksheet, sharedStrings, "A223").Should().Be("Viscara");
        GetWorkbookCellText(worksheet, sharedStrings, "B223").Should().Be("Ashwing Echo");
        GetWorkbookCellText(worksheet, sharedStrings, "C223").Should().Be("ashwing");
        GetWorkbookCellNumber(worksheet, sharedStrings, "D223").Should().Be(2m);
        GetWorkbookCellText(worksheet, sharedStrings, "E223").Should().Be("Elite");
        GetWorkbookCellText(worksheet, sharedStrings, "F223").Should().Be("Controller");
        GetWorkbookCellText(worksheet, sharedStrings, "G223").Should().Be("Aberration");
        GetWorkbookCellNumber(worksheet, sharedStrings, "AE223").Should().Be(-10m);
        GetWorkbookCellNumber(worksheet, sharedStrings, "AH223").Should().Be(-10m);
        GetWorkbookCellText(worksheet, sharedStrings, "AP223")
            .Should()
            .Be("Sonic Shriek, Disorienting Screech, Tactical Mark, Crippling Talons");
        GetWorkbookCellFormula(worksheet, "X202").Should().Contain("+$AF202", "Poison resistance should read the numeric Poison Res Adj column");
        GetWorkbookCellFormula(worksheet, "AA202").Should().Contain("+$AI202", "Mind resistance should read the numeric Mind Res Adj column");
        GetWorkbookCellFormula(worksheet, "AC206").Should().Contain("+$AK206", "Kess's Trauma resistance should read the numeric Trauma Res Adj column");
        GetWorkbookCellFormula(worksheet, "AN206").Should().Contain("'World NPC Weapon Delays'", "Blood Frenzy weapon delays should be calculated through the shared delay table");
        GetWorkbookCellFormula(worksheet, "N207").Should().Contain("$D207&\"|\"&$E207&\"|\"&$F207", "Old Scar HP should be calculated from the level 4 Elite Melee preset");
        GetWorkbookCellFormula(worksheet, "W207").Should().Contain("+$AE207", "Old Scar's fire vulnerability should be applied through the numeric adjustment column");
        GetWorkbookCellFormula(worksheet, "AN207").Should().Contain("'World NPC Weapon Delays'", "Old Scar delay should use the shared delay lookup with preset fallback");
        GetWorkbookCellFormula(worksheet, "N208").Should().Contain("$D208&\"|\"&$E208&\"|\"&$F208", "Stormplume HP should be calculated from the level 4 Elite Controller preset");
        GetWorkbookCellFormula(worksheet, "W208").Should().Contain("+$AE208", "Stormplume's fire vulnerability should be applied through the numeric adjustment column");
        GetWorkbookCellFormula(worksheet, "AN208").Should().Contain("'World NPC Weapon Delays'", "Stormplume delay should use the shared delay lookup with preset fallback");

        var formulaColumns = new[]
        {
            "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V",
            "W", "X", "Y", "Z", "AA", "AB", "AC", "AD", "AN", "AO",
        };

        foreach (var row in Enumerable.Range(202, 22))
        {
            foreach (var column in formulaColumns)
            {
                GetWorkbookCellFormula(worksheet, $"{column}{row}")
                    .Should()
                    .NotBeNullOrWhiteSpace($"{column}{row} should follow the World NPCs formula pattern");
            }
        }

        var handEntryStyle = GetWorkbookCellStyle(worksheet, "A202");
        foreach (var row in Enumerable.Range(2, 222))
        {
            foreach (var column in new[] { "AE", "AF", "AG", "AH", "AI", "AJ", "AK", "AL" })
            {
                GetWorkbookCellText(worksheet, sharedStrings, $"{column}{row}")
                    .Should()
                    .NotBeNullOrWhiteSpace($"{column}{row} should contain a numeric resistance adjustment, using 0 for no change");
                GetWorkbookCellFormula(worksheet, $"{column}{row}")
                    .Should()
                    .BeEmpty($"{column}{row} should be hand-entered, not formula-driven");
                GetWorkbookCellStyle(worksheet, $"{column}{row}")
                    .Should()
                    .Be(handEntryStyle, $"{column}{row} should use the same hand-entry style as the World NPCs input columns");
            }
        }

        worksheet
            .Descendants(ns + "autoFilter")
            .Single()
            .Attribute("ref")?
            .Value
            .Should()
            .Be("$A$1:$AR$223", "the reusable resistance override columns should be included in World NPCs filtering");

        worksheet
            .Descendants(ns + "dataValidation")
            .Single(validation => validation.Attribute("sqref")?.Value == "AE2:AL223")
            .Attribute("type")?
            .Value
            .Should()
            .Be("decimal", "resistance adjustments should be first-class numeric cells rather than text notes");

        weaponDelays
            .Descendants(ns + "autoFilter")
            .Single()
            .Attribute("ref")?
            .Value
            .Should()
            .Be("$A$1:$E$201", "the Blood Frenzy weapon-delay lookup rows should be filterable");

        GetWorkbookCellText(weaponDelays, sharedStrings, "A197").Should().Be("bf_scavenger");
        GetWorkbookCellNumber(weaponDelays, sharedStrings, "D197").Should().Be(230m);
        GetWorkbookCellText(weaponDelays, sharedStrings, "A201").Should().Be("bf_kess");
        GetWorkbookCellNumber(weaponDelays, sharedStrings, "D201").Should().Be(230m);
    }

    [Test]
    public void DualWieldWorldNPCs_TotalRuntimeWeaponDamageMatchesPreset()
    {
        var root = FindRepositoryRoot();

        foreach (var expected in ExpectedDualWieldDamageTotals)
        {
            using var utc = ReadJson(root, "Module", "utc", $"{expected.Resref}.utc.json");
            var rightHand = GetEquippedResref(utc.RootElement, RightHandSlot);
            var leftHand = GetEquippedResref(utc.RootElement, LeftHandSlot);

            rightHand.Should().NotBeNullOrWhiteSpace($"{expected.Resref} must have a right-hand weapon in slot {RightHandSlot}");
            leftHand.Should().NotBeNullOrWhiteSpace($"{expected.Resref} must have a left-hand weapon in slot {LeftHandSlot}");

            using var rightWeapon = ReadJson(root, "Module", "uti", $"{rightHand}.uti.json");
            using var leftWeapon = ReadJson(root, "Module", "uti", $"{leftHand}.uti.json");

            GetItemPropertyCost(rightWeapon.RootElement, ItemPropertyDelay).Should().NotBeNull($"{expected.Resref} right-hand weapon must use custom delay");
            GetItemPropertyCost(leftWeapon.RootElement, ItemPropertyDelay).Should().NotBeNull($"{expected.Resref} left-hand weapon must use custom delay");

            var totalDamage =
                GetItemPropertyCost(rightWeapon.RootElement, ItemPropertyDMG).GetValueOrDefault() +
                GetItemPropertyCost(leftWeapon.RootElement, ItemPropertyDMG).GetValueOrDefault();

            totalDamage.Should().Be(expected.TotalDMG, $"{expected.Resref} dual-wield runtime damage should match the World NPC preset total");
            GetString(rightWeapon.RootElement, "TemplateResRef").Should().Be(rightHand, $"{expected.Resref} right-hand weapon template reference should match its equipped resref");
            GetString(leftWeapon.RootElement, "TemplateResRef").Should().Be(leftHand, $"{expected.Resref} left-hand weapon template reference should match its equipped resref");
        }
    }

    [Test]
    public void FastCadenceNormalWorldNPCs_UseRestoredPresetWeaponDamage()
    {
        var root = FindRepositoryRoot();

        foreach (var expected in ExpectedRestoredFastCadenceNormalDamage)
        {
            using var utc = ReadJson(root, "Module", "utc", $"{expected.Resref}.utc.json");
            using var weapon = ReadJson(root, "Module", "uti", $"{expected.WeaponResref}.uti.json");

            GetEquippedWeaponResrefs(utc.RootElement)
                .Should()
                .Contain(expected.WeaponResref, $"{expected.Resref} should still equip the restored fast-cadence weapon");
            GetItemPropertyCost(weapon.RootElement, ItemPropertyDMG)
                .Should()
                .Be(expected.DMG, $"{expected.Resref} should use the restored Normal preset damage instead of delay-pressure nerfed damage");
        }
    }

    [Test]
    public void NPCResistanceThreats_CoverEveryResistanceFamily()
    {
        var root = FindRepositoryRoot();
        var templatesByFamily = ResistanceFamilies.ToDictionary(
            family => family,
            _ => new HashSet<string>());

        foreach (var file in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "utc"), "*.utc.json"))
        {
            using var utc = JsonDocument.Parse(File.ReadAllText(file));
            foreach (var feat in GetCreatureFeats(utc.RootElement))
            {
                if (ResistanceThreatFeats.TryGetValue(feat, out var family))
                    templatesByFamily[family].Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        foreach (var family in ResistanceFamilies)
        {
            templatesByFamily[family].Count
                .Should()
                .BeGreaterThanOrEqualTo(5, $"{family} needs enough authored NPC templates to feel like a real preparation choice");
        }
    }

    [Test]
    public void NPCResistanceVulnerabilities_AreCappedAndCoverEveryResistanceFamily()
    {
        var root = FindRepositoryRoot();
        var resistanceBySubtype = ResistanceFamilies.ToDictionary(type => (int)type, type => type);
        var vulnerableTemplatesByFamily = ResistanceFamilies.ToDictionary(
            family => family,
            _ => new HashSet<string>());

        foreach (var file in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "uti"), "*.uti.json"))
        {
            using var item = ReadJson(root, "Module", "uti", Path.GetFileName(file));
            if (GetItemPropertyCost(item.RootElement, ItemPropertyNPCLevel).HasValue == false)
                continue;

            foreach (var property in GetItemProperties(item.RootElement))
            {
                if (GetInt(property, "PropertyName") != ItemPropertyResistance)
                    continue;

                var subtype = GetInt(property, "Subtype");
                var costValue = GetInt(property, "CostValue");
                costValue.Should().BeGreaterThanOrEqualTo(0, $"{Path.GetFileName(file)} resistance CostValue must be a valid 2DA row id");

                var value = Resistance.DecodeItemPropertyCostTableValue(costValue);
                value.Should().BeGreaterThanOrEqualTo(-20, $"{Path.GetFileName(file)} resistance vulnerabilities should stay conservative this pass");

                if (value < 0 && resistanceBySubtype.TryGetValue(subtype, out var family))
                    vulnerableTemplatesByFamily[family].Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        foreach (var family in ResistanceFamilies)
        {
            vulnerableTemplatesByFamily[family].Count
                .Should()
                .BeGreaterThanOrEqualTo(5, $"{family} should have enough vulnerable NPC templates to make counter-picks visible");
        }
    }

    [Test]
    public void ResistanceItemProperties_UseResistanceCostTableRows()
    {
        var root = FindRepositoryRoot();
        var costTableRows = Read2DARows(root, "SWLOR_Haks", "sw_2da", "iprp_costtable.2da");
        var itempropdefRows = Read2DARows(root, "SWLOR_Haks", "sw_2da", "itempropdef.2da");
        var resistanceCostRows = Read2DARows(root, "SWLOR_Haks", "sw_2da", "iprp_swlrescost.2da");
        var resistanceAmountByRow = resistanceCostRows
            .ToDictionary(row => row.Id, row => int.Parse(row.Columns[3]));
        var customTlkTextById = ReadCustomTlkTextById(root);

        costTableRows.Single(row => row.Id == 7).Columns[1]
            .Should()
            .Be("IPRP_RESISTCOST", "cost table 7 is the base NWN damage-resistance table and must remain untouched");
        costTableRows.Single(row => row.Id == ResistanceCostTable).Columns[1]
            .Should()
            .Be("IPRP_SWLRESCOST", "SWLOR custom Resistance needs its own value table");

        itempropdefRows.Single(row => row.Id == ItemPropertyResistance).Columns[5]
            .Should()
            .Be(ResistanceCostTable.ToString(), "custom Resistance item properties should use SWLOR's custom resistance cost table");

        resistanceAmountByRow[0].Should().Be(0);
        resistanceAmountByRow[100].Should().Be(100);
        resistanceAmountByRow[105].Should().Be(-5);
        resistanceAmountByRow[120].Should().Be(-20);
        resistanceAmountByRow[200].Should().Be(-100);

        foreach (var row in resistanceCostRows.Where(row => int.Parse(row.Columns[3]) < 0))
        {
            var amount = int.Parse(row.Columns[3]);
            row.Columns[1].Should().NotBe("****", $"SWLOR resistance {amount} needs a display strref");

            if (int.Parse(row.Columns[1]) < CustomTlkOffset)
                continue;

            var tlkId = int.Parse(row.Columns[1]) - CustomTlkOffset;
            customTlkTextById.Should().ContainKey(tlkId, $"SWLOR resistance {amount} custom strref should exist in sw_tlk.tlk.json");
            customTlkTextById[tlkId].Should().Be(amount.ToString(), $"SWLOR resistance {amount} should display its signed amount");
        }

        foreach (var file in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "uti"), "*.uti.json"))
        {
            using var item = ReadJson(root, "Module", "uti", Path.GetFileName(file));
            foreach (var property in GetItemProperties(item.RootElement))
            {
                if (GetInt(property, "PropertyName") != ItemPropertyResistance)
                    continue;

                var costTable = GetInt(property, "CostTable");
                var costValue = GetInt(property, "CostValue");

                costTable.Should().Be(ResistanceCostTable, $"{Path.GetFileName(file)} Resistance property must point at iprp_swlrescost.2da");
                costValue.Should().BeGreaterThanOrEqualTo(0, $"{Path.GetFileName(file)} CostValue must be a cost-table row, not a signed gameplay amount");
                resistanceAmountByRow.Should().ContainKey(costValue, $"{Path.GetFileName(file)} CostValue {costValue} must exist in iprp_swlrescost.2da");
                resistanceAmountByRow[costValue]
                    .Should()
                    .Be(Resistance.DecodeItemPropertyCostTableValue(costValue), $"{Path.GetFileName(file)} encoded CostValue should decode to its 2DA Amount");
            }
        }
    }

    [Test]
    public void HutlarQionCreatures_PressureIceResistance()
    {
        var root = FindRepositoryRoot();
        var spawnSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "SpawnDefinition",
            "HutlarSpawnDefinition.cs"));

        var expectedAbilitiesByResref = new Dictionary<string, FeatType>
        {
            ["qion_slug"] = FeatType.GlacialSlime,
            ["qion_slug001"] = FeatType.HoarfrostGlob,
            ["qion_hive_tunnel"] = FeatType.PermafrostRupture,
            ["qion_tiger"] = FeatType.RimePounce,
            ["huthivebroodmoth"] = FeatType.CryoBile,
        };

        foreach (var (resref, feat) in expectedAbilitiesByResref)
        {
            AssertCreatureHasFeat(root, resref, feat);
            AssertCreatureDoesNotHaveFeat(root, resref, FeatType.FrostSpit);
            spawnSource.Should().Contain($"\"{resref}\"", $"{resref} should be reachable through Hutlar spawn definitions");
        }

        expectedAbilitiesByResref.Values.Should().OnlyHaveUniqueItems("each Hutlar Ice threat should use a distinct ability");
    }

    [Test]
    public void NewResistancePressureVariants_AreSpawnedAndHaveAuthoredAbilities()
    {
        var root = FindRepositoryRoot();
        var expectedVariants = new[]
        {
            (SpawnFile: "CZ220SpawnDefinition.cs", Resref: "czcryo_mynock", Feat: FeatType.FrostSpit),
            (SpawnFile: "HutlarSpawnDefinition.cs", Resref: "byysk_cryoadept", Feat: FeatType.HoarfrostGlob),
            (SpawnFile: "KorribanSpawnDefinition.cs", Resref: "korr_frostbind", Feat: FeatType.GlacialSlime),
        };

        foreach (var (spawnFile, resref, feat) in expectedVariants)
        {
            var spawnSource = File.ReadAllText(Path.Combine(
                root.FullName,
                "SWLOR.Game.Server",
                "Feature",
                "SpawnDefinition",
                spawnFile));

            AssertCreatureHasFeat(root, resref, feat);
            spawnSource.Should().Contain($"\"{resref}\"", $"{resref} should be reachable through its intended spawn definition");
        }
    }

    [Test]
    public void CZ220CoolantMynock_StaysStarterFriendly()
    {
        var root = FindRepositoryRoot();
        using var baseMynock = ReadJson(root, "Module", "utc", "mynock.utc.json");
        using var coolantMynock = ReadJson(root, "Module", "utc", "czcryo_mynock.utc.json");
        using var baseSkin = ReadJson(root, "Module", "uti", "mynock_sk.uti.json");
        using var coolantSkin = ReadJson(root, "Module", "uti", "czcryomyn_sk.uti.json");

        GetInt(coolantMynock.RootElement, "HitPoints").Should().Be(GetInt(baseMynock.RootElement, "HitPoints"));
        GetInt(coolantMynock.RootElement, "CurrentHitPoints").Should().Be(GetInt(baseMynock.RootElement, "CurrentHitPoints"));
        GetInt(coolantMynock.RootElement, "MaxHitPoints").Should().Be(GetInt(baseMynock.RootElement, "MaxHitPoints"));
        GetInt(coolantMynock.RootElement, "Str").Should().Be(GetInt(baseMynock.RootElement, "Str"));
        GetInt(coolantMynock.RootElement, "Dex").Should().Be(GetInt(baseMynock.RootElement, "Dex"));
        GetInt(coolantMynock.RootElement, "Con").Should().Be(GetInt(baseMynock.RootElement, "Con"));

        GetEquippedResref(coolantMynock.RootElement, CreatureWeaponSlot).Should().Be("mynock_wp");
        GetEquippedResref(coolantMynock.RootElement, CreatureArmorSlot).Should().Be("czcryomyn_sk");
        GetItemPropertyCost(coolantSkin.RootElement, ItemPropertyNPCLevel).Should().Be(GetItemPropertyCost(baseSkin.RootElement, ItemPropertyNPCLevel));
        GetItemPropertyCost(coolantSkin.RootElement, ItemPropertyNPCHP).Should().Be(GetItemPropertyCost(baseSkin.RootElement, ItemPropertyNPCHP));
        GetItemPropertyCost(coolantSkin.RootElement, ItemPropertyAttack).Should().Be(GetItemPropertyCost(baseSkin.RootElement, ItemPropertyAttack));
        GetItemPropertyCost(coolantSkin.RootElement, ItemPropertyForceAttack).Should().Be(GetItemPropertyCost(baseSkin.RootElement, ItemPropertyForceAttack));
        GetItemPropertyCost(coolantSkin.RootElement, ItemPropertyEvasion).Should().Be(GetItemPropertyCost(baseSkin.RootElement, ItemPropertyEvasion));

        GetCreatureFeats(coolantMynock.RootElement)
            .Intersect(ResistanceThreatFeats.Keys)
            .Should()
            .BeEquivalentTo(new[] { (int)FeatType.FrostSpit });
        GetJsonLocalInt(coolantMynock.RootElement, "QUEST_NPC_GROUP_ID")
            .Should()
            .Be((int)NPCGroupType.CZ220_Mynocks, "the starter-dungeon variant should count for Mynock kill quests");

        var spawnSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "SpawnDefinition",
            "CZ220SpawnDefinition.cs"))
            .Replace("\r\n", "\n");
        spawnSource.Should().Contain(".AddSpawn(ObjectType.Creature, \"czcryo_mynock\")\n                .WithFrequency(10)");
    }

    [Test]
    public void CZ220Droids_PressureElectricalResistance()
    {
        var root = FindRepositoryRoot();

        AssertCreatureHasFeat(root, "malsecdroid", FeatType.CapacitorSurge);
        AssertCreatureHasFeat(root, "malspiderdroid", FeatType.StaticWeb);
        AssertCreatureDoesNotHaveFeat(root, "malsecdroid", FeatType.IonBurst);
        AssertCreatureDoesNotHaveFeat(root, "malspiderdroid", FeatType.StaticBurst);
    }

    [Test]
    public void DroidEnemySkins_GrantTraumaImmunityForBleedResistance()
    {
        var root = FindRepositoryRoot();

        foreach (var (resref, skinResref) in ExpectedDroidEnemySkins)
        {
            using var utc = ReadJson(root, "Module", "utc", $"{resref}.utc.json");
            using var skin = ReadJson(root, "Module", "uti", $"{skinResref}.uti.json");

            GetEquippedResref(utc.RootElement, CreatureArmorSlot)
                .Should()
                .Be(skinResref, $"{resref} should use the droid stat skin carrying its Trauma immunity");
            GetItemPropertyCost(skin.RootElement, ItemPropertyResistance, (int)ResistanceType.Trauma)
                .Should()
                .Be(100, $"{skinResref} should make Bleed fail through the resistance system");
        }
    }

    [Test]
    public void KorribanForceCasters_PressureDisruptionResistance()
    {
        var root = FindRepositoryRoot();

        AssertCreatureHasFeat(root, "vkorrdunsorc", FeatType.ForceSunder);
        AssertCreatureHasFeat(root, "vkorrduninquis", FeatType.NullShock);
        AssertCreatureDoesNotHaveFeat(root, "vkorrdunsorc", FeatType.ForceRend);
        AssertCreatureDoesNotHaveFeat(root, "vkorrduninquis", FeatType.DarkShock);
    }

    private static void AssertCreatureHitPoints(JsonElement utc, ExpectedEnemy expected)
    {
        GetInt(utc, "CurrentHitPoints").Should().Be(expected.HP, expected.Resref);
        GetInt(utc, "HitPoints").Should().Be(expected.HP, expected.Resref);
        GetInt(utc, "MaxHitPoints").Should().Be(expected.HP, expected.Resref);
    }

    private static void AssertCreatureAttributes(JsonElement utc, ExpectedEnemy expected)
    {
        GetInt(utc, "Str").Should().Be(expected.Str, expected.Resref);
        GetInt(utc, "Dex").Should().Be(expected.Dex, expected.Resref);
        GetInt(utc, "Wis").Should().Be(expected.Wis, expected.Resref);
        GetInt(utc, "Con").Should().Be(expected.Con, expected.Resref);
        GetInt(utc, "Int").Should().Be(expected.Int, expected.Resref);
    }

    private static void AssertSkinCombatStats(JsonElement skin, ExpectedEnemy expected)
    {
        GetItemPropertyCost(skin, ItemPropertyNPCLevel).Should().Be(expected.Level, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyNPCHP).Should().Be(expected.HP, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyStamina).Should().Be(expected.Stamina, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyFP).Should().Be(expected.FP, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyAttack).Should().Be(expected.Attack, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyForceAttack).Should().Be(expected.ForceAttack, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyEvasion).Should().Be(expected.Evasion, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyDefense, PhysicalDefenseSubtype).Should().Be(expected.PhysicalDefense, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyDefense, ForceDefenseSubtype).Should().Be(expected.ForceDefense, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyDelay).Should().BeNull("attack delay belongs on equipped weapons, not creature armor");

        GetItemPropertySubtypes(skin, ItemPropertyResistance)
            .Should()
            .BeEquivalentTo(ResistanceSubtypes, expected.SkinResref);
    }

    private static void AssertWeaponStats(JsonElement weapon, ExpectedEnemy expected)
    {
        GetItemPropertyCost(weapon, ItemPropertyDMG).Should().Be(expected.WeaponDMG, expected.WeaponResref);
        GetItemPropertyCost(weapon, ItemPropertyDelay).Should().Be(expected.WeaponDelay, expected.WeaponResref);
    }

    private static JsonDocument ReadJson(DirectoryInfo root, params string[] pathParts)
    {
        var path = Path.Combine(new[] { root.FullName }.Concat(pathParts).ToArray());
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static int GetInt(JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetProperty("value").GetInt32();
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetProperty("value").GetString() ?? string.Empty;
    }

    private static string GetEquippedResref(JsonElement utc, int slot)
    {
        return utc
            .GetProperty("Equip_ItemList")
            .GetProperty("value")
            .EnumerateArray()
            .Where(entry => entry.GetProperty("__struct_id").GetInt32() == slot)
            .Select(entry => GetString(entry, "EquippedRes"))
            .SingleOrDefault();
    }

    private static string[] GetEquippedWeaponResrefs(JsonElement utc)
    {
        var weaponSlots = new[]
        {
            RightHandSlot,
            LeftHandSlot,
            CreatureLeftSlot,
            CreatureWeaponSlot,
            CreatureBiteSlot,
        };

        return weaponSlots
            .Select(slot => GetEquippedResref(utc, slot))
            .Where(resref => !string.IsNullOrWhiteSpace(resref))
            .ToArray()!;
    }

    private static int[] GetCreatureFeats(JsonElement utc)
    {
        return utc
            .GetProperty("FeatList")
            .GetProperty("value")
            .EnumerateArray()
            .Select(entry => GetInt(entry, "Feat"))
            .ToArray();
    }

    private static int GetJsonLocalInt(JsonElement utc, string variableName)
    {
        return utc
            .GetProperty("VarTable")
            .GetProperty("value")
            .EnumerateArray()
            .Where(entry => GetString(entry, "Name") == variableName)
            .Select(entry => GetInt(entry, "Value"))
            .Single();
    }

    private static void AssertCreatureHasFeat(DirectoryInfo root, string resref, FeatType feat)
    {
        using var utc = ReadJson(root, "Module", "utc", $"{resref}.utc.json");
        GetCreatureFeats(utc.RootElement)
            .Should()
            .Contain((int)feat, $"{resref} should pressure {ResistanceThreatFeats[(int)feat]} resistance");
    }

    private static void AssertCreatureDoesNotHaveFeat(DirectoryInfo root, string resref, FeatType feat)
    {
        using var utc = ReadJson(root, "Module", "utc", $"{resref}.utc.json");
        GetCreatureFeats(utc.RootElement)
            .Should()
            .NotContain((int)feat, $"{resref} should use its own authored resistance-pressure ability");
    }

    private static int? GetItemPropertyCost(JsonElement item, int propertyName, int? subtype = null)
    {
        return GetItemProperties(item)
            .Where(property =>
                GetInt(property, "PropertyName") == propertyName &&
                (!subtype.HasValue || GetInt(property, "Subtype") == subtype.Value))
            .Select(property => (int?)GetInt(property, "CostValue"))
            .SingleOrDefault();
    }

    private static int[] GetItemPropertySubtypes(JsonElement item, int propertyName)
    {
        return GetItemProperties(item)
            .Where(property => GetInt(property, "PropertyName") == propertyName)
            .Select(property => GetInt(property, "Subtype"))
            .OrderBy(subtype => subtype)
            .ToArray();
    }

    private static IEnumerable<JsonElement> GetItemProperties(JsonElement item)
    {
        return item
            .GetProperty("PropertiesList")
            .GetProperty("value")
            .EnumerateArray();
    }

    private static IReadOnlyList<TwoDARow> Read2DARows(DirectoryInfo root, params string[] pathParts)
    {
        var path = Path.Combine(new[] { root.FullName }.Concat(pathParts).ToArray());

        return File
            .ReadLines(path)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries))
            .Where(columns => columns.Length > 0 && int.TryParse(columns[0], out _))
            .Select(columns => new TwoDARow(int.Parse(columns[0]), columns))
            .ToList();
    }

    private static IReadOnlyDictionary<int, string> ReadCustomTlkTextById(DirectoryInfo root)
    {
        using var tlk = ReadJson(root, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json");

        return tlk.RootElement
            .GetProperty("entries")
            .EnumerateArray()
            .ToDictionary(entry => entry.GetProperty("id").GetInt32(), entry => entry.GetProperty("text").GetString() ?? string.Empty);
    }

    private static XDocument ReadWorkbookXml(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName);
        entry.Should().NotBeNull($"{entryName} should exist in the combat Bible workbook");

        using var stream = entry!.Open();
        return XDocument.Load(stream);
    }

    private static XDocument ReadWorksheetByName(ZipArchive archive, string sheetName)
    {
        var workbook = ReadWorkbookXml(archive, "xl/workbook.xml");
        var relationships = ReadWorkbookXml(archive, "xl/_rels/workbook.xml.rels");
        XNamespace workbookNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        var sheet = workbook
            .Descendants(workbookNs + "sheet")
            .Single(candidate => candidate.Attribute("name")?.Value == sheetName);
        var relationshipId = sheet.Attribute(relationshipNs + "id")?.Value;
        relationshipId.Should().NotBeNullOrWhiteSpace($"{sheetName} should have a workbook relationship id");

        var target = relationships
            .Descendants(packageRelationshipNs + "Relationship")
            .Single(candidate => candidate.Attribute("Id")?.Value == relationshipId)
            .Attribute("Target")?
            .Value
            .Replace('\\', '/');
        target.Should().NotBeNullOrWhiteSpace($"{sheetName} should resolve to a worksheet XML target");

        var entryName = target!.StartsWith("/", StringComparison.Ordinal)
            ? target.TrimStart('/')
            : $"xl/{target}";
        return ReadWorkbookXml(archive, entryName);
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
            return Array.Empty<string>();

        var sharedStrings = ReadWorkbookXml(archive, "xl/sharedStrings.xml");
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        return sharedStrings
            .Descendants(ns + "si")
            .Select(item => string.Concat(item.Descendants(ns + "t").Select(text => text.Value)))
            .ToArray();
    }

    private static string GetWorkbookCellText(XDocument worksheet, IReadOnlyList<string> sharedStrings, string address)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var cell = worksheet
            .Descendants(ns + "c")
            .SingleOrDefault(candidate => candidate.Attribute("r")?.Value == address);

        if (cell == null)
            return string.Empty;

        var type = cell.Attribute("t")?.Value;
        if (type == "inlineStr")
            return string.Concat(cell.Descendants(ns + "t").Select(text => text.Value));

        var value = cell.Element(ns + "v")?.Value;
        if (type == "s" && int.TryParse(value, out var index))
            return sharedStrings[index];

        return value ?? string.Empty;
    }

    private static decimal GetWorkbookCellNumber(XDocument worksheet, IReadOnlyList<string> sharedStrings, string address)
    {
        var text = GetWorkbookCellText(worksheet, sharedStrings, address);
        return decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private static string GetWorkbookCellFormula(XDocument worksheet, string address)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var formula = worksheet
            .Descendants(ns + "c")
            .SingleOrDefault(candidate => candidate.Attribute("r")?.Value == address)?
            .Element(ns + "f");

        if (formula == null)
            return string.Empty;

        return string.IsNullOrWhiteSpace(formula.Value)
            ? formula.Attribute("t")?.Value ?? string.Empty
            : formula.Value;
    }

    private static string GetWorkbookCellStyle(XDocument worksheet, string address)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return worksheet
            .Descendants(ns + "c")
            .SingleOrDefault(candidate => candidate.Attribute("r")?.Value == address)?
            .Attribute("s")?
            .Value ?? string.Empty;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private sealed record ExpectedEnemy(
        string Resref,
        string SkinResref,
        string WeaponResref,
        int Level,
        int HP,
        int Str,
        int Dex,
        int Wis,
        int Con,
        int Int,
        int Stamina,
        int FP,
        int Attack,
        int ForceAttack,
        int Evasion,
        int PhysicalDefense,
        int ForceDefense,
        int WeaponDMG,
        int WeaponDelay);

    private sealed record ExpectedDualWieldDamage(string Resref, int TotalDMG);

    private sealed record ExpectedRuntimeWeaponDamage(string Resref, string WeaponResref, int DMG);

    private sealed record TwoDARow(int Id, string[] Columns);
}
