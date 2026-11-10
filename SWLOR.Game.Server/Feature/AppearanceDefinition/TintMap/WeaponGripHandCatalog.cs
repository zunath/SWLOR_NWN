using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;

/// <summary>Alternate grasp poses retain their authored hand style and material identity.</summary>
public sealed class WeaponGripHandCatalog
{
    private static readonly Regex Source = new(@"^(p[mf][hedoz]0_hand[rl])(\d{3})$", RegexOptions.IgnoreCase);
    private static readonly Regex Render = new(@"^(p[mf][hedoz]0_hand[rl])(\d{3,5})$", RegexOptions.IgnoreCase);
    private readonly Dictionary<string, string> _renders = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string Source, BaseItem Weapon, int Model), string> _weaponRenders = new();
    private readonly Dictionary<string, string> _sources = new(StringComparer.OrdinalIgnoreCase);

    public WeaponGripHandCatalog(IEnumerable<(string Source, string Render)> rows)
        : this(rows.Select(row => new WeaponGripHandRow(row.Source, row.Render))) { }

    public WeaponGripHandCatalog(IEnumerable<WeaponGripHandRow> rows)
    {
        foreach (var row in rows)
        {
            var (source, render, weapon, model) = row;
            var original = Source.Match(source ?? string.Empty);
            var posed = Render.Match(render ?? string.Empty);
            if (!original.Success || !posed.Success ||
                !original.Groups[1].Value.Equals(posed.Groups[1].Value, StringComparison.OrdinalIgnoreCase) ||
                !ushort.TryParse(original.Groups[2].Value, out var sourceId) || sourceId == 0 ||
                !ushort.TryParse(posed.Groups[2].Value, out var id) ||
                (id < 1000 && !source.Equals(render, StringComparison.OrdinalIgnoreCase)) ||
                model < 0 || model > 65535 ||
                weapon != (BaseItem)(-1) && CanonicalWeapon(weapon) is not (BaseItem.Sling or BaseItem.Rifle) ||
                weapon == (BaseItem)(-1) && model != 0 ||
                _sources.TryGetValue(render, out var existing) && !existing.Equals(source, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid or duplicate weapon hand-pose row.");
            var added = weapon == (BaseItem)(-1)
                ? _renders.TryAdd(source, render)
                : _weaponRenders.TryAdd((source.ToLowerInvariant(), CanonicalWeapon(weapon), model), render);
            if (!added) throw new ArgumentException("Duplicate weapon hand-pose selection.");
            _sources.TryAdd(render, source);
        }
    }

    public string Authored(string model) => model != null && _sources.TryGetValue(model, out var source) ? source : model;

    public string Resolve(string model, BaseItem weapon, string side, int weaponModel = 0)
    {
        var source = Authored(model);
        if (source != null && side is "r" or "l")
        {
            var key = (source.ToLowerInvariant(), CanonicalWeapon(weapon), weaponModel);
            if (_weaponRenders.TryGetValue(key, out var selected) ||
                _weaponRenders.TryGetValue((key.Item1, key.Item2, 0), out selected)) return selected;
        }
        return NeedsGrasp(weapon, side) && source != null && _renders.TryGetValue(source, out var render) ? render : source;
    }

    private static BaseItem CanonicalWeapon(BaseItem weapon) =>
        weapon is BaseItem.Pistol or BaseItem.Sling or BaseItem.LegacyPistol or BaseItem.CompanionPistol ? BaseItem.Sling : weapon;

    public static bool NeedsGrasp(BaseItem weapon, string side) => side is "r" or "l" &&
        (weapon is BaseItem.Pistol or BaseItem.Sling or BaseItem.LegacyPistol or BaseItem.CompanionPistol ||
         side == "r" && weapon == BaseItem.Rifle);

    public ushort AuthoredStyle(string prefix, ushort style) =>
        ushort.Parse(Authored(prefix + style.ToString("D3"))[prefix.Length..]);

    public ushort ResolveStyle(string prefix, ushort style, BaseItem weapon, string side, int weaponModel = 0) =>
        ushort.Parse(Resolve(prefix + style.ToString("D3"), weapon, side, weaponModel)[prefix.Length..]);
}
