using System.Collections.Generic;
using System.Linq;

using NWN;
using static NWN._;

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

        public virtual int Class1 => _.GetClassByPosition(1, Object);

        public virtual int Class2 => _.GetClassByPosition(2, Object);

        public virtual int Class3 => _.GetClassByPosition(3, Object);

        public virtual bool IsCommandable
        {
            get => _.GetCommandable(Object) == 1;
            set => _.SetCommandable(value ? 1 : 0, Object);
        }

        public virtual int Size => _.GetCreatureSize(Object);

        public virtual int Phenotype
        {
            get => _.GetPhenoType(Object);
            set => _.SetPhenoType(value, Object);
        }

        public virtual string Deity
        {
            get => _.GetDeity(Object);
            set => _.SetDeity(Object, value);
        }

        public virtual int RacialType => _.GetRacialType(Object);

        public virtual int Gender => _.GetGender(Object);

        public virtual bool IsResting => _.GetIsResting(Object) == 1;

        public virtual float Weight => _.GetWeight(Object) * 0.1f;

        public virtual int Strength => _.GetAbilityScore(Object, ABILITY_STRENGTH);

        public virtual int Dexterity => _.GetAbilityScore(Object, ABILITY_DEXTERITY);

        public virtual int Constitution => _.GetAbilityScore(Object, ABILITY_CONSTITUTION);

        public virtual int Wisdom => _.GetAbilityScore(Object, ABILITY_WISDOM);
        public virtual int Intelligence => _.GetAbilityScore(Object, ABILITY_INTELLIGENCE);

        public virtual int Charisma => _.GetAbilityScore(Object, ABILITY_CHARISMA);
        
        public virtual int StrengthModifier => _.GetAbilityModifier(ABILITY_STRENGTH, Object);
        public virtual int DexterityModifier => _.GetAbilityModifier(ABILITY_DEXTERITY, Object);
        public virtual int ConstitutionModifier => _.GetAbilityModifier(ABILITY_CONSTITUTION, Object);
        public virtual int WisdomModifier => _.GetAbilityModifier(ABILITY_WISDOM, Object);
        public virtual int IntelligenceModifier => _.GetAbilityModifier(ABILITY_INTELLIGENCE, Object);
        public virtual int CharismaModifier => _.GetAbilityModifier(ABILITY_CHARISMA, Object);

        public virtual int XP
        {
            get => _.GetXP(Object);
            set => _.SetXP(Object, value);
        }

        public bool IsInCombat => _.GetIsInCombat(Object) == 1;

        public virtual void ClearAllActions(bool clearCombatState = false)
        {
            AssignCommand(() =>
            {
                _.ClearAllActions(clearCombatState ? TRUE : FALSE);
            });
        }

        public virtual NWItem Head => _.GetItemInSlot(INVENTORY_SLOT_HEAD, Object);
        public virtual NWItem Chest => _.GetItemInSlot(INVENTORY_SLOT_CHEST, Object);
        public virtual NWItem Boots => _.GetItemInSlot(INVENTORY_SLOT_BOOTS, Object);
        public virtual NWItem Arms => _.GetItemInSlot(INVENTORY_SLOT_ARMS, Object);
        public virtual NWItem RightHand => _.GetItemInSlot(INVENTORY_SLOT_RIGHTHAND, Object);
        public virtual NWItem LeftHand => _.GetItemInSlot(INVENTORY_SLOT_LEFTHAND, Object);
        public virtual NWItem Cloak => _.GetItemInSlot(INVENTORY_SLOT_CLOAK, Object);
        public virtual NWItem LeftRing => _.GetItemInSlot(INVENTORY_SLOT_LEFTRING, Object);
        public virtual NWItem RightRing => _.GetItemInSlot(INVENTORY_SLOT_RIGHTRING, Object);
        public virtual NWItem Neck => _.GetItemInSlot(INVENTORY_SLOT_NECK, Object);
        public virtual NWItem Belt => _.GetItemInSlot(INVENTORY_SLOT_BELT, Object);
        public virtual NWItem Arrows => _.GetItemInSlot(INVENTORY_SLOT_ARROWS, Object);
        public virtual NWItem Bullets => _.GetItemInSlot(INVENTORY_SLOT_BULLETS, Object);
        public virtual NWItem Bolts => _.GetItemInSlot(INVENTORY_SLOT_BOLTS, Object);
        public virtual NWItem CreatureWeaponLeft => _.GetItemInSlot(INVENTORY_SLOT_CWEAPON_L, Object);
        public virtual NWItem CreatureWeaponRight => _.GetItemInSlot(INVENTORY_SLOT_CWEAPON_R, Object);
        public virtual NWItem CreatureWeaponBite => _.GetItemInSlot(INVENTORY_SLOT_CWEAPON_B, Object);
        public virtual NWItem CreatureHide => _.GetItemInSlot(INVENTORY_SLOT_CARMOUR, Object);

        public virtual void FloatingText(string text, bool displayToFaction = false)
        {
            _.FloatingTextStringOnCreature(text, Object, displayToFaction ? 1 : 0);
        }

        public virtual void SendMessage(string text)
        {
            _.SendMessageToPC(Object, text);
        }

        public virtual bool IsDead => _.GetIsDead(Object) == 1;

        public virtual bool IsPossessedFamiliar => _.GetIsPossessedFamiliar(Object) == TRUE;

        public virtual bool IsDMPossessed => _.GetIsDMPossessed(Object) == TRUE;

        public bool HasAnyEffect(params int[] effectIDs)
        {
            Effect eff = _.GetFirstEffect(Object);
            while (_.GetIsEffectValid(eff) == TRUE)
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
                for (int slot = 0; slot < NUM_INVENTORY_SLOTS; slot++)
                {
                    yield return _.GetItemInSlot(slot, Object);
                }
            }
        }

        public virtual IEnumerable<NWCreature> PartyMembers
        {
            get
            {
                for (NWPlayer member = _.GetFirstFactionMember(Object, FALSE); member.IsValid; member = _.GetNextFactionMember(Object, FALSE))
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
