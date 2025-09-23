using System.Text;
using SWLOR.Component.Communication.Contracts;
using SWLOR.Component.Communication.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Entity;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Communication.Service
{
    public class Language : ILanguageService
    {
        private readonly IDatabaseService _db;
        private readonly IRandomService _random;
        private readonly ISkillService _skillService;
        private readonly IStatusEffectService _statusEffectService;
        private Dictionary<SkillType, ITranslator> _translators = new();
        private readonly TranslatorGeneric _genericTranslator = new();

        public Language(IDatabaseService db, IRandomService random, ISkillService skillService, IStatusEffectService statusEffectService)
        {
            _db = db;
            _random = random;
            _skillService = skillService;
            _statusEffectService = statusEffectService;
        }

        /// <summary>
        /// When the module loads, create translators for every language and store them into cache.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public void LoadTranslators()
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
                { SkillType.KelDor, new TranslatorKelDor() },
                { SkillType.Nautila, new TranslatorNautila() },
                { SkillType.Ewokese, new TranslatorEwokese() },
            };
        }

        public string TranslateSnippetForListener(uint speaker, uint listener, SkillType language, string snippet)
        {
            var translator = _translators.ContainsKey(language) ? _translators[language] : _genericTranslator;
            var languageSkill = _skillService.GetSkillDetails(language);

            if (GetIsPC(speaker))
            {
                var playerId = GetObjectUUID(speaker);
                var dbSpeaker = _db.Get<Player>(playerId);
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
                        if (_random.Next(100) <= garbledChance)
                        {
                            split[i] = new string(split[i].ToCharArray().OrderBy(s => (_random.Next(2) % 2) == 0).ToArray());
                        }
                    }

                    snippet = split.Aggregate((a, b) => a + " " + b);
                }
            }

            if (!GetIsPC(listener) || GetIsDM(listener) || GetIsDMPossessed(listener))
            {
                // Short circuit for a DM, NPC, or DM-possessed creature - they will always understand the text.
                return snippet;
            }

            // Let's grab the max rank for the listener skill, and then we roll for a successful translate based on that.
            var listenerId = GetObjectUUID(listener);
            var dbListener = _db.Get<Player>(listenerId);
            var rank = dbListener == null ?
                languageSkill.MaxRank :
                dbListener.Skills[language].Rank;
            var maxRank = languageSkill.MaxRank;

            // Check for the Comprehend Speech concentration ability.
            var grantSenseXP = false;
            var statusEffectBonus = 0;
            if (_statusEffectService.HasStatusEffect(listener, StatusEffectType.ComprehendSpeech1))
                statusEffectBonus = 5;
            else if (_statusEffectService.HasStatusEffect(listener, StatusEffectType.ComprehendSpeech2))
                statusEffectBonus = 10;
            else if (_statusEffectService.HasStatusEffect(listener, StatusEffectType.ComprehendSpeech3))
                statusEffectBonus = 15;
            else if (_statusEffectService.HasStatusEffect(listener, StatusEffectType.ComprehendSpeech4))
                statusEffectBonus = 20;

            if (statusEffectBonus > 0)
            {
                rank += statusEffectBonus;
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
                var englishChance = (int)((rank / (float)maxRank) * 100);

                var originalSplit = snippet.Split(' ');
                var foreignSplit = textAsForeignLanguage.Split(' ');

                var endResult = new StringBuilder();

                // WARNING: We're making the assumption that originalSplit.Length == foreignSplit.Length.
                // If this assumption changes, the below logic needs to change too.
                for (var i = 0; i < originalSplit.Length; ++i)
                {
                    if (_random.Next(100) <= englishChance)
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

                _skillService.GiveSkillXP(listener, language, amount, false, false);

                // Grant Force XP if player is concentrating Comprehend Speech.
                if (grantSenseXP)
                    _skillService.GiveSkillXP(listener, SkillType.Force, amount * 10, false, false);

                SetLocalInt(listener, "LAST_LANGUAGE_SKILL_INCREASE_LOW", (int)(now & 0xFFFFFFFF));
                SetLocalInt(listener, "LAST_LANGUAGE_SKILL_INCREASE_HIGH", (int)((now >> 32) & 0xFFFFFFFF));
            }

            return textAsForeignLanguage;
        }

        public (byte, byte, byte) GetColor(SkillType language)
        {
            byte r = 0;
            byte g = 0;
            byte b = 0;

            switch (language)
            {
                case SkillType.Basic: r = 255; g = 255; b = 255; break;
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
                case SkillType.Nautila: r = 76; g = 230; b = 104; break;
                case SkillType.Ewokese: r = 112; g = 28; b = 28; break;
            }

            return (r, g, b);
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
                case SkillType.Nautila: return "Nautila";
                case SkillType.Ewokese: return "Ewokese";
            }

            return "Basic";
        }

        public SkillType GetActiveLanguage(uint obj)
        {
            var ret = GetLocalInt(obj, "ACTIVE_LANGUAGE");

            if (ret == 0)
            {
                return SkillType.Basic;
            }

            return (SkillType)ret;
        }

        public void SetActiveLanguage(uint obj, SkillType language)
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

        private IEnumerable<LanguageCommand> _languages;

        public IEnumerable<LanguageCommand> Languages
        {
            get
            {
                if (_languages == null)
                {
                    var languages = new List<LanguageCommand>
                    {
                        new("Basic", SkillType.Basic, new [] { "basic" }),
                        new("Bothese", SkillType.Bothese, new[] {"bothese"}),
                        new("Catharese", SkillType.Catharese, new []{"catharese"}),
                        new("Cheunh", SkillType.Cheunh, new []{"cheunh"}),
                        new("Dosh", SkillType.Dosh, new []{"dosh"}),
                        new("Droidspeak", SkillType.Droidspeak, new []{"droidspeak"}),
                        new("Huttese", SkillType.Huttese, new []{"huttese"}),
                        new("KelDor", SkillType.KelDor, new []{"keldor"}),
                        new("Mando'a", SkillType.Mandoa, new []{"mandoa"}),
                        new("Mirialan", SkillType.Mirialan, new []{"mirialan"}),
                        new("Mon Calamarian", SkillType.MonCalamarian, new []{"moncalamarian", "moncal"}),
                        new("Nautila", SkillType.Nautila, new []{ "nautilan" }),
                        new("Rodese", SkillType.Rodese, new []{"rodese", "rodian"}),
                        new("Shyriiwook", SkillType.Shyriiwook, new []{"shyriiwook", "wookieespeak"}),
                        new("Togruti", SkillType.Togruti, new []{"togruti"}),
                        new("Twi'leki", SkillType.Twileki, new []{"twileki", "ryl"}),
                        new("Ugnaught", SkillType.Ugnaught, new []{"ugnaught"}),
                        new("Zabraki", SkillType.Zabraki, new []{"zabraki", "zabrak"}),
                        new("Ewokese", SkillType.Ewokese, new []{"ewokese", "yubnub"}),
                    };

                    _languages = languages;
                }

                return _languages;
            }
        }
    }
}
