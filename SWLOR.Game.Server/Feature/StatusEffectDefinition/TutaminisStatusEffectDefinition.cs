using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatusEffectService;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class TutaminisStatusEffectsEffectDefinition : IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            Tutaminis1(builder);
            Tutaminis2(builder);
            Tutaminis3(builder);
            Tutaminis4(builder);
            Tutaminis5(builder);

            return builder.Build();
        }

        private void Tutaminis1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Tutaminis1)
                .Name("Tutaminis I")
                .EffectIcon(EffectIconType.DamageResistance)
                .CannotReplace(StatusEffectType.Tutaminis1, StatusEffectType.Tutaminis2, StatusEffectType.Tutaminis4, StatusEffectType.Tutaminis5);      
               
        }
        private void Tutaminis2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Tutaminis2)
                .Name("Tutaminis II")
                .EffectIcon(EffectIconType.DamageResistance)
                .CannotReplace(StatusEffectType.Tutaminis1, StatusEffectType.Tutaminis3, StatusEffectType.Tutaminis4, StatusEffectType.Tutaminis5);      
               
        }
        private void Tutaminis3(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Tutaminis3)
                .Name("Tutaminis III")
                .EffectIcon(EffectIconType.DamageResistance)
                .CannotReplace(StatusEffectType.Tutaminis1, StatusEffectType.Tutaminis2, StatusEffectType.Tutaminis4, StatusEffectType.Tutaminis5);        
        }
        private void Tutaminis4(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Tutaminis4)
                .Name("Tutainis IV")
                .EffectIcon(EffectIconType.DamageResistance)
                .CannotReplace(StatusEffectType.Tutaminis1, StatusEffectType.Tutaminis2, StatusEffectType.Tutaminis3, StatusEffectType.Tutaminis5);                 
        }
        private void Tutaminis5(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Tutaminis5)
                .Name("Tutaminis V")
                .EffectIcon(EffectIconType.DamageResistance)
                .CannotReplace(StatusEffectType.Tutaminis1, StatusEffectType.Tutaminis2, StatusEffectType.Tutaminis3, StatusEffectType.Tutaminis4);      

        }  
                 
         
    }    
}
    


    

