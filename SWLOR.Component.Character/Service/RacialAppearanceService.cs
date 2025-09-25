using SWLOR.Component.Character.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Component.Character.Service
{
    public class RacialAppearanceService : IRacialAppearanceService
    {
        private static readonly Dictionary<AppearanceType, IRacialAppearanceDefinition> _racialAppearances = new();

        static RacialAppearanceService()
        {
            LoadRacialAppearances();
        }

        private static void LoadRacialAppearances()
        {
            _racialAppearances[AppearanceType.Human] = new HumanRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Bothan] = new BothanRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Chiss] = new ChissRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Zabrak] = new ZabrakRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Twilek] = new TwilekRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Mirialan] = new MirialanRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Echani] = new EchaniRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.KelDor] = new KelDorRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Cyborg] = new CyborgRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Cathar] = new CatharRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Rodian] = new RodianRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Trandoshan] = new TrandoshanRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Togruta] = new TogrutaRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Wookiee] = new WookieeRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.MonCalamari] = new MonCalamariRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Ugnaught] = new UgnaughtRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Droid] = new DroidRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Nautolan] = new NautolanRacialAppearanceDefinition();
            _racialAppearances[AppearanceType.Ewok] = new EwokRacialAppearanceDefinition();
        }

        /// <summary>
        /// Gets the first value from a RacialAppearanceDefinition for the specified racial type, creature part, and gender.
        /// </summary>
        /// <param name="racialType">The racial type to get the appearance for</param>
        /// <param name="creaturePart">The creature part to get the first value for</param>
        /// <param name="gender">The gender to get the appearance for (affects head selection)</param>
        /// <returns>The first integer value for the specified part, or 1 if not found</returns>
        public int GetFirstRacialAppearanceValue(RacialType racialType, CreaturePart creaturePart, Gender gender)
        {
            var appearanceType = ConvertRacialTypeToAppearanceType(racialType);
            
            if (!_racialAppearances.TryGetValue(appearanceType, out var racialAppearance))
            {
                return 1; // Default fallback
            }

            return creaturePart switch
            {
                CreaturePart.Head => GetGenderSpecificHead(racialAppearance, gender),
                CreaturePart.Torso => racialAppearance.Torsos?.FirstOrDefault() ?? 1,
                CreaturePart.Pelvis => racialAppearance.Pelvis?.FirstOrDefault() ?? 1,
                CreaturePart.RightBicep => racialAppearance.RightBicep?.FirstOrDefault() ?? 1,
                CreaturePart.RightForearm => racialAppearance.RightForearm?.FirstOrDefault() ?? 1,
                CreaturePart.RightHand => racialAppearance.RightHand?.FirstOrDefault() ?? 1,
                CreaturePart.RightThigh => racialAppearance.RightThigh?.FirstOrDefault() ?? 1,
                CreaturePart.RightShin => racialAppearance.RightShin?.FirstOrDefault() ?? 1,
                CreaturePart.RightFoot => racialAppearance.RightFoot?.FirstOrDefault() ?? 1,
                CreaturePart.LeftBicep => racialAppearance.LeftBicep?.FirstOrDefault() ?? 1,
                CreaturePart.LeftForearm => racialAppearance.LeftForearm?.FirstOrDefault() ?? 1,
                CreaturePart.LeftHand => racialAppearance.LeftHand?.FirstOrDefault() ?? 1,
                CreaturePart.LeftThigh => racialAppearance.LeftThigh?.FirstOrDefault() ?? 1,
                CreaturePart.LeftShin => racialAppearance.LeftShin?.FirstOrDefault() ?? 1,
                CreaturePart.LeftFoot => racialAppearance.LeftFoot?.FirstOrDefault() ?? 1,
                _ => 1 // Default fallback for unsupported parts
            };
        }

        /// <summary>
        /// Gets the first head value based on gender.
        /// </summary>
        /// <param name="racialAppearance">The racial appearance definition</param>
        /// <param name="gender">The gender to get the head for</param>
        /// <returns>The first head value for the specified gender, or 1 if not found</returns>
        private static int GetGenderSpecificHead(IRacialAppearanceDefinition racialAppearance, Gender gender)
        {
            return gender switch
            {
                Gender.Male => racialAppearance.MaleHeads?.FirstOrDefault() ?? 1,
                Gender.Female => racialAppearance.FemaleHeads?.FirstOrDefault() ?? 1,
                _ => racialAppearance.MaleHeads?.FirstOrDefault() ?? 1 // Default to male if unknown gender
            };
        }

        /// <summary>
        /// Converts a RacialType to the corresponding AppearanceType.
        /// </summary>
        /// <param name="racialType">The racial type to convert</param>
        /// <returns>The corresponding AppearanceType</returns>
        private static AppearanceType ConvertRacialTypeToAppearanceType(RacialType racialType)
        {
            return racialType switch
            {
                RacialType.Human => AppearanceType.Human,
                RacialType.Bothan => AppearanceType.Bothan,
                RacialType.Chiss => AppearanceType.Chiss,
                RacialType.Zabrak => AppearanceType.Zabrak,
                RacialType.Wookiee => AppearanceType.Wookiee,
                RacialType.Twilek => AppearanceType.Twilek,
                RacialType.Cyborg => AppearanceType.Cyborg,
                RacialType.Cathar => AppearanceType.Cathar,
                RacialType.Trandoshan => AppearanceType.Trandoshan,
                RacialType.Mirialan => AppearanceType.Mirialan,
                RacialType.Echani => AppearanceType.Echani,
                RacialType.MonCalamari => AppearanceType.MonCalamari,
                RacialType.Ugnaught => AppearanceType.Ugnaught,
                RacialType.Togruta => AppearanceType.Togruta,
                RacialType.Rodian => AppearanceType.Rodian,
                RacialType.KelDor => AppearanceType.KelDor,
                RacialType.Droid => AppearanceType.Droid,
                RacialType.Nautolan => AppearanceType.Nautolan,
                RacialType.Ewok => AppearanceType.Ewok,
                _ => AppearanceType.Human // Default fallback
            };
        }
    }
}
