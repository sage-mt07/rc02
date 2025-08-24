using System;
using System.Collections.Generic;
using System.Linq;

namespace Kafka.Ksql.Linq.Query.Analysis;

internal class DerivationDag
{
    private readonly Dictionary<string, HashSet<string>> _edges = new();

    public void AddNode(string id)
    {
        if (!_edges.ContainsKey(id))
            _edges[id] = new HashSet<string>();
    }

    public void AddEdge(string source, string target)
    {
        AddNode(source);
        AddNode(target);
        _edges[target].Add(source);
    }

    public IEnumerable<string> TopologicalSort()
    {
        var incoming = _edges.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        var queue = new Queue<string>(incoming.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));
        var result = new List<string>();
        while (queue.Count > 0)
        {
            var n = queue.Dequeue();
            result.Add(n);
            foreach (var kvp in _edges)
            {
                if (kvp.Value.Remove(n) && kvp.Value.Count == 0)
                    queue.Enqueue(kvp.Key);
            }
        }
        if (result.Count != _edges.Count)
            throw new InvalidOperationException("Cycle detected");
        return result;
    }

    public IReadOnlyDictionary<string, HashSet<string>> Edges => _edges;
}
