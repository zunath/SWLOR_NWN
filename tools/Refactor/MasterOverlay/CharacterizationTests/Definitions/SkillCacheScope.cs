using System.Reflection;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.CharacterizationTests.Definitions;

/// <summary>
/// Master version of the branch's SkillCacheScope (see MasterCopy.ps1, "master overlay"). Master's Skill is a static class,
/// so the scope fills its static detail cache; the facades argument only keeps the branch's constructor signature.
/// </summary>
internal sealed class SkillCacheScope : IDisposable
{
    private readonly Dictionary<SkillType, SkillAttribute> _cache;
    private readonly Dictionary<SkillType, SkillAttribute> _original;

    public SkillCacheScope(FacadeServices facades)
    {
        _cache = (Dictionary<SkillType, SkillAttribute>)typeof(Skill)
            .GetField("_allSkills", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
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
