using System.Reflection;
using System.Reflection.Emit;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Data
{
    /// <summary>One source weapon impact: its declared power, primary statuses and profile riders.</summary>
    internal sealed class WeaponImpact
    {
        public int BaseDamage { get; init; }
        public int Duration { get; init; }
        public IReadOnlyList<Type> Statuses { get; init; } = Array.Empty<Type>();
        public Func<IStatusEffect> StatusFactory { get; init; }
        public SkillType Skill { get; init; }
        public WeaponProfileReader Profile { get; init; } = new(null);
        public IReadOnlyList<string> UnmodeledRiders { get; init; } = Array.Empty<string>();
        public bool RestoresSecondaryTargetStamina { get; init; }
    }

    /// <summary>Reads a WeaponActiveAbilityDefinitionBase.WeaponAbilityProfile captured by a source impact.</summary>
    internal sealed class WeaponProfileReader
    {
        // Profile riders the weapon simulation models. Any other non-default rider refuses the ability.
        private static readonly HashSet<string> Modeled = new(StringComparer.Ordinal)
        {
            "ClearTargetActionsOnHit", "MaximumAreaTargets", "ExtraDamageIfTargetControlled",
            "RequiredTargetStatusEffectForConditionalStatus", "ConvertsRequiredTargetStatusEffect",
            "RequiredTargetStatusCategoryForConditionalStatus", "ConditionalTargetStatusEffect", "ConditionalTargetStatusDurationSeconds"
        };
        private readonly object _profile;
        public WeaponProfileReader(object profile) => _profile = profile;

        public T Read<T>(string name) => _profile == null ? default : (T)(_profile.GetType().GetProperty(name)
            ?? throw new InvalidDataException($"Weapon ability profile has no {name}.")).GetValue(_profile);

        public IEnumerable<string> UnmodeledRiders()
        {
            if (_profile == null) yield break;
            var defaults = Activator.CreateInstance(_profile.GetType(), true);
            foreach (var property in _profile.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead && p.GetIndexParameters().Length == 0))
                if (!Modeled.Contains(property.Name) && !Equals(property.GetValue(_profile), property.GetValue(defaults)))
                    yield return property.Name;
        }

        public ConditionalTargetStatus ReadConditionalTargetStatus()
        {
            var status = Read<Type>("ConditionalTargetStatusEffect");
            var duration = Read<int>("ConditionalTargetStatusDurationSeconds");
            if (status == null || duration <= 0) return null;
            return new ConditionalTargetStatus
            {
                Status = status, DurationSeconds = duration,
                RequiredStatus = Read<Type>("RequiredTargetStatusEffectForConditionalStatus"),
                RequiredCategory = Read<StatusEffectCategory>("RequiredTargetStatusCategoryForConditionalStatus"),
                ConvertsRequiredStatus = Read<bool>("ConvertsRequiredTargetStatusEffect")
            };
        }
    }

    /// <summary>
    /// Reads the source values of the weapon impacts the weapon simulation models. Closure captures come from the shared
    /// weapon helpers; a static impact method that only forwards literals to Ability.ApplyCombatImpact (Rending Strike)
    /// is read from its IL. Anything else is left unmodeled, never filled with copied numbers.
    /// </summary>
    internal static class WeaponImpactReader
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly string[] SharedHelpers = { "<ConfigureWeaponAbility>", "<ConfigureWeapon>", "<ConfigureSavageCleave>" };

        public static WeaponImpact Read(FeatType feat, AbilityDetail detail)
        {
            if (!detail.IsHostileAbility || detail.MeleeTechnique != null || detail.ImpactAction == null) return null;
            var whirl = feat is FeatType.SpinningWhirl1 or FeatType.SpinningWhirl2 or FeatType.SpinningWhirl3;
            var closure = detail.ImpactAction.Target;
            if (closure == null)
                return detail.SkillType is SkillType.Invalid or SkillType.Vibroblade ? ReadDirectCombatImpact(detail.ImpactAction.Method) : null;
            if (!whirl && detail.SkillType is not (SkillType.Katar or SkillType.Staff or SkillType.Vibroblade)) return null;
            if (!SharedHelpers.Any(detail.ImpactAction.Method.Name.StartsWith) || Capture(closure, "baseDamage") is not int baseDamage) return null;

            var statuses = new List<Type>();
            if (Capture(closure, "statusEffect") is Type status) statuses.Add(status);
            if (Capture(closure, "additionalStatusEffects") is IEnumerable<Type> additional) statuses.AddRange(additional.Where(t => t != null));
            if (Capture(closure, "additionalStatusEffect") is Type single) statuses.Add(single);
            var profile = new WeaponProfileReader(Capture(closure, "profile"));
            var unmodeled = profile.UnmodeledRiders().ToList();
            if (Capture(closure, "statusEffectFactory") != null) unmodeled.Add("statusEffectFactory");
            if (Capture(closure, "damageType") is CombatDamageType damageType && damageType != CombatDamageType.Physical) unmodeled.Add("damageType");
            if (Capture(closure, "combatImpactDamageAbility") is AbilityType damageAbility && damageAbility != AbilityType.Invalid) unmodeled.Add("combatImpactDamageAbility");
            return new WeaponImpact
            {
                BaseDamage = baseDamage,
                Duration = Capture(closure, "duration") as int? ?? 0,
                Statuses = statuses,
                Skill = Capture(closure, "skill") as SkillType? ?? detail.SkillType,
                Profile = profile,
                UnmodeledRiders = unmodeled,
                RestoresSecondaryTargetStamina = Capture(closure, "restoresSecondaryTargetStamina") is true
            };
        }

        private static object Capture(object closure, string name) => closure.GetType().GetField(name, Fields)?.GetValue(closure);

        /// <summary>
        /// A static impact whose only call is Ability.ApplyCombatImpact(activator, target, location, skill, power, duration,
        /// typeof(Status), false, ...): the three constants before the status token are the skill, power and duration.
        /// </summary>
        private static WeaponImpact ReadDirectCombatImpact(MethodInfo method)
        {
            var il = Decode(method);
            if (il == null) return null;
            var module = method.Module;
            var calls = il.Where(x => x.Code == OpCodes.Call || x.Code == OpCodes.Callvirt)
                .Select(x => module.ResolveMethod((int)x.Operand)).ToList();
            var impact = calls.Where(m => m.DeclaringType == typeof(Ability) && m.Name == nameof(Ability.ApplyCombatImpact)).ToList();
            if (impact.Count != 1 || calls.Any(m => !impact.Contains(m) && !(m.DeclaringType == typeof(Type) && m.Name == nameof(Type.GetTypeFromHandle))))
                return null;
            var token = il.FindIndex(x => x.Code == OpCodes.Ldtoken);
            if (token < 6 || il[token + 1].Code != OpCodes.Call || TryConstant(il[token + 2]) != 0 ||
                il[token - 6].Code != OpCodes.Ldarg_0 || il[token - 5].Code != OpCodes.Ldarg_1 || il[token - 4].Code != OpCodes.Ldarg_3 ||
                TryConstant(il[token - 3]) is not { } skill || TryConstant(il[token - 2]) is not { } power || TryConstant(il[token - 1]) is not { } duration)
                return null;
            var status = module.ResolveType((int)il[token].Operand);
            Func<IStatusEffect> factory = null;
            var ldftn = il.FindIndex(token, x => x.Code == OpCodes.Ldftn);
            if (ldftn >= 0 && module.ResolveMethod((int)il[ldftn].Operand) is MethodInfo lambda && typeof(IStatusEffect).IsAssignableFrom(lambda.ReturnType) && lambda.GetParameters().Length == 0)
            {
                var instance = lambda.IsStatic ? null : lambda.DeclaringType!.GetField("<>9", BindingFlags.Static | BindingFlags.Public)?.GetValue(null)
                    ?? throw new InvalidDataException($"Cannot create the status factory of {method.Name}.");
                factory = () => (IStatusEffect)lambda.Invoke(instance, null);
            }
            return new WeaponImpact { BaseDamage = power, Duration = duration, Statuses = new[] { status }, StatusFactory = factory, Skill = (SkillType)skill };
        }

        private static int? TryConstant((OpCode Code, long Operand) x)
        {
            if (x.Code == OpCodes.Ldc_I4 || x.Code == OpCodes.Ldc_I4_S) return (int)x.Operand;
            if (x.Code == OpCodes.Ldc_I4_M1) return -1;
            var simple = new[] { OpCodes.Ldc_I4_0, OpCodes.Ldc_I4_1, OpCodes.Ldc_I4_2, OpCodes.Ldc_I4_3, OpCodes.Ldc_I4_4, OpCodes.Ldc_I4_5, OpCodes.Ldc_I4_6, OpCodes.Ldc_I4_7, OpCodes.Ldc_I4_8 };
            var index = Array.IndexOf(simple, x.Code);
            return index >= 0 ? index : null;
        }

        private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (OpCode)f.GetValue(null)!).ToDictionary(o => o.Value);

        private static List<(OpCode Code, long Operand)> Decode(MethodInfo method)
        {
            var bytes = method.GetMethodBody()?.GetILAsByteArray();
            if (bytes == null) return null;
            var result = new List<(OpCode, long)>();
            for (var i = 0; i < bytes.Length;)
            {
                var value = (short)bytes[i++];
                if (value == 0xFE) value = unchecked((short)(0xFE00 | bytes[i++]));
                if (!OpCodesByValue.TryGetValue(value, out var code)) return null;
                long operand = 0;
                switch (code.OperandType)
                {
                    case OperandType.InlineNone: break;
                    case OperandType.ShortInlineI: operand = (sbyte)bytes[i]; i += 1; break;
                    case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineVar: operand = bytes[i]; i += 1; break;
                    case OperandType.InlineVar: operand = BitConverter.ToUInt16(bytes, i); i += 2; break;
                    case OperandType.InlineI8: case OperandType.InlineR: operand = BitConverter.ToInt64(bytes, i); i += 8; break;
                    case OperandType.InlineSwitch: i += 4 + 4 * BitConverter.ToInt32(bytes, i); break;
                    default: operand = BitConverter.ToInt32(bytes, i); i += 4; break;
                }
                result.Add((code, operand));
            }
            return result;
        }
    }
}
