namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// One quick craft batch: a short crafting animation per item, then the components are taken and the base item created.
/// Cancelling stops after the items already made; the item in progress takes nothing, so unused materials stay in the inventory.
/// Quick crafted items carry no enhancements, no blueprint bonuses and no XP.
/// </summary>
public sealed class QuickCraftBatch
{
    /// <summary>The crafting animation for each item lasts this long.</summary>
    public const float SecondsPerItem = 2.0f;

    private readonly IQuickCraftWorld _world;
    private readonly uint _player;
    private readonly RecipeDetail _recipe;
    private int _generation;

    public QuickCraftBatch(IQuickCraftWorld world, uint player, RecipeDetail recipe, int quantity)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _player = player;
        _recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
        Requested = quantity;
    }

    public int Requested { get; }

    public int Completed { get; private set; }

    public bool IsRunning { get; private set; }

    public QuickCraftEnd? Ended { get; private set; }

    /// <summary>Raised once when the batch ends, for any reason.</summary>
    public event Action<QuickCraftBatch> OnEnded;

    public void Start()
    {
        if (IsRunning || Ended != null)
            return;

        if (Requested < 1)
        {
            End(QuickCraftEnd.OutOfMaterials);
            return;
        }

        IsRunning = true;
        _world.BeginBatch(_player);
        _world.PlayStationWork(_player, _recipe.Skill);
        BeginNextItem();
    }

    public void Cancel()
    {
        if (!IsRunning)
            return;

        End(QuickCraftEnd.Cancelled);
    }

    /// <summary>Ends the batch because the player can no longer craft, such as after dying.</summary>
    public void Interrupt()
    {
        if (!IsRunning)
            return;

        End(QuickCraftEnd.Interrupted);
    }

    private void BeginNextItem()
    {
        var generation = ++_generation;
        _world.PlayItemAnimation(_player, SecondsPerItem);
        _world.Schedule(SecondsPerItem, () => FinishItem(generation));
    }

    private void FinishItem(int generation)
    {
        // A cancelled or ended batch leaves its scheduled step behind; it must do nothing.
        if (!IsRunning || generation != _generation)
            return;

        if (!_world.IsPlayerAvailable(_player))
        {
            End(QuickCraftEnd.Interrupted);
            return;
        }

        if (!_world.TryConsumeComponents(_player, _recipe))
        {
            End(QuickCraftEnd.OutOfMaterials);
            return;
        }

        _world.CreateItem(_player, _recipe);
        Completed++;

        if (Completed >= Requested)
            End(QuickCraftEnd.Completed);
        else
            BeginNextItem();
    }

    private void End(QuickCraftEnd reason)
    {
        var wasRunning = IsRunning;
        IsRunning = false;
        Ended = reason;
        _generation++;

        if (wasRunning)
            _world.EndBatch(_player);

        if (_world.IsPlayerAvailable(_player))
            _world.Message(_player, Summarize(reason));

        OnEnded?.Invoke(this);
    }

    private string Summarize(QuickCraftEnd reason)
    {
        var made = Completed == 1 ? "1 item" : $"{Completed} items";
        return reason switch
        {
            QuickCraftEnd.Completed => $"Quick craft finished: {made} made.",
            QuickCraftEnd.Cancelled => $"Quick craft cancelled: {made} made. Unused materials stay in your inventory.",
            QuickCraftEnd.OutOfMaterials => $"Quick craft stopped: you ran out of materials after {made}.",
            _ => $"Quick craft interrupted: {made} made."
        };
    }
}
