using System;
using System.Collections.Generic;

using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.GameObject
{
    public class NWObject
    {
        public virtual uint Object { get; }
        
        public NWObject(uint nwnObject)
        {
            Object = nwnObject;
        }

        public virtual bool IsInitializedAsPlayer
        {
            get
            {
                if (!IsPlayer) return false;

                string globalID = GetTag(Object);
                return !string.IsNullOrWhiteSpace(globalID);
            }
        }

        public virtual void InitializePlayer()
        {
            if (IsInitializedAsPlayer || !IsPlayer) return;
            
            string guid = Guid.NewGuid().ToString();
            SetTag(Object, guid);
        }

        public virtual Guid GlobalID => GetOrAssignGlobalID();

        public virtual Guid GetOrAssignGlobalID()
        {
            string globalID;
            if (IsPlayer)
            {
                if (!IsInitializedAsPlayer)
                {
                    throw new Exception("Must call Initialize() before getting GlobalID");
                }

                globalID = GetTag(Object);
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
            get => GetName(Object);
            set => SetName(Object, value);
        }

        public virtual string Tag
        {
            get => GetTag(Object);
            set => SetTag(Object, value);
        }

        public virtual string Resref => GetResRef(Object);

        public virtual NWLocation Location
        {
            get => GetLocation(Object);
            set
            {
                AssignCommand(() => JumpToLocation(value));
            }
        }

        public virtual NWArea Area => GetArea(Object);

        public virtual Vector Position => GetPosition(Object);

        public virtual bool HasInventory => GetHasInventory(Object);

        public virtual bool IsPlot
        {
            get => GetPlotFlag(Object);
            set => _.SetPlotFlag(Object, value);
        }

        public virtual float Facing
        {
            get => GetFacing(Object);
            set => AssignCommand(() => SetFacing(value));
        }

        public virtual int CurrentHP => GetCurrentHitPoints(Object);

        public virtual int MaxHP => GetMaxHitPoints(Object);

        public virtual bool IsValid => GetIsObjectValid(Object);

        public virtual string IdentifiedDescription
        {
            get => GetDescription(Object);
            set => SetDescription(Object, value);
        }

        public virtual string UnidentifiedDescription
        {
            get => GetDescription(Object, false, false);
            set => SetDescription(Object, value, false);
        }

        public virtual int Gold
        {
            get => GetGold(Object);
            set
            {
                AssignCommand(() =>
                {
                    TakeGoldFromCreature(Gold, Object, true);

                    if (value > 0)
                    {
                        GiveGoldToCreature(Object, value);
                    }
                });
            }
        }

        public virtual int GetLocalInt(string name)
        {
            return _.GetLocalInt(Object, name);
        }

        public virtual bool GetLocalBool(string name)
        {
            return _.GetLocalBool(Object, name);
        }

        public virtual void SetLocalInt(string name, int value)
        {
            _.SetLocalInt(Object, name, value);
        }

        public virtual void SetLocalBool(string name, bool value)
        {
            _.SetLocalBool(Object, name, value);
        }

        public virtual void DeleteLocalInt(string name)
        {
            _.DeleteLocalInt(Object, name);
        }

        public virtual void DeleteLocalBool(string name)
        {
            _.DeleteLocalBool(Object, name);
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
            NWItem item = GetFirstItemInInventory(Object);
            while (item.IsValid)
            {
                DestroyObject(item.Object);
                item = GetNextItemInInventory(Object);
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


        public virtual uint GetLocalObject(string name)
        {
            return _.GetLocalObject(Object, name);
        }

        public virtual void SetLocalObject(string name, uint value)
        {
            _.SetLocalObject(Object, name, value);
        }

        public virtual void DeleteLocalObject(string name)
        {
            _.DeleteLocalObject(Object, name);
        }

        public virtual void Destroy(float delay = 0.0f)
        {
            DestroyObject(Object, delay);
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
            where T: class
        {
            DelayCommand(seconds, () =>
            {
                MessageHub.Instance.Publish(data);
            });
        }

        public virtual void DelayAssignCommand(ActionDelegate action, float seconds)
        {
            DelayCommand(seconds, () =>
            {
                AssignCommand(action);
            });
        }

        public virtual bool IsPC => GetIsPC(Object);

        public virtual bool IsPlayer => GetIsPC(Object) && !GetIsDM(Object) && !GetIsDMPossessed(Object);

        public virtual bool IsDM =>  GetIsDM(Object) || GetIsDMPossessed(Object);

        public virtual bool IsNPC => !IsPlayer && !IsDM && IsCreature;
        
        public virtual bool IsCreature => GetObjectType(Object) == ObjectType.Creature;

        public virtual IEnumerable<NWItem> InventoryItems
        {
            get
            {
                if (GetHasInventory(Object) == false)
                {
                    throw new Exception("Object does not have an inventory.");
                }
                
                for (NWItem item = GetFirstItemInInventory(Object); GetIsObjectValid(item); item = GetNextItemInInventory(Object))
                {
                    yield return item;
                }
            }
        }

        public virtual IEnumerable<Effect> Effects
        {
            get
            {
                for (Effect effect = GetFirstEffect(Object); GetIsEffectValid(effect); effect = GetNextEffect(Object))
                {
                    yield return effect;
                }
            }
        }

        public ObjectType ObjectType => GetObjectType(Object);

        public void RemoveEffect(EffectTypeScript effectTypeID)
        {
            Effect effect = GetFirstEffect(Object);
            while (GetIsEffectValid(effect))
            {
                if (GetEffectType(effect) == effectTypeID)
                {
                    _.RemoveEffect(Object, effect);
                }

                effect = GetNextEffect(Object);
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

        public static implicit operator uint(NWObject o)
        {
            return o.Object;
        }

        public static implicit operator NWObject(uint o)
        {
            return new NWObject(o);
        }
    }
}
