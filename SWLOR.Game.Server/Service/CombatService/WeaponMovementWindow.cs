using System;
using System.Numerics;

namespace SWLOR.Game.Server.Service.CombatService;

/// <summary>One ordinary movement opportunity between landed weapon hits.</summary>
public sealed class WeaponMovementWindow
{
    private readonly System.Collections.Generic.HashSet<long> _ordinaryHits = new();
    private readonly System.Collections.Generic.Queue<long> _ordinaryHitOrder = new();
    private uint _area;
    private Vector3 _position;
    private bool _initialized;
    private float _distance;

    public float DistanceTravelled => _distance;

    public bool TryRememberOrdinaryHit(long rollId)
    {
        if (!_ordinaryHits.Add(rollId)) return false;
        _ordinaryHitOrder.Enqueue(rollId);
        if (_ordinaryHitOrder.Count > 256) _ordinaryHits.Remove(_ordinaryHitOrder.Dequeue());
        return true;
    }

    public void Observe(DateTime now, uint area, Vector3 position, bool walking = true)
    {
        if (!_initialized || area != _area)
        {
            _initialized = true;
            _distance = 0;
        }
        else
        {
            var distance = Vector2.Distance(new(_position.X, _position.Y), new(position.X, position.Y));
            // Any completed same-area reposition qualifies, including another skill's
            // movement ability. A requested destination contributes nothing before arrival.
            _distance += distance;
        }
        _area = area;
        _position = position;
    }

    public bool TryConsumeForHit(uint area, Vector3 position)
    {
        var qualifies = _initialized && area == _area && _distance >= 2f;
        _distance = 0;
        _area = area;
        _position = position;
        return qualifies;
    }

    public void Clear() { _initialized = false; _distance = 0; }
}
