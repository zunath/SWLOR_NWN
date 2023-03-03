using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class ForcePerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            ForcePush();
            BurstOfSpeed();
            ThrowLightsaber();
            ForceStun();
            ComprehendSpeech();
            BattleInsight();
            MindTrick();
            ForceHeal();
            ForceBurst();
            ForceBody();
            ForceDrain();
            ForceLightning();
            ForceMind();
            Premonition();
            Disturbance();
            Benevolence();
            ForceValor();
            ForceSpark();
            CreepingTerror();
            ForceRage();
            ThrowRock();
            ForceInspiration();

            return _builder.Build();
        }

        private void ForcePush()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ForcePush)
                .Name("Force Push")

                .AddPerkLevel()
                .Description("Attempt to knockdown a target for 2 seconds with an 8DC fortitude check. If resisted, target is slowed for 2 seconds. DC scales with WIL.")
                .Price(1)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush1)

                .AddPerkLevel()
                .Description("Attempt to knockdown a target for 2 seconds with a 12DC fortitude check. If resisted, target is slowed for 2 seconds. DC scales with WIL.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush2)

                .AddPerkLevel()
                .Description("Attempt to knockdown a target for 2 seconds with a 14DC fortitude check. If resisted, target is slowed for 2 seconds. DC scales with WIL.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush3)

                .AddPerkLevel()
                .Description("Attempt to knockdown a target for 2 seconds with a 16DC fortitude check. If resisted, target is slowed for 2 seconds. DC scales with WIL.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush4);
        }

        private void ThrowRock()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ThrowRock)
                .Name("Throw Rock")

                .AddPerkLevel()
                .Description("Telekinetically launches a chunk of the environment at the enemy. Deals 10 physical DMG to a single target.")
                .Price(1)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowRock1)

                .AddPerkLevel()
                .Description("Telekinetically launches a chunk of the environment at the enemy. Deals 15 physical DMG to a single target.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowRock2)

                .AddPerkLevel()
                .Description("Telekinetically launches a chunk of the environment at the enemy. Deals 25 physical DMG to a single target.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowRock3)

                .AddPerkLevel()
                .Description("Telekinetically launches a chunk of the environment at the enemy. Deals 34 physical DMG to a single target.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowRock4)

                .AddPerkLevel()
                .Description("Telekinetically launches a chunk of the environment at the enemy. Deals 43 physical DMG to a single target.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowRock5);
        }

        private void BurstOfSpeed()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.BurstOfSpeed)
                .Name("Burst of Speed")

                .AddPerkLevel()
                .Description("Increases the speed of your target by 15% and increases evasion by 5 for ten minutes.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.BurstOfSpeed1)                

                .AddPerkLevel()
                .Description("Increases the speed of your target by 25% and increases evasion by 10 for ten minutes.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.BurstOfSpeed2);
        }

        private void ThrowLightsaber()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ThrowLightsaber)
                .Name("Throw Lightsaber")

                .AddPerkLevel()
                .Description("Throw your equipped one-handed lightsaber or one-handed vibroblade up to 15m for 8 DMG. Can hit up to 1 targets along the path thrown.")
                .Price(2)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowLightsaber1)

                .AddPerkLevel()
                .Description("Throw your equipped one-handed lightsaber or one-handed vibroblade up to 15m for 17 DMG. Can hit up to 2 targets along the path thrown.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowLightsaber2)

                .AddPerkLevel()
                .Description("Throw your equipped one-handed lightsaber or one-handed vibroblade up to 15m for 24 DMG. Can hit up to 3 targets along the path thrown.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowLightsaber3);
        }

        private void ForceStun()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ForceStun)
                .Name("Force Stun")

                .AddPerkLevel()
                .Description("Attempt to tranquilize a single target for six seconds with a 12DC will check. If resisted, target gets -10 to Accuracy and Evasion.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceStun1)

                .AddPerkLevel()
                .Description("Target and nearest other enemy within 10m is Tranquilized for six seconds with a 12DC will check. If resisted, target gets -10 to Accuracy and Evasion.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceStun2)

                .AddPerkLevel()
                .Description("Target and all other enemies within 10m are Tranquilized for six seconds with a 12DC will check. If resisted, target gets -10 to Accuracy and Evasion.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceStun3);
        }

        private void ComprehendSpeech()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech")

                .AddPerkLevel()
                .Description("The caster counts has having 5 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(1)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ComprehendSpeech1)

                .AddPerkLevel()
                .Description("The caster counts has having 10 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(1)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ComprehendSpeech2)

                .AddPerkLevel()
                .Description("The caster counts has having 15 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(1)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ComprehendSpeech3)

                .AddPerkLevel()
                .Description("The caster counts has having 20 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(1)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ComprehendSpeech4);
        }

        private void BattleInsight()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.BattleInsight)
                .Name("Battle Insight")

                .AddPerkLevel()
                .Description("The caster gets -10 Accuracy & Evasion but nearby party members get +6 Accuracy & Evasion.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.BattleInsight1)

                .AddPerkLevel()
                .Description("The caster gets -16 Accuracy & Evasion but nearby party members get +12 Accuracy & Evasion.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.BattleInsight2);
        }

        private void MindTrick()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.MindTrick)
                .Name("Mind Trick")

                .AddPerkLevel()
                .Description("Attempt to confuse a single non-mechanical target with a 12DC will check for six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.MindTrick1)

                .AddPerkLevel()
                .Description("Attempt to confuse all hostile non-mechanical targets within 10m with a 12DC will check for six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.MindTrick2);
        }

        private void ForceHeal()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForceHeal)
                .Name("Force Heal")

                .AddPerkLevel()
                .Description("Heals a single target for 10 HP every six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceDrain)
                .GrantsFeat(FeatType.ForceHeal1)

                .AddPerkLevel()
                .Description("Heals a single target for 15 HP every six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceDrain)
                .GrantsFeat(FeatType.ForceHeal2)

                .AddPerkLevel()
                .Description("Heals a single target for 20 HP every six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceDrain)
                .GrantsFeat(FeatType.ForceHeal3)

                .AddPerkLevel()
                .Description("Heals a single target for 25 HP every six seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceDrain)
                .GrantsFeat(FeatType.ForceHeal4)

                .AddPerkLevel()
                .Description("Heals a single target for 30 HP every six seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceDrain)
                .GrantsFeat(FeatType.ForceHeal5);
        }

        private void ForceBurst()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForceBurst)
                .Name("Force Burst")

                .AddPerkLevel()
                .Description("Deals 12 DMG to a single target.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceLightning)
                .GrantsFeat(FeatType.ForceBurst1)

                .AddPerkLevel()
                .Description("Deals 19 DMG to a single target.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceLightning)
                .GrantsFeat(FeatType.ForceBurst2)

                .AddPerkLevel()
                .Description("Deals 28 DMG to a single target.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceLightning)
                .GrantsFeat(FeatType.ForceBurst3)

                .AddPerkLevel()
                .Description("Deals 40 DMG to a single target.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceLightning)
                .GrantsFeat(FeatType.ForceBurst4);
        }
        private void ForceMind()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForceMind)
                .Name("Force Mind")

                .AddPerkLevel()
                .Description("Converts 25% of the user's FP into HP.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceBody)
                .GrantsFeat(FeatType.ForceMind1)

                .AddPerkLevel()
                .Description("Converts 50% of the user's FP into HP.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceBody)
                .GrantsFeat(FeatType.ForceMind2);
        }

        private void ForceDrain()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceDrain)
                .Name("Force Drain")

                .AddPerkLevel()
                .Description("Attempts to steal 10 HP from a target every six seconds with a 14DC will check.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceHeal)
                .GrantsFeat(FeatType.ForceDrain1)

                .AddPerkLevel()
                .Description("Attempts to steal 15 HP from a target every six seconds with a 14DC will check.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceHeal)
                .GrantsFeat(FeatType.ForceDrain2)

                .AddPerkLevel()
                .Description("Attempts to steal 20 HP from a target every six seconds with a 14DC will check.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceHeal)
                .GrantsFeat(FeatType.ForceDrain3)

                .AddPerkLevel()
                .Description("Attempts to steal 25 HP from a target every six seconds with a 14DC will check.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceHeal)
                .GrantsFeat(FeatType.ForceDrain4)

                .AddPerkLevel()
                .Description("Attempts to steal 30 HP from a target every six seconds with a 14DC will check.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceHeal)
                .GrantsFeat(FeatType.ForceDrain5);
        }

        private void ForceLightning()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceLightning)
                .Name("Force Lightning")

                .AddPerkLevel()
                .Description("Deals 12 DMG to a single target.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceBurst)
                .GrantsFeat(FeatType.ForceLightning1)

                .AddPerkLevel()
                .Description("Deals 19 DMG to a single target.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceBurst)
                .GrantsFeat(FeatType.ForceLightning2)

                .AddPerkLevel()
                .Description("Deals 28 DMG to a single target.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceBurst)
                .GrantsFeat(FeatType.ForceLightning3)

                .AddPerkLevel()
                .Description("Deals 40 DMG to a single target.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceBurst)
                .GrantsFeat(FeatType.ForceLightning4);
        }

        private void ForceBody()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceBody)
                .Name("Force Body")

                .AddPerkLevel()
                .Description("Converts 25% of the user's HP into FP.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceMind)
                .GrantsFeat(FeatType.ForceBody1)

                .AddPerkLevel()
                .Description("Converts 50% of the user's HP into FP.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceMind)
                .GrantsFeat(FeatType.ForceBody2);
        }

        private void Premonition()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.Premonition)
                .Name("Premonition")

                .AddPerkLevel()
                .Description("Grants 5% concealment to other party members while concentrating.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Premonition1)

                .AddPerkLevel()
                .Description("Grants 10% concealment to other party members while concentrating.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Premonition2);
        }

        private void Disturbance()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.Disturbance)
                .Name("Disturbance")

                .AddPerkLevel()
                .Description("Deals 9 DMG to a single target and reduces target's accuracy by 10 with an 8DC will check for one minute.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceSpark)
                .GrantsFeat(FeatType.Disturbance1)

                .AddPerkLevel()
                .Description("Deals 14 DMG to a single target and reduces target's accuracy by 20 with a 12DC will check for one minute.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceSpark)
                .GrantsFeat(FeatType.Disturbance2)

                .AddPerkLevel()
                .Description("Deals 32 DMG to a single target and reduces target's accuracy by 30 with a 14DC will check for one minute.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceSpark)
                .GrantsFeat(FeatType.Disturbance3);
        }

        private void Benevolence()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.Benevolence)
                .Name("Benevolence")

                .AddPerkLevel()
                .Description("Restores 40 HP to a single target.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.CreepingTerror)
                .GrantsFeat(FeatType.Benevolence1)

                .AddPerkLevel()
                .Description("Restores 80 HP to a single target.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.CreepingTerror)
                .GrantsFeat(FeatType.Benevolence2)

                .AddPerkLevel()
                .Description("Restores 120 HP to a single target.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.CreepingTerror)
                .GrantsFeat(FeatType.Benevolence3);
        }

        private void ForceValor()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForceValor)
                .Name("Force Valor")

                .AddPerkLevel()
                .Description("Increases your target's physical defense by 10 for 15 minutes.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceRage)
                .GrantsFeat(FeatType.ForceValor1)

                .AddPerkLevel()
                .Description("Increases your target's physical defense by 20 for 15 minutes.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceRage)
                .GrantsFeat(FeatType.ForceValor2);
        }

        private void ForceSpark()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceSpark)
                .Name("Force Spark")

                .AddPerkLevel()
                .Description("Deals 9 DMG to a single target and reduces target's evasion by 2 with an 8DC will check for one minute.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.Disturbance)
                .GrantsFeat(FeatType.ForceSpark1)

                .AddPerkLevel()
                .Description("Deals 14 DMG to a single target and reduces target's evasion by 4 with a 12DC will check for one minute.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.Disturbance)
                .GrantsFeat(FeatType.ForceSpark2)

                .AddPerkLevel()
                .Description("Deals 32 DMG to a single target and reduces target's evasion by 6 with a 14DC will check for one minute.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.Disturbance)
                .GrantsFeat(FeatType.ForceSpark3);
        }

        private void CreepingTerror()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.CreepingTerror)
                .Name("Creeping Terror")

                .AddPerkLevel()
                .Description("Attempts to Paralyze a target for 2 seconds with an 8DC will check and inflicts Terror which deals 8 DMG every six seconds for 24 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.Benevolence)
                .GrantsFeat(FeatType.CreepingTerror1)

                .AddPerkLevel()
                .Description("Attempts to Paralyze a target for 2 seconds with a 12DC will check and inflicts Terror which deals 12 DMG every six seconds for 24 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.Benevolence)
                .GrantsFeat(FeatType.CreepingTerror2)

                .AddPerkLevel()
                .Description("Attempts to Paralyze a target for 2 seconds with a 14DC will check and inflicts Terror which deals 16 DMG every six seconds for 24 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.Benevolence)
                .GrantsFeat(FeatType.CreepingTerror3);
        }

        private void ForceRage()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceRage)
                .Name("Force Rage")

                .AddPerkLevel()
                .Description("Increases your target's Attack by 10 for 15 minutes.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceValor)
                .GrantsFeat(FeatType.ForceRage1)

                .AddPerkLevel()
                .Description("Increases your target's Attack by 20 for 15 minutes.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.ForceValor)
                .GrantsFeat(FeatType.ForceRage2);
        }

        private void ForceInspiration()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ForceInspiration)
                .Name("Force Inspiration")

                .AddPerkLevel()
                .Description("Increases your target's MGT, AGI, and WIL by 1 for 15 minutes. Does not stack with Combat Enhancement.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceInspiration1)

                .AddPerkLevel()
                .Description("Increases your target's MGT, AGI, and WIL by 2 for 15 minutes. Does not stack with Combat Enhancement.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceInspiration2)

                .AddPerkLevel()
                .Description("Increases your target's MGT, AGI, and WIL by 3 for 15 minutes. Does not stack with Combat Enhancement.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceInspiration3);
        }
    }
}
