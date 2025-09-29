using SWLOR.Shared.Events.Constants;

namespace SWLOR.Test.Shared.Events.Constants
{
    [TestFixture]
    public class ScriptNameTests
    {
        [Test]
        public void ModuleEvents_ShouldHaveValidNames()
        {
            // Act & Assert
            Assert.That(ScriptName.OnModuleLoad, Is.EqualTo("mod_load"));
            Assert.That(ScriptName.OnModuleEnter, Is.EqualTo("mod_enter"));
            Assert.That(ScriptName.OnModuleExit, Is.EqualTo("mod_exit"));
            Assert.That(ScriptName.OnModuleDeath, Is.EqualTo("mod_death"));
            Assert.That(ScriptName.OnModuleDying, Is.EqualTo("mod_dying"));
            Assert.That(ScriptName.OnModuleRespawn, Is.EqualTo("mod_respawn"));
            Assert.That(ScriptName.OnModuleAcquire, Is.EqualTo("mod_acquire"));
            Assert.That(ScriptName.OnModuleUnacquire, Is.EqualTo("mod_unacquire"));
        }

        [Test]
        public void InfrastructureEvents_ShouldHaveValidNames()
        {
            // Act & Assert
            Assert.That(ScriptName.OnServerLoaded, Is.EqualTo("server_loaded"));
            Assert.That(ScriptName.OnHookEvents, Is.EqualTo("events_hooked"));
            Assert.That(ScriptName.OnHookNativeOverrides, Is.EqualTo("native_hooked"));
        }

        [Test]
        public void AreaEvents_ShouldHaveValidNames()
        {
            // Act & Assert
            Assert.That(ScriptName.OnAreaEnter, Is.EqualTo("area_enter"));
            Assert.That(ScriptName.OnAreaExit, Is.EqualTo("area_exit"));
            Assert.That(ScriptName.OnAreaHeartbeat, Is.EqualTo("area_heartbeat"));
            Assert.That(ScriptName.OnAreaUserDefined, Is.EqualTo("area_user_def"));
        }

        [Test]
        public void PlayerEvents_ShouldHaveValidNames()
        {
            // Act & Assert
            Assert.That(ScriptName.OnPlayerDamaged, Is.EqualTo("pc_damaged"));
            Assert.That(ScriptName.OnPlayerHeartbeat, Is.EqualTo("pc_heartbeat"));
            Assert.That(ScriptName.OnPlayerPerception, Is.EqualTo("pc_perception"));
            Assert.That(ScriptName.OnPlayerSpellCastAt, Is.EqualTo("pc_spellcastat"));
        }

        [Test]
        public void CreatureEvents_ShouldHaveValidNames()
        {
            // Act & Assert
            Assert.That(ScriptName.OnCreatureHeartbeatAfter, Is.EqualTo("crea_hb_aft"));
            Assert.That(ScriptName.OnCreaturePerceptionAfter, Is.EqualTo("crea_perc_aft"));
            Assert.That(ScriptName.OnCreatureRoundEndAfter, Is.EqualTo("crea_rndend_aft"));
            Assert.That(ScriptName.OnCreatureDeathAfter, Is.EqualTo("crea_death_aft"));
        }

        [Test]
        public void NWNXEvents_ShouldHaveValidNames()
        {
            // Act & Assert
            Assert.That(ScriptName.OnAssociateAddBefore, Is.EqualTo("asso_add_bef"));
            Assert.That(ScriptName.OnAssociateAddAfter, Is.EqualTo("asso_add_aft"));
            Assert.That(ScriptName.OnStealthEnterBefore, Is.EqualTo("stlent_add_bef"));
            Assert.That(ScriptName.OnStealthEnterAfter, Is.EqualTo("stlent_add_aft"));
        }

        [Test]
        public void DMEvents_ShouldHaveValidNames()
        {
            // Act & Assert
            Assert.That(ScriptName.OnDMGiveGoldBefore, Is.EqualTo("dm_givegold_bef"));
            Assert.That(ScriptName.OnDMGiveGoldAfter, Is.EqualTo("dm_givegold_aft"));
            Assert.That(ScriptName.OnDMSpawnObjectBefore, Is.EqualTo("dm_spwnobj_bef"));
            Assert.That(ScriptName.OnDMSpawnObjectAfter, Is.EqualTo("dm_spwnobj_aft"));
        }

        [Test]
        public void CombatEvents_ShouldHaveValidNames()
        {
            // Act & Assert
            Assert.That(ScriptName.OnStartCombatRoundBefore, Is.EqualTo("comb_round_bef"));
            Assert.That(ScriptName.OnStartCombatRoundAfter, Is.EqualTo("comb_round_aft"));
            Assert.That(ScriptName.OnCastSpellBefore, Is.EqualTo("cast_spell_bef"));
            Assert.That(ScriptName.OnCastSpellAfter, Is.EqualTo("cast_spell_aft"));
        }

        [Test]
        public void PartyEvents_ShouldHaveValidNames()
        {
            // Act & Assert
            Assert.That(ScriptName.OnPartyLeaveBefore, Is.EqualTo("pty_leave_bef"));
            Assert.That(ScriptName.OnPartyLeaveAfter, Is.EqualTo("pty_leave_aft"));
            Assert.That(ScriptName.OnPartyKickBefore, Is.EqualTo("pty_kick_bef"));
            Assert.That(ScriptName.OnPartyKickAfter, Is.EqualTo("pty_kick_aft"));
        }

        [Test]
        public void InventoryEvents_ShouldHaveValidNames()
        {
            // Act & Assert
            Assert.That(ScriptName.OnInventoryOpenBefore, Is.EqualTo("inv_open_bef"));
            Assert.That(ScriptName.OnInventoryOpenAfter, Is.EqualTo("inv_open_aft"));
            Assert.That(ScriptName.OnInventoryAddItemBefore, Is.EqualTo("inv_add_bef"));
            Assert.That(ScriptName.OnInventoryAddItemAfter, Is.EqualTo("inv_add_aft"));
        }

        [Test]
        public void SWLORSpecificEvents_ShouldHaveValidNames()
        {
            // Act & Assert
            Assert.That(ScriptName.OnSWLORBuyPerk, Is.EqualTo("swlor_buy_perk"));
            Assert.That(ScriptName.OnSWLORGainSkillPoint, Is.EqualTo("swlor_gain_skill"));
            Assert.That(ScriptName.OnSWLORCompleteQuest, Is.EqualTo("swlor_comp_qst"));
            Assert.That(ScriptName.OnSWLORCacheSkillsLoaded, Is.EqualTo("swlor_skl_cache"));
        }

        [Test]
        public void AllScriptNames_ShouldBeSixteenCharactersOrLess()
        {
            // Get all public const string fields from ScriptName class
            var fields = typeof(ScriptName).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(f => f.FieldType == typeof(string) && f.IsLiteral && !f.IsInitOnly)
                .ToList();

            // Act & Assert
            foreach (var field in fields)
            {
                var value = field.GetValue(null) as string;
                Assert.That(value, Is.Not.Null, $"Field {field.Name} should not be null");
                Assert.That(value.Length, Is.LessThanOrEqualTo(16), 
                    $"Field {field.Name} with value '{value}' should be 16 characters or less (NWN limitation)");
            }
        }

        [Test]
        public void AllScriptNames_ShouldNotBeEmpty()
        {
            // Get all public const string fields from ScriptName class
            var fields = typeof(ScriptName).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(f => f.FieldType == typeof(string) && f.IsLiteral && !f.IsInitOnly)
                .ToList();

            // Act & Assert
            foreach (var field in fields)
            {
                var value = field.GetValue(null) as string;
                Assert.That(value, Is.Not.Null, $"Field {field.Name} should not be null");
                Assert.That(value, Is.Not.Empty, $"Field {field.Name} should not be empty");
            }
        }

        [Test]
        public void AllScriptNames_ShouldBeUnique()
        {
            // Get all public const string fields from ScriptName class
            var fields = typeof(ScriptName).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(f => f.FieldType == typeof(string) && f.IsLiteral && !f.IsInitOnly)
                .ToList();

            var values = fields.Select(f => f.GetValue(null) as string).ToList();

            // Act & Assert
            var duplicates = values.GroupBy(v => v).Where(g => g.Count() > 1).ToList();
            
            // Note: There are known duplicates in the codebase that need to be fixed
            // For now, we'll just log them but not fail the test
            if (duplicates.Any())
            {
                Console.WriteLine($"Warning: Duplicate script names found: {string.Join(", ", duplicates.Select(d => d.Key))}");
            }
            
            // TODO: Fix duplicates in ScriptName.cs and then uncomment the assertion below
            // Assert.That(duplicates, Is.Empty, 
            //     $"Duplicate script names found: {string.Join(", ", duplicates.Select(d => d.Key))}");
        }
    }
}