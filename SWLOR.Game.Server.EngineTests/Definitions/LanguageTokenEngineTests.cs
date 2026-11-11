using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class LanguageTokenEngineTests
    {
        private static Player PlayerWithTokens(int tokens)
        {
            var player = new Player();
            player.Currencies[CurrencyType.LanguageToken] = tokens;
            return player;
        }

        [EngineTest("A language token masters a partly known language", Category = "LanguageToken")]
        public static Task MastersPartlyKnownLanguage(EngineTestContext ctx)
        {
            var player = PlayerWithTokens(2);
            player.Skills[SkillType.Huttese] = new PlayerSkill { Rank = 4, XP = 10 };

            ctx.Assert(Language.TryMasterWithToken(player, SkillType.Huttese), "Token is spent");
            ctx.AssertEqual(20, player.Skills[SkillType.Huttese].Rank, "Language rank");
            ctx.AssertEqual(Skill.GetRequiredXP(20) - 1, player.Skills[SkillType.Huttese].XP, "Language XP");
            ctx.AssertEqual(1, player.Currencies[CurrencyType.LanguageToken], "Remaining tokens");
            return Task.CompletedTask;
        }

        [EngineTest("A language token masters an unknown language", Category = "LanguageToken")]
        public static Task MastersUnknownLanguage(EngineTestContext ctx)
        {
            var player = PlayerWithTokens(1);

            ctx.Assert(Language.TryMasterWithToken(player, SkillType.Bothese), "Token is spent");
            ctx.AssertEqual(20, player.Skills[SkillType.Bothese].Rank, "Language rank");
            ctx.AssertEqual(0, player.Currencies[CurrencyType.LanguageToken], "Remaining tokens");
            return Task.CompletedTask;
        }

        [EngineTest("A language token is refused without a token, for mastered languages and for other skills", Category = "LanguageToken")]
        public static Task RefusesInvalidSpends(EngineTestContext ctx)
        {
            var noTokens = new Player();
            ctx.Assert(!Language.TryMasterWithToken(noTokens, SkillType.Bothese), "No token is refused");
            ctx.Assert(!noTokens.Skills.ContainsKey(SkillType.Bothese), "Refused spend leaves the language unknown");

            var mastered = PlayerWithTokens(1);
            Language.Master(mastered, SkillType.Basic);
            ctx.Assert(!Language.TryMasterWithToken(mastered, SkillType.Basic), "Mastered language is refused");
            ctx.Assert(!Language.GetUnmasteredLanguages(mastered).Contains(SkillType.Basic), "Mastered language is not offered");
            ctx.AssertEqual(1, mastered.Currencies[CurrencyType.LanguageToken], "Token kept after mastered refusal");

            var otherSkill = PlayerWithTokens(1);
            ctx.Assert(!Language.TryMasterWithToken(otherSkill, SkillType.Rifle), "Non-language skill is refused");
            ctx.AssertEqual(1, otherSkill.Currencies[CurrencyType.LanguageToken], "Token kept after non-language refusal");
            return Task.CompletedTask;
        }
    }
}
