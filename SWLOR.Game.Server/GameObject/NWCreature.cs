using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.NWN;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.GameObject
{
    public class NWCreature : NWObject
    {
        public NWCreature(uint o)
            : base(o)
        {

        }

        public virtual int Age => GetAge(Object);

        public virtual float ChallengeRating => GetChallengeRating(Object);

        public virtual ClassType Class1 => GetClassByPosition(1, Object);

        public virtual ClassType Class2 => GetClassByPosition(2, Object);

        public virtual ClassType Class3 => GetClassByPosition(3, Object);

        public virtual bool IsCommandable
        {
            get => GetCommandable(Object);
            set => SetCommandable(value, Object);
        }

        public virtual CreatureSize Size => GetCreatureSize(Object);

        public virtual PhenoType Phenotype
        {
            get => GetPhenoType(Object);
            set => SetPhenoType(value, Object);
        }

        public virtual string Deity
        {
            get => GetDeity(Object);
            set => SetDeity(Object, value);
        }

        public virtual RacialType RacialType => GetRacialType(Object);

        public virtual Gender Gender => GetGender(Object);

        public virtual bool IsResting => GetIsResting(Object);

        public virtual float Weight => GetWeight(Object) * 0.1f;

        public virtual int Strength => GetAbilityScore(Object, AbilityType.Strength);

        public virtual int Dexterity => GetAbilityScore(Object, AbilityType.Dexterity);

        public virtual int Constitution => GetAbilityScore(Object, AbilityType.Constitution);

        public virtual int Wisdom => GetAbilityScore(Object, AbilityType.Wisdom);
        public virtual int Intelligence => GetAbilityScore(Object, AbilityType.Intelligence);

        public virtual int Charisma => GetAbilityScore(Object, AbilityType.Charisma);
        
        public virtual int StrengthModifier => GetAbilityModifier(AbilityType.Strength, Object);
        public virtual int DexterityModifier => GetAbilityModifier(AbilityType.Dexterity, Object);
        public virtual int ConstitutionModifier => GetAbilityModifier(AbilityType.Constitution, Object);
        public virtual int WisdomModifier => GetAbilityModifier(AbilityType.Wisdom, Object);
        public virtual int IntelligenceModifier => GetAbilityModifier(AbilityType.Intelligence, Object);
        public virtual int CharismaModifier => GetAbilityModifier(AbilityType.Charisma, Object);

        public virtual int XP
        {
            get => GetXP(Object);
            set => SetXP(Object, value);
        }

        public bool IsInCombat => GetIsInCombat(Object);

        public virtual void ClearAllActions(bool clearCombatState = false)
        {
            AssignCommand(() =>
            {
                NWScript.ClearAllActions(clearCombatState ? true : false);
            });
        }

        public virtual NWItem Head => GetItemInSlot(InventorySlot.Head, Object);
        public virtual NWItem Chest => GetItemInSlot(InventorySlot.Chest, Object);
        public virtual NWItem Boots => GetItemInSlot(InventorySlot.Boots, Object);
        public virtual NWItem Arms => GetItemInSlot(InventorySlot.Arms, Object);
        public virtual NWItem RightHand => GetItemInSlot(InventorySlot.RightHand, Object);
        public virtual NWItem LeftHand => GetItemInSlot(InventorySlot.LeftHand, Object);
        public virtual NWItem Cloak => GetItemInSlot(InventorySlot.Cloak, Object);
        public virtual NWItem LeftRing => GetItemInSlot(InventorySlot.LeftRing, Object);
        public virtual NWItem RightRing => GetItemInSlot(InventorySlot.RightRing, Object);
        public virtual NWItem Neck => GetItemInSlot(InventorySlot.Neck, Object);
        public virtual NWItem Belt => GetItemInSlot(InventorySlot.Belt, Object);
        public virtual NWItem Arrows => GetItemInSlot(InventorySlot.Arrows, Object);
        public virtual NWItem Bullets => GetItemInSlot(InventorySlot.Bullets, Object);
        public virtual NWItem Bolts => GetItemInSlot(InventorySlot.Bolts, Object);
        public virtual NWItem CreatureWeaponLeft => GetItemInSlot(InventorySlot.CreatureLeft, Object);
        public virtual NWItem CreatureWeaponRight => GetItemInSlot(InventorySlot.CreatureRight, Object);
        public virtual NWItem CreatureWeaponBite => GetItemInSlot(InventorySlot.CreatureBite, Object);
        public virtual NWItem CreatureHide => GetItemInSlot(InventorySlot.CreatureArmor, Object);

        public virtual void FloatingText(string text, bool displayToFaction = false)
        {
            FloatingTextStringOnCreature(text, Object, displayToFaction);
        }

        public virtual void SendMessage(string text)
        {
            SendMessageToPC(Object, text);
        }

        public virtual bool IsDead => GetIsDead(Object);

        public virtual bool IsPossessedFamiliar => GetIsPossessedFamiliar(Object);

        public virtual bool IsDMPossessed => GetIsDMPossessed(Object);

        public bool HasAnyEffect(params EffectTypeScript[] effectIDs)
        {
            var eff = GetFirstEffect(Object);
            while (GetIsEffectValid(eff) == true)
            {
                if (effectIDs.Contains(GetEffectType(eff)))
                {
                    return true;
                }

                eff = GetNextEffect(Object);
            }

            return false;
        }


        public virtual IEnumerable<NWItem> EquippedItems
        {
            get
            {
                for (int slot = 0; slot < NumberOfInventorySlots; slot++)
                {
                    yield return GetItemInSlot((InventorySlot)slot, Object);
                }
            }
        }

        public virtual IEnumerable<NWCreature> PartyMembers
        {
            get
            {
                for (NWPlayer member = GetFirstFactionMember(Object, false); member.IsValid; member = GetNextFactionMember(Object, false))
                {
                    yield return member;
                }
            }
        }

        public virtual bool IsBusy
        {
            get => GetLocalInt("IS_BUSY") == 1;
            set => SetLocalInt("IS_BUSY", value ? 1 : 0);
        }

        //
        // -- BELOW THIS POINT IS JUNK TO MAKE THE API FRIENDLIER!
        //

        public static bool operator ==(NWCreature lhs, NWCreature rhs)
        {
            bool lhsNull = lhs is null;
            bool rhsNull = rhs is null;
            return (lhsNull && rhsNull) || (!lhsNull && !rhsNull && lhs.Object == rhs.Object);
        }

        public static bool operator !=(NWCreature lhs, NWCreature rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object o)
        {
            NWCreature other = o as NWCreature;
            return other != null && other == this;
        }

        public override int GetHashCode()
        {
            return Object.GetHashCode();
        }

        public static implicit operator uint(NWCreature o)
        {
            return o.Object;
        }
        public static implicit operator NWCreature(uint o)
        {
            return new NWCreature(o);
        }
    }
}
