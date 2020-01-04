using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.NWScript.Enumerations;
using Skill = SWLOR.Game.Server.Enumeration.Skill;


namespace SWLOR.Game.Server.Service
{
    public static class LanguageService
    {
        public static string TranslateSnippetForListener(NWObject speaker, NWObject listener, Skill language, string snippet)
        {
            Dictionary<Skill, Type> map = new Dictionary<Skill, Type>
            {
                { Skill.Bothese, typeof(TranslatorBothese) },
                { Skill.Catharese, typeof(TranslatorCatharese) },
                { Skill.Cheunh, typeof(TranslatorCheunh) },
                { Skill.Dosh, typeof(TranslatorDosh) },
                { Skill.Droidspeak, typeof(TranslatorDroidspeak) },
                { Skill.Huttese, typeof(TranslatorHuttese) },
                { Skill.Mandoa, typeof(TranslatorMandoa) },
                { Skill.Shyriiwook, typeof(TranslatorShyriiwook) },
                { Skill.Twileki, typeof(TranslatorTwileki) },
                { Skill.Zabraki, typeof(TranslatorZabraki) },
                { Skill.Mirialan, typeof(TranslatorMirialan) },
                { Skill.MonCalamarian, typeof(TranslatorMonCalamarian) },
                { Skill.Ugnaught, typeof(TranslatorUgnaught) }
            };

            Type type = typeof(TranslatorGeneric);
            map.TryGetValue(language, out type);
            ITranslator translator = (ITranslator)Activator.CreateInstance(type);

            if (speaker.IsPC && !speaker.IsDM)
            {
                // Get the rank and max rank for the speaker, and garble their English text based on it.
                NWPlayer speakerAsPlayer = speaker.Object;
                int speakerSkillRank = SkillService.GetPCSkillRank(speakerAsPlayer, language);
                int speakerSkillMaxRank = SkillService.GetSkill(language).MaxRank;

                if (speakerSkillRank != speakerSkillMaxRank)
                {
                    int garbledChance = 100 - (int)(((float)speakerSkillRank / (float)speakerSkillMaxRank) * 100);

                    string[] split = snippet.Split(' ');
                    for (int i = 0; i < split.Length; ++i)
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
            int rank = SkillService.GetPCSkillRank(listenerAsPlayer, language);
            int maxRank = SkillService.GetSkill(language).MaxRank;

            // Check for the Comprehend Speech concentration ability.
            Player dbPlayer = DataService.Player.GetByID(listenerAsPlayer.GlobalID);
            bool grantSenseXP = false;
            if (dbPlayer.ActiveConcentrationPerkID == (int)PerkType.ComprehendSpeech)
            {
                int bonus = 5 * dbPlayer.ActiveConcentrationTier;
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

            string textAsForeignLanguage = translator.Translate(snippet);

            if (rank != 0)
            {
                int englishChance = (int)(((float)rank / (float)maxRank) * 100);

                string[] originalSplit = snippet.Split(' ');
                string[] foreignSplit = textAsForeignLanguage.Split(' ');

                StringBuilder endResult = new StringBuilder();

                // WARNING: We're making the assumption that originalSplit.Length == foreignSplit.Length.
                // If this assumption changes, the below logic needs to change too.
                for (int i = 0; i < originalSplit.Length; ++i)
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

            long now = DateTime.Now.Ticks;
            int lastSkillUpLow = listenerAsPlayer.GetLocalInt("LAST_LANGUAGE_SKILL_INCREASE_LOW");
            int lastSkillUpHigh = listenerAsPlayer.GetLocalInt("LAST_LANGUAGE_SKILL_INCREASE_HIGH");
            long lastSkillUp = lastSkillUpHigh;
            lastSkillUp = (lastSkillUp << 32) | (uint)lastSkillUpLow;
            long differenceInSeconds = (now - lastSkillUp) / 10000000;

            if (differenceInSeconds / 60 >= 5)
            {
                int amount = Math.Max(10, Math.Min(150, snippet.Length) / 3);
                // Reward exp towards the language - we scale this with character count, maxing at 50 exp for 150 characters.
                SkillService.GiveSkillXP(listenerAsPlayer, language, amount);

                // Grant Sense XP if player is concentrating Comprehend Speech.
                if (grantSenseXP)
                    SkillService.GiveSkillXP(listenerAsPlayer, Skill.ForceSense, amount * 10);

                listenerAsPlayer.SetLocalInt("LAST_LANGUAGE_SKILL_INCREASE_LOW", (int)(now & 0xFFFFFFFF));
                listenerAsPlayer.SetLocalInt("LAST_LANGUAGE_SKILL_INCREASE_HIGH", (int)((now >> 32) & 0xFFFFFFFF));
            }

            return textAsForeignLanguage;
        }

        public static int GetColour(Skill language)
        {
            byte r = 0;
            byte g = 0;
            byte b = 0;

            switch (language)
            {
                case Skill.Bothese: r = 132; g = 56; b = 18; break;
                case Skill.Catharese: r = 235; g = 235; b = 199; break;
                case Skill.Cheunh: r = 82; g = 143; b = 174; break;
                case Skill.Dosh: r = 166; g = 181; b = 73; break;
                case Skill.Droidspeak: r = 192; g = 192; b = 192; break;
                case Skill.Huttese: r = 162; g = 74; b = 10; break;
                case Skill.Mandoa: r = 255; g = 215; b = 0; break;
                case Skill.Shyriiwook: r = 149; g = 125; b = 86; break;
                case Skill.Twileki: r = 65; g = 105; b = 225; break;
                case Skill.Zabraki: r = 255; g = 102; b = 102; break;
                case Skill.Mirialan: r = 77; g = 230; b = 215; break;
                case Skill.MonCalamarian: r = 128; g = 128; b = 192; break;
                case Skill.Ugnaught: r = 255; g = 193; b = 233; break;
            }

            return r << 24 | g << 16 | b << 8;
        }

        public static string GetName(Skill language)
        {
            switch (language)
            {
                case Skill.Bothese: return "Bothese";
                case Skill.Catharese: return "Catharese";
                case Skill.Cheunh: return "Cheunh";
                case Skill.Dosh: return "Dosh";
                case Skill.Droidspeak: return "Droidspeak";
                case Skill.Huttese: return "Huttese";
                case Skill.Mandoa: return "Mandoa";
                case Skill.Shyriiwook: return "Shyriiwook";
                case Skill.Twileki: return "Twi'leki";
                case Skill.Zabraki: return "Zabraki";
                case Skill.Mirialan: return "Mirialan";
                case Skill.MonCalamarian: return "Mon Calamarian";
                case Skill.Ugnaught: return "Ugnaught";
            }

            return "Basic";
        }

        public static void InitializePlayerLanguages(NWPlayer player)
        {
            RacialType race = (RacialType)player.RacialType;
            ClassType background = (ClassType)player.Class1;
            var languages = new List<Skill>(new[] { Skill.Basic });

            switch (race)
            {
                case RacialType.Bothan:
                    languages.Add(Skill.Bothese);
                    break;
                case RacialType.Chiss:
                    languages.Add(Skill.Cheunh);
                    break;
                case RacialType.Zabrak:
                    languages.Add(Skill.Zabraki);
                    break;
                case RacialType.Wookiee:
                    languages.Add(Skill.Shyriiwook);
                    break;
                case RacialType.Twilek:
                    languages.Add(Skill.Twileki);
                    break;
                case RacialType.Cathar:
                    languages.Add(Skill.Catharese);
                    break;
                case RacialType.Trandoshan:
                    languages.Add(Skill.Dosh);
                    break;
                case RacialType.Cyborg:
                    languages.Add(Skill.Droidspeak);
                    break;
                case RacialType.Mirialan:
                    languages.Add(Skill.Mirialan);
                    break;
                case RacialType.MonCalamari:
                    languages.Add(Skill.MonCalamarian);
                    break;
                case RacialType.Ugnaught:
                    languages.Add(Skill.Ugnaught);
                    break;
            }

            switch (background)
            {
                case ClassType.Mandalorian:
                    languages.Add(Skill.Mandoa);
                    break;
            }

            // Fair warning: We're short-circuiting the skill system here.
            // Languages don't level up like normal skills (no stat increases, SP, etc.)
            // So it's safe to simply set the player's rank in the skill to max.

            var pcSkills = DataService.PCSkill.GetAllByPlayerIDAndSkillIDs(player.GlobalID, languages).ToList();

            foreach (var pcSkill in pcSkills)
            {
                var skill = SkillService.GetSkill(pcSkill.SkillID);
                int maxRank = skill.MaxRank;
                int maxRankXP = SkillService.SkillXPRequirements[maxRank];

                pcSkill.Rank = maxRank;
                pcSkill.XP = maxRankXP - 1;

                DataService.SubmitDataChange(pcSkill, DatabaseActionType.Update);
            }

        }

        public static Skill GetActiveLanguage(NWObject obj)
        {
            int ret = obj.GetLocalInt("ACTIVE_LANGUAGE");

            if (ret == 0)
            {
                return Skill.Basic;
            }

            return (Skill)ret;
        }

        public static void SetActiveLanguage(NWObject obj, Skill language)
        {
            if (language == Skill.Basic)
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
                        new LanguageCommand("Basic", Skill.Basic, new [] { "basic" }),
                        new LanguageCommand("Bothese", Skill.Bothese, new[] {"bothese"}),
                        new LanguageCommand("Catharese", Skill.Catharese, new []{"catharese"}),
                        new LanguageCommand("Cheunh", Skill.Cheunh, new []{"cheunh"}),
                        new LanguageCommand("Dosh", Skill.Dosh, new []{"dosh"}),
                        new LanguageCommand("Droidspeak", Skill.Droidspeak, new []{"droidspeak"}),
                        new LanguageCommand("Huttese", Skill.Huttese, new []{"huttese"}),
                        new LanguageCommand("Mando'a", Skill.Mandoa, new []{"mandoa"}),
                        new LanguageCommand("Mirialan", Skill.Mirialan, new []{"mirialan"}),
                        new LanguageCommand("Mon Calamarian", Skill.MonCalamarian, new []{"moncalamarian", "moncal"}),
                        new LanguageCommand("Shyriiwook", Skill.Shyriiwook, new []{"shyriiwook", "wookieespeak"}),
                        new LanguageCommand("Twi'leki", Skill.Twileki, new []{"twileki", "ryl"}),
                        new LanguageCommand("Ugnaught", Skill.Ugnaught, new []{"ugnaught"}),
                        new LanguageCommand("Zabraki", Skill.Zabraki, new []{"zabraki", "zabrak"}),
                    };

                    _languages = languages;
                }

                return _languages;
            }
        }
    }
}
