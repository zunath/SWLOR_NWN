﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
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
        private readonly IDataService _data;
        private readonly AppCache _cache;

        public LanguageService(
            ISkillService skillService,
            IRandomService randomService,
            IDataService data,
            AppCache cache)
        {
            _skillService = skillService;
            _randomService = randomService;
            _data = data;
            _cache = cache;
        }

        public string TranslateSnippetForListener(NWObject speaker, NWObject listener, SkillType language, string snippet)
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

            if (speaker.IsPC && !speaker.IsDM)
            {
                // Get the rank and max rank for the speaker, and garble their English text based on it.
                NWPlayer speakerAsPlayer = speaker.Object;
                int speakerSkillRank = _skillService.GetPCSkillRank(speakerAsPlayer, language);
                int speakerSkillMaxRank = _skillService.GetSkill(language).MaxRank;

                if (speakerSkillRank != speakerSkillMaxRank)
                {
                    int garbledChance = 100 - (int)(((float)speakerSkillRank / (float)speakerSkillMaxRank) * 100);

                    string[] split = snippet.Split(' ');
                    for (int i = 0; i < split.Length; ++i)
                    {
                        split[i] = new string(split[i].ToCharArray().OrderBy(s => (_randomService.Random(2) % 2) == 0).ToArray());
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
            int rank = _skillService.GetPCSkillRank(listenerAsPlayer, language);
            int maxRank = _skillService.GetSkill(language).MaxRank;

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
                // Reward exp towards the language - we scale this with character count, maxing at 50 exp for 150 characters.
                _skillService.GiveSkillXP(listenerAsPlayer, language, Math.Max(10, Math.Min(150, snippet.Length) / 3));
                listenerAsPlayer.SetLocalInt("LAST_LANGUAGE_SKILL_INCREASE_LOW", (int)(now & 0xFFFFFFFF));
                listenerAsPlayer.SetLocalInt("LAST_LANGUAGE_SKILL_INCREASE_HIGH", (int)((now >> 32) & 0xFFFFFFFF));
            }

            return textAsForeignLanguage;
        }

        public int GetColour(SkillType language)
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
            }

            return r << 24 | g << 16 | b << 8;
        }

        public string GetName(SkillType language)
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
            var pcSkills = _data.Where<PCSkill>
                (x => x.PlayerID == player.GlobalID &&
                            languages.Contains((SkillType)x.SkillID))
                .ToList();
            
            foreach (var pcSkill in pcSkills)
            {
                var skill = _data.Get<Skill>(pcSkill.SkillID);
                int maxRank = skill.MaxRank;
                int skillID = skill.ID;
                var xpRecord = _data.Single<SkillXPRequirement>(x => x.SkillID == skillID && x.Rank == maxRank);

                pcSkill.Rank = maxRank;
                pcSkill.XP = xpRecord.XP - 1;

                _data.SubmitDataChange(pcSkill, DatabaseActionType.Update);
            }
            
        }

        public SkillType GetActiveLanguage(NWObject obj)
        {
            int ret = obj.GetLocalInt("ACTIVE_LANGUAGE");

            if (ret == 0)
            {
                return SkillType.Basic;
            }

            return (SkillType)ret;
        }

        public void SetActiveLanguage(NWObject obj, SkillType language)
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

        public SkillType[] GetLanguages()
        {
            // TODO - Can this be improved? DB query based on the category?
            return new SkillType[]
            {
                SkillType.Basic,
                SkillType.Bothese,
                SkillType.Catharese,
                SkillType.Cheunh,
                SkillType.Dosh,
                SkillType.Droidspeak,
                SkillType.Huttese,
                SkillType.Mandoa,
                SkillType.Shyriiwook,
                SkillType.Twileki,
                SkillType.Zabraki,
            };
        }
    }
}
