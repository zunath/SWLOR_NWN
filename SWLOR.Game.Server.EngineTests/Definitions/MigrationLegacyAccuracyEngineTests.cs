using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    private static bool _startupLegacyPropertiesCaptured;
    private static string _startupLegacyPropertiesError;
    private static int _startupLegacyAccuracy;
    private static int _startupLegacyDamage;
    private static int _startupLegacyMarker;

    [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
    public static void CaptureLegacyPropertyLoadingBeforeStoredMigrations()
    {
        var settings = ApplicationSettings.Get();
        if (!settings.EngineTestsEnabled || !settings.ServerEnvironmentIsExplicit ||
            settings.ServerEnvironment == ServerEnvironmentType.Production)
            return;

        uint original = OBJECT_INVALID;
        uint loaded = OBJECT_INVALID;
        try
        {
            original = CreateObject(ObjectType.Item, "b_longsword", Location(GetFirstArea(), System.Numerics.Vector3.Zero, 0));
            if (!GetIsObjectValid(original))
                throw new System.InvalidOperationException("Could not create the startup legacy-property fixture.");
            ItemPlugin.SetBaseItemType(original, BaseItem.Lightsaber);
            AddLegacyProperty(original, ItemPropertyType.EnhancementBonus, 0, 2, 4);
            AddLegacyProperty(original, ItemPropertyType.AccuracyBonus, 0, 2, 6);
            AddLegacyProperty(original, ItemPropertyType.DamageBonus, 0, 4, FindFlatFiveDamageBonusRow());
            loaded = ObjectPlugin.Deserialize(ObjectPlugin.Serialize(original));
            if (!GetIsObjectValid(loaded))
                throw new System.InvalidOperationException("Could not load the startup legacy-property fixture.");
            _startupLegacyAccuracy = MeleePropertyTotal(loaded, ItemPropertyType.Accuracy);
            _startupLegacyDamage = Item.GetDMG(loaded);
            _startupLegacyMarker = GetLocalInt(loaded, LegacyItemProperties.ConvertedVariable);
            _startupLegacyPropertiesCaptured = true;
        }
        catch (System.Exception exception)
        {
            _startupLegacyPropertiesError = exception.ToString();
        }
        finally
        {
            if (GetIsObjectValid(original)) DestroyObject(original);
            if (GetIsObjectValid(loaded)) DestroyObject(loaded);
        }
    }

    [EngineTest("Native legacy property conversion precedes startup stored migrations", Category = "LegacyItemProperties")]
    public static Task LegacyPropertyConversionBeforeStoredMigrations(EngineTestContext ctx)
    {
        ctx.Assert(_startupLegacyPropertiesCaptured, $"Startup fixture was captured: {_startupLegacyPropertiesError}");
        ctx.AssertEqual(10, _startupLegacyAccuracy, "Legacy accuracy is retained before post-cache migrations start");
        ctx.AssertEqual(10, _startupLegacyDamage, "Baseline DMG 5 plus old flat damage 5 survive startup loading");
        ctx.AssertEqual(1, _startupLegacyMarker, "Startup loading records conversion for the existing migration to persist");
        return Task.CompletedTask;
    }

    [EngineTest("Native item loading preserves legacy accuracy through stored migration", Category = "LegacyItemProperties")]
    public static async Task LegacyAccuracyLoadAndStoredRetry(EngineTestContext ctx)
    {
        var executor = ctx.SpawnCreature("nw_rat001");
        var owner = ctx.SpawnCreature("nw_rat001");
        await ctx.WaitFrameAsync();
        var weapon = await CreateItemAsync(ctx, owner, "b_longsword", owner);
        string savedCreature = null;
        string itemIdentity = null;
        string creatureIdentity = null;
        await ctx.WaitFrameAsync();
        await ctx.ExecuteInCreatureContextAsync(executor, () =>
        {
            var originalProperties = new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var property = GetFirstItemProperty(weapon); GetIsItemPropertyValid(property); property = GetNextItemProperty(weapon))
                originalProperties.Add(property);

            // Add retired rows while a native passive property is still available
            // as the helper's duration/useability template.
            AddLegacyProperty(weapon, ItemPropertyType.EnhancementBonus, 0, 2, 4);
            AddLegacyProperty(weapon, ItemPropertyType.AccuracyBonus, 0, 2, 6);
            foreach (var property in originalProperties)
                Invoke("MigrationObject", "RemoveProperty", weapon, property);

            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, 5), weapon);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Delay, -1, 23), weapon);
            AddItemProperty(DurationType.Permanent,
                ItemPropertyCustom(ItemPropertyType.RequiresSkill, (int)SkillType.Vibroblade, 0), weapon);
            SetName(weapon, "Custom Basic Vibroblade");
            SetLocalString(weapon, "LEGACY_ACCURACY_FIXTURE", "keep");
            itemIdentity = GetObjectUUID(weapon);
            creatureIdentity = GetObjectUUID(owner);
            ctx.Assert(!string.IsNullOrWhiteSpace(itemIdentity), "The saved weapon has a native identity");
            ctx.Assert(!string.IsNullOrWhiteSpace(creatureIdentity), "The saved creature has a native identity");
            savedCreature = ObjectPlugin.Serialize(owner);
            DestroyObject(weapon);
            DestroyObject(owner);
        });
        await ctx.DelaySecondsAsync(0.3f);
        ctx.Assert(!GetIsObjectValid(weapon), "The original weapon releases its UUID before loading the saved creature");
        ctx.Assert(!GetIsObjectValid(owner), "The original creature releases its UUID before loading its saved copy");

        string loadedCreatureData = null;
        await ctx.ExecuteInCreatureContextAsync(executor, () =>
        {
            var loadedOwner = Deserialize(ctx, savedCreature);
            ctx.AssertEqual(false, GetIsPC(loadedOwner), "This creature fixture exercises item loading without claiming to be a player BIC");
            var loadedWeapon = GetFirstItemInInventory(loadedOwner);
            AssertLoadedLegacyAccuracy(ctx, loadedWeapon, itemIdentity);
            ctx.AssertEqual(creatureIdentity, GetObjectUUID(loadedOwner), "The loaded creature retains its identity");
            loadedCreatureData = ObjectPlugin.Serialize(loadedOwner);
            DestroyObject(loadedWeapon);
            DestroyObject(loadedOwner);
        });
        await ctx.DelaySecondsAsync(0.3f);

        string savedMigratedCreature = null;
        await ctx.ExecuteInCreatureContextAsync(executor, () =>
        {
            var (changed, migratedData) = MigrateStoredData(loadedCreatureData);
            ctx.Assert(changed, "The native conversion marker causes the persisted creature migration");
            ctx.Assert(ContainsIdentity(migratedData, creatureIdentity), "Stored migration preserves creature identity");
            ctx.Assert(ContainsIdentity(migratedData, itemIdentity), "Stored migration preserves weapon identity");
            var migratedOwner = Deserialize(ctx, migratedData);
            var migratedWeapon = GetFirstItemInInventory(migratedOwner);
            ctx.AssertEqual(creatureIdentity, GetObjectUUID(migratedOwner), "Migrated creature identity");
            AssertMigratedAccuracy(ctx, migratedWeapon, itemIdentity);
            savedMigratedCreature = ObjectPlugin.Serialize(migratedOwner);
            DestroyObject(migratedWeapon);
            DestroyObject(migratedOwner);
        });
        await ctx.DelaySecondsAsync(0.3f);

        await ctx.ExecuteInCreatureContextAsync(executor, () =>
        {
            var (changed, retryData) = MigrateStoredData(savedMigratedCreature);
            ctx.Assert(!changed, "The persisted accuracy migration is idempotent");
            ctx.AssertEqual(savedMigratedCreature, retryData, "Retry leaves the saved creature payload unchanged");
            var retriedOwner = Deserialize(ctx, retryData);
            var retriedWeapon = GetFirstItemInInventory(retriedOwner);
            ctx.AssertEqual(creatureIdentity, GetObjectUUID(retriedOwner), "Retry preserves creature identity");
            AssertMigratedAccuracy(ctx, retriedWeapon, itemIdentity);
        });
    }

    [EngineTest("Chiro Electroblade preserves legacy accuracy and Ice through stored migration", Category = "LegacyItemProperties")]
    public static async Task ChiroElectrobladeAccuracyAndRetry(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("nw_rat001");
        await ctx.WaitFrameAsync();
        var weapon = await CreateItemAsync(ctx, owner, "chi_electroblade", owner);
        string savedData = null;
        string migratedData = null;
        string identity = null;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            var originalProperties = new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var property = GetFirstItemProperty(weapon); GetIsItemPropertyValid(property); property = GetNextItemProperty(weapon))
                originalProperties.Add(property);

            // Reproduce the live Crashing Wave profile before its first native load.
            AddLegacyProperty(weapon, ItemPropertyType.DMG, (int)CombatDamageType.Physical, 34, 28);
            AddLegacyProperty(weapon, ItemPropertyType.DMG, (int)CombatDamageType.Ice, 34, 2);
            AddLegacyProperty(weapon, ItemPropertyType.AccuracyBonus, 0, 2, 5);
            AddLegacyProperty(weapon, ItemPropertyType.UseLimitationPerk, 18, 33, 5);
            foreach (var property in originalProperties)
                Invoke("MigrationObject", "RemoveProperty", weapon, property);
            AddItemProperty(DurationType.Permanent,
                ItemPropertyOnHitCastSpell(OnHitCastSpellType.ONHIT_UNIQUEPOWER, 40), weapon);
            SetName(weapon, "Crashing Wave - Chiro Electroblade");
            SetLocalString(weapon, "CHIRO_ACCURACY_FIXTURE", "keep");
            identity = GetObjectUUID(weapon);
            ctx.Assert(!string.IsNullOrWhiteSpace(identity), "The original Electroblade has a saved identity");
            savedData = ObjectPlugin.Serialize(weapon);
            DestroyObject(weapon);
        });
        await ctx.DelaySecondsAsync(0.3f);
        ctx.Assert(!GetIsObjectValid(weapon), "The original Electroblade releases its UUID before migration");

        uint migrated = OBJECT_INVALID;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            var (changed, data) = MigrateStoredData(savedData);
            ctx.Assert(changed, "The legacy Electroblade requires migration");
            ctx.Assert(ContainsIdentity(data, identity), "Migration retains the serialized item identity");
            migrated = Deserialize(ctx, data);
            AssertChiroElectroblade(ctx, migrated, identity);
        });
        await ctx.DelaySecondsAsync(0.3f);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            AssertChiroElectroblade(ctx, migrated, identity);
            migratedData = ObjectPlugin.Serialize(migrated);
            DestroyObject(migrated);
        });
        await ctx.DelaySecondsAsync(0.3f);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            var (changed, data) = MigrateStoredData(migratedData);
            ctx.Assert(!changed, "Retry does not migrate the Electroblade again");
            ctx.AssertEqual(migratedData, data, "Retry leaves the saved Electroblade unchanged");
            AssertChiroElectroblade(ctx, Deserialize(ctx, data), identity);
        });
    }

    private static void AssertChiroElectroblade(EngineTestContext ctx, uint item, string identity)
    {
        ctx.AssertEqual(BaseItem.Electroblade, GetBaseItemType(item), "The original base item type remains Electroblade");
        ctx.AssertEqual(5, MeleePropertyTotal(item, ItemPropertyType.Accuracy), "Native Accuracy Bonus +5 becomes custom Accuracy +5");
        ctx.AssertEqual(-1, PropertyValue(item, ItemPropertyType.AccuracyBonus), "The retired accuracy row is replaced");
        ctx.AssertEqual(26, MeleePropertyTotal(item, ItemPropertyType.DMG), "Physical 28 plus Ice 2 follow the current balanced damage scale");
        AssertDamageType(ctx, item, CombatDamageType.Ice);
        ctx.AssertEqual(24, PropertyValue(item, ItemPropertyType.Delay), "The Electroblade uses Delay 240");
        ctx.AssertEqual(50, PropertyValue(item, ItemPropertyType.RequiresSkill, (int)SkillType.Lightsaber), "The Chiro skill requirement is Lightsaber 50");
        ctx.AssertEqual(-1, PropertyValue(item, ItemPropertyType.UseLimitationPerk), "The retired proficiency requirement is replaced");
        ctx.AssertEqual((int)OnHitCastSpellType.ONHIT_UNIQUEPOWER,
            FindPropertySubtype(item, ItemPropertyType.OnHitCastSpell), "The unique on-hit power survives");
        ctx.AssertEqual("Level40", Get2DAString("iprp_spellcstr", "Label", PropertyValue(item, ItemPropertyType.OnHitCastSpell)),
            "The on-hit power retains caster level 40");
        ctx.AssertEqual(identity, GetObjectUUID(item), "The saved item identity survives");
        ctx.AssertEqual("Crashing Wave - Chiro Electroblade", GetName(item), "The custom name survives");
        ctx.AssertEqual("keep", GetLocalString(item, "CHIRO_ACCURACY_FIXTURE"), "Unrelated local data survives");
        ctx.AssertEqual(0, GetLocalInt(item, LegacyItemProperties.ConvertedVariable), "The existing migration consumes the native conversion marker");
    }

    private static void AssertLoadedLegacyAccuracy(EngineTestContext ctx, uint item, string identity)
    {
        ctx.AssertEqual(identity, GetObjectUUID(item), "Native load preserves weapon identity");
        ctx.AssertEqual(10, MeleePropertyTotal(item, ItemPropertyType.Accuracy), "The loader combines legacy values 4 and 6");
        ctx.AssertEqual(-1, PropertyValue(item, ItemPropertyType.EnhancementBonus), "Native EnhancementBonus row is converted");
        ctx.AssertEqual(-1, PropertyValue(item, ItemPropertyType.AccuracyBonus), "Native AccuracyBonus row is converted");
        ctx.AssertEqual(1, GetLocalInt(item, LegacyItemProperties.ConvertedVariable), "Native load sets the conversion marker");
        ctx.AssertEqual(5, PropertyValue(item, ItemPropertyType.DMG), "Native load preserves baseline damage");
        ctx.AssertEqual(23, PropertyValue(item, ItemPropertyType.Delay), "Native load preserves baseline delay");
        ctx.AssertEqual(0, PropertyValue(item, ItemPropertyType.RequiresSkill, (int)SkillType.Vibroblade),
            "Native load preserves the baseline requirement");
        ctx.AssertEqual("Custom Basic Vibroblade", GetName(item), "Native load preserves the custom name");
        ctx.AssertEqual("keep", GetLocalString(item, "LEGACY_ACCURACY_FIXTURE"), "Native load preserves unrelated local data");
    }

    private static void AssertMigratedAccuracy(EngineTestContext ctx, uint item, string identity)
    {
        ctx.AssertEqual(identity, GetObjectUUID(item), "Stored migration preserves weapon identity");
        ctx.AssertEqual(10, MeleePropertyTotal(item, ItemPropertyType.Accuracy), "Converted Accuracy total persists");
        ctx.AssertEqual(-1, PropertyValue(item, ItemPropertyType.EnhancementBonus), "No legacy EnhancementBonus row remains");
        ctx.AssertEqual(-1, PropertyValue(item, ItemPropertyType.AccuracyBonus), "No legacy AccuracyBonus row remains");
        ctx.AssertEqual(0, GetLocalInt(item, LegacyItemProperties.ConvertedVariable), "The conversion marker is consumed");
        ctx.AssertEqual(5, PropertyValue(item, ItemPropertyType.DMG), "Stored migration preserves baseline damage");
        ctx.AssertEqual(23, PropertyValue(item, ItemPropertyType.Delay), "Stored migration preserves baseline delay");
        ctx.AssertEqual(0, PropertyValue(item, ItemPropertyType.RequiresSkill, (int)SkillType.Vibroblade),
            "Stored migration preserves the baseline requirement");
        ctx.AssertEqual("Custom Basic Vibroblade", GetName(item), "Stored migration preserves the custom name");
        ctx.AssertEqual("keep", GetLocalString(item, "LEGACY_ACCURACY_FIXTURE"), "Stored migration preserves unrelated local data");
    }
}
