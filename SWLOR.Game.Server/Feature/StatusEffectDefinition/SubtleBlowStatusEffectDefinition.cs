using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class SubtleBlowStatusEffectDefinition: IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            SubtleBlow1(builder);
            SubtleBlow2(builder);

            return builder.Build();
        }

        private static void SubtleBlow1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.SubtleBlow1)
                .Name("Subtle Blow I")
                .EffectIcon(135)
                .GrantAction((target, duration) =>
                {
                    if (StatusEffect.HasStatusEffect(target, StatusEffectType.SubtleBlow2))
                    {
                        StatusEffect.Remove(target, StatusEffectType.SubtleBlow1);
                        SendMessageToPC(target, "A more powerful effect is already active.");
                    }
                });
        }

        private static void SubtleBlow2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.SubtleBlow2)
                .Name("Subtle Blow II")
                .EffectIcon(136)
                .GrantAction((target, duration) =>
                {
                    if (StatusEffect.HasStatusEffect(target, StatusEffectType.SubtleBlow1))
                    {
                        StatusEffect.Remove(target, StatusEffectType.SubtleBlow1);
                    }
                });
        }

        [NWNEventHandler("item_on_hit")]
        public static void RestoreStamina()
        {
            var attacker = OBJECT_SELF;
            if (!GetIsPC(attacker) || GetIsDM(attacker)) return;

            var item = GetSpellCastItem();
            var target = GetSpellTargetObject();

            if (GetObjectType(target) != ObjectType.Creature) return;

            var itemType = GetBaseItemType(item);

            // Ranged weapons don't fire.
            if (itemType == BaseItem.ShortBow ||
                itemType == BaseItem.Longbow ||
                itemType == BaseItem.Sling ||
                itemType == BaseItem.Rifle ||
                itemType == BaseItem.LightCrossbow ||
                itemType == BaseItem.Bullet ||
                itemType == BaseItem.Bolt || 
                itemType == BaseItem.Arrow)
                return;

            var hasSubtleBlow1 = StatusEffect.HasStatusEffect(attacker, StatusEffectType.SubtleBlow1);
            var hasSubtleBlow2 = StatusEffect.HasStatusEffect(attacker, StatusEffectType.SubtleBlow2);

            if (!hasSubtleBlow1 &&
                !hasSubtleBlow2) return;

            var restoreAmount = hasSubtleBlow2 ? 4 : 2;

            var playerId = GetObjectUUID(attacker);
            var dbPlayer = DB.Get<Player>(playerId);
            Stat.RestoreStamina(attacker, dbPlayer, restoreAmount);
            DB.Set(playerId, dbPlayer);
        }
    }
}
