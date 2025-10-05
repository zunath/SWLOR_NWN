using SWLOR.Component.Migration.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Component.Migration.Definitions.PlayerMigration
{
    public class _2_CorrectAppearanceType: IPlayerMigration
    {
        private readonly IDatabaseService _db;
        private readonly IRacialAppearanceService _racialAppearance;

        public _2_CorrectAppearanceType(
            IDatabaseService db,
            IRacialAppearanceService racialAppearance)
        {
            _db = db;
            _racialAppearance = racialAppearance;
        }
        
        public int Version => 2;
        public void Migrate(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var racialType = GetRacialType(player);
            AppearanceType appearanceType;
            int headId;
            var gender = GetGender(player);

            switch (racialType)
            {
                case RacialType.Human:
                    appearanceType = AppearanceType.Human;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Human, CreaturePartType.Head, gender);
                    break;
                case RacialType.Bothan:
                    appearanceType = AppearanceType.Bothan;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Bothan, CreaturePartType.Head, gender);
                    break;
                case RacialType.Chiss:
                    appearanceType = AppearanceType.Chiss;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Chiss, CreaturePartType.Head, gender);
                    break;
                case RacialType.Zabrak:
                    appearanceType = AppearanceType.Zabrak;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Zabrak, CreaturePartType.Head, gender);
                    break;
                case RacialType.Wookiee:
                    appearanceType = AppearanceType.Wookiee;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Wookiee, CreaturePartType.Head, gender);
                    break;
                case RacialType.Twilek:
                    appearanceType = AppearanceType.Twilek;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Twilek, CreaturePartType.Head, gender);
                    break;
                case RacialType.Cathar:
                    appearanceType = AppearanceType.Cathar;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePartType.Head, gender);
                    break;
                case RacialType.Trandoshan:
                    appearanceType = AppearanceType.Trandoshan;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Trandoshan, CreaturePartType.Head, gender);
                    break;
                case RacialType.Mirialan:
                    appearanceType = AppearanceType.Mirialan;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Mirialan, CreaturePartType.Head, gender);
                    break;
                case RacialType.Echani:
                    appearanceType = AppearanceType.Echani;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Echani, CreaturePartType.Head, gender);
                    break;
                case RacialType.MonCalamari:
                    appearanceType = AppearanceType.MonCalamari;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.MonCalamari, CreaturePartType.Head, gender);
                    break;
                case RacialType.Ugnaught:
                    appearanceType = AppearanceType.Ugnaught;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Ugnaught, CreaturePartType.Head, gender);
                    break;
                case RacialType.Togruta:
                    appearanceType = AppearanceType.Togruta;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Togruta, CreaturePartType.Head, gender);
                    break;
                case RacialType.Rodian:
                    appearanceType = AppearanceType.Rodian;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Rodian, CreaturePartType.Head, gender);
                    break;
                case RacialType.KelDor:
                    appearanceType = AppearanceType.KelDor;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.KelDor, CreaturePartType.Head, gender);
                    break;
                default:
                    return;
            }

            SetCreatureAppearanceType(player, appearanceType);
            SetCreatureBodyPart(CreaturePartType.Head, headId, player);

            dbPlayer.OriginalAppearanceType = GetAppearanceType(player);
            _db.Set(dbPlayer);
        }
    }
}
