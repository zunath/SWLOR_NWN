using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class DroidCPURecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Tier1();
            Tier2();
            Tier3();
            Tier4();
            Tier5();

            return _builder.Build();
        }

        private void Tier1()
        {
            // Blade CPU I - Variant M
            _builder.Create(RecipeType.BladeCPU1VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu1_m")
                .Level(5)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 13, assemblySharePercent: 38);

            // Blade CPU I - Variant P
            _builder.Create(RecipeType.BladeCPU1VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu1_p")
                .Level(6)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 3, loadBoostPercent: 13, assemblySharePercent: 38);

            // Blade CPU I - Variant V
            _builder.Create(RecipeType.BladeCPU1VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu1_v")
                .Level(7)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 3, loadBoostPercent: 12, assemblySharePercent: 36);

            // Blade CPU I - Variant W
            _builder.Create(RecipeType.BladeCPU1VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu1_w")
                .Level(8)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 12, assemblySharePercent: 45);

            // Blade CPU I - Variant A
            _builder.Create(RecipeType.BladeCPU1VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu1_a")
                .Level(9)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 13, assemblySharePercent: 41);

            // Blade CPU I - Variant S
            _builder.Create(RecipeType.BladeCPU1VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu1_s")
                .Level(10)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 11, assemblySharePercent: 35);

            // Light Martial CPU I - Variant M
            _builder.Create(RecipeType.LightMartialCPU1VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu1_m")
                .Level(10)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 11, assemblySharePercent: 44);

            // Light Martial CPU I - Variant P
            _builder.Create(RecipeType.LightMartialCPU1VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu1_p")
                .Level(9)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 12, assemblySharePercent: 35);

            // Light Martial CPU I - Variant V
            _builder.Create(RecipeType.LightMartialCPU1VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu1_v")
                .Level(8)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 12, assemblySharePercent: 45);

            // Light Martial CPU I - Variant W
            _builder.Create(RecipeType.LightMartialCPU1VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu1_w")
                .Level(7)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 11, assemblySharePercent: 44);

            // Light Martial CPU I - Variant A
            _builder.Create(RecipeType.LightMartialCPU1VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu1_a")
                .Level(6)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 3, loadBoostPercent: 12, assemblySharePercent: 41);

            // Light Martial CPU I - Variant S
            _builder.Create(RecipeType.LightMartialCPU1VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu1_s")
                .Level(5)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 3, loadBoostPercent: 12, assemblySharePercent: 39);

            // Ranger CPU I - Variant M
            _builder.Create(RecipeType.RangerCPU1VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu1_m")
                .Level(7)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 11, assemblySharePercent: 37);

            // Ranger CPU I - Variant P
            _builder.Create(RecipeType.RangerCPU1VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu1_p")
                .Level(6)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 11, assemblySharePercent: 43);

            // Ranger CPU I - Variant V
            _builder.Create(RecipeType.RangerCPU1VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu1_v")
                .Level(5)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 13, assemblySharePercent: 40);

            // Ranger CPU I - Variant W
            _builder.Create(RecipeType.RangerCPU1VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu1_w")
                .Level(9)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 11, assemblySharePercent: 42);

            // Ranger CPU I - Variant A
            _builder.Create(RecipeType.RangerCPU1VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu1_a")
                .Level(10)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 3, loadBoostPercent: 11, assemblySharePercent: 43);

            // Ranger CPU I - Variant S
            _builder.Create(RecipeType.RangerCPU1VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu1_s")
                .Level(8)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 13, assemblySharePercent: 36);

            // Heavy Martial CPU I - Variant M
            _builder.Create(RecipeType.HeavyMartialCPU1VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu1_m")
                .Level(8)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 12, assemblySharePercent: 36);

            // Heavy Martial CPU I - Variant P
            _builder.Create(RecipeType.HeavyMartialCPU1VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu1_p")
                .Level(7)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 3, loadBoostPercent: 11, assemblySharePercent: 42);

            // Heavy Martial CPU I - Variant V
            _builder.Create(RecipeType.HeavyMartialCPU1VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu1_v")
                .Level(9)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 13, assemblySharePercent: 36);

            // Heavy Martial CPU I - Variant W
            _builder.Create(RecipeType.HeavyMartialCPU1VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu1_w")
                .Level(5)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 11, assemblySharePercent: 41);

            // Heavy Martial CPU I - Variant A
            _builder.Create(RecipeType.HeavyMartialCPU1VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu1_a")
                .Level(6)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 3, loadBoostPercent: 13, assemblySharePercent: 38);

            // Heavy Martial CPU I - Variant S
            _builder.Create(RecipeType.HeavyMartialCPU1VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu1_s")
                .Level(10)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 13, assemblySharePercent: 45);

            // Assault CPU I - Variant M
            _builder.Create(RecipeType.AssaultCPU1VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu1_m")
                .Level(9)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 3, loadBoostPercent: 13, assemblySharePercent: 40);

            // Assault CPU I - Variant P
            _builder.Create(RecipeType.AssaultCPU1VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu1_p")
                .Level(10)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 11, assemblySharePercent: 38);

            // Assault CPU I - Variant V
            _builder.Create(RecipeType.AssaultCPU1VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu1_v")
                .Level(7)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 13, assemblySharePercent: 35);

            // Assault CPU I - Variant W
            _builder.Create(RecipeType.AssaultCPU1VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu1_w")
                .Level(8)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 3, loadBoostPercent: 11, assemblySharePercent: 36);

            // Assault CPU I - Variant A
            _builder.Create(RecipeType.AssaultCPU1VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu1_a")
                .Level(6)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 3, loadBoostPercent: 11, assemblySharePercent: 39);

            // Assault CPU I - Variant S
            _builder.Create(RecipeType.AssaultCPU1VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu1_s")
                .Level(5)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 11, assemblySharePercent: 37);

            // Jaguar CPU I - Variant M
            _builder.Create(RecipeType.JaguarCPU1VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu1_m")
                .Level(8)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 3, loadBoostPercent: 13, assemblySharePercent: 40);

            // Jaguar CPU I - Variant P
            _builder.Create(RecipeType.JaguarCPU1VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu1_p")
                .Level(7)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 12, assemblySharePercent: 37);

            // Jaguar CPU I - Variant V
            _builder.Create(RecipeType.JaguarCPU1VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu1_v")
                .Level(10)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 12, assemblySharePercent: 35);

            // Jaguar CPU I - Variant W
            _builder.Create(RecipeType.JaguarCPU1VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu1_w")
                .Level(5)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 11, assemblySharePercent: 43);

            // Jaguar CPU I - Variant A
            _builder.Create(RecipeType.JaguarCPU1VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu1_a")
                .Level(6)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 3, loadBoostPercent: 12, assemblySharePercent: 39);

            // Jaguar CPU I - Variant S
            _builder.Create(RecipeType.JaguarCPU1VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu1_s")
                .Level(9)
                .Quantity(1)
                .Component("c_unit1", 1)
                .Component("l_unit1", 1)
                .Component("ca_unit1", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 11, assemblySharePercent: 38);


        }
        private void Tier2()
        {
            // Blade CPU II - Variant M
            _builder.Create(RecipeType.BladeCPU2VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu2_m")
                .Level(15)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 13, assemblySharePercent: 41);

            // Blade CPU II - Variant P
            _builder.Create(RecipeType.BladeCPU2VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu2_p")
                .Level(16)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 12, assemblySharePercent: 44);

            // Blade CPU II - Variant V
            _builder.Create(RecipeType.BladeCPU2VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu2_v")
                .Level(17)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 11, assemblySharePercent: 45);

            // Blade CPU II - Variant W
            _builder.Create(RecipeType.BladeCPU2VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu2_w")
                .Level(18)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 13, assemblySharePercent: 44);

            // Blade CPU II - Variant A
            _builder.Create(RecipeType.BladeCPU2VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu2_a")
                .Level(19)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 12, assemblySharePercent: 45);

            // Blade CPU II - Variant S
            _builder.Create(RecipeType.BladeCPU2VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu2_s")
                .Level(20)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 13, assemblySharePercent: 45);

            // Light Martial CPU II - Variant M
            _builder.Create(RecipeType.LightMartialCPU2VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu2_m")
                .Level(20)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 13, assemblySharePercent: 44);

            // Light Martial CPU II - Variant P
            _builder.Create(RecipeType.LightMartialCPU2VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu2_p")
                .Level(19)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 12, assemblySharePercent: 37);

            // Light Martial CPU II - Variant V
            _builder.Create(RecipeType.LightMartialCPU2VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu2_v")
                .Level(18)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 11, assemblySharePercent: 40);

            // Light Martial CPU II - Variant W
            _builder.Create(RecipeType.LightMartialCPU2VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu2_w")
                .Level(17)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 13, assemblySharePercent: 38);

            // Light Martial CPU II - Variant A
            _builder.Create(RecipeType.LightMartialCPU2VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu2_a")
                .Level(16)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 13, assemblySharePercent: 45);

            // Light Martial CPU II - Variant S
            _builder.Create(RecipeType.LightMartialCPU2VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu2_s")
                .Level(15)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 11, assemblySharePercent: 39);

            // Ranger CPU II - Variant M
            _builder.Create(RecipeType.RangerCPU2VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu2_m")
                .Level(17)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 11, assemblySharePercent: 40);

            // Ranger CPU II - Variant P
            _builder.Create(RecipeType.RangerCPU2VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu2_p")
                .Level(16)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 11, assemblySharePercent: 39);

            // Ranger CPU II - Variant V
            _builder.Create(RecipeType.RangerCPU2VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu2_v")
                .Level(15)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 36);

            // Ranger CPU II - Variant W
            _builder.Create(RecipeType.RangerCPU2VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu2_w")
                .Level(19)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 45);

            // Ranger CPU II - Variant A
            _builder.Create(RecipeType.RangerCPU2VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu2_a")
                .Level(20)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 39);

            // Ranger CPU II - Variant S
            _builder.Create(RecipeType.RangerCPU2VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu2_s")
                .Level(18)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 13, assemblySharePercent: 40);

            // Heavy Martial CPU II - Variant M
            _builder.Create(RecipeType.HeavyMartialCPU2VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu2_m")
                .Level(18)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 11, assemblySharePercent: 35);

            // Heavy Martial CPU II - Variant P
            _builder.Create(RecipeType.HeavyMartialCPU2VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu2_p")
                .Level(17)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 11, assemblySharePercent: 36);

            // Heavy Martial CPU II - Variant V
            _builder.Create(RecipeType.HeavyMartialCPU2VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu2_v")
                .Level(19)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 11, assemblySharePercent: 35);

            // Heavy Martial CPU II - Variant W
            _builder.Create(RecipeType.HeavyMartialCPU2VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu2_w")
                .Level(15)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 11, assemblySharePercent: 44);

            // Heavy Martial CPU II - Variant A
            _builder.Create(RecipeType.HeavyMartialCPU2VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu2_a")
                .Level(16)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 11, assemblySharePercent: 38);

            // Heavy Martial CPU II - Variant S
            _builder.Create(RecipeType.HeavyMartialCPU2VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu2_s")
                .Level(20)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 13, assemblySharePercent: 44);

            // Assault CPU II - Variant M
            _builder.Create(RecipeType.AssaultCPU2VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu2_m")
                .Level(19)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 11, assemblySharePercent: 45);

            // Assault CPU II - Variant P
            _builder.Create(RecipeType.AssaultCPU2VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu2_p")
                .Level(20)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 43);

            // Assault CPU II - Variant V
            _builder.Create(RecipeType.AssaultCPU2VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu2_v")
                .Level(17)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 41);

            // Assault CPU II - Variant W
            _builder.Create(RecipeType.AssaultCPU2VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu2_w")
                .Level(18)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 12, assemblySharePercent: 37);

            // Assault CPU II - Variant A
            _builder.Create(RecipeType.AssaultCPU2VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu2_a")
                .Level(16)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 36);

            // Assault CPU II - Variant S
            _builder.Create(RecipeType.AssaultCPU2VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu2_s")
                .Level(15)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 12, assemblySharePercent: 44);

            // Jaguar CPU II - Variant M
            _builder.Create(RecipeType.JaguarCPU2VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu2_m")
                .Level(18)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 13, assemblySharePercent: 35);

            // Jaguar CPU II - Variant P
            _builder.Create(RecipeType.JaguarCPU2VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu2_p")
                .Level(17)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 13, assemblySharePercent: 36);

            // Jaguar CPU II - Variant V
            _builder.Create(RecipeType.JaguarCPU2VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu2_v")
                .Level(20)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 13, assemblySharePercent: 35);

            // Jaguar CPU II - Variant W
            _builder.Create(RecipeType.JaguarCPU2VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu2_w")
                .Level(15)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 11, assemblySharePercent: 38);

            // Jaguar CPU II - Variant A
            _builder.Create(RecipeType.JaguarCPU2VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu2_a")
                .Level(16)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 4, loadBoostPercent: 12, assemblySharePercent: 43);

            // Jaguar CPU II - Variant S
            _builder.Create(RecipeType.JaguarCPU2VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu2_s")
                .Level(19)
                .Quantity(1)
                .Component("c_unit2", 1)
                .Component("l_unit2", 1)
                .Component("ca_unit2", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 45);


        }
        private void Tier3()
        {
            // Blade CPU III - Variant M
            _builder.Create(RecipeType.BladeCPU3VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu3_m")
                .Level(25)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 42);

            // Blade CPU III - Variant P
            _builder.Create(RecipeType.BladeCPU3VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu3_p")
                .Level(26)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 40);

            // Blade CPU III - Variant V
            _builder.Create(RecipeType.BladeCPU3VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu3_v")
                .Level(27)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 11, assemblySharePercent: 36);

            // Blade CPU III - Variant W
            _builder.Create(RecipeType.BladeCPU3VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu3_w")
                .Level(28)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 12, assemblySharePercent: 35);

            // Blade CPU III - Variant A
            _builder.Create(RecipeType.BladeCPU3VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu3_a")
                .Level(29)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 11, assemblySharePercent: 38);

            // Blade CPU III - Variant S
            _builder.Create(RecipeType.BladeCPU3VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu3_s")
                .Level(30)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 11, assemblySharePercent: 35);

            // Light Martial CPU III - Variant M
            _builder.Create(RecipeType.LightMartialCPU3VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu3_m")
                .Level(30)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 45);

            // Light Martial CPU III - Variant P
            _builder.Create(RecipeType.LightMartialCPU3VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu3_p")
                .Level(29)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 13, assemblySharePercent: 44);

            // Light Martial CPU III - Variant V
            _builder.Create(RecipeType.LightMartialCPU3VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu3_v")
                .Level(28)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 12, assemblySharePercent: 35);

            // Light Martial CPU III - Variant W
            _builder.Create(RecipeType.LightMartialCPU3VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu3_w")
                .Level(27)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 13, assemblySharePercent: 39);

            // Light Martial CPU III - Variant A
            _builder.Create(RecipeType.LightMartialCPU3VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu3_a")
                .Level(26)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 11, assemblySharePercent: 40);

            // Light Martial CPU III - Variant S
            _builder.Create(RecipeType.LightMartialCPU3VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu3_s")
                .Level(25)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 12, assemblySharePercent: 39);

            // Ranger CPU III - Variant M
            _builder.Create(RecipeType.RangerCPU3VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu3_m")
                .Level(27)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 11, assemblySharePercent: 39);

            // Ranger CPU III - Variant P
            _builder.Create(RecipeType.RangerCPU3VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu3_p")
                .Level(26)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 13, assemblySharePercent: 44);

            // Ranger CPU III - Variant V
            _builder.Create(RecipeType.RangerCPU3VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu3_v")
                .Level(25)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 43);

            // Ranger CPU III - Variant W
            _builder.Create(RecipeType.RangerCPU3VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu3_w")
                .Level(29)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 13, assemblySharePercent: 42);

            // Ranger CPU III - Variant A
            _builder.Create(RecipeType.RangerCPU3VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu3_a")
                .Level(30)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 11, assemblySharePercent: 40);

            // Ranger CPU III - Variant S
            _builder.Create(RecipeType.RangerCPU3VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu3_s")
                .Level(28)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 12, assemblySharePercent: 43);

            // Heavy Martial CPU III - Variant M
            _builder.Create(RecipeType.HeavyMartialCPU3VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu3_m")
                .Level(28)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 11, assemblySharePercent: 38);

            // Heavy Martial CPU III - Variant P
            _builder.Create(RecipeType.HeavyMartialCPU3VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu3_p")
                .Level(27)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 13, assemblySharePercent: 41);

            // Heavy Martial CPU III - Variant V
            _builder.Create(RecipeType.HeavyMartialCPU3VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu3_v")
                .Level(29)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 13, assemblySharePercent: 37);

            // Heavy Martial CPU III - Variant W
            _builder.Create(RecipeType.HeavyMartialCPU3VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu3_w")
                .Level(25)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 13, assemblySharePercent: 39);

            // Heavy Martial CPU III - Variant A
            _builder.Create(RecipeType.HeavyMartialCPU3VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu3_a")
                .Level(26)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 12, assemblySharePercent: 42);

            // Heavy Martial CPU III - Variant S
            _builder.Create(RecipeType.HeavyMartialCPU3VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu3_s")
                .Level(30)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 11, assemblySharePercent: 41);

            // Assault CPU III - Variant M
            _builder.Create(RecipeType.AssaultCPU3VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu3_m")
                .Level(29)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 12, assemblySharePercent: 36);

            // Assault CPU III - Variant P
            _builder.Create(RecipeType.AssaultCPU3VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu3_p")
                .Level(30)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 13, assemblySharePercent: 35);

            // Assault CPU III - Variant V
            _builder.Create(RecipeType.AssaultCPU3VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu3_v")
                .Level(27)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 44);

            // Assault CPU III - Variant W
            _builder.Create(RecipeType.AssaultCPU3VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu3_w")
                .Level(28)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 13, assemblySharePercent: 39);

            // Assault CPU III - Variant A
            _builder.Create(RecipeType.AssaultCPU3VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu3_a")
                .Level(26)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 12, assemblySharePercent: 44);

            // Assault CPU III - Variant S
            _builder.Create(RecipeType.AssaultCPU3VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu3_s")
                .Level(25)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 11, assemblySharePercent: 45);

            // Jaguar CPU III - Variant M
            _builder.Create(RecipeType.JaguarCPU3VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu3_m")
                .Level(28)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 37);

            // Jaguar CPU III - Variant P
            _builder.Create(RecipeType.JaguarCPU3VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu3_p")
                .Level(27)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 12, assemblySharePercent: 40);

            // Jaguar CPU III - Variant V
            _builder.Create(RecipeType.JaguarCPU3VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu3_v")
                .Level(30)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 11, assemblySharePercent: 35);

            // Jaguar CPU III - Variant W
            _builder.Create(RecipeType.JaguarCPU3VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu3_w")
                .Level(25)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 39);

            // Jaguar CPU III - Variant A
            _builder.Create(RecipeType.JaguarCPU3VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu3_a")
                .Level(26)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 13, assemblySharePercent: 42);

            // Jaguar CPU III - Variant S
            _builder.Create(RecipeType.JaguarCPU3VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu3_s")
                .Level(29)
                .Quantity(1)
                .Component("c_unit3", 1)
                .Component("l_unit3", 1)
                .Component("ca_unit3", 1)
                .CircuitParameters(overloadLine: 5, loadBoostPercent: 13, assemblySharePercent: 37);


        }
        private void Tier4()
        {
            // Blade CPU IV - Variant M
            _builder.Create(RecipeType.BladeCPU4VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu4_m")
                .Level(35)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 11, assemblySharePercent: 38);

            // Blade CPU IV - Variant P
            _builder.Create(RecipeType.BladeCPU4VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu4_p")
                .Level(36)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 13, assemblySharePercent: 42);

            // Blade CPU IV - Variant V
            _builder.Create(RecipeType.BladeCPU4VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu4_v")
                .Level(37)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 11, assemblySharePercent: 38);

            // Blade CPU IV - Variant W
            _builder.Create(RecipeType.BladeCPU4VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu4_w")
                .Level(38)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 12, assemblySharePercent: 38);

            // Blade CPU IV - Variant A
            _builder.Create(RecipeType.BladeCPU4VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu4_a")
                .Level(39)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 11, assemblySharePercent: 43);

            // Blade CPU IV - Variant S
            _builder.Create(RecipeType.BladeCPU4VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu4_s")
                .Level(40)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 12, assemblySharePercent: 44);

            // Light Martial CPU IV - Variant M
            _builder.Create(RecipeType.LightMartialCPU4VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu4_m")
                .Level(40)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 13, assemblySharePercent: 36);

            // Light Martial CPU IV - Variant P
            _builder.Create(RecipeType.LightMartialCPU4VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu4_p")
                .Level(39)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 13, assemblySharePercent: 40);

            // Light Martial CPU IV - Variant V
            _builder.Create(RecipeType.LightMartialCPU4VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu4_v")
                .Level(38)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 11, assemblySharePercent: 40);

            // Light Martial CPU IV - Variant W
            _builder.Create(RecipeType.LightMartialCPU4VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu4_w")
                .Level(37)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 13, assemblySharePercent: 43);

            // Light Martial CPU IV - Variant A
            _builder.Create(RecipeType.LightMartialCPU4VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu4_a")
                .Level(36)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 11, assemblySharePercent: 42);

            // Light Martial CPU IV - Variant S
            _builder.Create(RecipeType.LightMartialCPU4VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu4_s")
                .Level(35)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 13, assemblySharePercent: 42);

            // Ranger CPU IV - Variant M
            _builder.Create(RecipeType.RangerCPU4VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu4_m")
                .Level(37)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 13, assemblySharePercent: 42);

            // Ranger CPU IV - Variant P
            _builder.Create(RecipeType.RangerCPU4VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu4_p")
                .Level(36)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 13, assemblySharePercent: 40);

            // Ranger CPU IV - Variant V
            _builder.Create(RecipeType.RangerCPU4VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu4_v")
                .Level(35)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 12, assemblySharePercent: 44);

            // Ranger CPU IV - Variant W
            _builder.Create(RecipeType.RangerCPU4VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu4_w")
                .Level(39)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 39);

            // Ranger CPU IV - Variant A
            _builder.Create(RecipeType.RangerCPU4VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu4_a")
                .Level(40)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 11, assemblySharePercent: 42);

            // Ranger CPU IV - Variant S
            _builder.Create(RecipeType.RangerCPU4VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu4_s")
                .Level(38)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 12, assemblySharePercent: 42);

            // Heavy Martial CPU IV - Variant M
            _builder.Create(RecipeType.HeavyMartialCPU4VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu4_m")
                .Level(38)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 13, assemblySharePercent: 35);

            // Heavy Martial CPU IV - Variant P
            _builder.Create(RecipeType.HeavyMartialCPU4VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu4_p")
                .Level(37)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 13, assemblySharePercent: 39);

            // Heavy Martial CPU IV - Variant V
            _builder.Create(RecipeType.HeavyMartialCPU4VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu4_v")
                .Level(39)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 11, assemblySharePercent: 41);

            // Heavy Martial CPU IV - Variant W
            _builder.Create(RecipeType.HeavyMartialCPU4VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu4_w")
                .Level(35)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 12, assemblySharePercent: 38);

            // Heavy Martial CPU IV - Variant A
            _builder.Create(RecipeType.HeavyMartialCPU4VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu4_a")
                .Level(36)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 13, assemblySharePercent: 45);

            // Heavy Martial CPU IV - Variant S
            _builder.Create(RecipeType.HeavyMartialCPU4VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu4_s")
                .Level(40)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 11, assemblySharePercent: 41);

            // Assault CPU IV - Variant M
            _builder.Create(RecipeType.AssaultCPU4VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu4_m")
                .Level(39)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 13, assemblySharePercent: 45);

            // Assault CPU IV - Variant P
            _builder.Create(RecipeType.AssaultCPU4VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu4_p")
                .Level(40)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 35);

            // Assault CPU IV - Variant V
            _builder.Create(RecipeType.AssaultCPU4VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu4_v")
                .Level(37)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 11, assemblySharePercent: 40);

            // Assault CPU IV - Variant W
            _builder.Create(RecipeType.AssaultCPU4VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu4_w")
                .Level(38)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 13, assemblySharePercent: 42);

            // Assault CPU IV - Variant A
            _builder.Create(RecipeType.AssaultCPU4VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu4_a")
                .Level(36)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 44);

            // Assault CPU IV - Variant S
            _builder.Create(RecipeType.AssaultCPU4VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu4_s")
                .Level(35)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 12, assemblySharePercent: 37);

            // Jaguar CPU IV - Variant M
            _builder.Create(RecipeType.JaguarCPU4VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu4_m")
                .Level(38)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 12, assemblySharePercent: 36);

            // Jaguar CPU IV - Variant P
            _builder.Create(RecipeType.JaguarCPU4VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu4_p")
                .Level(37)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 11, assemblySharePercent: 40);

            // Jaguar CPU IV - Variant V
            _builder.Create(RecipeType.JaguarCPU4VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu4_v")
                .Level(40)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 13, assemblySharePercent: 36);

            // Jaguar CPU IV - Variant W
            _builder.Create(RecipeType.JaguarCPU4VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu4_w")
                .Level(35)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 13, assemblySharePercent: 41);

            // Jaguar CPU IV - Variant A
            _builder.Create(RecipeType.JaguarCPU4VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu4_a")
                .Level(36)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 12, assemblySharePercent: 35);

            // Jaguar CPU IV - Variant S
            _builder.Create(RecipeType.JaguarCPU4VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu4_s")
                .Level(39)
                .Quantity(1)
                .Component("c_unit4", 1)
                .Component("l_unit4", 1)
                .Component("ca_unit4", 1)
                .CircuitParameters(overloadLine: 6, loadBoostPercent: 11, assemblySharePercent: 45);


        }
        private void Tier5()
        {
            // Blade CPU V - Variant M
            _builder.Create(RecipeType.BladeCPU5VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu5_m")
                .Level(45)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 13, assemblySharePercent: 42);

            // Blade CPU V - Variant P
            _builder.Create(RecipeType.BladeCPU5VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu5_p")
                .Level(46)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 11, assemblySharePercent: 38);

            // Blade CPU V - Variant V
            _builder.Create(RecipeType.BladeCPU5VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu5_v")
                .Level(47)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 9, loadBoostPercent: 11, assemblySharePercent: 36);

            // Blade CPU V - Variant W
            _builder.Create(RecipeType.BladeCPU5VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu5_w")
                .Level(48)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 13, assemblySharePercent: 43);

            // Blade CPU V - Variant A
            _builder.Create(RecipeType.BladeCPU5VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu5_a")
                .Level(49)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 12, assemblySharePercent: 39);

            // Blade CPU V - Variant S
            _builder.Create(RecipeType.BladeCPU5VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_bl_cpu5_s")
                .Level(50)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 11, assemblySharePercent: 38);

            // Light Martial CPU V - Variant M
            _builder.Create(RecipeType.LightMartialCPU5VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu5_m")
                .Level(50)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 9, loadBoostPercent: 13, assemblySharePercent: 35);

            // Light Martial CPU V - Variant P
            _builder.Create(RecipeType.LightMartialCPU5VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu5_p")
                .Level(49)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 9, loadBoostPercent: 13, assemblySharePercent: 42);

            // Light Martial CPU V - Variant V
            _builder.Create(RecipeType.LightMartialCPU5VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu5_v")
                .Level(48)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 12, assemblySharePercent: 45);

            // Light Martial CPU V - Variant W
            _builder.Create(RecipeType.LightMartialCPU5VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu5_w")
                .Level(47)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 11, assemblySharePercent: 37);

            // Light Martial CPU V - Variant A
            _builder.Create(RecipeType.LightMartialCPU5VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu5_a")
                .Level(46)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 12, assemblySharePercent: 45);

            // Light Martial CPU V - Variant S
            _builder.Create(RecipeType.LightMartialCPU5VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_lm_cpu5_s")
                .Level(45)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 13, assemblySharePercent: 35);

            // Ranger CPU V - Variant M
            _builder.Create(RecipeType.RangerCPU5VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu5_m")
                .Level(47)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 11, assemblySharePercent: 44);

            // Ranger CPU V - Variant P
            _builder.Create(RecipeType.RangerCPU5VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu5_p")
                .Level(46)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 13, assemblySharePercent: 37);

            // Ranger CPU V - Variant V
            _builder.Create(RecipeType.RangerCPU5VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu5_v")
                .Level(45)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 9, loadBoostPercent: 13, assemblySharePercent: 42);

            // Ranger CPU V - Variant W
            _builder.Create(RecipeType.RangerCPU5VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu5_w")
                .Level(49)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 11, assemblySharePercent: 41);

            // Ranger CPU V - Variant A
            _builder.Create(RecipeType.RangerCPU5VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu5_a")
                .Level(50)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 12, assemblySharePercent: 38);

            // Ranger CPU V - Variant S
            _builder.Create(RecipeType.RangerCPU5VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_rn_cpu5_s")
                .Level(48)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 11, assemblySharePercent: 45);

            // Heavy Martial CPU V - Variant M
            _builder.Create(RecipeType.HeavyMartialCPU5VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu5_m")
                .Level(48)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 13, assemblySharePercent: 39);

            // Heavy Martial CPU V - Variant P
            _builder.Create(RecipeType.HeavyMartialCPU5VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu5_p")
                .Level(47)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 13, assemblySharePercent: 35);

            // Heavy Martial CPU V - Variant V
            _builder.Create(RecipeType.HeavyMartialCPU5VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu5_v")
                .Level(49)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 12, assemblySharePercent: 37);

            // Heavy Martial CPU V - Variant W
            _builder.Create(RecipeType.HeavyMartialCPU5VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu5_w")
                .Level(45)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 11, assemblySharePercent: 45);

            // Heavy Martial CPU V - Variant A
            _builder.Create(RecipeType.HeavyMartialCPU5VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu5_a")
                .Level(46)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 11, assemblySharePercent: 38);

            // Heavy Martial CPU V - Variant S
            _builder.Create(RecipeType.HeavyMartialCPU5VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_hm_cpu5_s")
                .Level(50)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 11, assemblySharePercent: 43);

            // Assault CPU V - Variant M
            _builder.Create(RecipeType.AssaultCPU5VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu5_m")
                .Level(49)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 11, assemblySharePercent: 36);

            // Assault CPU V - Variant P
            _builder.Create(RecipeType.AssaultCPU5VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu5_p")
                .Level(50)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 9, loadBoostPercent: 12, assemblySharePercent: 40);

            // Assault CPU V - Variant V
            _builder.Create(RecipeType.AssaultCPU5VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu5_v")
                .Level(47)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 13, assemblySharePercent: 38);

            // Assault CPU V - Variant W
            _builder.Create(RecipeType.AssaultCPU5VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu5_w")
                .Level(48)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 12, assemblySharePercent: 41);

            // Assault CPU V - Variant A
            _builder.Create(RecipeType.AssaultCPU5VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu5_a")
                .Level(46)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 9, loadBoostPercent: 12, assemblySharePercent: 36);

            // Assault CPU V - Variant S
            _builder.Create(RecipeType.AssaultCPU5VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_as_cpu5_s")
                .Level(45)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 12, assemblySharePercent: 37);

            // Jaguar CPU V - Variant M
            _builder.Create(RecipeType.JaguarCPU5VariantM, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu5_m")
                .Level(48)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 9, loadBoostPercent: 13, assemblySharePercent: 36);

            // Jaguar CPU V - Variant P
            _builder.Create(RecipeType.JaguarCPU5VariantP, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu5_p")
                .Level(47)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 9, loadBoostPercent: 13, assemblySharePercent: 44);

            // Jaguar CPU V - Variant V
            _builder.Create(RecipeType.JaguarCPU5VariantV, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu5_v")
                .Level(50)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 9, loadBoostPercent: 11, assemblySharePercent: 45);

            // Jaguar CPU V - Variant W
            _builder.Create(RecipeType.JaguarCPU5VariantW, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu5_w")
                .Level(45)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 9, loadBoostPercent: 13, assemblySharePercent: 44);

            // Jaguar CPU V - Variant A
            _builder.Create(RecipeType.JaguarCPU5VariantA, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu5_a")
                .Level(46)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 7, loadBoostPercent: 13, assemblySharePercent: 45);

            // Jaguar CPU V - Variant S
            _builder.Create(RecipeType.JaguarCPU5VariantS, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidCPU)
                .Resref("d_jg_cpu5_s")
                .Level(49)
                .Quantity(1)
                .Component("c_unit5", 1)
                .Component("l_unit5", 1)
                .Component("ca_unit5", 1)
                .CircuitParameters(overloadLine: 8, loadBoostPercent: 11, assemblySharePercent: 38);


        }

    }
}
