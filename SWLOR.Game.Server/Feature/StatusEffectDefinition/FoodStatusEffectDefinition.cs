using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition.StatusEffectData;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class FoodStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            Food();
            PetFood();
            
            return _builder.Build();
        }

        private void Food()
        {
            _builder.Create(StatusEffectType.Food)
                .Name("Food")
                .EffectIcon(EffectIconType.Food)
                .GrantAction((source, target, length, data) =>
                {
                    if (data == null)
                        return;

                    var foodEffect = (FoodEffectData)data;

                    if (foodEffect.HP > 0)
                    {
                        var playerId = GetObjectUUID(target);
                        var dbPlayer = DB.Get<Player>(playerId);

                        dbPlayer.TemporaryFoodHP = foodEffect.HP;

                        Stat.AdjustPlayerMaxHP(dbPlayer, target, foodEffect.HP);
                        DB.Set(dbPlayer);
                    }
                })
                .RemoveAction((target, data) =>
                {
                    if (data == null)
                        return;

                    var foodEffect = (FoodEffectData)data;

                    if (foodEffect.HP > 0)
                    {
                        var playerId = GetObjectUUID(target);
                        var dbPlayer = DB.Get<Player>(playerId);

                        dbPlayer.TemporaryFoodHP = 0;

                        Stat.AdjustPlayerMaxHP(dbPlayer, target, -foodEffect.HP);
                        DB.Set(dbPlayer);
                    }
                });
        }

        private void PetFood()
        {
            _builder.Create(StatusEffectType.PetFood)
                .Name("Pet Food")
                .EffectIcon(EffectIconType.Food);
        }
    }
}
