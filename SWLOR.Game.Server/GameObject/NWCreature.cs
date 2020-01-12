using System.Collections.Generic;
using System.Linq;

using NWN;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using static SWLOR.Game.Server.NWScript._;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.GameObject
{
    public class NWCreature : NWObject
    {
        public NWCreature(NWGameObject o)
            : base(o)
        {

        }

        public virtual int Age => _.GetAge(Object);

        public virtual float ChallengeRating => _.GetChallengeRating(Object);

        public virtual ClassType Class1 => _.GetClassByPosition(ClassPosition.First, Object);

        public virtual ClassType Class2 => _.GetClassByPosition(ClassPosition.Second, Object);

        public virtual ClassType Class3 => _.GetClassByPosition(ClassPosition.Third, Object);

        public virtual bool IsCommandable
        {
            get => _.GetCommandable(Object);
            set => _.SetCommandable(value, Object);
        }

        public virtual CreatureSize Size => _.GetCreatureSize(Object);

        public virtual PhenoType Phenotype
        {
            get => _.GetPhenoType(Object);
            set => _.SetPhenoType(value, Object);
        }

        public virtual string Deity
        {
            get => _.GetDeity(Object);
            set => _.SetDeity(Object, value);
        }

        public virtual RacialType RacialType => _.GetRacialType(Object);

        public virtual Gender Gender => _.GetGender(Object);

        public virtual bool IsResting => _.GetIsResting(Object);

        public virtual float Weight => _.GetWeight(Object) * 0.1f;

        public virtual int Strength => _.GetAbilityScore(Object, Ability.Strength);

        public virtual int Dexterity => _.GetAbilityScore(Object, Ability.Dexterity);

        public virtual int Constitution => _.GetAbilityScore(Object, Ability.Constitution);

        public virtual int Wisdom => _.GetAbilityScore(Object, Ability.Wisdom);
        public virtual int Intelligence => _.GetAbilityScore(Object, Ability.Intelligence);

        public virtual int Charisma => _.GetAbilityScore(Object, Ability.Charisma);
        
        public virtual int StrengthModifier => _.GetAbilityModifier(Ability.Strength, Object);
        public virtual int DexterityModifier => _.GetAbilityModifier(Ability.Dexterity, Object);
        public virtual int ConstitutionModifier => _.GetAbilityModifier(Ability.Constitution, Object);
        public virtual int WisdomModifier => _.GetAbilityModifier(Ability.Wisdom, Object);
        public virtual int IntelligenceModifier => _.GetAbilityModifier(Ability.Intelligence, Object);
        public virtual int CharismaModifier => _.GetAbilityModifier(Ability.Charisma, Object);

        public virtual int XP
        {
            get => _.GetXP(Object);
            set => _.SetXP(Object, value);
        }

        public bool IsInCombat => _.GetIsInCombat(Object);

        public virtual void ClearAllActions(bool clearCombatState = false)
        {
            AssignCommand(() =>
            {
                _.ClearAllActions(clearCombatState ? true : false);
            });
        }

        public virtual NWItem Head => _.GetItemInSlot(InventorySlot.Head, Object);
        public virtual NWItem Chest => _.GetItemInSlot(InventorySlot.Chest, Object);
        public virtual NWItem Boots => _.GetItemInSlot(InventorySlot.Boots, Object);
        public virtual NWItem Arms => _.GetItemInSlot(InventorySlot.Arms, Object);
        public virtual NWItem RightHand => _.GetItemInSlot(InventorySlot.RightHand, Object);
        public virtual NWItem LeftHand => _.GetItemInSlot(InventorySlot.LeftHand, Object);
        public virtual NWItem Cloak => _.GetItemInSlot(InventorySlot.Cloak, Object);
        public virtual NWItem LeftRing => _.GetItemInSlot(InventorySlot.LeftRing, Object);
        public virtual NWItem RightRing => _.GetItemInSlot(InventorySlot.RightRing, Object);
        public virtual NWItem Neck => _.GetItemInSlot(InventorySlot.Neck, Object);
        public virtual NWItem Belt => _.GetItemInSlot(InventorySlot.Belt, Object);
        public virtual NWItem Arrows => _.GetItemInSlot(InventorySlot.Arrows, Object);
        public virtual NWItem Bullets => _.GetItemInSlot(InventorySlot.Bullets, Object);
        public virtual NWItem Bolts => _.GetItemInSlot(InventorySlot.Bolts, Object);
        public virtual NWItem CreatureWeaponLeft => _.GetItemInSlot(InventorySlot.CreatureWeaponLeft, Object);
        public virtual NWItem CreatureWeaponRight => _.GetItemInSlot(InventorySlot.CreatureWeaponRight, Object);
        public virtual NWItem CreatureWeaponBite => _.GetItemInSlot(InventorySlot.CreatureWeaponBite, Object);
        public virtual NWItem CreatureHide => _.GetItemInSlot(InventorySlot.CreatureSkin, Object);

        public virtual void FloatingText(string text, bool displayToFaction = false)
        {
            _.FloatingTextStringOnCreature(text, Object, displayToFaction);
        }

        public virtual void SendMessage(string text)
        {
            _.SendMessageToPC(Object, text);
        }

        public virtual bool IsDead => _.GetIsDead(Object);

        public virtual bool IsPossessedFamiliar => _.GetIsPossessedFamiliar(Object) == true;

        public virtual bool IsDMPossessed => _.GetIsDMPossessed(Object) == true;

        public bool HasAnyEffect(params EffectType[] effectIDs)
        {
            Effect eff = _.GetFirstEffect(Object);
            while (_.GetIsEffectValid(eff) == true)
            {
                if (effectIDs.Contains(_.GetEffectType(eff)))
                {
                    return true;
                }

                eff = _.GetNextEffect(Object);
            }

            return false;
        }


        public virtual IEnumerable<NWItem> EquippedItems
        {
            get
            {
                for (int slot = 0; slot < NWNConstants.NumberOfInventorySlots; slot++)
                {
                    yield return _.GetItemInSlot((InventorySlot)slot, Object);
                }
            }
        }

        public virtual IEnumerable<NWCreature> PartyMembers
        {
            get
            {
                for (NWPlayer member = _.GetFirstFactionMember(Object, false); member.IsValid; member = _.GetNextFactionMember(Object, false))
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

        public static implicit operator NWGameObject(NWCreature o)
        {
            return o.Object;
        }
        public static implicit operator NWCreature(NWGameObject o)
        {
            return new NWCreature(o);
        }
    }
}
