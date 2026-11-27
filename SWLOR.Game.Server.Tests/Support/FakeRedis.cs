using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A stand-in for the Redis connection that <c>DBService</c> uses, at the lowest level the service touches: the
/// <see cref="IConnectionMultiplexer"/> and the <see cref="IDatabase"/> it hands out. It keeps JSON documents, search
/// hashes and strings in dictionaries and answers the RediSearch and RedisJSON commands the service sends, recording every
/// command in order so a test can assert on what went over the wire.
/// </summary>
public sealed class FakeRedis
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _multiplexer;

    public FakeRedis()
    {
        _database = DispatchProxy.Create<IDatabase, DatabaseProxy>();
        ((DatabaseProxy)(object)_database).Owner = this;
        _multiplexer = DispatchProxy.Create<IConnectionMultiplexer, MultiplexerProxy>();
        ((MultiplexerProxy)(object)_multiplexer).Owner = this;
    }

    public IConnectionMultiplexer Multiplexer => _multiplexer;

    public IDatabase Database => _database;

    /// <summary>Stored JSON documents by key.</summary>
    public Dictionary<string, string> Json { get; } = new(StringComparer.Ordinal);

    /// <summary>Search hashes by key (the "Index:" documents).</summary>
    public Dictionary<string, HashEntry[]> Hashes { get; } = new(StringComparer.Ordinal);

    /// <summary>Plain string keys (the index fingerprints).</summary>
    public Dictionary<string, string> Strings { get; } = new(StringComparer.Ordinal);

    /// <summary>Every command in the order it was sent: the command name followed by its arguments as text.</summary>
    public List<List<string>> Commands { get; } = new();

    /// <summary>Stream entries added.</summary>
    public List<(string Key, NameValueEntry[] Entries, int? MaxLength, bool Approximate)> Streams { get; } = new();

    /// <summary><c>IsConnected</c> reports false for this many reads, then true.</summary>
    public int DisconnectedReads { get; set; }

    /// <summary>Search-hash ids FT.SEARCH answers with.</summary>
    public List<string> SearchHits { get; } = new();

    /// <summary>Invoked before each command; a test throws from it to fail the command.</summary>
    public Action<List<string>>? Before { get; set; }

    /// <summary>The FT.INFO answer for an index; null means the index does not exist (the server's "Unknown Index name" error).</summary>
    public Func<string, RedisResult[]?> IndexInfo { get; set; } = _ => new[]
    {
        RedisResult.Create("percent_indexed", ResultType.BulkString), RedisResult.Create("1", ResultType.BulkString)
    };

    public IEnumerable<List<string>> CommandsNamed(string name) => Commands.Where(command => command[0] == name);

    private object? Handle(MethodInfo method, object?[] args)
    {
        var name = method.Name;
        var isAsync = name.EndsWith("Async", StringComparison.Ordinal);
        var baseName = isAsync ? name[..^5] : name;
        object? result;

        switch (baseName)
        {
            case "Execute":
                result = ExecuteCommand((string)args[0]!, (args[1] as IEnumerable<object>)?.ToList() ?? new List<object>());
                break;
            case "KeyExists":
                Record("EXISTS", args[0]);
                result = Json.ContainsKey(args[0]!.ToString()!) || Hashes.ContainsKey(args[0]!.ToString()!);
                break;
            case "StringGet":
                Record("GET", args[0]);
                result = Strings.TryGetValue(args[0]!.ToString()!, out var text) ? (RedisValue)text : RedisValue.Null;
                break;
            case "StringSet":
                Record("SET", args[0], args[1]);
                Strings[args[0]!.ToString()!] = args[1]!.ToString()!;
                result = true;
                break;
            case "HashGetAll":
                Record("HGETALL", args[0]);
                result = Hashes.TryGetValue(args[0]!.ToString()!, out var hash) ? hash : Array.Empty<HashEntry>();
                break;
            case "StreamAdd":
                Record("XADD", args[0]);
                var parameters = method.GetParameters();
                int IndexOf(string parameter) => Array.FindIndex(parameters, p => p.Name == parameter);
                var maxLength = args[IndexOf("maxLength")];
                Streams.Add((args[0]!.ToString()!,
                    (NameValueEntry[])args[IndexOf("streamPairs")]!,
                    maxLength == null ? null : Convert.ToInt32(maxLength),
                    (bool)args[IndexOf("useApproximateMaxLength")]!));
                result = (RedisValue)"1-0";
                break;
            default:
                throw new InvalidOperationException($"Unexpected database call: {name}");
        }

        if (!isAsync)
            return result;

        var taskType = method.ReturnType.GetGenericArguments()[0];
        return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(taskType).Invoke(null, new[] { result });
    }

    private void Record(string name, params object?[] args)
    {
        var command = new List<string> { name };
        command.AddRange(args.Select(value => value?.ToString() ?? string.Empty));
        Commands.Add(command);
        Before?.Invoke(command);
    }

    private RedisResult ExecuteCommand(string command, List<object> arguments)
    {
        Record(command, arguments.Cast<object?>().ToArray());
        var text = arguments.Select(value => value.ToString()!).ToList();
        switch (command)
        {
            case "JSON.SET":
            {
                var key = text[0];
                var path = text[1];
                if (text.Contains("XX") && !Json.ContainsKey(key))
                    return RedisResult.Create(RedisValue.Null, ResultType.BulkString);
                if (path == ".")
                {
                    Json[key] = text[2];
                }
                else
                {
                    var document = JObject.Parse(Json[key]);
                    document[path.TrimStart('.')] = JToken.Parse(text[2]);
                    Json[key] = document.ToString(Formatting.None);
                }
                return RedisResult.Create("OK", ResultType.SimpleString);
            }
            case "JSON.GET":
                return Json.TryGetValue(text[0], out var json)
                    ? RedisResult.Create(json, ResultType.BulkString)
                    : RedisResult.Create(RedisValue.Null, ResultType.BulkString);
            case "JSON.DEL":
                return RedisResult.Create(Json.Remove(text[0]) ? 1 : 0);
            case "FT.ADD":
            {
                var fields = new List<HashEntry>();
                var start = text.IndexOf("FIELDS") + 1;
                for (var i = start; i + 1 < text.Count; i += 2)
                    fields.Add(new HashEntry(text[i], text[i + 1]));
                Hashes[text[1]] = fields.ToArray();
                return RedisResult.Create("OK", ResultType.SimpleString);
            }
            case "FT.DEL":
                Hashes.Remove(text[1]);
                return RedisResult.Create(1);
            case "FT.SEARCH":
                return SearchAnswer(text);
            case "FT.INFO":
            {
                var info = IndexInfo(text[0]);
                if (info == null)
                    throw new RedisServerException("Unknown Index name");
                return RedisResult.Create(info);
            }
            case "FT.CREATE":
            case "FT.DROPINDEX":
                return RedisResult.Create("OK", ResultType.SimpleString);
            case "SCAN":
            {
                var pattern = text[text.IndexOf("MATCH") + 1];
                var prefix = pattern.TrimEnd('*');
                var keys = Json.Keys.Concat(Hashes.Keys).Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToList();
                return RedisResult.Create(new[]
                {
                    RedisResult.Create("0", ResultType.BulkString),
                    RedisResult.Create(keys.Select(key => RedisResult.Create(key, ResultType.BulkString)).ToArray())
                });
            }
            default:
                throw new InvalidOperationException($"Unexpected command: {command}");
        }
    }

    private RedisResult SearchAnswer(List<string> text)
    {
        var limit = text.IndexOf("LIMIT");
        var countOnly = limit >= 0 && text[limit + 2] == "0";
        var parts = new List<RedisResult> { RedisResult.Create(SearchHits.Count) };
        if (!countOnly)
        {
            foreach (var hit in SearchHits)
            {
                parts.Add(RedisResult.Create(hit, ResultType.BulkString));
                parts.Add(RedisResult.Create(Array.Empty<RedisResult>()));
            }
        }
        return RedisResult.Create(parts.ToArray());
    }

    public class DatabaseProxy : DispatchProxy
    {
        public FakeRedis Owner { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Owner.Handle(targetMethod!, args!);
    }

    public class MultiplexerProxy : DispatchProxy
    {
        public FakeRedis Owner { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod!.Name switch
            {
                "GetDatabase" => Owner._database,
                "get_IsConnected" => Owner.DisconnectedReads-- <= 0,
                _ => throw new InvalidOperationException($"Unexpected multiplexer call: {targetMethod.Name}")
            };
        }
    }
}
