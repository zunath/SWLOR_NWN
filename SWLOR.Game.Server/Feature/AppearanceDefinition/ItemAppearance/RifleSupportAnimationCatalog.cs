using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

internal sealed class RifleSupportAnimationCatalog
{
    private static readonly Regex Hand = new(@"^(p[fm][hedoz]0)_hand([rl])([0-9]{3,5})$", RegexOptions.IgnoreCase);
    private static readonly Regex Clip = new(@"^rf[a-z0-9_]{1,12}_[irs]$");
    private readonly Dictionary<(string Right, string Left, int Model), RifleSupportPoses> _poses = new();

    internal RifleSupportAnimationCatalog(IEnumerable<(string Right, string Left, int Model, RifleSupportPoses Poses)> rows)
    {
        foreach (var (right, left, model, poses) in rows)
        {
            var primary = Hand.Match(right ?? "");
            var support = Hand.Match(left ?? "");
            if (!primary.Success || !support.Success || !primary.Groups[2].Value.Equals("r", StringComparison.OrdinalIgnoreCase) ||
                !support.Groups[2].Value.Equals("l", StringComparison.OrdinalIgnoreCase) ||
                !primary.Groups[1].Value.Equals(support.Groups[1].Value, StringComparison.OrdinalIgnoreCase) ||
                !ushort.TryParse(primary.Groups[3].Value, out var rightId) || rightId == 0 ||
                !ushort.TryParse(support.Groups[3].Value, out var leftId) || leftId == 0 || model is < 1 or > 65535 ||
                poses == null || new[] { poses.Idle, poses.Ready, poses.Shot }.Where((name, index) =>
                    name == null || !Clip.IsMatch(name) || !name.EndsWith("_" + "irs"[index], StringComparison.Ordinal)).Any() ||
                !_poses.TryAdd((right.ToLowerInvariant(), left.ToLowerInvariant(), model), poses))
                throw new ArgumentException("Invalid or duplicate rifle support animation selection.");
        }
    }

    internal RifleSupportPoses Resolve(string right, string left, int model) =>
        right != null && left != null && _poses.TryGetValue((right.ToLowerInvariant(), left.ToLowerInvariant(), model), out var poses)
            ? poses : null;
}
