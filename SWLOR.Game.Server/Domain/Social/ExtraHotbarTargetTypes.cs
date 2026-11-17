using System.Globalization;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// What an ability or an item can target, read from the feat and spell tables, and the object types the targeting mode takes.
    /// </summary>
    internal static class ExtraHotbarTargetTypes
    {
        // spells.2da TargetType bits.
        public const int TargetSelf = 0x01;
        public const int TargetCreature = 0x02;
        public const int TargetGround = 0x04;
        public const int TargetItem = 0x08;
        public const int TargetDoor = 0x10;
        public const int TargetPlaceable = 0x20;

        public static int GetFeatTargetTypes(FeatType feat)
        {
            if (Get2DAString("feat", "TARGETSELF", (int)feat) == "1")
                return TargetSelf;

            return int.TryParse(Get2DAString("feat", "SPELLID", (int)feat), out var spellId)
                ? GetSpellTargetTypes(spellId)
                : TargetCreature;
        }

        public static int GetSpellTargetTypes(int spellId)
        {
            if (spellId < 0)
                return TargetSelf;

            var text = Get2DAString("spells", "TargetType", spellId);
            if (text.StartsWith("0x") &&
                int.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
                return hex;

            return int.TryParse(text, out var value) ? value : TargetSelf;
        }

        public static ObjectType ToObjectTypes(int targetTypes)
        {
            var objectTypes = (ObjectType)0;
            if ((targetTypes & (TargetSelf | TargetCreature)) != 0)
                objectTypes |= ObjectType.Creature;
            if ((targetTypes & TargetItem) != 0)
                objectTypes |= ObjectType.Item;
            if ((targetTypes & TargetDoor) != 0)
                objectTypes |= ObjectType.Door;
            if ((targetTypes & TargetPlaceable) != 0)
                objectTypes |= ObjectType.Placeable;
            if ((targetTypes & TargetGround) != 0)
                objectTypes |= ObjectType.Tile;

            return objectTypes == 0 ? ObjectType.Creature : objectTypes;
        }
    }
}
