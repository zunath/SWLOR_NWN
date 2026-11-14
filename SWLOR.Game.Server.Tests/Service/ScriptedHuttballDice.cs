using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.HuttballService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Hands out the rolls a test lists, in order, and fails the test if the rules roll more dice than expected.
/// </summary>
public class ScriptedHuttballDice : IHuttballDice
{
    private readonly Queue<int> _rolls = new();

    public void Queue(params int[] rolls)
    {
        foreach (var roll in rolls)
            _rolls.Enqueue(roll);
    }

    public int Remaining => _rolls.Count;

    public int Roll(int sides)
    {
        if (_rolls.Count == 0)
            throw new InvalidOperationException($"An unexpected d{sides} was rolled.");

        var roll = _rolls.Dequeue();
        if (roll < 1 || roll > sides)
            throw new InvalidOperationException($"Scripted roll {roll} does not fit a d{sides}.");

        return roll;
    }
}
