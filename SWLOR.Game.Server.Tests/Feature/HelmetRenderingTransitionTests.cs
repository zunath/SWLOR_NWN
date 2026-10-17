using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Service;

namespace SWLOR.Game.Server.Tests.Feature;

public class HelmetRenderingTransitionTests
{
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
