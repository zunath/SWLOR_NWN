using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>
/// The enhancements a cook has picked for a dish before cooking starts, one entry per slot. An empty slot holds
/// <c>OBJECT_INVALID</c>, which is what <see cref="CraftSessionStartRequest"/> expects for a slot with no enhancement.
/// </summary>
public sealed class KitchenEnhancementSlots
{
    private readonly uint[] _items;
    private readonly string[] _names;
    private readonly int[] _progressPenalties;

    public KitchenEnhancementSlots(int count)
    {
        _items = new uint[count];
        _names = new string[count];
        _progressPenalties = new int[count];
        ClearAll();
    }

    public int Count => _items.Length;

    public bool IsFilled(int slot) => _items[slot] != OBJECT_INVALID;

    public string NameAt(int slot) => _names[slot];

    public bool Contains(uint item) => item != OBJECT_INVALID && _items.Contains(item);

    public void Fill(int slot, uint item, string name, int progressPenalty)
    {
        _items[slot] = item;
        _names[slot] = name;
        _progressPenalties[slot] = progressPenalty;
    }

    public void Clear(int slot)
    {
        _items[slot] = OBJECT_INVALID;
        _names[slot] = string.Empty;
        _progressPenalties[slot] = 0;
    }

    public void ClearAll()
    {
        for (var slot = 0; slot < Count; slot++)
            Clear(slot);
    }

    /// <summary>The item in every slot in order, with <c>OBJECT_INVALID</c> in the empty ones.</summary>
    public IReadOnlyList<uint> Items => _items.ToList();

    /// <summary>The numbers (counting from 1) of the slots that hold an enhancement.</summary>
    public IReadOnlyList<int> FilledSlotNumbers =>
        Enumerable.Range(0, Count).Where(IsFilled).Select(slot => slot + 1).ToList();

    /// <summary>The progress penalty of every chosen enhancement added together.</summary>
    public int TotalProgressPenalty => _progressPenalties.Sum();
}
