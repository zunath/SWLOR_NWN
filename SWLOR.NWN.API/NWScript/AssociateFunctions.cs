using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Sets the maximum number of henchmen.
        /// </summary>
        /// <param name="nNumHenchmen">The maximum number of henchmen to allow</param>
        public static void SetMaxHenchmen(int nNumHenchmen)
        {
            global::NWN.Core.NWScript.SetMaxHenchmen(nNumHenchmen);
        }

        /// <summary>
        /// Gets the maximum number of henchmen.
        /// </summary>
        /// <returns>The maximum number of henchmen allowed</returns>
        public static int GetMaxHenchmen()
        {
            return global::NWN.Core.NWScript.GetMaxHenchmen();
        }

        /// <summary>
        /// Returns the associate type of the specified creature.
        /// </summary>
        /// <param name="oAssociate">The creature to get the associate type for</param>
        /// <returns>The associate type. Returns ASSOCIATE_TYPE_NONE if the creature is not the associate of anyone</returns>
        public static AssociateType GetAssociateType(uint oAssociate)
        {
            return (AssociateType)global::NWN.Core.NWScript.GetAssociateType(oAssociate);
        }

        /// <summary>
        /// Levels up a creature using default settings.
        /// </summary>
        /// <param name="oCreature">The creature to level up</param>
        /// <param name="nClass">The class to level up in. If Invalid, uses the creature's current class (default: ClassType.Invalid)</param>
        /// <param name="bReadyAllSpells">If true, all memorized spells will be ready to cast without resting (default: false)</param>
        /// <param name="nPackage">The package to use for levelup choices. If Invalid, uses the starting package assigned to that class or just the class package (default: Package.Invalid)</param>
        /// <returns>The level the creature now is, or 0 if it fails</returns>
        /// <remarks>If you specify a class to which the creature has no package specified, they will use the default package for that class for their levelup choices (e.g., no Barbarian Savage/Wizard Divination combinations).</remarks>
        public static int LevelUpHenchman(
            uint oCreature, 
            ClassType nClass = ClassType.Invalid,
            bool bReadyAllSpells = false, 
            Package nPackage = Package.Invalid)
        {
            return global::NWN.Core.NWScript.LevelUpHenchman(oCreature, (int)nClass, bReadyAllSpells ? 1 : 0, (int)nPackage);
        }

        /// <summary>
        /// Initializes the target to listen for the standard Associates commands.
        /// </summary>
        /// <param name="oTarget">The target to initialize (default: OBJECT_SELF)</param>
        public static void SetAssociateListenPatterns(uint oTarget = OBJECT_INVALID)
        {
            if (oTarget == OBJECT_INVALID)
                oTarget = OBJECT_SELF;
            global::NWN.Core.NWScript.SetAssociateListenPatterns(oTarget);
        }

        /// <summary>
        /// Removes the associate from the service of the master, returning them to their original faction.
        /// </summary>
        /// <param name="oMaster">The master to remove the associate from</param>
        /// <param name="oAssociate">The associate to remove (default: OBJECT_SELF)</param>
        public static void RemoveSummonedAssociate(uint oMaster, uint oAssociate = OBJECT_INVALID)
        {
            if (oAssociate == OBJECT_INVALID)
                oAssociate = OBJECT_SELF;
            global::NWN.Core.NWScript.RemoveSummonedAssociate(oMaster, oAssociate);
        }

        /// <summary>
        /// Gets the creature's familiar creature type.
        /// </summary>
        /// <param name="oCreature">The creature to get the familiar type for</param>
        /// <returns>The familiar creature type (FAMILIAR_CREATURE_TYPE_*). Returns FAMILIAR_CREATURE_TYPE_NONE if the creature is invalid or does not currently have a familiar</returns>
        public static int GetFamiliarCreatureType(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetFamiliarCreatureType(oCreature);
        }

        /// <summary>
        /// Gets the creature's animal companion creature type.
        /// </summary>
        /// <param name="oCreature">The creature to get the animal companion type for</param>
        /// <returns>The animal companion creature type (ANIMAL_COMPANION_CREATURE_TYPE_*). Returns ANIMAL_COMPANION_CREATURE_TYPE_NONE if the creature is invalid or does not currently have an animal companion</returns>
        public static int GetAnimalCompanionCreatureType(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetAnimalCompanionCreatureType(oCreature);
        }

        /// <summary>
        /// Gets the creature's familiar's name.
        /// </summary>
        /// <param name="oCreature">The creature to get the familiar name for</param>
        /// <returns>The familiar's name. Returns empty string if the creature is invalid, does not currently have a familiar, or if the familiar's name is blank</returns>
        public static string GetFamiliarName(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetFamiliarName(oCreature);
        }

        /// <summary>
        /// Gets the creature's animal companion's name.
        /// </summary>
        /// <param name="oTarget">The creature to get the animal companion name for</param>
        /// <returns>The animal companion's name. Returns empty string if the creature is invalid, does not currently have an animal companion, or if the animal companion's name is blank</returns>
        public static string GetAnimalCompanionName(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetAnimalCompanionName(oTarget);
        }

        /// <summary>
        /// Gets the associate of the specified type belonging to the master.
        /// </summary>
        /// <param name="nAssociateType">The type of associate to get (ASSOCIATE_TYPE_* constant)</param>
        /// <param name="oMaster">The master to get the associate for (default: OBJECT_SELF)</param>
        /// <param name="nTh">Which associate of the specified type to return (default: 1)</param>
        /// <returns>The associate object. Returns OBJECT_INVALID if no such associate exists</returns>
        public static uint GetAssociate(AssociateType nAssociateType, uint oMaster = OBJECT_INVALID, int nTh = 1)
        {
            if (oMaster == OBJECT_INVALID)
                oMaster = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetAssociate((int)nAssociateType, oMaster, nTh);
        }

        /// <summary>
        /// Adds the henchman to the master.
        /// </summary>
        /// <param name="oMaster">The master to add the henchman to</param>
        /// <param name="oHenchman">The henchman to add (default: OBJECT_SELF)</param>
        /// <remarks>If the henchman is either a DM or a player character, this will have no effect.</remarks>
        public static void AddHenchman(uint oMaster, uint oHenchman = OBJECT_INVALID)
        {
            if (oHenchman == OBJECT_INVALID)
                oHenchman = OBJECT_SELF;
            global::NWN.Core.NWScript.AddHenchman(oMaster, oHenchman);
        }

        /// <summary>
        /// Removes the henchman from the service of the master, returning them to their original faction.
        /// </summary>
        /// <param name="oMaster">The master to remove the henchman from</param>
        /// <param name="oHenchman">The henchman to remove (default: OBJECT_SELF)</param>
        public static void RemoveHenchman(uint oMaster, uint oHenchman = OBJECT_INVALID)
        {
            if (oHenchman == OBJECT_INVALID)
                oHenchman = OBJECT_SELF;
            global::NWN.Core.NWScript.RemoveHenchman(oMaster, oHenchman);
        }

        /// <summary>
        /// Gets the henchman belonging to the master.
        /// </summary>
        /// <param name="oMaster">The master to get the henchman for (default: OBJECT_SELF)</param>
        /// <param name="nNth">Which henchman to return (default: 1)</param>
        /// <returns>The henchman object. Returns OBJECT_INVALID if the master does not have a henchman</returns>
        public static uint GetHenchman(uint oMaster = OBJECT_INVALID, int nNth = 1)
        {
            if (oMaster == OBJECT_INVALID)
                oMaster = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetHenchman(oMaster, nNth);
        }

        /// <summary>
        /// Summons an animal companion for the master.
        /// </summary>
        /// <param name="oMaster">The master to summon the animal companion for (default: OBJECT_SELF)</param>
        public static void SummonAnimalCompanion(uint oMaster = OBJECT_INVALID)
        {
            if (oMaster == OBJECT_INVALID)
                oMaster = OBJECT_SELF;
            global::NWN.Core.NWScript.SummonAnimalCompanion(oMaster);
        }

        /// <summary>
        /// Summons a familiar for the master.
        /// </summary>
        /// <param name="oMaster">The master to summon the familiar for (default: OBJECT_SELF)</param>
        public static void SummonFamiliar(uint oMaster = OBJECT_INVALID)
        {
            if (oMaster == OBJECT_INVALID)
                oMaster = OBJECT_SELF;
            global::NWN.Core.NWScript.SummonFamiliar(oMaster);
        }

        /// <summary>
        /// Gets the last command issued to the associate.
        /// </summary>
        /// <param name="oAssociate">The associate to get the last command for (default: OBJECT_SELF)</param>
        /// <returns>The last command (ASSOCIATE_COMMAND_* constant)</returns>
        public static int GetLastAssociateCommand(uint oAssociate = OBJECT_INVALID)
        {
            if (oAssociate == OBJECT_INVALID)
                oAssociate = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetLastAssociateCommand(oAssociate);
        }

        /// <summary>
        /// Gets the master of the associate.
        /// </summary>
        /// <param name="oAssociate">The associate to get the master for (default: OBJECT_SELF)</param>
        /// <returns>The master object</returns>
        public static uint GetMaster(uint oAssociate = OBJECT_INVALID)
        {
            if (oAssociate == OBJECT_INVALID)
                oAssociate = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetMaster(oAssociate);
        }
    }
}