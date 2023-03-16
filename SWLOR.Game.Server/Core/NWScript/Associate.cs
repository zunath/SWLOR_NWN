using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Associate;

namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Sets the maximum number of henchmen
        /// </summary>
        public static void SetMaxHenchmen(int nNumHenchmen)
        {
            VM.StackPush(nNumHenchmen);
            VM.Call(746);
        }

        /// <summary>
        ///   Gets the maximum number of henchmen
        /// </summary>
        public static int GetMaxHenchmen()
        {
            VM.Call(747);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Returns the associate type of the specified creature.
        ///   - Returns ASSOCIATE_TYPE_NONE if the creature is not the associate of anyone.
        /// </summary>
        public static AssociateType GetAssociateType(uint oAssociate)
        {
            VM.StackPush(oAssociate);
            VM.Call(748);
            return (AssociateType)VM.StackPopInt();
        }

        /// <summary>
        ///   Levels up a creature using default settings.
        ///   If successfull it returns the level the creature now is, or 0 if it fails.
        ///   If you want to give them a different level (ie: Give a Fighter a level of Wizard)
        ///   you can specify that in the nClass.
        ///   However, if you specify a class to which the creature no package specified,
        ///   they will use the default package for that class for their levelup choices.
        ///   (ie: no Barbarian Savage/Wizard Divination combinations)
        ///   If you turn on bReadyAllSpells, all memorized spells will be ready to cast without resting.
        ///   if nPackage is PACKAGE_INVALID then it will use the starting package assigned to that class or just the class package
        /// </summary>
        public static int LevelUpHenchman(
            uint oCreature, 
            ClassType nClass = ClassType.Invalid,
            bool bReadyAllSpells = false, 
            Package nPackage = Package.Invalid)
        {
            VM.StackPush((int)nPackage);
            VM.StackPush(bReadyAllSpells ? 1 : 0);
            VM.StackPush((int)nClass);
            VM.StackPush(oCreature);
            VM.Call(704);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Removes oAssociate from the service of oMaster, returning them to their
        ///   original faction.
        /// </summary>
        public static void RemoveSummonedAssociate(uint oMaster, uint oAssociate = OBJECT_INVALID)
        {
            VM.StackPush(oAssociate);
            VM.StackPush(oMaster);
            VM.Call(503);
        }

        /// <summary>
        ///   Get oCreature's familiar creature type (FAMILIAR_CREATURE_TYPE_*).
        ///   * Returns FAMILIAR_CREATURE_TYPE_NONE if oCreature is invalid or does not
        ///   currently have a familiar.
        /// </summary>
        public static int GetFamiliarCreatureType(uint oCreature)
        {
            VM.StackPush(oCreature);
            VM.Call(497);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get oCreature's animal companion creature type
        ///   (ANIMAL_COMPANION_CREATURE_TYPE_*).
        ///   * Returns ANIMAL_COMPANION_CREATURE_TYPE_NONE if oCreature is invalid or does
        ///   not currently have an animal companion.
        /// </summary>
        public static int GetAnimalCompanionCreatureType(uint oCreature)
        {
            VM.StackPush(oCreature);
            VM.Call(498);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get oCreature's familiar's name.
        ///   * Returns "" if oCreature is invalid, does not currently
        ///   have a familiar or if the familiar's name is blank.
        /// </summary>
        public static string GetFamiliarName(uint oCreature)
        {
            VM.StackPush(oCreature);
            VM.Call(499);
            return VM.StackPopString();
        }

        /// <summary>
        ///   Get oCreature's animal companion's name.
        ///   * Returns "" if oCreature is invalid, does not currently
        ///   have an animal companion or if the animal companion's name is blank.
        /// </summary>
        public static string GetAnimalCompanionName(uint oTarget)
        {
            VM.StackPush(oTarget);
            VM.Call(500);
            return VM.StackPopString();
        }

        /// <summary>
        ///   Get the associate of type nAssociateType belonging to oMaster.
        ///   - nAssociateType: ASSOCIATE_TYPE_*
        ///   - nMaster
        ///   - nTh: Which associate of the specified type to return
        ///   * Returns OBJECT_INVALID if no such associate exists.
        /// </summary>
        public static uint GetAssociate(AssociateType nAssociateType, uint oMaster = OBJECT_INVALID, int nTh = 1)
        {
            VM.StackPush(nTh);
            VM.StackPush(oMaster);
            VM.StackPush((int)nAssociateType);
            VM.Call(364);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Add oHenchman as a henchman to oMaster
        ///   If oHenchman is either a DM or a player character, this will have no effect.
        /// </summary>
        public static void AddHenchman(uint oMaster, uint oHenchman = OBJECT_INVALID)
        {
            VM.StackPush(oHenchman);
            VM.StackPush(oMaster);
            VM.Call(365);
        }

        /// <summary>
        ///   Remove oHenchman from the service of oMaster, returning them to their original faction.
        /// </summary>
        public static void RemoveHenchman(uint oMaster, uint oHenchman = OBJECT_INVALID)
        {
            VM.StackPush(oHenchman);
            VM.StackPush(oMaster);
            VM.Call(366);
        }

        /// <summary>
        ///   Get the henchman belonging to oMaster.
        ///   * Return OBJECT_INVALID if oMaster does not have a henchman.
        ///   -nNth: Which henchman to return.
        /// </summary>
        public static uint GetHenchman(uint oMaster = OBJECT_INVALID, int nNth = 1)
        {
            VM.StackPush(nNth);
            VM.StackPush(oMaster);
            VM.Call(354);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Summon an Animal Companion
        /// </summary>
        public static void SummonAnimalCompanion(uint oMaster = OBJECT_INVALID)
        {
            VM.StackPush(oMaster);
            VM.Call(334);
        }

        /// <summary>
        ///   Summon a Familiar
        /// </summary>
        public static void SummonFamiliar(uint oMaster = OBJECT_INVALID)
        {
            VM.StackPush(oMaster);
            VM.Call(335);
        }

        /// <summary>
        ///   Get the last command (ASSOCIATE_COMMAND_*) issued to oAssociate.
        /// </summary>
        public static int GetLastAssociateCommand(uint oAssociate = OBJECT_INVALID)
        {
            VM.StackPush(oAssociate);
            VM.Call(321);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the master of oAssociate.
        /// </summary>
        public static uint GetMaster(uint oAssociate = OBJECT_INVALID)
        {
            VM.StackPush(oAssociate);
            VM.Call(319);
            return VM.StackPopObject();
        }
    }
}