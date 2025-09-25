using SWLOR.Component.Character.Service;
using SWLOR.Component.Migration.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Entities;

namespace SWLOR.Component.Migration.Feature.PlayerMigration
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
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Human, CreaturePart.Head, gender);
                    break;
                case RacialType.Bothan:
                    appearanceType = AppearanceType.Bothan;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Bothan, CreaturePart.Head, gender);
                    break;
                case RacialType.Chiss:
                    appearanceType = AppearanceType.Chiss;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Chiss, CreaturePart.Head, gender);
                    break;
                case RacialType.Zabrak:
                    appearanceType = AppearanceType.Zabrak;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Zabrak, CreaturePart.Head, gender);
                    break;
                case RacialType.Wookiee:
                    appearanceType = AppearanceType.Wookiee;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Wookiee, CreaturePart.Head, gender);
                    break;
                case RacialType.Twilek:
                    appearanceType = AppearanceType.Twilek;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Twilek, CreaturePart.Head, gender);
                    break;
                case RacialType.Cathar:
                    appearanceType = AppearanceType.Cathar;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Cathar, CreaturePart.Head, gender);
                    break;
                case RacialType.Trandoshan:
                    appearanceType = AppearanceType.Trandoshan;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Trandoshan, CreaturePart.Head, gender);
                    break;
                case RacialType.Mirialan:
                    appearanceType = AppearanceType.Mirialan;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Mirialan, CreaturePart.Head, gender);
                    break;
                case RacialType.Echani:
                    appearanceType = AppearanceType.Echani;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Echani, CreaturePart.Head, gender);
                    break;
                case RacialType.MonCalamari:
                    appearanceType = AppearanceType.MonCalamari;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.MonCalamari, CreaturePart.Head, gender);
                    break;
                case RacialType.Ugnaught:
                    appearanceType = AppearanceType.Ugnaught;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Ugnaught, CreaturePart.Head, gender);
                    break;
                case RacialType.Togruta:
                    appearanceType = AppearanceType.Togruta;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Togruta, CreaturePart.Head, gender);
                    break;
                case RacialType.Rodian:
                    appearanceType = AppearanceType.Rodian;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.Rodian, CreaturePart.Head, gender);
                    break;
                case RacialType.KelDor:
                    appearanceType = AppearanceType.KelDor;
                    headId = _racialAppearance.GetFirstRacialAppearanceValue(RacialType.KelDor, CreaturePart.Head, gender);
                    break;
                default:
                    return;
            }

            SetCreatureAppearanceType(player, appearanceType);
            SetCreatureBodyPart(CreaturePart.Head, headId, player);

            dbPlayer.OriginalAppearanceType = GetAppearanceType(player);
            _db.Set(dbPlayer);
        }
    }
}
