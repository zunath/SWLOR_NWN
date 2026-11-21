using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    /// <summary>
    /// The client's targeting data for each ability feat (the shape and size of the targeting cursor), set when a player
    /// enters, when a DM possesses a creature and when a perk changes a size. Split from <c>AbilityTargeting</c>, whose
    /// target queries moved to Combat.
    /// </summary>
    public static class AbilityClientTargeting
    {
        public static IReadOnlyDictionary<FeatType, AbilityTargetingDetail> GetAllTargetingDetails()
        {
            return AbilityTargetingCache.GetAll();
        }

        public static void RefreshClientTargeting(uint creature)
        {
            if (!IsClientControlled(creature))
                return;

            foreach (var (feat, targeting) in AbilityTargetingCache.GetAll())
            {
                var hasFeat = QuickCast.KnowsFeat(creature, feat);
                var sizeX = targeting.ResolveSizeX(creature, hasFeat);
                var sizeY = targeting.ResolveSizeY();
                ValidateResolvedTargeting(feat, targeting, sizeX, sizeY);

                SetSpellTargetingData(
                    creature,
                    targeting.Spell,
                    (int)targeting.Shape,
                    sizeX,
                    sizeY,
                    (int)targeting.Flags);
            }
        }

        [PlayerSession(PlayerReload.LoginOnly,
            "The client targeting data stays in the engine.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RefreshClientTargetingOnEnter()
        {
            RefreshClientTargeting(GetEnteringObject());
        }

        [NWNEventHandler(ScriptName.OnDMPossessBefore)]
        [NWNEventHandler(ScriptName.OnDMPossessFullPowerBefore)]
        public static void RefreshClientTargetingOnDMPossess()
        {
            var target = StringToObject(EventsPlugin.GetEventData("TARGET"));
            var creature = GetIsObjectValid(target)
                ? target
                : GetMaster(OBJECT_SELF);

            if (!GetIsObjectValid(creature))
                return;

            DelayCommand(0.1f, () => RefreshClientTargeting(creature));
        }

        private static bool IsClientControlled(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   (GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature));
        }

        private static void ValidateResolvedTargeting(
            FeatType feat,
            AbilityTargetingDetail targeting,
            float sizeX,
            float sizeY)
        {
            AbilityTargetingCacheService.ValidateSize(feat, sizeX, sizeY);

            if (!Enum.IsDefined(typeof(AbilityTargetingShapeType), targeting.Shape))
            {
                throw new InvalidOperationException(
                    $"Ability '{feat}' resolved unsupported targeting shape '{targeting.Shape}'.");
            }
        }
    }
}
