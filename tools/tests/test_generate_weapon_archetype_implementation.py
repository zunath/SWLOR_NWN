import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
GENERATOR_PATH = ROOT / "tools" / "GenerateWeaponArchetypeImplementation.py"
SPEC = importlib.util.spec_from_file_location("weapon_generator", GENERATOR_PATH)
GENERATOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(GENERATOR)


class GeneratedWeaponTargetingTests(unittest.TestCase):
    def test_headshot_queues_and_only_grants_critical_rate_after_idle_window(self):
        row = {
            "Tab": "Rifle",
            "PerkName": "Headshot I",
            "Type": "Combat",
            "CastingTime": "Instant",
            "Description": (
                "Queues your next auto-attack to deal weapon DMG + 16. "
                "If Headshot is used after 3 seconds without attacking, "
                "that attack gains +15% Critical Rate."
            ),
        }

        properties = dict(GENERATOR.profile_property_lines(row, 1, None))

        self.assertEqual("true", properties["IsQueuedWeaponAbility"])
        self.assertEqual("3.0f", properties["IdleWindowSeconds"])
        self.assertEqual("15", properties["CriticalRateIfIdle"])
        self.assertNotIn("CriticalRatePercentAdjustment", properties)
        self.assertNotIn("SelfCriticalRatePercent", properties)

    def test_kill_box_generates_placed_status_and_suppression_window(self):
        row = {
            "Tab": "Rifle",
            "PerkName": "Kill Box",
            "Type": "Capstone",
            "CastingTime": "2 seconds",
            "Description": (
                "Target an enemy or location to deal weapon DMG + 20 to enemies within 8m "
                "and apply Kill Box for 45 seconds. While Kill Box remains, any player's "
                "ranged attacks against affected enemies add Suppression stacks lasting "
                "30 seconds using the Kill Box caster's Suppressing Shot stack strength; "
                "each stack reduces Evasion by an additional 3%."
            ),
        }

        properties = dict(GENERATOR.profile_property_lines(row, 1, None))

        self.assertEqual("() => new KillBoxStatusEffect()", properties["StatusEffectFactory"])
        self.assertEqual("30", properties["TemporaryRangedHitSuppressionStackDurationSeconds"])
        self.assertEqual(3, properties["TemporarySuppressionStackEvasionPenaltyPercentAdjustment"])
        self.assertEqual("45", properties["TemporaryDefeatedEnemyEffectDurationSeconds"])

    def test_kill_box_enemy_or_location_is_an_aimed_sphere(self):
        row = {
            "Tab": "Rifle",
            "Description": "Target an enemy or location to deal weapon DMG + 20 to enemies within 8m.",
        }

        self.assertTrue(GENERATOR.is_aimed_area(row))
        values, owns_targeting = GENERATOR.generated_targeting_update(row, False)
        self.assertTrue(owns_targeting)
        self.assertEqual("sphere", values["TargetShape"])
        self.assertEqual("8", values["TargetSizeX"])
        self.assertEqual("1", values["TargetFlags"])

    def test_dead_center_applies_to_abilities_and_opening_auto_attacks(self):
        row = {
            "Tab": "Rifle",
            "PerkName": "Dead Center",
            "Type": "Trait",
            "Description": (
                "After 3 seconds without attacking, if your next attack is a critical hit, "
                "it deals +15% damage."
            ),
        }

        stats = dict(GENERATOR.description_stat_entries(row, "Dead Center"))

        self.assertEqual("(int)SkillType.Rifle", stats["IdleSkillAbilitySkillType"])
        self.assertEqual("3", stats["IdleSkillAbilityRequiredIdleSeconds"])
        self.assertEqual("15", stats["IdleSkillAbilityCriticalDamagePercentAdjustment"])
        self.assertEqual("(int)SkillType.Rifle", stats["OpeningAutoAttackSkillType"])
        self.assertEqual("3", stats["OpeningAutoAttackIdleSeconds"])
        self.assertEqual("15", stats["OpeningAutoAttackCriticalDamagePercentAdjustment"])

    def test_point_blank_burst_applies_self_status_on_activation(self):
        properties = dict(GENERATOR.profile_property_lines(
            {
                "Type": "Active",
                "PerkName": "Point Blank Burst I",
                "Description": "Deals weapon DMG + 16 to enemies within 5m. Grants +10% Evasion for 30 seconds.",
            },
            1,
            None))

        self.assertEqual("() => new PointBlankBurstStatusEffect(10)", properties["SelfStatusEffectFactory"])
        self.assertEqual("30", properties["SelfStatDurationSeconds"])
        self.assertEqual("true", properties["ApplySelfModifiersOnHostileActivation"])

    def test_ownership_manifest_matches_current_inferred_area_rows(self):
        rows = GENERATOR.read_manifest()
        _, feat_values = GENERATOR.parse_enum_values(
            ROOT / "SWLOR.NWN.API" / "NWScript" / "Enum" / "FeatType.cs")
        feat_rows = GENERATOR.read_2da(ROOT / "SWLOR_Haks" / "sw_2da" / "feat.2da")
        spell_rows = GENERATOR.read_2da(ROOT / "SWLOR_Haks" / "sw_2da" / "spells.2da")
        inferred_spell_ids = set()

        for row in rows:
            if row["Type"] not in GENERATOR.ACTIVE_TYPES:
                continue
            if not GENERATOR.infer_targeting_from_description(row):
                continue

            feat = GENERATOR.active_feat_name(row, feat_values)
            feat_id = feat_values.get(feat)
            feat_row = feat_rows.get(str(feat_id))
            if feat_row and feat_row["SPELLID"].isdigit():
                spell_id = feat_row["SPELLID"]
                inferred_spell_ids.add(spell_id)
                expected_values, remains_owned = GENERATOR.generated_targeting_update(row, True)
                self.assertTrue(remains_owned)
                self.assertIsNotNone(expected_values)
                for header, expected_value in expected_values.items():
                    self.assertEqual(
                        expected_value,
                        spell_rows[spell_id][header],
                        f"spell {spell_id} {header} must already match the generator-owned value")

        self.assertEqual(
            inferred_spell_ids,
            GENERATOR.read_generated_targeting_spell_ids())

    def test_area_to_single_target_transition_clears_only_owned_fields_and_is_idempotent(self):
        area_row = {
            "Tab": "Pistol",
            "Description": "Deals damage to enemies within 5m.",
        }
        single_target_row = {
            "Tab": "Pistol",
            "Description": "Deals weapon DMG + 30 to one target.",
        }

        area_values, owns_area = GENERATOR.generated_targeting_update(area_row, False)
        cleared_values, owns_single_target = GENERATOR.generated_targeting_update(single_target_row, owns_area)
        repeated_values, remains_unowned = GENERATOR.generated_targeting_update(
            single_target_row,
            owns_single_target)

        self.assertTrue(owns_area)
        self.assertEqual("sphere", area_values["TargetShape"])
        self.assertEqual("5", area_values["TargetSizeX"])
        self.assertEqual(
            {
                "TargetShape": "****",
                "TargetSizeX": "****",
                "TargetSizeY": "****",
                "TargetFlags": "****",
            },
            cleared_values)
        self.assertFalse(owns_single_target)
        self.assertIsNone(repeated_values)
        self.assertFalse(remains_unowned)

    def test_non_area_row_preserves_targeting_that_the_generator_does_not_own(self):
        row = {
            "Tab": "Pistol",
            "Description": "Deals weapon DMG + 30 to one target.",
        }

        values, owns_targeting = GENERATOR.generated_targeting_update(row, False)

        self.assertIsNone(values)
        self.assertFalse(owns_targeting)

    def test_empty_update_clears_all_previously_owned_rows_and_ownership(self):
        with tempfile.TemporaryDirectory() as directory:
            temporary_root = Path(directory)
            spells_path = temporary_root / "SWLOR_Haks" / "sw_2da" / "spells.2da"
            spells_path.parent.mkdir(parents=True)
            spells_path.write_text(
                "2DA V2.0\n\n"
                "Label TargetShape TargetSizeX TargetSizeY TargetFlags\n"
                "123 Owned sphere 5 2 17\n")
            ownership_path = temporary_root / "tools" / "GeneratedWeaponSpellTargeting.json"
            ownership_path.parent.mkdir(parents=True)
            ownership_path.write_text(json.dumps({"spell_ids": [123]}))

            original_root = GENERATOR.ROOT
            original_ownership_path = GENERATOR.GENERATED_TARGETING_OWNERSHIP
            try:
                GENERATOR.ROOT = temporary_root
                GENERATOR.GENERATED_TARGETING_OWNERSHIP = ownership_path
                GENERATOR.update_spell_targeting({})
            finally:
                GENERATOR.ROOT = original_root
                GENERATOR.GENERATED_TARGETING_OWNERSHIP = original_ownership_path

            row = GENERATOR.read_2da(spells_path)["123"]
            self.assertEqual("****", row["TargetShape"])
            self.assertEqual("****", row["TargetSizeX"])
            self.assertEqual("****", row["TargetSizeY"])
            self.assertEqual("****", row["TargetFlags"])
            self.assertEqual({"spell_ids": []}, json.loads(ownership_path.read_text()))


class StatConfiguredIconPatternTests(unittest.TestCase):
    def test_valid_attribute_and_class_combinations_are_detected(self):
        fixtures = (
            "[StatConfiguredIcon]\npublic class FirstStatusEffect {}",
            "[StatConfiguredIcon]\n[SomeOther]\ninternal sealed class SecondStatusEffect {}",
            "[SomeOther]\ninternal class Before {}\n"
            "[StatConfiguredIcon]\n[AnotherAttribute]\npublic abstract partial class ThirdStatusEffect {}",
        )

        for fixture in fixtures:
            with self.subTest(fixture=fixture):
                self.assertIsNotNone(GENERATOR.STAT_CONFIGURED_ICON_CLASS_PATTERN.search(fixture))

    def test_unrelated_or_argument_bearing_attributes_are_not_detected(self):
        fixtures = (
            "[SomeOther]\npublic class StatusEffect {}",
            "[StatConfiguredIcon(12)]\npublic class InvalidStatusEffect {}",
        )

        for fixture in fixtures:
            with self.subTest(fixture=fixture):
                self.assertIsNone(GENERATOR.STAT_CONFIGURED_ICON_CLASS_PATTERN.search(fixture))


if __name__ == "__main__":
    unittest.main()
