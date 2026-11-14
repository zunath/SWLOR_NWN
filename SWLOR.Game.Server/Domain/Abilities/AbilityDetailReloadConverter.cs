using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// Writes a reference to an ability definition held in carried state (the ability that granted a status effect) as the feat
    /// it belongs to, and reads it back as the reloaded build's definition of that feat, which its rerun ability cache built
    /// (<see cref="Core.ReloadStateCarrier"/>). A definition the ability cache does not hold (built on the fly) is written as null,
    /// as is a feat the reloaded build no longer has: the state keeps everything else.
    /// </summary>
    public sealed class AbilityDetailReloadConverter : JsonConverter
    {
        private readonly Func<IReadOnlyDictionary<FeatType, AbilityDetail>> _abilities;
        private Dictionary<AbilityDetail, FeatType> _featsByAbility;

        /// <param name="abilities">The build's ability definitions by feat (<see cref="IAbilityService.GetAllAbilityDetails"/>).</param>
        public AbilityDetailReloadConverter(Func<IReadOnlyDictionary<FeatType, AbilityDetail>> abilities)
        {
            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(AbilityDetail);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (_featsByAbility == null)
            {
                _featsByAbility = new Dictionary<AbilityDetail, FeatType>(ReferenceEqualityComparer.Instance);
                foreach (var (abilityFeat, detail) in _abilities())
                {
                    if (detail != null)
                        _featsByAbility.TryAdd(detail, abilityFeat);
                }
            }

            if (value is AbilityDetail ability && _featsByAbility.TryGetValue(ability, out var feat))
                writer.WriteValue(feat.ToString());
            else
                writer.WriteNull();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String || !Enum.TryParse<FeatType>((string)reader.Value, out var feat))
                return null;

            return _abilities().TryGetValue(feat, out var ability) ? ability : null;
        }
    }
}
