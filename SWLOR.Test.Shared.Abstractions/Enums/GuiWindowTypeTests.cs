using NUnit.Framework;
using SWLOR.Shared.Abstractions.Enums;

namespace SWLOR.Test.Shared.Abstractions.Enums
{
    [TestFixture]
    public class GuiWindowTypeTests
    {
        [Test]
        public void EnumValues_ShouldBeCorrect()
        {
            // Assert key values
            Assert.That((int)GuiWindowType.Invalid, Is.EqualTo(0));
            Assert.That((int)GuiWindowType.CharacterSheet, Is.EqualTo(1));
            Assert.That((int)GuiWindowType.CustomizeCharacter, Is.EqualTo(2));
            Assert.That((int)GuiWindowType.Quests, Is.EqualTo(3));
            Assert.That((int)GuiWindowType.Recipes, Is.EqualTo(4));
            Assert.That((int)GuiWindowType.KeyItems, Is.EqualTo(5));
            Assert.That((int)GuiWindowType.Achievements, Is.EqualTo(6));
            Assert.That((int)GuiWindowType.Skills, Is.EqualTo(7));
            Assert.That((int)GuiWindowType.Perks, Is.EqualTo(8));
            Assert.That((int)GuiWindowType.Settings, Is.EqualTo(9));
            Assert.That((int)GuiWindowType.AppearanceEditor, Is.EqualTo(10));
            Assert.That((int)GuiWindowType.Bank, Is.EqualTo(11));
            Assert.That((int)GuiWindowType.CharacterMigration, Is.EqualTo(12));
            Assert.That((int)GuiWindowType.Outfits, Is.EqualTo(13));
            Assert.That((int)GuiWindowType.MarketListing, Is.EqualTo(14));
            Assert.That((int)GuiWindowType.MarketBuying, Is.EqualTo(15));
            Assert.That((int)GuiWindowType.PriceSelection, Is.EqualTo(16));
            Assert.That((int)GuiWindowType.ExamineItem, Is.EqualTo(17));
            Assert.That((int)GuiWindowType.Notes, Is.EqualTo(18));
            Assert.That((int)GuiWindowType.DistributeRPXP, Is.EqualTo(19));
            Assert.That((int)GuiWindowType.ShipManagement, Is.EqualTo(20));
            Assert.That((int)GuiWindowType.ChangeDescription, Is.EqualTo(21));
            Assert.That((int)GuiWindowType.Craft, Is.EqualTo(22));
            Assert.That((int)GuiWindowType.ManageStaff, Is.EqualTo(23));
            Assert.That((int)GuiWindowType.ManageCitizenship, Is.EqualTo(24));
            Assert.That((int)GuiWindowType.ManageCity, Is.EqualTo(25));
            Assert.That((int)GuiWindowType.Election, Is.EqualTo(26));
            Assert.That((int)GuiWindowType.RentApartment, Is.EqualTo(27));
            Assert.That((int)GuiWindowType.ManageApartment, Is.EqualTo(28));
            Assert.That((int)GuiWindowType.RenameItem, Is.EqualTo(29));
            Assert.That((int)GuiWindowType.PermissionManagement, Is.EqualTo(30));
            Assert.That((int)GuiWindowType.ManageStructures, Is.EqualTo(31));
            Assert.That((int)GuiWindowType.PropertyItemStorage, Is.EqualTo(32));
            Assert.That((int)GuiWindowType.BugReport, Is.EqualTo(33));
            Assert.That((int)GuiWindowType.DMTools, Is.EqualTo(34));
            Assert.That((int)GuiWindowType.PlayerStatus, Is.EqualTo(35));
            Assert.That((int)GuiWindowType.TargetStatus, Is.EqualTo(36));
            Assert.That((int)GuiWindowType.ManageBans, Is.EqualTo(37));
            Assert.That((int)GuiWindowType.Refinery, Is.EqualTo(38));
            Assert.That((int)GuiWindowType.DMPlayerExamine, Is.EqualTo(39));
            Assert.That((int)GuiWindowType.AreaNotes, Is.EqualTo(40));
            Assert.That((int)GuiWindowType.CreatureManager, Is.EqualTo(41));
            Assert.That((int)GuiWindowType.AreaManager, Is.EqualTo(42));
            Assert.That((int)GuiWindowType.Emotes, Is.EqualTo(43));
            Assert.That((int)GuiWindowType.DroidAssembly, Is.EqualTo(44));
            Assert.That((int)GuiWindowType.DroidAI, Is.EqualTo(45));
            Assert.That((int)GuiWindowType.Currencies, Is.EqualTo(46));
            Assert.That((int)GuiWindowType.StatRebuild, Is.EqualTo(47));
            Assert.That((int)GuiWindowType.AssociateCharacterSheet, Is.EqualTo(48));
            Assert.That((int)GuiWindowType.HoloNet, Is.EqualTo(49));
            Assert.That((int)GuiWindowType.Stables, Is.EqualTo(50));
            Assert.That((int)GuiWindowType.TrainingStore, Is.EqualTo(51));
            Assert.That((int)GuiWindowType.Incubator, Is.EqualTo(52));
            Assert.That((int)GuiWindowType.Research, Is.EqualTo(53));
            Assert.That((int)GuiWindowType.TargetDescription, Is.EqualTo(54));
            Assert.That((int)GuiWindowType.DebugEnmity, Is.EqualTo(900));
            Assert.That((int)GuiWindowType.ChangePortrait, Is.EqualTo(9999));
        }

        [Test]
        public void EnumValues_ShouldBeUnique()
        {
            // Arrange
            var values = Enum.GetValues<GuiWindowType>().Cast<int>().ToArray();

            // Assert
            Assert.That(values, Is.Unique);
        }

        [Test]
        public void EnumToString_ShouldReturnCorrectName()
        {
            // Assert
            Assert.That(GuiWindowType.Invalid.ToString(), Is.EqualTo("Invalid"));
            Assert.That(GuiWindowType.CharacterSheet.ToString(), Is.EqualTo("CharacterSheet"));
            Assert.That(GuiWindowType.CustomizeCharacter.ToString(), Is.EqualTo("CustomizeCharacter"));
            Assert.That(GuiWindowType.Quests.ToString(), Is.EqualTo("Quests"));
            Assert.That(GuiWindowType.Recipes.ToString(), Is.EqualTo("Recipes"));
            Assert.That(GuiWindowType.KeyItems.ToString(), Is.EqualTo("KeyItems"));
            Assert.That(GuiWindowType.Achievements.ToString(), Is.EqualTo("Achievements"));
            Assert.That(GuiWindowType.Skills.ToString(), Is.EqualTo("Skills"));
            Assert.That(GuiWindowType.Perks.ToString(), Is.EqualTo("Perks"));
            Assert.That(GuiWindowType.Settings.ToString(), Is.EqualTo("Settings"));
            Assert.That(GuiWindowType.AppearanceEditor.ToString(), Is.EqualTo("AppearanceEditor"));
            Assert.That(GuiWindowType.Bank.ToString(), Is.EqualTo("Bank"));
            Assert.That(GuiWindowType.CharacterMigration.ToString(), Is.EqualTo("CharacterMigration"));
            Assert.That(GuiWindowType.Outfits.ToString(), Is.EqualTo("Outfits"));
            Assert.That(GuiWindowType.MarketListing.ToString(), Is.EqualTo("MarketListing"));
            Assert.That(GuiWindowType.MarketBuying.ToString(), Is.EqualTo("MarketBuying"));
            Assert.That(GuiWindowType.PriceSelection.ToString(), Is.EqualTo("PriceSelection"));
            Assert.That(GuiWindowType.ExamineItem.ToString(), Is.EqualTo("ExamineItem"));
            Assert.That(GuiWindowType.Notes.ToString(), Is.EqualTo("Notes"));
            Assert.That(GuiWindowType.DistributeRPXP.ToString(), Is.EqualTo("DistributeRPXP"));
            Assert.That(GuiWindowType.ShipManagement.ToString(), Is.EqualTo("ShipManagement"));
            Assert.That(GuiWindowType.ChangeDescription.ToString(), Is.EqualTo("ChangeDescription"));
            Assert.That(GuiWindowType.Craft.ToString(), Is.EqualTo("Craft"));
            Assert.That(GuiWindowType.ManageStaff.ToString(), Is.EqualTo("ManageStaff"));
            Assert.That(GuiWindowType.ManageCitizenship.ToString(), Is.EqualTo("ManageCitizenship"));
            Assert.That(GuiWindowType.ManageCity.ToString(), Is.EqualTo("ManageCity"));
            Assert.That(GuiWindowType.Election.ToString(), Is.EqualTo("Election"));
            Assert.That(GuiWindowType.RentApartment.ToString(), Is.EqualTo("RentApartment"));
            Assert.That(GuiWindowType.ManageApartment.ToString(), Is.EqualTo("ManageApartment"));
            Assert.That(GuiWindowType.RenameItem.ToString(), Is.EqualTo("RenameItem"));
            Assert.That(GuiWindowType.PermissionManagement.ToString(), Is.EqualTo("PermissionManagement"));
            Assert.That(GuiWindowType.ManageStructures.ToString(), Is.EqualTo("ManageStructures"));
            Assert.That(GuiWindowType.PropertyItemStorage.ToString(), Is.EqualTo("PropertyItemStorage"));
            Assert.That(GuiWindowType.BugReport.ToString(), Is.EqualTo("BugReport"));
            Assert.That(GuiWindowType.DMTools.ToString(), Is.EqualTo("DMTools"));
            Assert.That(GuiWindowType.PlayerStatus.ToString(), Is.EqualTo("PlayerStatus"));
            Assert.That(GuiWindowType.TargetStatus.ToString(), Is.EqualTo("TargetStatus"));
            Assert.That(GuiWindowType.ManageBans.ToString(), Is.EqualTo("ManageBans"));
            Assert.That(GuiWindowType.Refinery.ToString(), Is.EqualTo("Refinery"));
            Assert.That(GuiWindowType.DMPlayerExamine.ToString(), Is.EqualTo("DMPlayerExamine"));
            Assert.That(GuiWindowType.AreaNotes.ToString(), Is.EqualTo("AreaNotes"));
            Assert.That(GuiWindowType.CreatureManager.ToString(), Is.EqualTo("CreatureManager"));
            Assert.That(GuiWindowType.AreaManager.ToString(), Is.EqualTo("AreaManager"));
            Assert.That(GuiWindowType.Emotes.ToString(), Is.EqualTo("Emotes"));
            Assert.That(GuiWindowType.DroidAssembly.ToString(), Is.EqualTo("DroidAssembly"));
            Assert.That(GuiWindowType.DroidAI.ToString(), Is.EqualTo("DroidAI"));
            Assert.That(GuiWindowType.Currencies.ToString(), Is.EqualTo("Currencies"));
            Assert.That(GuiWindowType.StatRebuild.ToString(), Is.EqualTo("StatRebuild"));
            Assert.That(GuiWindowType.AssociateCharacterSheet.ToString(), Is.EqualTo("AssociateCharacterSheet"));
            Assert.That(GuiWindowType.HoloNet.ToString(), Is.EqualTo("HoloNet"));
            Assert.That(GuiWindowType.Stables.ToString(), Is.EqualTo("Stables"));
            Assert.That(GuiWindowType.TrainingStore.ToString(), Is.EqualTo("TrainingStore"));
            Assert.That(GuiWindowType.Incubator.ToString(), Is.EqualTo("Incubator"));
            Assert.That(GuiWindowType.Research.ToString(), Is.EqualTo("Research"));
            Assert.That(GuiWindowType.TargetDescription.ToString(), Is.EqualTo("TargetDescription"));
            Assert.That(GuiWindowType.DebugEnmity.ToString(), Is.EqualTo("DebugEnmity"));
            Assert.That(GuiWindowType.ChangePortrait.ToString(), Is.EqualTo("ChangePortrait"));
        }

        [Test]
        public void EnumParse_ShouldWorkCorrectly()
        {
            // Assert
            Assert.That(Enum.Parse<GuiWindowType>("Invalid"), Is.EqualTo(GuiWindowType.Invalid));
            Assert.That(Enum.Parse<GuiWindowType>("CharacterSheet"), Is.EqualTo(GuiWindowType.CharacterSheet));
            Assert.That(Enum.Parse<GuiWindowType>("CustomizeCharacter"), Is.EqualTo(GuiWindowType.CustomizeCharacter));
            Assert.That(Enum.Parse<GuiWindowType>("Quests"), Is.EqualTo(GuiWindowType.Quests));
            Assert.That(Enum.Parse<GuiWindowType>("Recipes"), Is.EqualTo(GuiWindowType.Recipes));
            Assert.That(Enum.Parse<GuiWindowType>("KeyItems"), Is.EqualTo(GuiWindowType.KeyItems));
            Assert.That(Enum.Parse<GuiWindowType>("Achievements"), Is.EqualTo(GuiWindowType.Achievements));
            Assert.That(Enum.Parse<GuiWindowType>("Skills"), Is.EqualTo(GuiWindowType.Skills));
            Assert.That(Enum.Parse<GuiWindowType>("Perks"), Is.EqualTo(GuiWindowType.Perks));
            Assert.That(Enum.Parse<GuiWindowType>("Settings"), Is.EqualTo(GuiWindowType.Settings));
            Assert.That(Enum.Parse<GuiWindowType>("AppearanceEditor"), Is.EqualTo(GuiWindowType.AppearanceEditor));
            Assert.That(Enum.Parse<GuiWindowType>("Bank"), Is.EqualTo(GuiWindowType.Bank));
            Assert.That(Enum.Parse<GuiWindowType>("CharacterMigration"), Is.EqualTo(GuiWindowType.CharacterMigration));
            Assert.That(Enum.Parse<GuiWindowType>("Outfits"), Is.EqualTo(GuiWindowType.Outfits));
            Assert.That(Enum.Parse<GuiWindowType>("MarketListing"), Is.EqualTo(GuiWindowType.MarketListing));
            Assert.That(Enum.Parse<GuiWindowType>("MarketBuying"), Is.EqualTo(GuiWindowType.MarketBuying));
            Assert.That(Enum.Parse<GuiWindowType>("PriceSelection"), Is.EqualTo(GuiWindowType.PriceSelection));
            Assert.That(Enum.Parse<GuiWindowType>("ExamineItem"), Is.EqualTo(GuiWindowType.ExamineItem));
            Assert.That(Enum.Parse<GuiWindowType>("Notes"), Is.EqualTo(GuiWindowType.Notes));
            Assert.That(Enum.Parse<GuiWindowType>("DistributeRPXP"), Is.EqualTo(GuiWindowType.DistributeRPXP));
            Assert.That(Enum.Parse<GuiWindowType>("ShipManagement"), Is.EqualTo(GuiWindowType.ShipManagement));
            Assert.That(Enum.Parse<GuiWindowType>("ChangeDescription"), Is.EqualTo(GuiWindowType.ChangeDescription));
            Assert.That(Enum.Parse<GuiWindowType>("Craft"), Is.EqualTo(GuiWindowType.Craft));
            Assert.That(Enum.Parse<GuiWindowType>("ManageStaff"), Is.EqualTo(GuiWindowType.ManageStaff));
            Assert.That(Enum.Parse<GuiWindowType>("ManageCitizenship"), Is.EqualTo(GuiWindowType.ManageCitizenship));
            Assert.That(Enum.Parse<GuiWindowType>("ManageCity"), Is.EqualTo(GuiWindowType.ManageCity));
            Assert.That(Enum.Parse<GuiWindowType>("Election"), Is.EqualTo(GuiWindowType.Election));
            Assert.That(Enum.Parse<GuiWindowType>("RentApartment"), Is.EqualTo(GuiWindowType.RentApartment));
            Assert.That(Enum.Parse<GuiWindowType>("ManageApartment"), Is.EqualTo(GuiWindowType.ManageApartment));
            Assert.That(Enum.Parse<GuiWindowType>("RenameItem"), Is.EqualTo(GuiWindowType.RenameItem));
            Assert.That(Enum.Parse<GuiWindowType>("PermissionManagement"), Is.EqualTo(GuiWindowType.PermissionManagement));
            Assert.That(Enum.Parse<GuiWindowType>("ManageStructures"), Is.EqualTo(GuiWindowType.ManageStructures));
            Assert.That(Enum.Parse<GuiWindowType>("PropertyItemStorage"), Is.EqualTo(GuiWindowType.PropertyItemStorage));
            Assert.That(Enum.Parse<GuiWindowType>("BugReport"), Is.EqualTo(GuiWindowType.BugReport));
            Assert.That(Enum.Parse<GuiWindowType>("DMTools"), Is.EqualTo(GuiWindowType.DMTools));
            Assert.That(Enum.Parse<GuiWindowType>("PlayerStatus"), Is.EqualTo(GuiWindowType.PlayerStatus));
            Assert.That(Enum.Parse<GuiWindowType>("TargetStatus"), Is.EqualTo(GuiWindowType.TargetStatus));
            Assert.That(Enum.Parse<GuiWindowType>("ManageBans"), Is.EqualTo(GuiWindowType.ManageBans));
            Assert.That(Enum.Parse<GuiWindowType>("Refinery"), Is.EqualTo(GuiWindowType.Refinery));
            Assert.That(Enum.Parse<GuiWindowType>("DMPlayerExamine"), Is.EqualTo(GuiWindowType.DMPlayerExamine));
            Assert.That(Enum.Parse<GuiWindowType>("AreaNotes"), Is.EqualTo(GuiWindowType.AreaNotes));
            Assert.That(Enum.Parse<GuiWindowType>("CreatureManager"), Is.EqualTo(GuiWindowType.CreatureManager));
            Assert.That(Enum.Parse<GuiWindowType>("AreaManager"), Is.EqualTo(GuiWindowType.AreaManager));
            Assert.That(Enum.Parse<GuiWindowType>("Emotes"), Is.EqualTo(GuiWindowType.Emotes));
            Assert.That(Enum.Parse<GuiWindowType>("DroidAssembly"), Is.EqualTo(GuiWindowType.DroidAssembly));
            Assert.That(Enum.Parse<GuiWindowType>("DroidAI"), Is.EqualTo(GuiWindowType.DroidAI));
            Assert.That(Enum.Parse<GuiWindowType>("Currencies"), Is.EqualTo(GuiWindowType.Currencies));
            Assert.That(Enum.Parse<GuiWindowType>("StatRebuild"), Is.EqualTo(GuiWindowType.StatRebuild));
            Assert.That(Enum.Parse<GuiWindowType>("AssociateCharacterSheet"), Is.EqualTo(GuiWindowType.AssociateCharacterSheet));
            Assert.That(Enum.Parse<GuiWindowType>("HoloNet"), Is.EqualTo(GuiWindowType.HoloNet));
            Assert.That(Enum.Parse<GuiWindowType>("Stables"), Is.EqualTo(GuiWindowType.Stables));
            Assert.That(Enum.Parse<GuiWindowType>("TrainingStore"), Is.EqualTo(GuiWindowType.TrainingStore));
            Assert.That(Enum.Parse<GuiWindowType>("Incubator"), Is.EqualTo(GuiWindowType.Incubator));
            Assert.That(Enum.Parse<GuiWindowType>("Research"), Is.EqualTo(GuiWindowType.Research));
            Assert.That(Enum.Parse<GuiWindowType>("TargetDescription"), Is.EqualTo(GuiWindowType.TargetDescription));
            Assert.That(Enum.Parse<GuiWindowType>("DebugEnmity"), Is.EqualTo(GuiWindowType.DebugEnmity));
            Assert.That(Enum.Parse<GuiWindowType>("ChangePortrait"), Is.EqualTo(GuiWindowType.ChangePortrait));
        }
    }
}
