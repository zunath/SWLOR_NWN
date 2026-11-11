using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>Selects qualified rifle support clips for the current rendered hands and weapon.</summary>
public static class RifleSupportAnimation
{
    private static RifleSupportAnimationCatalog _catalog = new(Array.Empty<(string, string, int, RifleSupportPoses)>());
    private static readonly RifleSupportAnimationPlayback Playback = new(new RifleSupportAnimationRuntime());
    private static bool _registered;

    [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
    public static void Load()
    {
        var rows = new List<(string, string, int, RifleSupportPoses)>();
        for (var row = 0; row < Get2DARowCount("riflegrips"); row++)
            rows.Add((Get2DAString("riflegrips", "RIGHT", row), Get2DAString("riflegrips", "LEFT", row),
                int.Parse(Get2DAString("riflegrips", "MODEL", row)), new RifleSupportPoses(
                    Get2DAString("riflegrips", "IDLE", row), Get2DAString("riflegrips", "READY", row),
                    Get2DAString("riflegrips", "SHOT", row))));
        _catalog = new RifleSupportAnimationCatalog(rows);
        if (_registered) return;
        NamedAnimation.EquipmentMappingsReleased += Playback.Refresh;
        _registered = true;
    }

    public static void Apply(uint creature) => Playback.Apply(creature, CurrentPoses(creature));

    public static AnimationClip ResolveAuthored(uint creature, AnimationClip clip) =>
        clip.Name == AuthoredAnimation.AimedShot.Name
            ? CurrentPoses(creature)?.ResolveAuthored(clip) ?? clip : clip;

    private static RifleSupportPoses CurrentPoses(uint creature)
    {
        if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature) return null;
        var rifle = GetItemInSlot(InventorySlot.RightHand, creature);
        if (!GetIsObjectValid(rifle) || GetBaseItemType(rifle) != BaseItem.Rifle ||
            WeaponFit.GetPart(rifle, AppearanceWeapon.Top) != 101 || WeaponFit.GetPart(rifle, AppearanceWeapon.Bottom) != 101)
            return null;
        return _catalog.Resolve(WeaponGripHandRenderer.GetHandModel(creature, CreaturePart.RightHand, "r"),
            WeaponGripHandRenderer.GetHandModel(creature, CreaturePart.LeftHand, "l"),
            WeaponFit.GetPart(rifle, AppearanceWeapon.Middle));
    }

    [NWNEventHandler(ScriptName.OnModuleHeartbeat)]
    public static void Prune() => Playback.Prune();
}
