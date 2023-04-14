using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _2_CorrectAppearanceType: IPlayerMigration
    {
        public int Version => 2;
        public void Migrate(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var racialType = GetRacialType(player);
            AppearanceType appearanceType;
            int headId;
            var gender = GetGender(player);

            switch (racialType)
            {
                case RacialType.Human:
                    appearanceType = AppearanceType.Human;
                    if (gender == Gender.Male)
                        headId = new HumanRacialAppearanceDefinition().MaleHeads.First();
                    else
                        headId = new HumanRacialAppearanceDefinition().FemaleHeads.First();
                    break;
                case RacialType.Bothan:
                    appearanceType = AppearanceType.Bothan;
                    if (gender == Gender.Male)
                        headId = new BothanRacialAppearanceDefinition().MaleHeads.First();
                    else
                        headId = new BothanRacialAppearanceDefinition().FemaleHeads.First();
                    break;
                case RacialType.Chiss:
                    appearanceType = AppearanceType.Chiss;
                    if (gender == Gender.Male)
                        headId = new ChissRacialAppearanceDefinition().MaleHeads.First();
                    else
                        headId = new ChissRacialAppearanceDefinition().FemaleHeads.First();
                    break;
                case RacialType.Zabrak:
                    appearanceType = AppearanceType.Zabrak;
                    if (gender == Gender.Male)
                        headId = new ZabrakRacialAppearanceDefinition().MaleHeads.First();
                    else
                        headId = new ZabrakRacialAppearanceDefinition().FemaleHeads.First();
                    break;
                case RacialType.Wookiee:
                    appearanceType = AppearanceType.Wookiee;
                    if (gender == Gender.Male)
                        headId = new WookieeRacialAppearanceDefinition().MaleHeads.First();
                    else
                        headId = new WookieeRacialAppearanceDefinition().FemaleHeads.First();
                    break;
                case RacialType.Twilek:
                    appearanceType = AppearanceType.Twilek;
                    if (gender == Gender.Male)
                        headId = new TwilekRacialAppearanceDefinition().MaleHeads.First();
                    else
                        headId = new TwilekRacialAppearanceDefinition().FemaleHeads.First();
                    break;
                case RacialType.Cathar:
                    appearanceType = AppearanceType.Cathar;
                    if (gender == Gender.Male)
                        headId = new CatharRacialAppearanceDefinition().MaleHeads.First();
                    else
                        headId = new CatharRacialAppearanceDefinition().FemaleHeads.First();
                    break;
                case RacialType.Trandoshan:
                    appearanceType = AppearanceType.Trandoshan;
                    if (gender == Gender.Male)
                        headId = new TrandoshanRacialAppearanceDefinition().MaleHeads.First();
                    else
                        headId = new TrandoshanRacialAppearanceDefinition().FemaleHeads.First();
                    break;
                case RacialType.Mirialan:
                    appearanceType = AppearanceType.Mirialan;
                    if (gender == Gender.Male)
                        headId = new MirialanRacialAppearanceDefinition().MaleHeads.First();
                    else
                        headId = new MirialanRacialAppearanceDefinition().FemaleHeads.First();
                    break;
                case RacialType.Echani:
                    appearanceType = AppearanceType.Echani;
                    if (gender == Gender.Male)
                        headId = new EchaniRacialAppearanceDefinition().MaleHeads.First();
                    else
                        headId = new EchaniRacialAppearanceDefinition().FemaleHeads.First();
                    break;
                case RacialType.MonCalamari:
                    appearanceType = AppearanceType.MonCalamari;
                    if (gender == Gender.Male)
                        headId = new MonCalamariRacialAppearanceDefinition().MaleHeads.First();
                    else
                        headId = new MonCalamariRacialAppearanceDefinition().FemaleHeads.First();
                    break;
                case RacialType.Ugnaught:
                    appearanceType = AppearanceType.Ugnaught;
                    if (gender == Gender.Male)
                        headId = new UgnaughtRacialAppearanceDefinition().MaleHeads.First();
                    else
                        headId = new UgnaughtRacialAppearanceDefinition().FemaleHeads.First();
                    break;
                case RacialType.Togruta:
                    appearanceType = AppearanceType.Togruta;
                    if (gender == Gender.Male)
                        headId = new TogrutaRacialAppearanceDefinition().MaleHeads.First();
                    else
                        headId = new TogrutaRacialAppearanceDefinition().FemaleHeads.First();
                    break;
                case RacialType.Rodian:
                    appearanceType = AppearanceType.Rodian;
                    if (gender == Gender.Male)
                        headId = new RodianRacialAppearanceDefinition().MaleHeads.First();
                    else
                        headId = new RodianRacialAppearanceDefinition().FemaleHeads.First();
                    break;
                case RacialType.KelDor:
                    appearanceType = AppearanceType.KelDor;
                    if (gender == Gender.Male)
                        headId = new KelDorRacialAppearanceDefinition().MaleHeads.First();
                    else
                        headId = new KelDorRacialAppearanceDefinition().FemaleHeads.First();
                    break;
                default:
                    return;
            }

            SetCreatureAppearanceType(player, appearanceType);
            SetCreatureBodyPart(CreaturePart.Head, headId, player);

            dbPlayer.OriginalAppearanceType = GetAppearanceType(player);
            DB.Set(dbPlayer);
        }
    }
}
