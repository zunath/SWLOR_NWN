using System.Linq;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>Gives each playable race its own appearance and that race's first head.</summary>
internal sealed class _2_CorrectAppearanceType : ICharacterMigration
{
    public int Version => 2;

    public void Migrate(SavedCharacter character)
    {
        var male = character.ReadInteger("Gender") == (int)Gender.Male;
        IRacialAppearanceDefinition definition;
        AppearanceType appearance;
        switch ((RacialType)character.ReadInteger("Race"))
        {
            case RacialType.Human: appearance = AppearanceType.Human; definition = new HumanRacialAppearanceDefinition(); break;
            case RacialType.Bothan: appearance = AppearanceType.Bothan; definition = new BothanRacialAppearanceDefinition(); break;
            case RacialType.Chiss: appearance = AppearanceType.Chiss; definition = new ChissRacialAppearanceDefinition(); break;
            case RacialType.Zabrak: appearance = AppearanceType.Zabrak; definition = new ZabrakRacialAppearanceDefinition(); break;
            case RacialType.Wookiee: appearance = AppearanceType.Wookiee; definition = new WookieeRacialAppearanceDefinition(); break;
            case RacialType.Twilek: appearance = AppearanceType.Twilek; definition = new TwilekRacialAppearanceDefinition(); break;
            case RacialType.Cathar: appearance = AppearanceType.Cathar; definition = new CatharRacialAppearanceDefinition(); break;
            case RacialType.Trandoshan: appearance = AppearanceType.Trandoshan; definition = new TrandoshanRacialAppearanceDefinition(); break;
            case RacialType.Mirialan: appearance = AppearanceType.Mirialan; definition = new MirialanRacialAppearanceDefinition(); break;
            case RacialType.Echani: appearance = AppearanceType.Echani; definition = new EchaniRacialAppearanceDefinition(); break;
            case RacialType.MonCalamari: appearance = AppearanceType.MonCalamari; definition = new MonCalamariRacialAppearanceDefinition(); break;
            case RacialType.Ugnaught: appearance = AppearanceType.Ugnaught; definition = new UgnaughtRacialAppearanceDefinition(); break;
            case RacialType.Togruta: appearance = AppearanceType.Togruta; definition = new TogrutaRacialAppearanceDefinition(); break;
            case RacialType.Rodian: appearance = AppearanceType.Rodian; definition = new RodianRacialAppearanceDefinition(); break;
            case RacialType.KelDor: appearance = AppearanceType.KelDor; definition = new KelDorRacialAppearanceDefinition(); break;
            default: return;
        }
        character.WriteInteger("Appearance_Type", (int)appearance, 2);
        character.SetCreatureBodyPart("Appearance_Head", male ? definition.MaleHeads.First() : definition.FemaleHeads.First());
        if (character.AppliesRecordEffects)
            character.Record.OriginalAppearanceType = appearance;
    }
}

/// <summary>Login applies the player movement rate (<see cref="Service.Stat.ApplyPlayerStats"/>), so a saved character needs no change.</summary>
internal sealed class _3_AdjustPlayerSpeeds : ICharacterMigration
{
    public int Version => 3;
    public void Migrate(SavedCharacter character) { }
}

internal sealed class _5_UpdatePerks : ICharacterMigration
{
    public int Version => 5;
    public void Migrate(SavedCharacter character) => character.RemoveFeat(FeatType.RapidShot);
}

internal sealed class _6_RecalculateStats : ICharacterMigration
{
    public int Version => 6;
    public void Migrate(SavedCharacter character) => CharacterRecalculation.RecalculateStats(character);
}

internal sealed class _7_FixDroids : ICharacterMigration
{
    public int Version => 7;

    public void Migrate(SavedCharacter character)
    {
        CharacterRecalculation.RecalculateStats(character);
        if (character.ReadInteger("Race") != (int)RacialType.Droid)
            return;
        if (character.AppliesRecordEffects)
            character.Record.OriginalAppearanceType = AppearanceType.Droid;
        character.WriteInteger("Appearance_Type", (int)AppearanceType.Droid, 2);
    }
}

/// <summary>Native saving throws were wrong after 2DA changes; SWLOR keeps them at 0.</summary>
internal sealed class _9_FixSavingThrows : ICharacterMigration
{
    public int Version => 9;
    public void Migrate(SavedCharacter character) => character.SetBaseSavingThrows(0);
}

internal sealed class _10_SPAdjustments : ICharacterMigration
{
    private static readonly int[] RemovedFeats =
    {
        1350, 1351, 1352, 1353, // ForcePush1-4
        1359, 1360, 1361, // ThrowLightsaber1-3
        1364, 1365, 1366, // ForceStun1-3
        1371, 1372, // BattleInsight1-2
        1373, 1374, // MindTrick1-2
        1820, 1821, // Premonition1-2
        1850, 1851, 1852, 1853, 1854, // ThrowRock1-5
        1866, 1867, 1868, // ForceInspiration1-3
        1734, 1735, 1736, 1737, 1738, // ForceHeal1-5
        1719, 1720, 1721, 1722, // ForceBurst1-4
        1727, 1728, // ForceMind1-2
        1825, 1826, 1827, // Benevolence1-3
        1828, 1829, // ForceValor1-2
        1729, 1730, 1731, 1732, 1733, // ForceDrain1-5
        1723, 1724, 1725, 1726, // ForceLightning1-4
        1717, 1718, // ForceBody1-2
        1833, 1834, 1835, // CreepingTerror1-3
        1836, 1837, // FuryStance1-2
    };

    public int Version => 10;

    public void Migrate(SavedCharacter character)
    {
        foreach (var feat in RemovedFeats)
            character.RemoveFeat((FeatType)feat);
    }
}

internal sealed class _11_RemoveMobility : ICharacterMigration
{
    public int Version => 11;
    public void Migrate(SavedCharacter character) => character.RemoveFeat(FeatType.Mobility);
}

/// <summary>Refunds perks whose owned level is above the level the character meets the requirements for.</summary>
internal sealed class _12_RemoveDecayedPerks : ICharacterMigration
{
    public int Version => 12;

    public void Migrate(SavedCharacter character)
    {
        foreach (var (perkType, level) in character.Record.Perks.ToList())
        {
            if (level != CharacterRecalculation.GetEffectivePerkLevel(character, perkType))
                CharacterRecalculation.RefundPerk(character, perkType);
        }
    }
}
