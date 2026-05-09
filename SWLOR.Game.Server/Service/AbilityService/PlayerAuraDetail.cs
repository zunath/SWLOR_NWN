namespace SWLOR.Game.Server.Service.AbilityService
{
    public class PlayerAuraDetail
    {
        public Type StatusEffect { get; set; }
        public bool TargetsSelf { get; set; }
        public bool TargetsParty { get; set; }
        public bool TargetsEnemies { get; set; }

        public PlayerAuraDetail(Type statusEffect, bool targetsSelf, bool targetsParty, bool targetsEnemies)
        {
            StatusEffect = statusEffect;
            TargetsSelf = targetsSelf;
            TargetsParty = targetsParty;
            TargetsEnemies = targetsEnemies;
        }
    }
}
