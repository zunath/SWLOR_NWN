using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Moves anything that still uses a retired appearance (<see cref="RetiredAppearances"/>) to one that still ships,
/// so a part picked between the usage audit and the release keeps rendering. A retired armor part becomes the editor's
/// bare option, which shows the wearer's own body part: 0 ("None"), or 1 on shins, whose list has no 0. A retired
/// creature body part becomes 1, the bare limb, or 0 on shoulders and belts, which bodies do not have. A retired head
/// becomes the first head the race offers, a retired helmet becomes helmet 1, and a retired cloak becomes the Plain
/// cloak (row 1).
/// Runs in the release's enzyme visit (player migration 26, server migration 31).
/// </summary>
public static class RetiredAppearanceMigration
{
    public const int HelmetReplacement = 1;
    public const int CloakReplacement = 1;
    private const int HeadReplacement = 1;

    /// <summary>The GFF part names an item saves (ArmorPart_*) and a creature saves (BodyPart_*), with their part 2DA.</summary>
    public static IReadOnlyList<(string Slot, RetiredArmorPart Part)> Slots { get; } = new[]
    {
        ("Neck", RetiredArmorPart.Neck), ("Torso", RetiredArmorPart.Torso), ("Belt", RetiredArmorPart.Belt),
        ("Pelvis", RetiredArmorPart.Pelvis), ("Robe", RetiredArmorPart.Robe),
        ("LShoul", RetiredArmorPart.Shoulder), ("RShoul", RetiredArmorPart.Shoulder),
        ("LBicep", RetiredArmorPart.Bicep), ("RBicep", RetiredArmorPart.Bicep),
        ("LFArm", RetiredArmorPart.Forearm), ("RFArm", RetiredArmorPart.Forearm),
        ("LHand", RetiredArmorPart.Hand), ("RHand", RetiredArmorPart.Hand),
        ("LThigh", RetiredArmorPart.Thigh), ("RThigh", RetiredArmorPart.Thigh),
        ("LShin", RetiredArmorPart.Shin), ("RShin", RetiredArmorPart.Shin),
        ("LFoot", RetiredArmorPart.Foot), ("RFoot", RetiredArmorPart.Foot),
    };

    /// <summary>
    /// The model letter (appearance.2da RACE) of every parts-based appearance a player or DM template can have: the
    /// native dynamic races and the playable races.
    /// </summary>
    public static IReadOnlyDictionary<int, char> ModelLetters { get; } = new Dictionary<int, char>
    {
        [0] = 'd', [1] = 'e', [2] = 'g', [3] = 'a', [4] = 'h', [5] = 'o', [6] = 'h',
        [(int)AppearanceType.Bothan] = 'e', [(int)AppearanceType.Chiss] = 'h', [(int)AppearanceType.Zabrak] = 'h',
        [(int)AppearanceType.Wookiee] = 'e', [(int)AppearanceType.Twilek] = 'h', [(int)AppearanceType.Cyborg] = 'h',
        [(int)AppearanceType.Cathar] = 'o', [(int)AppearanceType.Trandoshan] = 'h', [(int)AppearanceType.Mirialan] = 'h',
        [(int)AppearanceType.Echani] = 'h', [(int)AppearanceType.MonCalamari] = 'h', [(int)AppearanceType.Ugnaught] = 'd',
        [(int)AppearanceType.Rodian] = 'e', [(int)AppearanceType.Togruta] = 'h', [(int)AppearanceType.KelDor] = 'h',
        [(int)AppearanceType.Droid] = 'z', [(int)AppearanceType.Nautolan] = 'h', [(int)AppearanceType.Ewok] = 'd',
    };

    // Built once at type initialization, before any worker thread converts saved data.
    private static readonly IReadOnlyDictionary<(int Appearance, bool Female), int> RaceHeads = BuildRaceHeads();

    private static Dictionary<(int, bool), int> BuildRaceHeads()
    {
        var heads = new Dictionary<(int, bool), int>();
        foreach (var appearance in RacialAppearanceRegistry.GetAppearanceTypes())
        {
            RacialAppearanceRegistry.TryGet(appearance, out var definition);
            heads[((int)appearance, false)] = definition.MaleHeads[0];
            heads[((int)appearance, true)] = definition.FemaleHeads[0];
        }
        return heads;
    }

    /// <summary>The style an armor item saves after the migration.</summary>
    public static int ReplaceArmorPart(RetiredArmorPart part, int style) =>
        RetiredAppearances.ArmorParts[part].Contains(style) ? part == RetiredArmorPart.Shin ? 1 : 0 : style;

    /// <summary>The body part a creature saves after the migration.</summary>
    public static int ReplaceBodyPart(RetiredArmorPart part, int style) =>
        RetiredAppearances.ArmorParts[part].Contains(style)
            ? part is RetiredArmorPart.Shoulder or RetiredArmorPart.Belt ? 0 : 1
            : style;

    /// <summary>The head a creature saves after the migration. Gender 1 is female; any other gender uses male models.</summary>
    public static int ReplaceHead(int appearance, int gender, int head)
    {
        var female = gender == 1;
        if (!ModelLetters.TryGetValue(appearance, out var letter) ||
            !RetiredAppearances.Heads.TryGetValue($"p{(female ? 'f' : 'm')}{letter}", out var retired) ||
            !retired.Contains(head))
            return head;
        return RaceHeads.TryGetValue((appearance, female), out var replacement) ? replacement : HeadReplacement;
    }

    public static int ReplaceHelmet(int model) => RetiredAppearances.Helmets.Contains(model) ? HelmetReplacement : model;

    public static int ReplaceCloak(int row) => RetiredAppearances.Cloaks.Contains(row) ? CloakReplacement : row;

    /// <summary>
    /// The species whose heads a saved character uses. A character saved while flying has its ship's appearance;
    /// leaving space restores the species kept on its record (<see cref="Player.OriginalAppearanceType"/>).
    /// </summary>
    public static int SpeciesAppearance(int savedAppearance, Player record) =>
        !ModelLetters.ContainsKey(savedAppearance) && record != null &&
        ModelLetters.ContainsKey((int)record.OriginalAppearanceType)
            ? (int)record.OriginalAppearanceType
            : savedAppearance;

    /// <summary>
    /// Updates a constructed droid's saved body (its controller's <c>AppearanceParts</c>), which the summon reapplies.
    /// Constructed droids are male droid bodies. Returns the original text when nothing is retired.
    /// </summary>
    public static string MigrateDroidAppearance(string droidJson)
    {
        if (string.IsNullOrWhiteSpace(droidJson)) return droidJson;
        var droid = JObject.Parse(droidJson);
        if (droid["AppearanceParts"] is not JObject parts) return droidJson;
        var changed = false;
        foreach (var entry in parts.Properties())
        {
            if (!Enum.TryParse<CreaturePart>(entry.Name, out var creaturePart) ||
                entry.Value.Type != JTokenType.Integer)
                continue;
            var current = (int)entry.Value;
            var replacement = creaturePart == CreaturePart.Head
                ? ReplaceHead((int)AppearanceType.Droid, 0, current)
                : DroidParts.TryGetValue(creaturePart, out var part) ? ReplaceBodyPart(part, current) : current;
            if (replacement == current) continue;
            entry.Value = replacement;
            changed = true;
        }
        return changed ? droid.ToString(Formatting.None) : droidJson;
    }

    private static readonly IReadOnlyDictionary<CreaturePart, RetiredArmorPart> DroidParts = new Dictionary<CreaturePart, RetiredArmorPart>
    {
        [CreaturePart.Neck] = RetiredArmorPart.Neck, [CreaturePart.Torso] = RetiredArmorPart.Torso,
        [CreaturePart.Belt] = RetiredArmorPart.Belt, [CreaturePart.Pelvis] = RetiredArmorPart.Pelvis,
        [CreaturePart.LeftShoulder] = RetiredArmorPart.Shoulder, [CreaturePart.RightShoulder] = RetiredArmorPart.Shoulder,
        [CreaturePart.LeftBicep] = RetiredArmorPart.Bicep, [CreaturePart.RightBicep] = RetiredArmorPart.Bicep,
        [CreaturePart.LeftForearm] = RetiredArmorPart.Forearm, [CreaturePart.RightForearm] = RetiredArmorPart.Forearm,
        [CreaturePart.LeftHand] = RetiredArmorPart.Hand, [CreaturePart.RightHand] = RetiredArmorPart.Hand,
        [CreaturePart.LeftThigh] = RetiredArmorPart.Thigh, [CreaturePart.RightThigh] = RetiredArmorPart.Thigh,
        [CreaturePart.LeftShin] = RetiredArmorPart.Shin, [CreaturePart.RightShin] = RetiredArmorPart.Shin,
        [CreaturePart.LeftFoot] = RetiredArmorPart.Foot, [CreaturePart.RightFoot] = RetiredArmorPart.Foot,
    };

    /// <summary>Keeps an outfit's listed part numbers in step with its saved garment and helmet.</summary>
    public static bool UpdateOutfitIds(PlayerOutfit outfit)
    {
        var changed = false;
        void Update(RetiredArmorPart part, Func<int> read, Action<int> write)
        {
            var current = read();
            var replacement = ReplaceArmorPart(part, current);
            if (replacement == current) return;
            write(replacement);
            changed = true;
        }
        Update(RetiredArmorPart.Neck, () => outfit.NeckId, value => outfit.NeckId = value);
        Update(RetiredArmorPart.Torso, () => outfit.TorsoId, value => outfit.TorsoId = value);
        Update(RetiredArmorPart.Belt, () => outfit.BeltId, value => outfit.BeltId = value);
        Update(RetiredArmorPart.Pelvis, () => outfit.PelvisId, value => outfit.PelvisId = value);
        Update(RetiredArmorPart.Robe, () => outfit.RobeId, value => outfit.RobeId = value);
        Update(RetiredArmorPart.Bicep, () => outfit.LeftBicepId, value => outfit.LeftBicepId = value);
        Update(RetiredArmorPart.Bicep, () => outfit.RightBicepId, value => outfit.RightBicepId = value);
        Update(RetiredArmorPart.Foot, () => outfit.LeftFootId, value => outfit.LeftFootId = value);
        Update(RetiredArmorPart.Foot, () => outfit.RightFootId, value => outfit.RightFootId = value);
        Update(RetiredArmorPart.Forearm, () => outfit.LeftForearmId, value => outfit.LeftForearmId = value);
        Update(RetiredArmorPart.Forearm, () => outfit.RightForearmId, value => outfit.RightForearmId = value);
        Update(RetiredArmorPart.Hand, () => outfit.LeftHandId, value => outfit.LeftHandId = value);
        Update(RetiredArmorPart.Hand, () => outfit.RightHandId, value => outfit.RightHandId = value);
        Update(RetiredArmorPart.Shin, () => outfit.LeftShinId, value => outfit.LeftShinId = value);
        Update(RetiredArmorPart.Shin, () => outfit.RightShinId, value => outfit.RightShinId = value);
        Update(RetiredArmorPart.Shoulder, () => outfit.LeftShoulderId, value => outfit.LeftShoulderId = value);
        Update(RetiredArmorPart.Shoulder, () => outfit.RightShoulderId, value => outfit.RightShoulderId = value);
        Update(RetiredArmorPart.Thigh, () => outfit.LeftThighId, value => outfit.LeftThighId = value);
        Update(RetiredArmorPart.Thigh, () => outfit.RightThighId, value => outfit.RightThighId = value);
        var helmet = ReplaceHelmet(outfit.HelmetId);
        if (helmet != outfit.HelmetId)
        {
            outfit.HelmetId = helmet;
            changed = true;
        }
        return changed;
    }
}
