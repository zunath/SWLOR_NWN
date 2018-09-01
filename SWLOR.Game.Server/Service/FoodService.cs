using System;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class FoodService: IFoodService
    {
        private readonly INWScript _;
        private readonly IRandomService _random;
        private readonly IColorTokenService _color;
        private readonly ICustomEffectService _customEffect;
        private readonly IDataContext _db;
        private readonly IMenuService _menu;

        public FoodService(INWScript script,
            IRandomService random,
            IColorTokenService color,
            ICustomEffectService customEffect,
            IDataContext db,
            IMenuService menu)
        {
            _ = script;
            _random = random;
            _color = color;
            _customEffect = customEffect;
            _db = db;
            _menu = menu;
        }

        public PlayerCharacter RunHungerCycle(NWPlayer pc, PlayerCharacter entity)
        {
            string sAreaTag = pc.Area.Tag; 
            if (sAreaTag == "ooc_area" || sAreaTag == "death_realm") return entity;
            int hungerTick = entity.CurrentHungerTick - 1;

            if (hungerTick <= 0 && entity.CurrentHunger >= 0)
            {
                hungerTick = 300 + _random.Random(300); // 5 minutes + random amount of time (up to +5 minutes)
                entity.CurrentHunger--;
                entity = ApplyHungerPenalties(entity, pc);
            }
            entity.CurrentHungerTick = hungerTick;
            return entity;
        }

        public PlayerCharacter ApplyHungerPenalties(PlayerCharacter entity, NWPlayer pc)
        {
            int penalty = 0;

            if (entity.CurrentHunger >= 40 && entity.CurrentHunger <= 50)
            {
                pc.FloatingText("You are starving! You should eat soon.");
            }
            else if (entity.CurrentHunger >= 30 && entity.CurrentHunger < 40)
            {
                penalty = 1;
                pc.FloatingText("You are starving! You are suffering from starvation penalties.");
            }
            else if (entity.CurrentHunger >= 20 && entity.CurrentHunger < 30)
            {
                penalty = 2;
                pc.FloatingText("You are starving! You are suffering from starvation penalties.");
            }
            else if (entity.CurrentHunger >= 10 && entity.CurrentHunger < 20)
            {
                penalty = 3;
                pc.FloatingText("You are starving! You are suffering from starvation penalties.");
            }
            else if (entity.CurrentHunger < 10)
            {
                penalty = 4;
                pc.FloatingText(_color.Red("You are starving! You are about to starve to death!"));
            }

            var effects = pc.Effects;
            foreach (Effect effect in effects)
            {
                if ( _.GetEffectTag(effect) == "EFFECT_HUNGER_PENALTIES")
                {
                    _.RemoveEffect(pc.Object, effect);
                }
            }

            if (penalty > 0)
            {
                Effect effect = _.EffectAbilityDecrease(NWScript.ABILITY_STRENGTH, penalty);
                effect = _.EffectLinkEffects(effect, _.EffectAbilityDecrease(NWScript.ABILITY_DEXTERITY, penalty));
                effect = _.EffectLinkEffects(effect, _.EffectAbilityDecrease(NWScript.ABILITY_CONSTITUTION, penalty));

                effect = _.TagEffect(effect, "EFFECT_HUNGER_PENALTIES");
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_PERMANENT, effect, pc.Object);
            }

            if (entity.CurrentHunger <= 0)
            {
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectDeath(), pc.Object);
                entity.CurrentHunger = 20;
                pc.FloatingText("You starved to death!");
            }

            return entity;
        }

        public void IncreaseHungerLevel(NWPlayer oPC, int amount, bool isTainted)
        {
            if (!oPC.IsPlayer) return;

            PlayerCharacter entity = _db.PlayerCharacters.Single(x => x.PlayerID == oPC.GlobalID);
            entity.CurrentHunger += amount;
            if (entity.CurrentHunger > entity.MaxHunger) entity.CurrentHunger = entity.MaxHunger;

            oPC.AssignCommand(() =>
            {
                _.ActionPlayAnimation(NWScript.ANIMATION_FIREFORGET_SALUTE);
            });

            oPC.SendMessage("Nourishment: " + _menu.BuildBar(entity.CurrentHunger, entity.MaxHunger, 100));
            _db.SaveChanges();

            if (isTainted)
            {
                if (_random.Random(100) + 1 <= 40)
                {
                    int ticks = 600 + _random.Random(300);

                    _customEffect.ApplyCustomEffect(oPC, oPC, CustomEffectType.FoodDisease, ticks, 0);
                }
            }
        }

        public PlayerCharacter DecreaseHungerLevel(PlayerCharacter entity, NWPlayer oPC, int amount)
        {
            amount = Math.Abs(amount);
            entity.CurrentHunger -= amount;
            entity = ApplyHungerPenalties(entity, oPC);

            return entity;
        }

        public void DecreaseHungerLevel(NWPlayer oPC, int amount)
        {
            amount = Math.Abs(amount);
            PlayerCharacter entity = _db.PlayerCharacters.Single(x => x.PlayerID == oPC.GlobalID);
            entity.CurrentHunger -= amount;
            ApplyHungerPenalties(entity, oPC);

            _db.SaveChanges();
        }
    }
}
