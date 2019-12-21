using System;
using System.Collections.Generic;

using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.ValueObject;
using static NWN._;

namespace SWLOR.Game.Server.GameObject
{
    public class NWObject
    {
        public virtual NWGameObject Object { get; }
        
        public NWObject(NWGameObject nwnObject)
        {
            Object = nwnObject;
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
            
            string guid = Guid.NewGuid().ToString();
            _.SetTag(Object, guid);
        }

        public virtual Guid GlobalID => GetOrAssignGlobalID();

        public virtual Guid GetOrAssignGlobalID()
        {
            if (Object == null || Object == NWGameObject.OBJECT_INVALID)
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
                    globalID = Guid.NewGuid().ToString();
                    _.SetLocalString(Object, "GLOBAL_ID", globalID);
                }
            }
            
            return Guid.Parse(globalID.ToUpper());
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

        public virtual NWLocation Location
        {
            get => _.GetLocation(Object);
            set
            {
                AssignCommand(() => _.JumpToLocation(value));
            }
        }

        public virtual NWArea Area => _.GetArea(Object);

        public virtual Vector Position => _.GetPosition(Object);

        public virtual bool HasInventory => _.GetHasInventory(Object);

        public virtual bool IsPlot
        {
            get => _.GetPlotFlag(Object);
            set => _.SetPlotFlag(Object, value);
        }

        public virtual float Facing
        {
            get => _.GetFacing(Object);
            set => AssignCommand(() => _.SetFacing(value));
        }

        public virtual int CurrentHP => _.GetCurrentHitPoints(Object);

        public virtual int MaxHP => _.GetMaxHitPoints(Object);

        public virtual bool IsValid => Object != null && _.GetIsObjectValid(Object);

        public virtual string IdentifiedDescription
        {
            get => _.GetDescription(Object);
            set => _.SetDescription(Object, value);
        }

        public virtual string UnidentifiedDescription
        {
            get => _.GetDescription(Object, false, false);
            set => _.SetDescription(Object, value, false);
        }

        public virtual int Gold
        {
            get => _.GetGold(Object);
            set
            {
                AssignCommand(() =>
                {
                    _.TakeGoldFromCreature(Gold, Object, true);

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

        public virtual bool GetLocalBoolean(string name)
        {
            return _.GetLocalBoolean(Object, name);
        }

        public virtual void SetLocalBoolean(string name, bool val)
        {
            _.SetLocalBoolean(Object, name, val);
        }

        public virtual void DeleteLocalBoolean(string name)
        {
            _.DeleteLocalBoolean(Object, name);
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
            NWItem item = _.GetFirstItemInInventory(Object);
            while (item.IsValid)
            {
                _.DestroyObject(item.Object);
                item = _.GetNextItemInInventory(Object);
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


        public virtual NWGameObject GetLocalObject(string name)
        {
            return _.GetLocalObject(Object, name);
        }

        public virtual void SetLocalObject(string name, NWGameObject value)
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

        public virtual void SpeakString(string message, TalkVolume talkVolume = TalkVolume.Talk)
        {
            _.AssignCommand(Object, () =>
            {
                _.SpeakString(message);
            });
        }

        public virtual void DelayEvent<T>(float seconds, T data)
        {
            _.DelayCommand(seconds, () =>
            {
                MessageHub.Instance.Publish(data);
            });
        }

        public virtual void DelayAssignCommand(ActionDelegate action, float seconds)
        {
            _.DelayCommand(seconds, () =>
            {
                AssignCommand(action);
            });
        }

        public virtual bool IsPC => _.GetIsPC(Object);

        public virtual bool IsPlayer => _.GetIsPC(Object) && !_.GetIsDM(Object) && !_.GetIsDMPossessed(Object);

        public virtual bool IsDM =>  _.GetIsDM(Object) || _.GetIsDMPossessed(Object);

        public virtual bool IsNPC => !IsPlayer && !IsDM && IsCreature;
        
        public virtual bool IsCreature => _.GetObjectType(Object) == ObjectType.Creature;

        public virtual IEnumerable<NWItem> InventoryItems
        {
            get
            {
                if (_.GetHasInventory(Object) == false)
                {
                    throw new Exception("Object does not have an inventory.");
                }
                
                for (NWItem item = _.GetFirstItemInInventory(Object); _.GetIsObjectValid(item) == true; item = _.GetNextItemInInventory(Object))
                {
                    yield return item;
                }
            }
        }

        public virtual IEnumerable<Effect> Effects
        {
            get
            {
                for (Effect effect = _.GetFirstEffect(Object); _.GetIsEffectValid(effect) == true; effect = _.GetNextEffect(Object))
                {
                    yield return effect;
                }
            }
        }

        public ObjectType ObjectType => _.GetObjectType(Object);

        public void RemoveEffect(EffectType effectTypeID)
        {
            Effect effect = _.GetFirstEffect(Object);
            while (_.GetIsEffectValid(effect))
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
                if (!AppCache.CustomObjectData.ContainsKey(GlobalID))
                {
                    AppCache.CustomObjectData.Add(GlobalID, new CustomData(this));
                }

                return AppCache.CustomObjectData[GlobalID];
            }
        }

        //
        // -- BELOW THIS POINT IS JUNK TO MAKE THE API FRIENDLIER!
        //

        public static bool operator ==(NWObject lhs, NWObject rhs)
        {
            bool lhsNull = lhs is null;
            bool rhsNull = rhs is null;
            return (lhsNull && rhsNull) || (!lhsNull && !rhsNull && lhs.Object == rhs.Object);
        }

        public static bool operator !=(NWObject lhs, NWObject rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object o)
        {
            NWObject other = o as NWObject;
            return other != null && other == this;
        }

        public override int GetHashCode()
        {
            return Object.GetHashCode();
        }

        public static implicit operator NWGameObject(NWObject o)
        {
            return o.Object;
        }

        public static implicit operator NWObject(NWGameObject o)
        {
            return new NWObject(o);
        }
    }
}
