using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Service;

namespace SWLOR.Game.Server.Tests.Feature;

public class HelmetRenderingTransitionTests
{
    [TestCase(true, false)]
    [TestCase(true, true)]
    [TestCase(false, false)]
    public void RefreshDeliversNativeHelmetAttachmentChanges(bool projected, bool projectionAlreadyApplied)
    {
        var harness = DMPossessionHarness.Compile($$"""
            using static Engine;
            public static class EquippedItemAppearance
            {
                private const float QuickbarRefreshDelaySeconds = .2f;
                {{DMPossessionHarness.Method("Feature/AppearanceDefinition/ItemAppearance/EquippedItemAppearance.cs", "Refresh")}}
                private static void RefreshInventorySlot(Player player, object gui, uint creature, uint item, bool clearIcon) { }
                private static void RestoreQuickbarReferences(uint creature, uint item) { }
                private static void RefreshHandAppearance(uint creature, uint item) { }
            }
            public enum InventorySlot { Head, Chest, LeftHand, RightHand }
            public static class NWNXLib { public static Manager g_pAppManager = new(); }
            public class Manager { public Server m_pServerExoApp = new(); }
            public class Server
            {
                public GameObject GetGameObject(uint id) => new();
                public Message GetNWSMessage() => Transport;
                public Player[] GetPlayerList() => [Observer];
                public void SetForceUpdate() { }
            }
            public class GameObject
            {
                public Creature AsNWSCreature() => Wearer;
                public Item AsNWSItem() => new();
            }
            public class Item { public uint m_oidPossessor = 1; }
            public class Inventory { public int GetItemInInventory(Item item) => 1; }
            public class Appearance
            {
                public uint m_oidHeadItem = 2;
                public uint m_oidChestItem, m_oidCloakItem, m_oidLeftHandItem, m_oidRightHandItem;
            }
            public class Creature
            {
                public Inventory m_pInventory = new();
                public Appearance m_cAppearance = new();
                public void UpdateAppearanceForEquippedItems() =>
                    m_cAppearance.m_oidHeadItem = Hidden ? OBJECT_INVALID : 2;
            }
            public class Player
            {
                public Appearance m_cAppearance = new();
                public object m_pInventoryGUI, m_pOtherInventoryGUI;
                public Player GetLastUpdateObject(uint creature) => this;
            }
            public class Message
            {
                public int Destroyed;
                public void SendServerPlayerItemUpdate_DestroyItem(Player player, uint item) => Destroyed++;
            }
            public static class RobeAppearance { public static void RemoveUnavailableRobe(uint creature, uint item) { } }
            public static class NeckAppearance { public static void RemoveUnavailableNeck(uint creature, uint item) { } }
            public static class Droid { public static void UpdateEquippedItemSnapshot(uint creature, uint item) { } }
            public static class TintMapModelResolver { public static object GetCurrentSelections(uint creature) => null; }
            public static class HelmetModelRenderer
            {
                public static void Apply(uint creature, object selections)
                {
                    Hidden = Projected;
                    Wearer.m_cAppearance.m_oidHeadItem = Projected ? OBJECT_INVALID : 2;
                }
            }
            public static class TintMapService
            {
                public static void ApplyCurrentColors(uint creature, bool resetShaderOverrides) => HelmetModelRenderer.Apply(creature, null);
            }
            public static class Engine
            {
                public static Creature Wearer = new();
                public static Player Observer = new();
                public static Message Transport = new();
                public static bool Hidden, Projected;
                public static uint GetItemInSlot(InventorySlot slot, uint creature) => slot == InventorySlot.Head ? 2u : OBJECT_INVALID;
                public static void DelayCommand(float seconds, Action action) { }
            }
            public static class Regression
            {
                public static void Run(bool projected, bool projectionAlreadyApplied)
                {
                    Projected = projected;
                    if (projectionAlreadyApplied) HelmetModelRenderer.Apply(1, null);
                    EquippedItemAppearance.Refresh(1, 2);

                    // The client still has the native attachment from model64. The
                    // normal creature delta can remove it only if its old cache survives.
                    uint clientAttachment = 2;
                    if (Observer.m_cAppearance.m_oidHeadItem != Wearer.m_cAppearance.m_oidHeadItem)
                        clientAttachment = Wearer.m_cAppearance.m_oidHeadItem;
                    if (clientAttachment != (projected ? OBJECT_INVALID : 2))
                        throw new Exception("The client never received the native helmet detach.");
                    if (Transport.Destroyed != (projected ? 0 : 1))
                        throw new Exception("Only an attachment that remains equipped needs an item replacement.");
                }
            }
            """);

        var run = () => harness.Invoke("Regression", "Run", projected, projectionAlreadyApplied);
        run.Should().NotThrow();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NativeHelmetRebuildsItsAttachmentAfterAProjectedHelmet(bool canonicalHeadAlreadyRestored)
    {
        var harness = DMPossessionHarness.Compile($$"""
            using System.Globalization;
            using static Engine;
            {{DMPossessionHarness.Type("Feature/AppearanceDefinition/TintMap/HelmetModelRenderer.cs", "HelmetModelRenderer")}}

            public enum InventorySlot { Head }
            public enum Gender { Female, Male }
            public enum ObjectVisualTransform { Scale }
            public enum ObjectVisualTransformDataScopeType { CreatureHead }
            public class TintMapMaterialSelection
            {
                public bool IsWornHelmet => true;
                public uint PaletteSource => 2;
                public uint CreaturePaletteSource => 1;
                public string ModelResref { get; init; }
            }
            public static class ModelResource { public static bool Exists(string model) => true; }
            public static class NWNXLib { public static Manager g_pAppManager = new(); }
            public class Manager { public Server m_pServerExoApp = new(); }
            public class Server
            {
                public Creature GetCreatureByGameObjectID(uint id) => Wearer;
                public void SetForceUpdate() { }
            }
            public class Stats { public ushort m_nHeadVariation = 121; }
            public class Appearance
            {
                public ushort m_nHeadVariation = 121;
                public uint m_oidHeadItem = 2;
            }
            public class Creature
            {
                public Stats m_pStats = new();
                public Appearance m_cAppearance = new();
                public void UpdateAppearanceForEquippedItems() =>
                    m_cAppearance.m_oidHeadItem = Hidden ? OBJECT_INVALID : 2;
            }
            public static class Engine
            {
                public static Creature Wearer = new();
                public static bool Hidden;
                public static float Scale = 1.2f;
                public static Dictionary<string, int> Locals = new();
                public static uint GetItemInSlot(InventorySlot slot, uint creature) => 2;
                public static int GetHiddenWhenEquipped(uint item) => Hidden ? 1 : 0;
                public static void SetHiddenWhenEquipped(uint item, bool hidden) => Hidden = hidden;
                public static int GetLocalInt(uint item, string key) => Locals.GetValueOrDefault(key);
                public static void SetLocalInt(uint item, string key, int value) => Locals[key] = value;
                public static void DeleteLocalInt(uint item, string key) => Locals.Remove(key);
                public static int GetAppearanceType(uint creature) => 6;
                public static Gender GetGender(uint creature) => Gender.Female;
                public static string Get2DAString(string table, string column, int row) => column switch
                {
                    "MODELTYPE" => "P", "RACE" => "H", _ => "0.85"
                };
                public static float GetObjectVisualTransform(uint creature, ObjectVisualTransform transform,
                    ObjectVisualTransformDataScopeType nScope) => Scale;
                public static void SetObjectVisualTransform(uint creature, ObjectVisualTransform transform,
                    float scale, ObjectVisualTransformDataScopeType nScope) => Scale = scale;
            }
            public static class Regression
            {
                public static void Run(bool canonicalHeadAlreadyRestored)
                {
                    var tintable = new[] { new TintMapMaterialSelection { ModelResref = "helm_063" } };
                    HelmetModelRenderer.Apply(1, tintable);
                    Require(Wearer.m_cAppearance.m_nHeadVariation == 1063 && Hidden,
                        "Control helmet must render through its head");

                    // A native head refresh can precede the tint refresh. The render-only
                    // hidden flag and scaled head still belong to the earlier projection.
                    if (canonicalHeadAlreadyRestored)
                        Wearer.m_cAppearance.m_nHeadVariation = Wearer.m_pStats.m_nHeadVariation;
                    HelmetModelRenderer.Apply(1, Array.Empty<TintMapMaterialSelection>());
                    Require(Wearer.m_cAppearance.m_nHeadVariation == 121 &&
                            Wearer.m_cAppearance.m_oidHeadItem == 2 && !Hidden,
                        "The non-tintable helmet must regain its visible native attachment");
                    Require(Math.Abs(Scale - 1.2f) < .0001f, "The player's head size must be restored");

                    HelmetModelRenderer.SetShownByOwner(2, false);
                    HelmetModelRenderer.Apply(1, Array.Empty<TintMapMaterialSelection>());
                    Require(Wearer.m_cAppearance.m_oidHeadItem == OBJECT_INVALID && Hidden,
                        "Hiding a native helmet must remove its cached attachment");
                    HelmetModelRenderer.SetShownByOwner(2, true);
                    HelmetModelRenderer.Apply(1, Array.Empty<TintMapMaterialSelection>());
                    Require(Wearer.m_cAppearance.m_oidHeadItem == 2 && !Hidden,
                        "Showing a native helmet must restore its cached attachment");

                    tintable[0] = new TintMapMaterialSelection { ModelResref = "helm_065" };
                    HelmetModelRenderer.Apply(1, tintable);
                    Require(Wearer.m_cAppearance.m_nHeadVariation == 1065 && Hidden,
                        "A later tintable helmet must render without relogging");
                    HelmetModelRenderer.SetShownByOwner(2, false);
                    HelmetModelRenderer.Apply(1, tintable);
                    Require(Wearer.m_cAppearance.m_nHeadVariation == 121 &&
                            Wearer.m_cAppearance.m_oidHeadItem == OBJECT_INVALID && Hidden &&
                            !HelmetModelRenderer.IsShownByOwner(2), "Explicit owner hiding must survive restoration");
                    HelmetModelRenderer.SetShownByOwner(2, true);
                    HelmetModelRenderer.Apply(1, tintable);
                    Require(Wearer.m_cAppearance.m_nHeadVariation == 1065 &&
                            HelmetModelRenderer.IsShownByOwner(2), "Showing must restore the later helmet");
                    Require(Wearer.m_pStats.m_nHeadVariation == 121, "Canonical head must remain unchanged");
                }
                private static void Require(bool value, string message)
                {
                    if (!value) throw new Exception(message);
                }
            }
            """);

        var run = () => harness.Invoke("Regression", "Run", canonicalHeadAlreadyRestored);
        run.Should().NotThrow();
    }
}
