using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class InfiniteConduitStatusEffect : StatusEffectBase
    {
        public override string Name => "Infinite Conduit";
        public override EffectIconType Icon => EffectIconType.Haste;

        protected override void OnDamageDealt(uint attacker, uint defender, int damage)
        {
            Stat.RestoreFP(attacker, 5);
        }
    }
}
