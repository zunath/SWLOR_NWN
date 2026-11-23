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
}
