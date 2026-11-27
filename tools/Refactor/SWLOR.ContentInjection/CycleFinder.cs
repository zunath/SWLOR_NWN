namespace SWLOR.ContentInjection;

/// <summary>The groups of a directed graph that reach each other (strongly connected components of more than one node, or a node with an edge to itself).</summary>
internal static class CycleFinder
{
    public static IReadOnlyList<IReadOnlyList<T>> Find<T>(IReadOnlyList<T> nodes, Func<T, IEnumerable<T>> edges)
        where T : notnull
    {
        var index = new Dictionary<T, int>();
        var low = new Dictionary<T, int>();
        var stack = new Stack<T>();
        var onStack = new HashSet<T>();
        var cycles = new List<IReadOnlyList<T>>();
        var counter = 0;

        void Visit(T node)
        {
            index[node] = low[node] = counter++;
            stack.Push(node);
            onStack.Add(node);
            foreach (var next in edges(node))
            {
                if (!index.ContainsKey(next))
                {
                    Visit(next);
                    low[node] = Math.Min(low[node], low[next]);
                }
                else if (onStack.Contains(next))
                {
                    low[node] = Math.Min(low[node], index[next]);
                }
            }

            if (low[node] != index[node])
                return;

            var component = new List<T>();
            T member;
            do
            {
                member = stack.Pop();
                onStack.Remove(member);
                component.Add(member);
            }
            while (!EqualityComparer<T>.Default.Equals(member, node));

            if (component.Count > 1 || edges(node).Contains(node))
                cycles.Add(component);
        }

        foreach (var node in nodes)
        {
            if (!index.ContainsKey(node))
                Visit(node);
        }

        return cycles;
    }
}
