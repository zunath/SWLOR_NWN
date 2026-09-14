using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Verifies the blueprint document views (Utc/Uti/Utp/Utd/Utw/Uts/Utt/Utm) against one real
    /// corpus file per type.
    /// </summary>
    public class BlueprintDocumentTests
    {
        private static string ModulePath(string folder, string file) =>
            Path.Combine(CorpusLocator.ModuleDirectory, folder, file);

        [Test]
        public void ZombGuardUtc_KnownValues_ReadCorrectly()
        {
            var document = UtcDocument.Load(ModulePath("utc", "zomb_guard.utc.json"));

            document.TemplateResRef.Should().Be("zomb_guard");
            document.Tag.Should().Be("zomb_guard");
            document.FirstName.Text.Should().Be("Zombie, Guard");
            document.FactionID.Should().Be(1);
            document.AppearanceType.Should().Be(2095);
            document.PortraitId.Should().Be(3832);
            document.ScriptAttacked.Should().Be(string.Empty);
        }

        [Test]
        public void UtiItem_KnownValues_ReadCorrectly()
        {
            var document = UtiDocument.Load(ModulePath("uti", "001.uti.json"));

            document.TemplateResRef.Should().Be("001");
            document.Tag.Should().Be("tat_militia_helmet");
            document.LocalizedName.Text.Should().Be("Tatooine Militia Field Helmet");
            document.LocalizedName.StrRef.Should().Be(90531u);
            document.BaseItem.Should().Be(17);
            document.StackSize.Should().Be(1);
            document.Cost.Should().Be(30u);
            document.AddCost.Should().Be(29u);
            document.PropertiesList.Should().HaveCount(2);
        }

        [Test]
        public void UtpPlaceable_KnownValues_ReadCorrectly()
        {
            var document = UtpDocument.Load(ModulePath("utp", "afr_kraal_hut001.utp.json"));

            document.TemplateResRef.Should().Be("afr_kraal_hut001");
            document.Tag.Should().Be("afr_kraal_fence");
            document.LocName.Text.Should().Be("Africa Kraal Fence");
            document.Appearance.Should().Be(25343u);
            document.Useable.Should().Be(false);
            document.Static.Should().Be(true);
            document.Plot.Should().Be(true);
        }

        [Test]
        public void UtdDoor_KnownValues_ReadCorrectly()
        {
            var document = UtdDocument.Load(ModulePath("utd", "locked_door.utd.json"));

            document.TemplateResRef.Should().Be("locked_door");
            document.Tag.Should().Be("locked_door");
            document.LocName.Text.Should().Be("Locked Door");
            document.Locked.Should().Be(true);
        }

        [Test]
        public void UtwWaypoint_KnownValues_ReadCorrectly()
        {
            var document = UtwDocument.Load(ModulePath("utw", "beetle_spwn001.utw.json"));

            document.Tag.Should().Be("TATOOINE_WRAID");
            document.TemplateResRef.Should().Be("beetle_spwn001");
            document.LocalizedName.Text.Should().Be("Tatooine - Sand Beetle");
        }

        [Test]
        public void UtsSound_KnownValues_ReadCorrectly()
        {
            var document = UtsDocument.Load(ModulePath("uts", "night_bazzarnois.uts.json"));

            document.TemplateResRef.Should().Be("night_bazzarnois");
            document.Tag.Should().Be("BazzarNoises");
            document.LocName.Text.Should().Be("BazzarNoises");
            document.Volume.Should().Be(49);
            document.Active.Should().Be(true);
            document.Sounds.Should().NotBeEmpty();
            UtsDocument.GetSoundResRef(document.Sounds[0]).Should().Be("al_pl_bazarwalla");
        }

        [Test]
        public void UttTrigger_KnownValues_ReadCorrectly()
        {
            var document = UttDocument.Load(ModulePath("utt", "anti_spawn_trigg.utt.json"));

            document.TemplateResRef.Should().Be("anti_spawn_trigg");
            document.Tag.Should().Be("anti_spawn_trigg");
            document.LocalizedName.Text.Should().Be("No Spawn Zone");
        }

        [Test]
        public void UtmStore_KnownValues_ReadCorrectly()
        {
            var document = UtmDocument.Load(ModulePath("utm", "bartender.utm.json"));

            // Deviation from the generic blueprint pattern: .utm uses "ResRef", not
            // "TemplateResRef", for the template resref field.
            document.ResRef.Should().Be("bartender");
            document.Tag.Should().Be("bartender");
            document.LocName.Text.Should().Be("Bartender");
            document.MarkUp.Should().Be(100);
            document.MarkDown.Should().Be(65);
            document.StoreList.Should().HaveCount(5);
        }

        [Test]
        public void SettingUtcTag_ThenSerializing_RoundTripsTheNewValue()
        {
            var path = ModulePath("utc", "zomb_guard.utc.json");
            var document = UtcDocument.Parse(File.ReadAllBytes(path));

            document.Tag = "zomb_guard_renamed";
            var written = document.ToBytes();

            var reparsed = UtcDocument.Parse(written);
            reparsed.Tag.Should().Be("zomb_guard_renamed");
            reparsed.FirstName.Text.Should().Be("Zombie, Guard");
        }
    }
}
