using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class PropertyInstancePlayersEngineTests
{
    /// <summary>
    /// A ship explosion kills everyone on this list, so it must name the players inside and nobody else. The native
    /// area keeps the last entering and last exiting objects separately, and an entry must read the former: the latter
    /// is OBJECT_INVALID for the first arrival and a departed player for anyone who follows them in.
    /// </summary>
    [EngineTest("A property instance lists the players who enter it, not the last one who left", Category = "PropertyInstancePlayers")]
    public static async Task ListFollowsEntriesAndExits(EngineTestContext ctx)
    {
        using var departed = await PlayerAbilityFixture.CreateAsync(ctx);
        using var crew = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
        var instance = ctx.CreateInstancedArea(GetResRef(ctx.Arena));
        var propertyId = "engine-instance-players/" + crew.Id;
        var instances = (IDictionary)typeof(Property)
            .GetField("_propertyInstances", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

        Property.RegisterInstance(propertyId, instance, default);
        try
        {
            ctx.Assert(Property.TryGetLoadedInstance(propertyId, out var loaded), "the instance is loaded");

            RunAreaEvent(instance, Property.EnterPropertyInstance, entered: crew.Creature, left: OBJECT_INVALID);
            ctx.Assert(loaded.Players.Contains(crew.Creature), "the first player in is listed although nobody has left yet");

            RunAreaEvent(instance, Property.EnterPropertyInstance, entered: crew.Creature, left: departed.Creature);
            ctx.Assert(!loaded.Players.Contains(departed.Creature), "a player who already left is not listed when someone else enters");
            ctx.AssertEqual(1, loaded.Players.Count, "entering twice lists a player once");

            RunAreaEvent(instance, Property.ExitPropertyInstance, entered: departed.Creature, left: crew.Creature);
            ctx.AssertEqual(0, loaded.Players.Count, "the player who exits comes off the list");

            // GetIsPC is false for a player logging out, as it is for this creature.
            var loggingOut = CreateObject(ObjectType.Creature, "thune", GetLocation(crew.Creature));
            ctx.Assert(!GetIsPC(loggingOut), "the stand-in does not read as a PC");
            loaded.Players.Add(loggingOut);
            RunAreaEvent(instance, Property.ExitPropertyInstance, entered: crew.Creature, left: loggingOut);
            DestroyObject(loggingOut);
            ctx.AssertEqual(0, loaded.Players.Count, "an exit that no longer reads as a PC still comes off the list");
        }
        finally
        {
            instances.Remove(propertyId);
        }
    }

    /// <summary>Dispatches a handler with the area's native last-entered and last-left objects set as given.</summary>
    private static void RunAreaEvent(uint area, System.Action handler, uint entered, uint left)
    {
        var nativeArea = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(area).AsNWSArea();
        nativeArea.m_oidLastEntered = entered;
        nativeArea.m_oidLastLeft = left;
        ScriptExecutionProvider.Current.ExecuteInScriptContext(handler, area);
    }
}
