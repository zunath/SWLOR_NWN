using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Service.MigrationService;

/// <summary>Reports record-based progress without treating unchanged records as unfinished work.</summary>
public sealed class ServerMigrationProgress
{
    private readonly int _version;
    private readonly Dictionary<string, int> _sections;
    private readonly long _total;
    private readonly Action<string> _write;
    private readonly Func<TimeSpan> _elapsed;
    private long _processed;
    private long _changed;
    private int _sectionIndex;
    private string _section;
    private int _sectionProcessed;
    private int _sectionChanged;
    private TimeSpan _lastReport;

    public ServerMigrationProgress(int version, params (string Name, int Count)[] sections)
        : this(version, sections, message => Log.Write(LogGroup.Migration, message, true),
            System.Diagnostics.Stopwatch.StartNew()) { }

    private ServerMigrationProgress(int version, (string Name, int Count)[] sections,
        Action<string> write, System.Diagnostics.Stopwatch stopwatch)
        : this(version, sections, write, () => stopwatch.Elapsed) { }

    public ServerMigrationProgress(int version, (string Name, int Count)[] sections,
        Action<string> write, Func<TimeSpan> elapsed)
    {
        _version = version;
        _sections = sections.ToDictionary(x => x.Name, x => x.Count);
        _total = sections.Sum(x => (long)x.Count);
        _write = write;
        _elapsed = elapsed;
        _write($"Migration #{_version}: {_total} records across {_sections.Count} sections to scan.");
    }

    public static (string Name, int Count) CountRecords<T>() where T : EntityBase
        => (typeof(T).Name, checked((int)DB.SearchCount(new DBQuery<T>())));

    public int BeginSection<T>() where T : EntityBase
    {
        _section = typeof(T).Name;
        _sectionIndex++;
        _sectionProcessed = 0;
        _sectionChanged = 0;
        Report("Loading");
        return _sections[_section];
    }

    public void RecordProcessed(bool changed)
    {
        _processed++;
        _sectionProcessed++;
        if (changed)
        {
            _changed++;
            _sectionChanged++;
        }
        if (_elapsed() - _lastReport >= TimeSpan.FromSeconds(5))
            Report("Scanning");
    }

    public void FinishSection() => Report("Finished");

    private void Report(string action)
    {
        var elapsed = _elapsed();
        var remaining = Math.Max(0, _total - _processed);
        var percent = _total == 0 ? 100.0 : _processed * 100.0 / _total;
        var eta = remaining == 0 ? "0s" : _processed == 0 ? "calculating" :
            $"about {elapsed.TotalSeconds / _processed * remaining:0}s (record-based estimate)";
        _write($"Migration #{_version}: {action} section {_sectionIndex}/{_sections.Count} {_section} " +
            $"({_sectionProcessed}/{_sections[_section]} records, {_sectionChanged} changed). " +
            $"Overall: {_processed}/{_total} records ({percent:0.0}%), {remaining} remaining, {_changed} changed. " +
            $"Elapsed: {elapsed.TotalSeconds:0}s. ETA: {eta}.");
        _lastReport = elapsed;
    }
}
