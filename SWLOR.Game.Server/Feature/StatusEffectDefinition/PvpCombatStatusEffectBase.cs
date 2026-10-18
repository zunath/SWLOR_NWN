using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Shows a player's PvP combat on the status bar. The rules themselves are applied by the PvP
    /// combat service, so these statuses carry no stats and can't be cleansed away.
    /// </summary>
    public abstract class PvpCombatStatusEffectBase : StatusEffectBase
    {
        public override StatusEffectActivationType ActivationType => StatusEffectActivationType.Passive;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
    }
}
