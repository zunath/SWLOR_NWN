using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _10_SPAdjustments: PlayerMigrationBase
    {
        public override int Version => 10;
        public override void Migrate(uint player)
        {
            CreaturePlugin.RemoveFeat(player, FeatType.ForcePush1);
            CreaturePlugin.RemoveFeat(player, FeatType.ForcePush2);
            CreaturePlugin.RemoveFeat(player, FeatType.ForcePush3);
            CreaturePlugin.RemoveFeat(player, FeatType.ForcePush4);

            CreaturePlugin.RemoveFeat(player, FeatType.ThrowLightsaber1);
            CreaturePlugin.RemoveFeat(player, FeatType.ThrowLightsaber2);
            CreaturePlugin.RemoveFeat(player, FeatType.ThrowLightsaber3);

            CreaturePlugin.RemoveFeat(player, FeatType.ForceStun1);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceStun2);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceStun3);

            CreaturePlugin.RemoveFeat(player, FeatType.BattleInsight1);
            CreaturePlugin.RemoveFeat(player, FeatType.BattleInsight2);

            CreaturePlugin.RemoveFeat(player, FeatType.MindTrick1);
            CreaturePlugin.RemoveFeat(player, FeatType.MindTrick2);

            CreaturePlugin.RemoveFeat(player, FeatType.Premonition1);
            CreaturePlugin.RemoveFeat(player, FeatType.Premonition2);

            CreaturePlugin.RemoveFeat(player, FeatType.ThrowRock1);
            CreaturePlugin.RemoveFeat(player, FeatType.ThrowRock2);
            CreaturePlugin.RemoveFeat(player, FeatType.ThrowRock3);
            CreaturePlugin.RemoveFeat(player, FeatType.ThrowRock4);
            CreaturePlugin.RemoveFeat(player, FeatType.ThrowRock5);

            CreaturePlugin.RemoveFeat(player, FeatType.ForceInspiration1);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceInspiration2);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceInspiration3);

            CreaturePlugin.RemoveFeat(player, FeatType.ForceHeal1);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceHeal2);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceHeal3);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceHeal4);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceHeal5);

            CreaturePlugin.RemoveFeat(player, FeatType.ForceBurst1);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceBurst2);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceBurst3);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceBurst4);

            CreaturePlugin.RemoveFeat(player, FeatType.ForceMind1);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceMind2);

            CreaturePlugin.RemoveFeat(player, FeatType.Benevolence1);
            CreaturePlugin.RemoveFeat(player, FeatType.Benevolence2);
            CreaturePlugin.RemoveFeat(player, FeatType.Benevolence3);

            CreaturePlugin.RemoveFeat(player, FeatType.ForceValor1);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceValor2);

            CreaturePlugin.RemoveFeat(player, FeatType.ForceDrain1);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceDrain2);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceDrain3);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceDrain4);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceDrain5);

            CreaturePlugin.RemoveFeat(player, FeatType.ForceLightning1);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceLightning2);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceLightning3);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceLightning4);

            CreaturePlugin.RemoveFeat(player, FeatType.ForceBody1);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceBody2);

            CreaturePlugin.RemoveFeat(player, FeatType.CreepingTerror1);
            CreaturePlugin.RemoveFeat(player, FeatType.CreepingTerror2);
            CreaturePlugin.RemoveFeat(player, FeatType.CreepingTerror3);

            CreaturePlugin.RemoveFeat(player, FeatType.ForceRage1);
            CreaturePlugin.RemoveFeat(player, FeatType.ForceRage2);
        }
    }
}
