using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Language;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class LanguageService : ILanguageService
    {
        private readonly ISkillService _skillService;
        private readonly IRandomService _randomService;
        private readonly IDataContext _db;

        public LanguageService(
            ISkillService skillService,
            IRandomService randomService,
            IDataContext db)
        {
            _skillService = skillService;
            _randomService = randomService;
            _db = db;
        }

        public string TranslateSnippetForListener(NWPlayer player, SkillType language, string snippet)
        {
            Dictionary<SkillType, Type> map = new Dictionary<SkillType, Type>
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
                { SkillType.Zabraki, typeof(TranslatorZabraki) }
            };

            Type type = typeof(TranslatorGeneric);
            map.TryGetValue(language, out type);
            ITranslator translator = Activator.CreateInstance(type) as ITranslator;

            // This gives us the thing written in Bothese.
            string textAsForeignLanguage = translator.Translate(snippet);

            // Now we know what the English text is and we know what the Bothese text is.
            // Let's grab the max rank for our skill, and then we roll for a successful translate based on that.

            PCSkill skill = _skillService.GetPCSkill(player, language);
            int rank = skill.Rank;
            int maxRank = skill.Skill.MaxRank;

            if (rank == maxRank)
            {
                // Guaranteed success - return original.
                return snippet;
            }
            else if (rank == 0)
            {
                // Guaranteed failure - return translated.
                return textAsForeignLanguage;
            }

            int englishChance = (int)(((float)rank / (float)maxRank) * 100);

            string[] originalSplit = snippet.Split(' ');
            string[] foreignSplit = textAsForeignLanguage.Split(' ');

            StringBuilder endResult = new StringBuilder();

            // WARNING: We're making the assumption that originalSplit.Length == foreignSplit.Length.
            // If this assumption changes, the below logic needs to change too.
            for (int i = 0; i < originalSplit.Length; ++i)
            {
                if (_randomService.Random(100) <= englishChance)
                {
                    endResult.Append(originalSplit[i]);
                }
                else
                {
                    endResult.Append(foreignSplit[i]);
                }

                endResult.Append(" ");
            }

            // TODO: Skill increase
            return endResult.ToString();
        }

        public int GetColour(SkillType language)
        {
            byte r;
            byte g;
            byte b;

            // TODO - Database
            r = 100;
            g = 149;
            b = 237;

            return r << 24 | g << 16 | b << 8;
        }

        public string GetName(SkillType language)
        {
            // TODO - Database???

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
            }

            return "Basic";
        }



        public void InitializePlayerLanguages(NWPlayer player)
        {
            CustomRaceType race = (CustomRaceType)player.RacialType;
            var languages = new List<SkillType>(new[] { SkillType.Basic });

            switch (race)
            {
                case CustomRaceType.Bothan:
                    languages.Add(SkillType.Bothese);
                    break;
                case CustomRaceType.Chiss:
                    languages.Add(SkillType.Cheunh);
                    break;
                case CustomRaceType.Zabrak:
                    languages.Add(SkillType.Zabraki);
                    break;
                case CustomRaceType.Wookiee:
                    languages.Add(SkillType.Shyriiwook);
                    break;
                case CustomRaceType.Twilek:
                    languages.Add(SkillType.Twileki);
                    break;
                case CustomRaceType.Cathar:
                    languages.Add(SkillType.Catharese);
                    break;
                case CustomRaceType.Trandoshan:
                    languages.Add(SkillType.Dosh);
                    break;
            }

            // Fair warning: We're short-circuiting the skill system here.
            // Languages don't level up like normal skills (no stat increases, SP, etc.)
            // So it's safe to simply set the player's rank in the skill to max.
            var dbSkills = _db.PCSkills
                .Where(x => languages.Contains((SkillType)x.SkillID))
                .ToList();
            foreach(var skill in dbSkills)
            {
                int maxRank = skill.Skill.MaxRank;
                int skillID = skill.SkillID;
                var xpRecord = skill.Skill.SkillXPRequirements.Single(x => x.SkillID == skillID && x.Rank == maxRank);

                skill.Rank = maxRank;
                skill.XP = xpRecord.XP - 1;
            }

            _db.SaveChanges();
        }

    }
}
