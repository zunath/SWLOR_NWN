using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LanguageService;
using SWLOR.Game.Server.Service.LanguageService.Bothese;
using SWLOR.Game.Server.Service.LanguageService.Catharese;
using SWLOR.Game.Server.Service.LanguageService.Cheunh;
using SWLOR.Game.Server.Service.LanguageService.Dosh;
using SWLOR.Game.Server.Service.LanguageService.Huttese;
using SWLOR.Game.Server.Service.LanguageService.KelDor;
using SWLOR.Game.Server.Service.LanguageService.Mandoa;
using SWLOR.Game.Server.Service.LanguageService.Mirialan;
using SWLOR.Game.Server.Service.LanguageService.MonCalamarian;
using SWLOR.Game.Server.Service.LanguageService.Rodese;
using SWLOR.Game.Server.Service.LanguageService.Togruti;
using SWLOR.Game.Server.Service.LanguageService.Twileki;
using SWLOR.Game.Server.Service.LanguageService.Ugnaught;
using SWLOR.Game.Server.Service.LanguageService.Zabraki;
using SWLOR.Game.Server.Service.StatusEffectService;
using SkillType = SWLOR.Game.Server.Service.SkillService.SkillType;

namespace SWLOR.Game.Server.Service
{
    public static class Language
    {
        private static Dictionary<SkillType, ITranslator> _translators = new Dictionary<SkillType, ITranslator>();
        private static readonly TranslatorGeneric _genericTranslator = new TranslatorGeneric();

        /// <summary>
        /// When the module loads, create translators for every language and store them into cache.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void LoadTranslators()
        {
            _translators = new Dictionary<SkillType, ITranslator>
            {
                { SkillType.Bothese, new TranslatorBothese() },
                { SkillType.Catharese, new TranslatorCatharese() },
                { SkillType.Cheunh, new TranslatorCheunh() },
                { SkillType.Dosh, new TranslatorDosh() },
                { SkillType.Droidspeak, new TranslatorDroidspeak() },
                { SkillType.Huttese, new TranslatorHuttese() },
                { SkillType.Mandoa,  new TranslatorMandoa() },
                { SkillType.Shyriiwook, new TranslatorShyriiwook() },
                { SkillType.Twileki, new TranslatorTwileki() },
                { SkillType.Zabraki, new TranslatorZabraki() },
                { SkillType.Togruti, new TranslatorTogruti() },
                { SkillType.Rodese, new TranslatorRodese() },
                { SkillType.Mirialan, new TranslatorMirialan() },
                { SkillType.MonCalamarian, new TranslatorMonCalamarian() },
                { SkillType.Ugnaught, new TranslatorUgnaught() },
                { SkillType.KelDor, new TranslatorKelDor() }
            };
        }

        public static string TranslateSnippetForListener(uint speaker, uint listener, SkillType language, string snippet, out string languageText, out bool isMaxRank, out bool isMinRank)
        {
            isMaxRank = false;
            isMinRank = false;
            languageText = "";
            var translator = _translators.ContainsKey(language) ? _translators[language] : _genericTranslator;
            var languageSkill = Skill.GetSkillDetails(language);


            if (GetIsPC(speaker))
            {
                var playerId = GetObjectUUID(speaker);
                var dbSpeaker = DB.Get<Player>(playerId);
                // Get the rank and max rank for the speaker, and garble their English text based on it.
                var speakerSkillRank = dbSpeaker == null ? 
                    languageSkill.MaxRank : 
                    dbSpeaker.Skills[language].Rank;



                if (speakerSkillRank != languageSkill.MaxRank)
                {
                    var garbledChance = 100 - (int)((speakerSkillRank / (float)languageSkill.MaxRank) * 100);

                    var split = snippet.Split(' ');
                    for (var i = 0; i < split.Length; ++i)
                    {
                        if (Random.Next(100) <= garbledChance)
                        {
                            split[i] = new string(split[i].ToCharArray().OrderBy(s => (Random.Next(2) % 2) == 0).ToArray());
                        }
                    }

                    snippet = split.Aggregate((a, b) => a + " " + b);
                }
                else
                    isMaxRank = true;
            }

            if (!GetIsPC(listener) || GetIsDM(listener))
            {
                // Short circuit for a DM or NPC - they will always understand the text.
                return snippet;
            }

            // Let's grab the max rank for the listener skill, and then we roll for a successful translate based on that.
            var listenerId = GetObjectUUID(listener);
            var dbListener = DB.Get<Player>(listenerId);
            var rank = dbListener == null ? 
                languageSkill.MaxRank : 
                dbListener.Skills[language].Rank;
            var maxRank = languageSkill.MaxRank;

            // Check for the Comprehend Speech concentration ability.
            var grantSenseXP = false;
            var statusEffectBonus = 0;
            if (StatusEffect.HasStatusEffect(listener, StatusEffectType.ComprehendSpeech1))
                statusEffectBonus = 5;
            else if (StatusEffect.HasStatusEffect(listener, StatusEffectType.ComprehendSpeech2))
                statusEffectBonus = 10;
            else if (StatusEffect.HasStatusEffect(listener, StatusEffectType.ComprehendSpeech3))
                statusEffectBonus = 15;
            else if (StatusEffect.HasStatusEffect(listener, StatusEffectType.ComprehendSpeech4))
                statusEffectBonus = 20;

            if (statusEffectBonus > 0)
            {
                rank += statusEffectBonus;
                grantSenseXP = true;
            }

            // Ensure we don't go over the maximum.
            if (rank > maxRank)
                rank = maxRank;


            var englishChance = (int)((rank / (float)maxRank) * 100);
            string textForListener = "";

            languageText = translator.Translate(snippet, englishChance, out textForListener);

                
            isMaxRank = rank == maxRank;

            if (rank == maxRank || speaker == listener)
            {
                // Guaranteed success - return original.
                return snippet;
            }

            isMinRank = rank == 0;

            //20221126 Hans: Now the partially comprehended version is created as its translated  
            if (!(translator is BaseRecursiveLanguageTranslator))
            {
                textForListener = languageText;
                if (rank != 0)
                {
                    var originalSplit = snippet.Split(' ');
                    var foreignSplit = languageText.Split(' ');

                    var endResult = new StringBuilder();

                    // WARNING: We're making the assumption that originalSplit.Length == foreignSplit.Length.
                    // If this assumption changes, the below logic needs to change too.
                    for (var i = 0; i < originalSplit.Length; ++i)
                    {
                        if (Random.Next(100) <= englishChance)
                        {
                            endResult.Append(originalSplit[i]);
                        }
                        else
                        {
                            endResult.Append(foreignSplit[i]);
                        }

                        endResult.Append(" ");
                    }

                    textForListener = endResult.ToString();
                }
            }
            

            var now = DateTime.Now.Ticks;
            var lastSkillUpLow = GetLocalInt(listener, "LAST_LANGUAGE_SKILL_INCREASE_LOW");
            var lastSkillUpHigh = GetLocalInt(listener, "LAST_LANGUAGE_SKILL_INCREASE_HIGH");
            long lastSkillUp = lastSkillUpHigh;
            lastSkillUp = (lastSkillUp << 32) | (uint)lastSkillUpLow;
            var differenceInSeconds = (now - lastSkillUp) / 10000000;

            if (differenceInSeconds / 60 >= 2)
            {
                // Reward exp towards the language - we scale this with character count, maxing at 50 exp for 150 characters.
                // A bonus is given if listener's Social modifier is greater than zero.
                var amount = Math.Max(10, Math.Min(150, snippet.Length) / 3);
                var socialModifier = GetAbilityModifier(AbilityType.Social, listener);
                if (socialModifier > 0)
                {
                    amount += socialModifier * 10;
                }

                Skill.GiveSkillXP(listener, language, amount);

                // Grant Force XP if player is concentrating Comprehend Speech.
                if (grantSenseXP)
                    Skill.GiveSkillXP(listener, SkillType.Force, amount * 10);

                SetLocalInt(listener, "LAST_LANGUAGE_SKILL_INCREASE_LOW", (int)(now & 0xFFFFFFFF));
                SetLocalInt(listener, "LAST_LANGUAGE_SKILL_INCREASE_HIGH", (int)((now >> 32) & 0xFFFFFFFF));
            }

            return textForListener;
        }

        public static int GetColor(SkillType language)
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
                case SkillType.KelDor: r = 162; g = 162; b = 0; break;
                case SkillType.Mandoa: r = 255; g = 215; b = 0; break;
                case SkillType.Rodese: r = 82; g = 255; b = 82; break;
                case SkillType.Shyriiwook: r = 149; g = 125; b = 86; break;
                case SkillType.Togruti: r = 82; g = 82; b = 255; break;
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
                case SkillType.KelDor: return "KelDor";
                case SkillType.Mandoa: return "Mandoa";
                case SkillType.Rodese: return "Rodese";
                case SkillType.Shyriiwook: return "Shyriiwook";
                case SkillType.Togruti: return "Togruti";
                case SkillType.Twileki: return "Twi'leki";
                case SkillType.Zabraki: return "Zabraki";
                case SkillType.Mirialan: return "Mirialan";
                case SkillType.MonCalamarian: return "Mon Calamarian";
                case SkillType.Ugnaught: return "Ugnaught";
            }

            return "Basic";
        }

        public static SkillType GetActiveLanguage(uint obj)
        {
            var ret = GetLocalInt(obj, "ACTIVE_LANGUAGE");

            if (ret == 0)
            {
                return SkillType.Basic;
            }

            return (SkillType)ret;
        }

        public static void SetActiveLanguage(uint obj, SkillType language)
        {
            if (language == SkillType.Basic)
            {
                DeleteLocalInt(obj, "ACTIVE_LANGUAGE");
            }
            else
            {
                SetLocalInt(obj, "ACTIVE_LANGUAGE", (int)language);
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
                        new LanguageCommand("Basic", SkillType.Basic, new [] { "basic","ba" }),
                        new LanguageCommand("Bothese", SkillType.Bothese, new[] {"bothese","both","bo"}),
                        new LanguageCommand("Catharese", SkillType.Catharese, new []{"catharese", "cath", "ca"}),
                        new LanguageCommand("Cheunh", SkillType.Cheunh, new []{"cheunh", "chiss", "ch"}),
                        new LanguageCommand("Dosh", SkillType.Dosh, new []{"dosh", "trandoshian", "do"}),
                        new LanguageCommand("Droidspeak", SkillType.Droidspeak, new []{"droidspeak","binary", "dr"}),
                        new LanguageCommand("Huttese", SkillType.Huttese, new []{"huttese", "hutt", "hu"}),
                        new LanguageCommand("KelDor", SkillType.KelDor, new []{"keldor", "kel", "ke"}),
                        new LanguageCommand("Mando'a", SkillType.Mandoa, new []{"mandoa", "mando", "ma"}),
                        new LanguageCommand("Mirialan", SkillType.Mirialan, new []{"mirialan","mir", "mi"}),
                        new LanguageCommand("Mon Calamarian", SkillType.MonCalamarian, new []{"moncalamarian", "moncal", "mo"}),
                        new LanguageCommand("Rodese", SkillType.Rodese, new []{"rodese", "rodian", "ro"}),
                        new LanguageCommand("Shyriiwook", SkillType.Shyriiwook, new []{"shyriiwook", "wookieespeak", "sh"}),
                        new LanguageCommand("Togruti", SkillType.Togruti, new []{"togruti", "to"}),
                        new LanguageCommand("Twi'leki", SkillType.Twileki, new []{"twileki", "ryl", "tw"}),
                        new LanguageCommand("Ugnaught", SkillType.Ugnaught, new []{"ugnaught", "ug"}),
                        new LanguageCommand("Zabraki", SkillType.Zabraki, new []{"zabraki", "zabrak", "za"}),
                    };

                    _languages = languages;
                }

                return _languages;
            }
        }
    }
}
