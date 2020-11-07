using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class OneHandedPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            Doublehand(builder);
            DualWield(builder);
            WeaponFinesse(builder);
            WeaponFocusVibroblades(builder);
            ImprovedCriticalVibroblades(builder);
            VibrobladeProficiency(builder);
            VibrobladeMastery(builder);
            HackingBlade(builder);
            RiotBlade(builder);
            WeaponFocusFinesseVibroblades(builder);
            ImprovedCriticalFinesseVibroblades(builder);
            FinesseVibrobladeProficiency(builder);
            FinesseVibrobladeMastery(builder);
            PoisonStab(builder);
            Backstab(builder);
            WeaponFocusLightsabers(builder);
            ImprovedCriticalLightsabers(builder);
            LightsaberProficiency(builder);
            LightsaberMastery(builder);
            ForceLeap(builder);
            SaberStrike(builder);

            return builder.Build();
        }

        private void Doublehand(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedGeneral, PerkType.Doublehand);
        }

        private void DualWield(PerkBuilder builder)
        {

        }

        private void WeaponFinesse(PerkBuilder builder)
        {

        }

        private void WeaponFocusVibroblades(PerkBuilder builder)
        {

        }

        private void ImprovedCriticalVibroblades(PerkBuilder builder)
        {

        }

        private void VibrobladeProficiency(PerkBuilder builder)
        {

        }

        private void VibrobladeMastery(PerkBuilder builder)
        {

        }

        private void HackingBlade(PerkBuilder builder)
        {

        }

        private void RiotBlade(PerkBuilder builder)
        {

        }

        private void WeaponFocusFinesseVibroblades(PerkBuilder builder)
        {

        }

        private void ImprovedCriticalFinesseVibroblades(PerkBuilder builder)
        {

        }

        private void FinesseVibrobladeProficiency(PerkBuilder builder)
        {

        }

        private void FinesseVibrobladeMastery(PerkBuilder builder)
        {

        }

        private void PoisonStab(PerkBuilder builder)
        {

        }

        private void Backstab(PerkBuilder builder)
        {

        }

        private void WeaponFocusLightsabers(PerkBuilder builder)
        {

        }

        private void ImprovedCriticalLightsabers(PerkBuilder builder)
        {

        }

        private void LightsaberProficiency(PerkBuilder builder)
        {

        }

        private void LightsaberMastery(PerkBuilder builder)
        {

        }

        private void ForceLeap(PerkBuilder builder)
        {

        }

        private void SaberStrike(PerkBuilder builder)
        {

        }
    }
}
