using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Service
{
    public class Droid
    {
        private static readonly Dictionary<int, Dictionary<PerkType, int>> _defaultPerksByTier = new();

        [NWNEventHandler("mod_cache")]
        public static void CacheData()
        {
            CacheDefaultTierPerks();
        }

        private static void CacheDefaultTierPerks()
        {
            // Standard perks to give droids per level.
            for (var level = 1; level <= 5; level++)
            {
                _defaultPerksByTier[level] = new Dictionary<PerkType, int>()
                {
                    { PerkType.VibrobladeProficiency, level},
                    { PerkType.FinesseVibrobladeProficiency, level},
                    { PerkType.HeavyVibrobladeProficiency, level},
                    { PerkType.PolearmProficiency, level},
                    { PerkType.TwinBladeProficiency, level},
                    { PerkType.KatarProficiency, level},
                    { PerkType.StaffProficiency, level},
                    { PerkType.PistolProficiency, level},
                    { PerkType.RifleProficiency, level},
                    { PerkType.CloakProficiency, level},
                    { PerkType.BeltProficiency, level},
                    { PerkType.RingProficiency, level},
                    { PerkType.NecklaceProficiency, level},
                    { PerkType.ShieldProficiency, level},
                    { PerkType.BreastplateProficiency, level},
                    { PerkType.HelmetProficiency, level},
                    { PerkType.BracerProficiency, level},
                    { PerkType.LeggingProficiency, level},
                    { PerkType.TunicProficiency, level},
                    { PerkType.CapProficiency, level},
                    { PerkType.GloveProficiency, level},
                    { PerkType.BootProficiency, level},
                };
            }

            // Tier 1
            _defaultPerksByTier[1][PerkType.WeaponFocusVibroblades] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusFinesseVibroblades] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusHeavyVibroblades] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusPolearms] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusTwinBlades] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusKatars] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusStaves] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusPistols] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusRifles] = 1;

            // Tier 2
            _defaultPerksByTier[2][PerkType.WeaponFocusVibroblades] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusFinesseVibroblades] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusHeavyVibroblades] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusPolearms] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusTwinBlades] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusKatars] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusStaves] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusPistols] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusRifles] = 2;

            // Tier 3
            _defaultPerksByTier[3][PerkType.ImprovedCriticalVibroblades] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalFinesseVibroblades] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalHeavyVibroblades] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalPolearms] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalTwinBlades] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalKatars] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalStaves] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalPistols] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalRifles] = 1;

            _defaultPerksByTier[3][PerkType.VibrobladeMastery] = 1;
            _defaultPerksByTier[3][PerkType.FinesseVibrobladeMastery] = 1;
            _defaultPerksByTier[3][PerkType.HeavyVibrobladeMastery] = 1;
            _defaultPerksByTier[3][PerkType.PolearmMastery] = 1;
            _defaultPerksByTier[3][PerkType.TwinBladeMastery] = 1;
            _defaultPerksByTier[3][PerkType.KatarMastery] = 1;
            _defaultPerksByTier[3][PerkType.StaffMastery] = 1;
            _defaultPerksByTier[3][PerkType.PistolMastery] = 1;
            _defaultPerksByTier[3][PerkType.RifleMastery] = 1;

            // Tier 4

            // Tier 5
            _defaultPerksByTier[5][PerkType.VibrobladeMastery] = 2;
            _defaultPerksByTier[5][PerkType.FinesseVibrobladeMastery] = 2;
            _defaultPerksByTier[5][PerkType.HeavyVibrobladeMastery] = 2;
            _defaultPerksByTier[5][PerkType.PolearmMastery] = 2;
            _defaultPerksByTier[5][PerkType.TwinBladeMastery] = 2;
            _defaultPerksByTier[5][PerkType.KatarMastery] = 2;
            _defaultPerksByTier[5][PerkType.StaffMastery] = 2;
            _defaultPerksByTier[5][PerkType.PistolMastery] = 2;
            _defaultPerksByTier[5][PerkType.RifleMastery] = 2;

        }

        [NWNEventHandler("droid_ass_used")]
        public static void UseDroidAssemblyTerminal()
        {
            var player = GetLastUsedBy();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            Gui.TogglePlayerWindow(player, GuiWindowType.DroidAssembly, null, OBJECT_SELF);
        }
    }
}
