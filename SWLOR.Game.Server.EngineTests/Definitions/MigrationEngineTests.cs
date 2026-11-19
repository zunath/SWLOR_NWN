using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        [EngineTest("Migration removes equipped item properties and their native effects", Category = "Migration")]
        public static async Task EquippedPropertyRemoval(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.DelaySecondsAsync(0.5f);
            var weapon = await ctx.EquipItemAsync(owner, "nw_wswls001", InventorySlot.RightHand);
            var original = GetAbilityScore(owner, AbilityType.Might);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
                AddItemProperty(DurationType.Permanent, ItemPropertyAbilityBonus(AbilityType.Might, 4), weapon));
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(original + 4, GetAbilityScore(owner, AbilityType.Might), "Equipped fixture applies its native ability bonus");
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                for (var property = GetFirstItemProperty(weapon); GetIsItemPropertyValid(property); property = GetNextItemProperty(weapon))
                {
                    if (GetItemPropertyType(property) != ItemPropertyType.AbilityBonus)
                        continue;
                    Invoke("MigrationObject", "RemoveProperty", weapon, property);
                    break;
                }
                ctx.AssertEqual(-1, PropertyValue(weapon, ItemPropertyType.AbilityBonus), "The property is removed immediately");
            });
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(original, GetAbilityScore(owner, AbilityType.Might), "Its equipped effect is removed too");
        }

        private static async Task<uint> CreateItemAsync(EngineTestContext ctx, uint actor, string resref, uint possessor)
        {
            var item = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                item = CreateItemOnObject(resref, possessor);
                ctx.Assert(GetIsObjectValid(item), $"Could not create test item {resref}.");
                ctx.Track(item);
            });
            await ctx.WaitFrameAsync();
            return item;
        }
        private static uint Deserialize(EngineTestContext ctx, string data)
        {
            var item = ObjectPlugin.Deserialize(data);
            ctx.Assert(GetIsObjectValid(item), "Serialized test item must deserialize.");
            ctx.Track(item);
            return item;
        }

        private static object Invoke(string type, string method, params object[] args) =>
            typeof(SWLOR.Game.Server.Service.Migration).Assembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition." + type)
                .GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);

        private static bool ContainsIdentity(string serializedObject, string identity) =>
            System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(serializedObject))
                .Contains(identity, System.StringComparison.Ordinal);

        // Read Redis directly so these assertions cannot pass on a mutated cache object
        // whose database write was omitted.
        private static T ReadPersisted<T>(string id) where T : EntityBase =>
            JsonConvert.DeserializeObject<T>(DB.GetRawJson<T>(id));

        private static void AssertDamageType(EngineTestContext ctx, uint item, CombatDamageType expected)
        {
            var types = new List<int>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                if (GetItemPropertyType(ip) == ItemPropertyType.WeaponDamageType)
                    types.Add(GetItemPropertySubType(ip));
            // Physical damage is the default and carries no damage-type property.
            var expectedTypes = expected == CombatDamageType.Physical ? new int[0] : new[] { (int)expected };
            ctx.Assert(types.SequenceEqual(expectedTypes),
                $"Expected {(expected == CombatDamageType.Physical ? "no" : $"exactly one {expected}")} damage-type property; found {string.Join(", ", types)}.");
        }

        private static int MeleePropertyTotal(uint item, ItemPropertyType type)
        {
            var total = 0;
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
                if (GetItemPropertyType(property) == type) total += GetItemPropertyCostTableValue(property);
            return total;
        }

        private static void ClearProperties(uint item)
        {
            var properties = new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                properties.Add(ip);
            foreach (var ip in properties)
                RemoveItemProperty(item, ip);
        }

        private static void AddLegacyProperty(uint item, ItemPropertyType type, int subtype, int table, int value)
        {
            // Stamp the serialized property list directly; current item-property
            // constructors reject some retired subtype and cost-table combinations.
            var nativeItem = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(item).AsNWSItem();
            var template = nativeItem.GetPassiveProperty(0);
            using var property = new global::NWN.Native.API.CNWItemProperty
            {
                m_nPropertyName = (ushort)type, m_nSubType = (ushort)subtype,
                m_nCostTable = (byte)table, m_nCostTableValue = (ushort)value,
                m_nParam1 = 255, m_nParam1Value = 255, m_nChanceOfAppearing = 100,
                m_nDurationType = template.m_nDurationType, m_bUseable = template.m_bUseable,
                m_nID = 1000000 + (ulong)type
            };
            nativeItem.AddPassiveProperty(property);
        }

        private static int PropertyValue(uint item, ItemPropertyType type, int? subtype = null)
        {
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                if (GetItemPropertyType(ip) == type && (!subtype.HasValue || GetItemPropertySubType(ip) == subtype.Value))
                    return GetItemPropertyCostTableValue(ip);
            return -1;
        }

        private static string DescribeProperties(uint item)
        {
            var properties = new List<string>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                properties.Add($"{GetItemPropertyType(ip)}:{GetItemPropertySubType(ip)}:{GetItemPropertyCostTableValue(ip)}");
            return string.Join(", ", properties);
        }

    }
}
