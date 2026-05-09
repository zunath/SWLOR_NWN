using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AbilityService
{
    public class ActiveConcentrationAbility
    {
        public ActiveConcentrationAbility(uint target, FeatType feat, Type statusEffect)
        {
            Target = target;
            Feat = feat;
            StatusEffect = statusEffect;
        }

        public uint Target { get; set; }
        public FeatType Feat { get; set; }
        public Type StatusEffect { get; set; }
    }
}
