using SWLOR.Game.Server.Service.CivicExchangeService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// The Civic Exchange's long buff (Player City Revamp, "Civic Exchange"): as strong as a top-tier food buff, and it does not
    /// stack with food. Buying it removes any food buff the citizen is under, and eating food removes it, through the
    /// less-powerful lists of both effects (<see cref="FoodStatusEffect"/> lists this one). It holds no HP, so it never touches the
    /// temporary HP a food keeps. The numbers are the Exchange catalog's (<see cref="CivicExchangeCatalog"/>).
    /// </summary>
    public sealed class CivicProvisionsStatusEffect : StatusEffectBase
    {
        public override string Name => CivicExchangeCatalog.BuffName;

        // The food icon stands in until the Exchange gets art of its own.
        public override EffectIconType Icon => EffectIconType.FoodStatusEffect;

        public CivicProvisionsStatusEffect()
        {
            StatGroup.Stats[StatType.MaxFP] = CivicExchangeCatalog.BuffMaxFP;
            StatGroup.Stats[StatType.MaxStamina] = CivicExchangeCatalog.BuffMaxStamina;
            StatGroup.Stats[StatType.PhysicalDefense] = CivicExchangeCatalog.BuffPhysicalDefense;
            StatGroup.Stats[StatType.ForceDefense] = CivicExchangeCatalog.BuffForceDefense;

            // Replaces a food buff. Food replaces this one in return, from FoodStatusEffect's own list; neither lists the other as more
            // powerful, which would refuse the newer buff instead.
            LessPowerfulEffectTypes.Add(typeof(FoodStatusEffect));
        }
    }
}
