using System.Reflection;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.CharacterizationTests.Definitions;

/// <summary>
/// Fills the skill detail cache that <c>Skill.GetSkillDetails</c> reads with the attribute data of every
/// <see cref="SkillType"/>, the same data <c>Skill.CacheData</c> caches at module load, and restores the previous
/// contents when disposed. <c>Skill.CacheData</c> itself signals an engine event and cannot run outside the engine.
/// The cache is process-wide, so a fixture that uses this scope must be <c>[NonParallelizable]</c>.
/// </summary>
internal sealed class SkillCacheScope : IDisposable
{
    private readonly Dictionary<SkillType, SkillAttribute> _cache;
    private readonly Dictionary<SkillType, SkillAttribute> _original;

    public SkillCacheScope(FacadeServices facades)
    {
        var service = facades.Get<ISkillService>();
        _cache = (Dictionary<SkillType, SkillAttribute>)service.GetType()
            .GetField("_allSkills", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(service)!;
        _original = new Dictionary<SkillType, SkillAttribute>(_cache);

        _cache.Clear();
        foreach (var type in Enum.GetValues<SkillType>())
            _cache[type] = type.GetAttribute<SkillType, SkillAttribute>();
    }

    public void Dispose()
    {
        _cache.Clear();
        foreach (var (type, detail) in _original)
            _cache[type] = detail;
    }
}
