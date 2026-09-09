using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;

namespace SWLOR.AnimationDrafts;

/// <summary>Resolves installed clips with the same child-before-parent precedence as model inheritance.</summary>
public sealed class InstalledMotionLibrary
{
    private readonly MdlModel _overlay;
    private readonly Func<string, MdlModel?> _loadSuperModel;
    private readonly Dictionary<string, MdlModel> _models = new(StringComparer.OrdinalIgnoreCase);

    public InstalledMotionLibrary(MdlModel overlay, Func<string, MdlModel?> loadSuperModel)
    {
        _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
        _loadSuperModel = loadSuperModel ?? throw new ArgumentNullException(nameof(loadSuperModel));
        _models[overlay.Name] = overlay;
    }

    public (MdlModel Owner, MdlAnimation Animation) Resolve(string animationName)
    {
        AnimationProject.ValidateToken(animationName, 16);
        var current = _overlay;
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { current.Name };
        for (var depth = 0; depth < AnimationInstall.MaximumModelChainDepth; depth++)
        {
            var animation = current.Animations.SingleOrDefault(a =>
                a.Name.Equals(animationName, StringComparison.OrdinalIgnoreCase));
            if (animation != null) return (current, animation);

            var parent = current.SuperModel;
            if (string.IsNullOrWhiteSpace(parent) || parent.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Installed animation '{animationName}' was not found in the '{_overlay.Name}' model chain.");
            AnimationProject.ValidateToken(parent, 16);
            if (!visited.Add(parent))
                throw new InvalidDataException($"Cycle in the installed animation model chain at '{parent}'.");
            if (depth + 1 == AnimationInstall.MaximumModelChainDepth)
                throw new InvalidDataException("Installed animation model chain exceeds the supported depth.");
            if (!_models.TryGetValue(parent, out var loaded))
            {
                loaded = _loadSuperModel(parent) ?? throw new InvalidDataException($"Missing animation supermodel '{parent}' while resolving '{animationName}'.");
                _models.Add(parent, loaded);
            }
            current = loaded;
        }
        throw new InvalidDataException("Installed animation model chain exceeds the supported depth.");
    }
}
