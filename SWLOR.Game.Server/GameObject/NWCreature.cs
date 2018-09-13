using System;
using System.Linq;
using SWLOR.Game.Server.GameObject.Contracts;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.GameObject
{
    public class NWCreature : NWObject, INWCreature
    {
        private readonly INWNXCreature _nwnxCreature;

        public NWCreature(INWScript script,
            INWNXCreature creature,
            AppState state)
            : base(script, state)
        {
            _nwnxCreature = creature;
        }


        public new static NWCreature Wrap(Object @object)
        {
            NWCreature obj = (NWCreature)App.Resolve<INWCreature>();
            obj.Object = @object;

            return obj;
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

        public virtual int RawStrength
        {
            get => _nwnxCreature.GetRawAbilityScore(this, ABILITY_STRENGTH);
            set => _nwnxCreature.SetRawAbilityScore(this, ABILITY_STRENGTH, value);
        }

        public virtual int RawDexterity
        {
            get => _nwnxCreature.GetRawAbilityScore(this, ABILITY_DEXTERITY);
            set => _nwnxCreature.SetRawAbilityScore(this, ABILITY_DEXTERITY, value);
        }

        public virtual int RawConstitution
        {
            get => _nwnxCreature.GetRawAbilityScore(this, ABILITY_CONSTITUTION);
            set => _nwnxCreature.SetRawAbilityScore(this, ABILITY_CONSTITUTION, value);
        }

        public virtual int RawWisdom
        {
            get => _nwnxCreature.GetRawAbilityScore(this, ABILITY_WISDOM);
            set => _nwnxCreature.SetRawAbilityScore(this, ABILITY_WISDOM, value);
        }

        public virtual int RawIntelligence
        {
            get => _nwnxCreature.GetRawAbilityScore(this, ABILITY_INTELLIGENCE);
            set => _nwnxCreature.SetRawAbilityScore(this, ABILITY_INTELLIGENCE, value);
        }

        public virtual int RawCharisma
        {
            get => _nwnxCreature.GetRawAbilityScore(this, ABILITY_CHARISMA);
            set => _nwnxCreature.SetRawAbilityScore(this, ABILITY_CHARISMA, value);
        }

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

        public virtual NWItem Head => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_HEAD, Object));
        public virtual NWItem Chest => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_CHEST, Object));
        public virtual NWItem Boots => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_BOOTS, Object));
        public virtual NWItem Arms => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_ARMS, Object));
        public virtual NWItem RightHand => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_RIGHTHAND, Object));
        public virtual NWItem LeftHand => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_LEFTHAND, Object));
        public virtual NWItem Cloak => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_CLOAK, Object));
        public virtual NWItem LeftRing => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_LEFTRING, Object));
        public virtual NWItem RightRing => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_RIGHTRING, Object));
        public virtual NWItem Neck => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_NECK, Object));
        public virtual NWItem Belt => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_BELT, Object));
        public virtual NWItem Arrows => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_ARROWS, Object));
        public virtual NWItem Bullets => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_BULLETS, Object));
        public virtual NWItem Bolts => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_BOLTS, Object));
        public virtual NWItem CreatureWeaponLeft => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_CWEAPON_L, Object));
        public virtual NWItem CreatureWeaponRight => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_CWEAPON_R, Object));
        public virtual NWItem CreatureWeaponBite => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_CWEAPON_B, Object));
        public virtual NWItem CreatureHide => NWItem.Wrap(_.GetItemInSlot(INVENTORY_SLOT_CARMOUR, Object));

        public virtual void FloatingText(string text, bool displayToFaction = false)
        {
            _.FloatingTextStringOnCreature(text, Object, displayToFaction ? 1 : 0);
        }

        public virtual void SendMessage(string text)
        {
            _.SendMessageToPC(Object, text);
        }

        public virtual bool IsDead => _.GetIsDead(Object) == 1;


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


        public static implicit operator Object(NWCreature o)
        {
            return o.Object;
        }
        public static implicit operator NWCreature(Object o)
        {
            INWScript _ = App.Resolve<INWScript>();

            return (_.GetObjectType(o) == OBJECT_TYPE_CREATURE) ?
                Wrap(o) :
                throw new InvalidCastException();
        }
    }
}
