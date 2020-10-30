using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Core.NWScript.Enum;


namespace SWLOR.Game.Server.Service
{
    public static class LanguageService
    {
        public static string TranslateSnippetForListener(NWObject speaker, NWObject listener, SkillType language, string snippet)
        {
            var map = new Dictionary<SkillType, Type>
            {
                { SkillType.Bothese, typeof(TranslatorBothese) },
                { SkillType.Catharese, typeof(TranslatorCatharese) },
                { SkillType.Cheunh, typeof(TranslatorCheunh) },
                { SkillType.Dosh, typeof(TranslatorDosh) },
                { SkillType.Droidspeak, typeof(TranslatorDroidspeak) },
                { SkillType.Huttese, typeof(TranslatorHuttese) },
                { SkillType.Mandoa, typeof(TranslatorMandoa) },
                { SkillType.Shyriiwook, typeof(TranslatorShyriiwook) },
                { SkillType.Twileki, typeof(TranslatorTwileki) },
                { SkillType.Zabraki, typeof(TranslatorZabraki) },
                { SkillType.Mirialan, typeof(TranslatorMirialan) },
                { SkillType.MonCalamarian, typeof(TranslatorMonCalamarian) },
                { SkillType.Ugnaught, typeof(TranslatorUgnaught) }
            };

            var type = typeof(TranslatorGeneric);
            map.TryGetValue(language, out type);
            var translator = (ITranslator)Activator.CreateInstance(type);

            if (speaker.IsPC && !speaker.IsDM)
            {
                // Get the rank and max rank for the speaker, and garble their English text based on it.
                NWPlayer speakerAsPlayer = speaker.Object;
                var speakerSkillRank = SkillService.GetPCSkillRank(speakerAsPlayer, language);
                var speakerSkillMaxRank = SkillService.GetSkill(language).MaxRank;

                if (speakerSkillRank != speakerSkillMaxRank)
                {
                    var garbledChance = 100 - (int)(((float)speakerSkillRank / (float)speakerSkillMaxRank) * 100);

                    var split = snippet.Split(' ');
                    for (var i = 0; i < split.Length; ++i)
                    {
                        if (RandomService.Random(100) <= garbledChance)
                        {
                            split[i] = new string(split[i].ToCharArray().OrderBy(s => (RandomService.Random(2) % 2) == 0).ToArray());
                        }
                    }

                    snippet = split.Aggregate((a, b) => a + " " + b);
                }
            }

            if (!listener.IsPC || listener.IsDM)
            {
                // Short circuit for a DM or NPC - they will always understand the text.
                return snippet;
            }

            // Let's grab the max rank for the listener skill, and then we roll for a successful translate based on that.
            NWPlayer listenerAsPlayer = listener.Object;
            var rank = SkillService.GetPCSkillRank(listenerAsPlayer, language);
            var maxRank = SkillService.GetSkill(language).MaxRank;

            // Check for the Comprehend Speech concentration ability.
            var dbPlayer = DataService.Player.GetByID(listenerAsPlayer.GlobalID);
            var grantSenseXP = false;
            if (dbPlayer.ActiveConcentrationPerkID == (int)PerkType.ComprehendSpeech)
            {
                var bonus = 5 * dbPlayer.ActiveConcentrationTier;
                rank += bonus;
                grantSenseXP = true;
            }

            // Ensure we don't go over the maximum.
            if (rank > maxRank)
                rank = maxRank;

            if (rank == maxRank || speaker == listener)
            {
                // Guaranteed success - return original.
                return snippet;
            }

            var textAsForeignLanguage = translator.Translate(snippet);

            if (rank != 0)
            {
                var englishChance = (int)(((float)rank / (float)maxRank) * 100);

                var originalSplit = snippet.Split(' ');
                var foreignSplit = textAsForeignLanguage.Split(' ');

                var endResult = new StringBuilder();

                // WARNING: We're making the assumption that originalSplit.Length == foreignSplit.Length.
                // If this assumption changes, the below logic needs to change too.
                for (var i = 0; i < originalSplit.Length; ++i)
                {
                    if (RandomService.Random(100) <= englishChance)
                    {
                        endResult.Append(originalSplit[i]);
                    }
                    else
                    {
                        endResult.Append(foreignSplit[i]);
                    }

                    endResult.Append(" ");
                }

                textAsForeignLanguage = endResult.ToString();
            }

            var now = DateTime.Now.Ticks;
            var lastSkillUpLow = listenerAsPlayer.GetLocalInt("LAST_LANGUAGE_SKILL_INCREASE_LOW");
            var lastSkillUpHigh = listenerAsPlayer.GetLocalInt("LAST_LANGUAGE_SKILL_INCREASE_HIGH");
            long lastSkillUp = lastSkillUpHigh;
            lastSkillUp = (lastSkillUp << 32) | (uint)lastSkillUpLow;
            var differenceInSeconds = (now - lastSkillUp) / 10000000;

            if (differenceInSeconds / 60 >= 2)
            {
                var amount = Math.Max(10, Math.Min(150, snippet.Length) / 3);
                // Reward exp towards the language - we scale this with character count, maxing at 50 exp for 150 characters.
                SkillService.GiveSkillXP(listenerAsPlayer, language, amount);

                // Grant Sense XP if player is concentrating Comprehend Speech.
                if (grantSenseXP)
                    SkillService.GiveSkillXP(listenerAsPlayer, SkillType.ForceSense, amount * 10);

                listenerAsPlayer.SetLocalInt("LAST_LANGUAGE_SKILL_INCREASE_LOW", (int)(now & 0xFFFFFFFF));
                listenerAsPlayer.SetLocalInt("LAST_LANGUAGE_SKILL_INCREASE_HIGH", (int)((now >> 32) & 0xFFFFFFFF));
            }

            return textAsForeignLanguage;
        }

        public static int GetColour(SkillType language)
        {
            byte r = 0;
            byte g = 0;
            byte b = 0;

            switch (language)
            {
                case SkillType.Bothese: r = 132; g = 56; b = 18; break;
                case SkillType.Catharese: r = 235; g = 235; b = 199; break;
                case SkillType.Cheunh: r = 82; g = 143; b = 174; break;
                case SkillType.Dosh: r = 166; g = 181; b = 73; break;
                case SkillType.Droidspeak: r = 192; g = 192; b = 192; break;
                case SkillType.Huttese: r = 162; g = 74; b = 10; break;
                case SkillType.Mandoa: r = 255; g = 215; b = 0; break;
                case SkillType.Shyriiwook: r = 149; g = 125; b = 86; break;
                case SkillType.Twileki: r = 65; g = 105; b = 225; break;
                case SkillType.Zabraki: r = 255; g = 102; b = 102; break;
                case SkillType.Mirialan: r = 77; g = 230; b = 215; break;
                case SkillType.MonCalamarian: r = 128; g = 128; b = 192; break;
                case SkillType.Ugnaught: r = 255; g = 193; b = 233; break;
            }

            return r << 24 | g << 16 | b << 8;
        }

        public static string GetName(SkillType language)
        {
            switch (language)
            {
                case SkillType.Bothese: return "Bothese";
                case SkillType.Catharese: return "Catharese";
                case SkillType.Cheunh: return "Cheunh";
                case SkillType.Dosh: return "Dosh";
                case SkillType.Droidspeak: return "Droidspeak";
                case SkillType.Huttese: return "Huttese";
                case SkillType.Mandoa: return "Mandoa";
                case SkillType.Shyriiwook: return "Shyriiwook";
                case SkillType.Twileki: return "Twi'leki";
                case SkillType.Zabraki: return "Zabraki";
                case SkillType.Mirialan: return "Mirialan";
                case SkillType.MonCalamarian: return "Mon Calamarian";
                case SkillType.Ugnaught: return "Ugnaught";
            }

            return "Basic";
        }

        public static void InitializePlayerLanguages(NWPlayer player)
        {
            var race = (RacialType)player.RacialType;
            var background = (BackgroundType)player.Class1;
            var languages = new List<SkillType>(new[] { SkillType.Basic });

            switch (race)
            {
                case RacialType.Bothan:
                    languages.Add(SkillType.Bothese);
                    break;
                case RacialType.Chiss:
                    languages.Add(SkillType.Cheunh);
                    break;
                case RacialType.Zabrak:
                    languages.Add(SkillType.Zabraki);
                    break;
                case RacialType.Wookiee:
                    languages.Add(SkillType.Shyriiwook);
                    break;
                case RacialType.Twilek:
                    languages.Add(SkillType.Twileki);
                    break;
                case RacialType.Cathar:
                    languages.Add(SkillType.Catharese);
                    break;
                case RacialType.Trandoshan:
                    languages.Add(SkillType.Dosh);
                    break;
                case RacialType.Cyborg:
                    languages.Add(SkillType.Droidspeak);
                    break;
                case RacialType.Mirialan:
                    languages.Add(SkillType.Mirialan);
                    break;
                case RacialType.MonCalamari:
                    languages.Add(SkillType.MonCalamarian);
                    break;
                case RacialType.Ugnaught:
                    languages.Add(SkillType.Ugnaught);
                    break;
            }

            switch (background)
            {
                case BackgroundType.Mandalorian:
                    languages.Add(SkillType.Mandoa);
                    break;
            }

            // Fair warning: We're short-circuiting the skill system here.
            // Languages don't level up like normal skills (no stat increases, SP, etc.)
            // So it's safe to simply set the player's rank in the skill to max.

            var languageSkillIDs = languages.ConvertAll(x => (int) x);
            var pcSkills = DataService.PCSkill.GetAllByPlayerIDAndSkillIDs(player.GlobalID, languageSkillIDs).ToList();

            foreach (var pcSkill in pcSkills)
            {
                var skill = DataService.Skill.GetByID(pcSkill.SkillID);
                var maxRank = skill.MaxRank;
                var maxRankXP = SkillService.SkillXPRequirements[maxRank];

                pcSkill.Rank = maxRank;
                pcSkill.XP = maxRankXP - 1;

                DataService.SubmitDataChange(pcSkill, DatabaseActionType.Update);
            }

        }

        public static SkillType GetActiveLanguage(NWObject obj)
        {
            var ret = obj.GetLocalInt("ACTIVE_LANGUAGE");

            if (ret == 0)
            {
                return SkillType.Basic;
            }

            return (SkillType)ret;
        }

        public static void SetActiveLanguage(NWObject obj, SkillType language)
        {
            if (language == SkillType.Basic)
            {
                obj.DeleteLocalInt("ACTIVE_LANGUAGE");
            }
            else
            {
                obj.SetLocalInt("ACTIVE_LANGUAGE", (int)language);
            }
        }

        private static IEnumerable<LanguageCommand> _languages;

        public static IEnumerable<LanguageCommand> Languages
        {
            get
            {
                if (_languages == null)
                {
                    var languages = new List<LanguageCommand>
                    {
                        new LanguageCommand("Basic", SkillType.Basic, new [] { "basic" }),
                        new LanguageCommand("Bothese", SkillType.Bothese, new[] {"bothese"}),
                        new LanguageCommand("Catharese", SkillType.Catharese, new []{"catharese"}),
                        new LanguageCommand("Cheunh", SkillType.Cheunh, new []{"cheunh"}),
                        new LanguageCommand("Dosh", SkillType.Dosh, new []{"dosh"}),
                        new LanguageCommand("Droidspeak", SkillType.Droidspeak, new []{"droidspeak"}),
                        new LanguageCommand("Huttese", SkillType.Huttese, new []{"huttese"}),
                        new LanguageCommand("Mando'a", SkillType.Mandoa, new []{"mandoa"}),
                        new LanguageCommand("Mirialan", SkillType.Mirialan, new []{"mirialan"}),
                        new LanguageCommand("Mon Calamarian", SkillType.MonCalamarian, new []{"moncalamarian", "moncal"}),
                        new LanguageCommand("Shyriiwook", SkillType.Shyriiwook, new []{"shyriiwook", "wookieespeak"}),
                        new LanguageCommand("Twi'leki", SkillType.Twileki, new []{"twileki", "ryl"}),
                        new LanguageCommand("Ugnaught", SkillType.Ugnaught, new []{"ugnaught"}),
                        new LanguageCommand("Zabraki", SkillType.Zabraki, new []{"zabraki", "zabrak"}),
                    };

                    _languages = languages;
                }

                return _languages;
            }
        }
    }
}
