using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Entities;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Combat.ValueObjects;
using SWLOR.Shared.Domain.Crafting.ValueObjects;

namespace SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition
{
    public class FoodStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly IDatabaseService _db;
        private readonly IStatService _statService;
        private readonly StatusEffectBuilder _builder = new();

        public FoodStatusEffectDefinition(IDatabaseService db, IStatService statService)
        {
            _db = db;
            _statService = statService;
        }

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
                        var dbPlayer = _db.Get<Player>(playerId);

                        dbPlayer.TemporaryFoodHP = foodEffect.HP;

                        _statService.AdjustPlayerMaxHP(dbPlayer, target, foodEffect.HP);
                        _db.Set(dbPlayer);
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
                        var dbPlayer = _db.Get<Player>(playerId);

                        dbPlayer.TemporaryFoodHP = 0;

                        _statService.AdjustPlayerMaxHP(dbPlayer, target, -foodEffect.HP);
                        _db.Set(dbPlayer);
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
