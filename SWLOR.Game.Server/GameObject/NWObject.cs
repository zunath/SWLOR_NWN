using System;
using System.Collections.Generic;
using SWLOR.Game.Server.GameObject.Contracts;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.ValueObject;
using Object = SWLOR.Game.Server.NWN.NWScript.Object;

namespace SWLOR.Game.Server.GameObject
{
    public class NWObject : INWObject
    {
        public virtual Object Object { get; protected set; }
        protected readonly INWScript _;
        private readonly AppState _state;

        public NWObject(INWScript script,
            AppState state)
        {
            _ = script;
            _state = state;
        }

        public static NWObject Wrap(Object @object)
        {
            var obj = (NWObject)App.Resolve<INWObject>();
            obj.Object = @object;
            
            return obj;
        }

        public virtual bool IsInitializedAsPlayer
        {
            get
            {
                if (!IsPlayer) return false;

                string globalID = _.GetTag(Object);
                return !string.IsNullOrWhiteSpace(globalID);
            }
        }

        public virtual void InitializePlayer()
        {
            if (IsInitializedAsPlayer || !IsPlayer) return;
            
            string guid = Guid.NewGuid().ToString("N");
            _.SetTag(Object, guid);
        }

        public virtual string GlobalID
        {
            get
            {
                if (Object == null || Object == NWScript.OBJECT_TYPE_INVALID)
                    throw new Exception("NWN object has not been set for this wrapper.");

                string globalID;
                if (IsPlayer)
                {
                    if (!IsInitializedAsPlayer)
                    {
                        throw new Exception("Must call Initialize() before getting GlobalID");
                    }

                    globalID = _.GetTag(Object);
                }
                else
                {
                    globalID = _.GetLocalString(Object, "GLOBAL_ID");
                    if (string.IsNullOrWhiteSpace(globalID))
                    {
                        globalID = Guid.NewGuid().ToString("N");
                        _.SetLocalString(Object, "GLOBAL_ID", globalID);
                    }
                }

                return globalID;
            }
        }

        public virtual string Name
        {
            get => _.GetName(Object);
            set => _.SetName(Object, value);
        }

        public virtual string Tag
        {
            get => _.GetTag(Object);
            set => _.SetTag(Object, value);
        }

        public virtual string Resref => _.GetResRef(Object);

        public virtual Location Location
        {
            get => _.GetLocation(Object);
            set
            {
                AssignCommand(() => _.JumpToLocation(value));
            }
        }

        public virtual NWArea Area => NWArea.Wrap(_.GetArea(Object));

        public virtual Vector Position => _.GetPosition(Object);

        public virtual bool HasInventory => _.GetHasInventory(Object) == 1;

        public virtual bool IsPlot
        {
            get => _.GetPlotFlag(Object) == 1;
            set => _.SetPlotFlag(Object, value ? 1 : 0);
        }

        public virtual float Facing
        {
            get => _.GetFacing(Object);
            set => AssignCommand(() => _.SetFacing(value));
        }

        public virtual int CurrentHP => _.GetCurrentHitPoints(Object);

        public virtual int MaxHP => _.GetMaxHitPoints(Object);

        public virtual bool IsValid => Object != null && _.GetIsObjectValid(Object) == 1;

        public virtual string IdentifiedDescription
        {
            get => _.GetDescription(Object);
            set => _.SetDescription(Object, value);
        }

        public virtual string UnidentifiedDescription
        {
            get => _.GetDescription(Object, NWScript.FALSE, NWScript.FALSE);
            set => _.SetDescription(Object, value, NWScript.FALSE);
        }

        public virtual int Gold
        {
            get => _.GetGold(Object);
            set
            {
                AssignCommand(() =>
                {
                    _.TakeGoldFromCreature(Gold, Object, NWScript.TRUE);

                    if (value > 0)
                    {
                        _.GiveGoldToCreature(Object, value);
                    }
                });
            }
        }

        public virtual int GetLocalInt(string name)
        {
            return _.GetLocalInt(Object, name);
        }

        public virtual void SetLocalInt(string name, int value)
        {
            _.SetLocalInt(Object, name, value);
        }

        public virtual void DeleteLocalInt(string name)
        {
            _.DeleteLocalInt(Object, name);
        }


        public virtual string GetLocalString(string name)
        {
            return _.GetLocalString(Object, name);
        }

        public virtual void SetLocalString(string name, string value)
        {
            _.SetLocalString(Object, name, value);
        }

        public virtual void DeleteLocalString(string name)
        {
            _.DeleteLocalString(Object, name);
        }


        public virtual float GetLocalFloat(string name)
        {
            return _.GetLocalFloat(Object, name);
        }

        public virtual void SetLocalFloat(string name, float value)
        {
            _.SetLocalFloat(Object, name, value);
        }

        public virtual void DestroyAllInventoryItems()
        {
            NWItem item = NWItem.Wrap(_.GetFirstItemInInventory(Object));
            while (item.IsValid)
            {
                _.DestroyObject(item.Object);
                item = NWItem.Wrap(_.GetNextItemInInventory(Object));
            }
        }

        public virtual void DeleteLocalFloat(string name)
        {
            _.DeleteLocalFloat(Object, name);
        }


        public virtual Location GetLocalLocation(string name)
        {
            return _.GetLocalLocation(Object, name);
        }

        public virtual void SetLocalLocation(string name, Location value)
        {
            _.SetLocalLocation(Object, name, value);
        }

        public virtual void DeleteLocalLocation(string name)
        {
            _.DeleteLocalLocation(Object, name);
        }


        public virtual Object GetLocalObject(string name)
        {
            return _.GetLocalObject(Object, name);
        }

        public virtual void SetLocalObject(string name, Object value)
        {
            _.SetLocalObject(Object, name, value);
        }

        public virtual void DeleteLocalObject(string name)
        {
            _.DeleteLocalObject(Object, name);
        }

        public virtual void Destroy(float delay = 0.0f)
        {
            _.DestroyObject(Object, delay);
        }

        public virtual void AssignCommand(ActionDelegate action)
        {
            _.AssignCommand(Object, action);
        }

        public virtual void DelayCommand(ActionDelegate action, float seconds)
        {
            _.DelayCommand(seconds, action);
        }

        public virtual void DelayAssignCommand(ActionDelegate action, float seconds)
        {
            _.DelayCommand(seconds, () =>
            {
                AssignCommand(action);
            });
        }

        public virtual bool IsPlayer => _.GetIsPC(Object) == 1 && _.GetIsDM(Object) == 0 && _.GetIsDMPossessed(Object) == 0;

        public virtual bool IsDM => _.GetIsPC(Object) == 0 && (_.GetIsDM(Object) == 1 || _.GetIsDMPossessed(Object) == 1);

        public virtual bool IsNPC => !IsPlayer && !IsDM;

        public virtual List<NWItem> InventoryItems
        {
            get
            {
                if (_.GetHasInventory(Object) == NWScript.FALSE)
                {
                    throw new Exception("Object does not have an inventory.");
                }

                List<NWItem> items = new List<NWItem>();
                Object item = _.GetFirstItemInInventory(Object);
                while (_.GetIsObjectValid(item) == NWScript.TRUE)
                {
                    items.Add(NWItem.Wrap(item));

                    item = _.GetNextItemInInventory(Object);
                }

                return items;
            }
        }

        public virtual List<Effect> Effects
        {
            get
            {
                List<Effect> effects = new List<Effect>();
                Effect effect = _.GetFirstEffect(Object);
                while (_.GetIsEffectValid(effect) == NWScript.TRUE)
                {
                    effects.Add(effect);
                    effect = _.GetNextEffect(Object);
                }

                return effects;
            }
        }

        public int ObjectType => _.GetObjectType(Object);

        public void RemoveEffect(int effectTypeID)
        {
            Effect effect = _.GetFirstEffect(Object);
            while (_.GetIsEffectValid(effect) == NWScript.TRUE)
            {
                if (_.GetEffectType(effect) == effectTypeID)
                {
                    _.RemoveEffect(Object, effect);
                }

                effect = _.GetNextEffect(Object);
            }
        }

        public CustomData Data
        {
            get
            {
                if (!_state.CustomObjectData.ContainsKey(GlobalID))
                {
                    _state.CustomObjectData.Add(GlobalID, new CustomData(this));
                }

                return _state.CustomObjectData[GlobalID];
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is NWObject item)) return false;

            return item.Object == Object;
        }

        public override int GetHashCode()
        {
            return Object.GetHashCode();
        }
    }
}
