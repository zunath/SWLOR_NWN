namespace SWLOR.Game.Server.Service.FrameProfilerService
{
    /// <summary>
    /// Work counted per server frame by the frame profiler. Scheduler, Script, NativeHook, Closure and
    /// ObjectUpdateHook time is top-level only, so their sum is the frame's managed time. Other timed metrics are inclusive.
    /// </summary>
    public enum FrameMetric
    {
        Scheduler,
        Script,
        NativeHook,
        Closure,
        ObjectUpdateHook,
        DBGet,
        DBGetCached,
        DBGetJsonCached,
        DBDeserialize,
        DBSet,
        DBSearch,
        DBSearchDocument,
        DBSearchRetry,
        DBOther,
        Broadcast,
        BroadcastReceiver,
        PlayerNameLookup,
        StatusEffectApply,
        StatusEffectRemove,
        StatAdjustment,
        CharacterSheetRefresh,
        GuiBindSend,
        TelegraphPlayerUpdate,
        TelegraphUniform,
        LogWrite,
    }
}
