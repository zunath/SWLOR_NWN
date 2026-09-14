using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance
{
    public static class RacialAppearanceRegistry
    {
        private static readonly Dictionary<AppearanceType, IRacialAppearanceDefinition> Definitions = new();
        private static bool _isLoaded;

        public static void EnsureLoaded()
        {
            if (_isLoaded)
                return;

            Definitions[AppearanceType.Human] = new HumanRacialAppearanceDefinition();
            Definitions[AppearanceType.Bothan] = new BothanRacialAppearanceDefinition();
            Definitions[AppearanceType.Chiss] = new ChissRacialAppearanceDefinition();
            Definitions[AppearanceType.Zabrak] = new ZabrakRacialAppearanceDefinition();
            Definitions[AppearanceType.Twilek] = new TwilekRacialAppearanceDefinition();
            Definitions[AppearanceType.Mirialan] = new MirialanRacialAppearanceDefinition();
            Definitions[AppearanceType.Echani] = new EchaniRacialAppearanceDefinition();
            Definitions[AppearanceType.KelDor] = new KelDorRacialAppearanceDefinition();
            Definitions[AppearanceType.Cyborg] = new CyborgRacialAppearanceDefinition();
            Definitions[AppearanceType.Cathar] = new CatharRacialAppearanceDefinition();
            Definitions[AppearanceType.Rodian] = new RodianRacialAppearanceDefinition();
            Definitions[AppearanceType.Trandoshan] = new TrandoshanRacialAppearanceDefinition();
            Definitions[AppearanceType.Togruta] = new TogrutaRacialAppearanceDefinition();
            Definitions[AppearanceType.Wookiee] = new WookieeRacialAppearanceDefinition();
            Definitions[AppearanceType.MonCalamari] = new MonCalamariRacialAppearanceDefinition();
            Definitions[AppearanceType.Ugnaught] = new UgnaughtRacialAppearanceDefinition();
            Definitions[AppearanceType.Droid] = new DroidRacialAppearanceDefinition();
            Definitions[AppearanceType.Nautolan] = new NautolanRacialAppearanceDefinition();
            Definitions[AppearanceType.Ewok] = new EwokRacialAppearanceDefinition();

            _isLoaded = true;
        }

        public static bool TryGet(AppearanceType appearanceType, out IRacialAppearanceDefinition definition)
        {
            EnsureLoaded();
            return Definitions.TryGetValue(appearanceType, out definition);
        }
    }
}
