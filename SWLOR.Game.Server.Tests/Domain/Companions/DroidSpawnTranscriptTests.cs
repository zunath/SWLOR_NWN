using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>
/// The ordered transcript of summoning a droid, taken before the summon is cut into the spawn and restore services: every call that reaches
/// the engine, the four plugins and the thirteen neighbours, in the order the code makes it, with the saved data the controller holds before
/// and after. The scenarios are a controller with nothing saved, a saved appearance with its tint overrides and portrait and sound set, a saved
/// loadout (equipped gear, carried items, a stored container and a pending return, an item the droid cannot take and its retry on the next
/// summon) and an open AI window with no custom name. Items carry the droid item id they were saved with, so the identifiers are fixed;
/// the retry of an item that has none (a new identifier by <c>Guid</c>) is not part of it. Regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c>
/// only for a deliberate change.
/// </summary>
[TestFixture]
public class DroidSpawnTranscriptTests
{
    private const string Path = "SWLOR.Game.Server.Tests/Fixtures/CompanionTranscripts/DroidSpawn.txt";

    private sealed class Scene : IDisposable
    {
        private readonly IDisposable _random;

        public Scene()
        {
            Log = new OrderedLog();
            Log.Name(DroidWorld.Module, "Module");
            Log.Name(DroidWorld.Player, "Player");
            Log.Name(DroidWorld.Controller, "Controller");
            Log.Name(DroidWorld.Area, "Area");
            Log.Quiet.UnionWith(new[]
            {
                "GetFirstItemProperty", "GetNextItemProperty", "GetIsItemPropertyValid", "GetItemPropertyType", "GetItemPropertySubType",
                "GetItemPropertyCostTableValue"
            });
            World = new DroidWorld(Log);
            Log.Resolver = id => id == World.Droid ? "Droid" : null;
            // The personality picks its greeting with the shared generator: seeded, every draw is written to the transcript.
            _random = RecordingRandom.Install(20261123, Log.Lines, id => Log.Format(id));
            Service = new TestDroid(World);
            Service.CacheData();
        }

        public OrderedLog Log { get; }
        public DroidWorld World { get; }
        public TestDroid Service { get; }

        public void ConfigureController()
        {
            World.AddStat(DroidWorld.Controller, DroidStatSubType.Tier, 3);
            World.AddStat(DroidWorld.Controller, DroidStatSubType.HP, 40);
            World.AddStat(DroidWorld.Controller, DroidStatSubType.STM, 7);
            World.AddStat(DroidWorld.Controller, DroidStatSubType.MGT, 11);
            World.AddStat(DroidWorld.Controller, DroidStatSubType.PER, 12);
            World.AddStat(DroidWorld.Controller, DroidStatSubType.VIT, 13);
            World.AddStat(DroidWorld.Controller, DroidStatSubType.WIL, 14);
            World.AddStat(DroidWorld.Controller, DroidStatSubType.AGI, 15);
            World.AddStat(DroidWorld.Controller, DroidStatSubType.SOC, 16);
            World.AddStat(DroidWorld.Controller, DroidStatSubType.ResistanceFire, 5);
            World.AddStat(DroidWorld.Controller, DroidStatSubType.ResistanceMind, 3);
            World.AddStat(DroidWorld.Controller, DroidStatSubType.Armor, 2);
            World.AddStat(DroidWorld.Controller, DroidStatSubType.Rifle, 4);
            World.AddProperty(DroidWorld.Controller, ItemPropertyType.DroidPersonality, (int)DroidPersonalityType.Sarcastic, 0);
            World.AddStat(DroidWorld.Controller, DroidStatSubType.AISlots, 2);
            Save(new ConstructedDroid { Name = "Beepy" });
        }

        public ConstructedDroid Saved() => Service.Droids.LoadConstructedDroid(DroidWorld.Controller);

        public void Save(ConstructedDroid droid) => Service.Droids.SaveConstructedDroid(DroidWorld.Controller, droid);

        public string Blob(string resRef, BaseItem baseItem, string itemId, bool container = false)
        {
            var blob = World.Blob(resRef, baseItem, container);
            World.ItemIds[blob] = itemId;
            return blob;
        }

        public void Dispose()
        {
            World.Dispose();
            _random.Dispose();
        }
    }

    private static void Summon(Scene scene, StringBuilder text, string name)
    {
        scene.Log.Lines.Clear();
        text.Append("== ").Append(name).Append('\n');
        scene.Service.Spawn.SpawnDroid(DroidWorld.Player, DroidWorld.Controller);
        scene.Log.Lines.Add("-- the delayed commands run");
        scene.World.RunDeferred();
        foreach (var line in scene.Log.Lines)
            text.Append(line).Append('\n');
        scene.Log.Lines.Clear();
        text.Append("-- saved after the summon: ").Append(Describe(scene.Saved())).Append('\n');
    }

    private static string Describe(ConstructedDroid saved) =>
        $"name={saved.Name} portrait={saved.PortraitId} sound={saved.SoundSetId} equipped=[{string.Join(",", saved.EquippedItems.OrderBy(pair => (int)pair.Key).Select(pair => pair.Key))}] " +
        $"inventory=[{string.Join(",", saved.Inventory.Keys.OrderBy(key => key, StringComparer.Ordinal))}] parts={saved.AppearanceParts.Count} tint={saved.TintOverrides.Count}";

    private static void SaveLoadout(Scene scene, ConstructedDroid saved)
    {
        saved.EquippedItems[InventorySlot.RightHand] = scene.Blob("oph_rifle", BaseItem.Rifle, "id-rifle");
        saved.EquippedItems[InventorySlot.Belt] = scene.Blob("belt_plain", BaseItem.Belt, "id-belt");
        saved.Inventory["medpac-id"] = scene.Blob("medpac", BaseItem.Potions, "id-medpac");
        saved.Inventory["bag"] = scene.Blob("bag", BaseItem.MiscSmall, "id-bag", container: true);
        saved.Inventory["DROID_RETURN_ITEM:abc"] = scene.Blob("ration", BaseItem.MiscSmall, "id-ration");
        saved.Inventory["unreadable"] = "not an item";
    }

    [Test]
    public void SummoningADroidProducesTheCapturedTranscript()
    {
        var text = new StringBuilder();

        using (var scene = new Scene())
        {
            scene.ConfigureController();
            Summon(scene, text, "nothing saved on the controller");
        }

        using (var scene = new Scene())
        {
            scene.ConfigureController();
            var saved = scene.Saved();
            saved.AppearanceParts[CreaturePart.Head] = 55;
            saved.AppearanceParts[CreaturePart.LeftFoot] = 66;
            saved.TintOverrides["TINT_A"] = 9;
            saved.TintOverrides["TINT_B"] = 0;
            saved.PortraitId = 5;
            saved.SoundSetId = 6;
            scene.Save(saved);
            Summon(scene, text, "a saved appearance, tint overrides, portrait and sound set");
        }

        using (var scene = new Scene())
        {
            scene.ConfigureController();
            var saved = scene.Saved();
            SaveLoadout(scene, saved);
            scene.Save(saved);
            Summon(scene, text, "a saved loadout: equipped gear, carried items, a stored container and a pending return");
        }

        using (var scene = new Scene())
        {
            scene.ConfigureController();
            var saved = scene.Saved();
            SaveLoadout(scene, saved);
            scene.Save(saved);
            scene.World.RefusedBlobs.Add(saved.EquippedItems[InventorySlot.RightHand]);
            scene.World.RefusedBlobs.Add(saved.Inventory["medpac-id"]);
            Summon(scene, text, "the droid refuses two items: they are kept under a retry key");
            scene.World.RefusedBlobs.Clear();
            Summon(scene, text, "the next summon retries them");
        }

        using (var scene = new Scene())
        {
            scene.ConfigureController();
            scene.Save(new ConstructedDroid { Name = string.Empty });
            scene.World.OpenWindows = true;
            Summon(scene, text, "the AI window is open and the droid has no custom name");
        }

        CombatTranscriptBench.AssertGolden(Path, text.ToString());
    }

    [Test]
    public void TheSavedItemSetIsRestoredToTheSameSlotsAndCarriedItems()
    {
        using var scene = new Scene();
        scene.ConfigureController();
        var saved = scene.Saved();
        SaveLoadout(scene, saved);
        saved.AppearanceParts[CreaturePart.Head] = 55;
        saved.PortraitId = 5;
        saved.SoundSetId = 6;
        scene.Save(saved);

        scene.Service.Spawn.SpawnDroid(DroidWorld.Player, DroidWorld.Controller);

        var world = scene.World;
        var droid = world.Droid;
        world.ResRefOf(world.InSlot(droid, InventorySlot.RightHand)).Should().Be("oph_rifle");
        world.ResRefOf(world.InSlot(droid, InventorySlot.Belt)).Should().Be("belt_plain");
        world.PossessorOf(world.InSlot(droid, InventorySlot.RightHand)).Should().Be(droid);
        world.Inventory(droid).Select(world.ResRefOf).Should().Equal("medpac");
        world.Inventory(DroidWorld.Player).Select(world.ResRefOf).Should().BeEquivalentTo("bag", "ration");
        var after = scene.Saved();
        after.EquippedItems.Keys.Should().BeEquivalentTo(new[] { InventorySlot.RightHand, InventorySlot.Belt });
        after.Inventory.Keys.Should().Equal("medpac-id");
        after.AppearanceParts[CreaturePart.Head].Should().Be(55);
        after.PortraitId.Should().Be(5);
        after.SoundSetId.Should().Be(6);
        world.Locals.String(world.InSlot(droid, InventorySlot.RightHand), "DROID_ITEM_ID").Should().Be("id-rifle");
    }
}
