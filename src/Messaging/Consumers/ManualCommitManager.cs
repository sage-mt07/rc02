using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Confluent.Kafka;

namespace Kafka.Ksql.Linq.Messaging.Consumers;

internal class ManualCommitManager : ICommitManager, EventSet<object>.ICommitRegistrar
{
    private record Binding(string Topic, object Consumer);
    private sealed class MetaBox { public MessageMeta Meta; public MetaBox(MessageMeta m) { Meta = m; } }

    private readonly Dictionary<Type, Binding> _bindings = new();
    private readonly ConditionalWeakTable<object, MetaBox> _meta = new();
    private readonly Dictionary<(string Topic, int Partition), SortedDictionary<long, WeakReference<object>>> _index = new();
    private readonly Dictionary<(string Topic, int Partition), long> _committed = new();
    private readonly object _lock = new();

    public void Bind(Type pocoType, string topic, object consumer)
    {
        if (pocoType == null) throw new ArgumentNullException(nameof(pocoType));
        if (topic == null) throw new ArgumentNullException(nameof(topic));
        if (consumer == null) throw new ArgumentNullException(nameof(consumer));
        _bindings[pocoType] = new Binding(topic, consumer);
    }

    void EventSet<object>.ICommitRegistrar.Track(object entity, MessageMeta meta)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (!_bindings.ContainsKey(entity.GetType()))
            return;
        _meta.Add(entity, new MetaBox(meta));
        var key = (meta.Topic, meta.Partition);
        lock (_lock)
        {
            if (!_index.TryGetValue(key, out var dict))
                _index[key] = dict = new SortedDictionary<long, WeakReference<object>>();
            dict[meta.Offset] = new WeakReference<object>(entity);
        }
    }

    public void Commit(object entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (!_meta.TryGetValue(entity, out var box))
            return;
        var meta = box.Meta;
        if (!_bindings.TryGetValue(entity.GetType(), out var bind))
            return;
        var key = (meta.Topic, meta.Partition);
        lock (_lock)
        {
            var committed = _committed.TryGetValue(key, out var c) ? c : -1;
            if (meta.Offset <= committed)
            {
                Cleanup(key, committed);
                _meta.Remove(entity);
                return;
            }
            var tpo = new TopicPartitionOffset(meta.Topic, new Partition(meta.Partition), new Offset(meta.Offset + 1));
            dynamic cons = bind.Consumer;
            cons.Commit(tpo);
            _committed[key] = meta.Offset;
            Cleanup(key, meta.Offset);
            _meta.Remove(entity);
        }
    }

    private void Cleanup((string Topic, int Partition) key, long upto)
    {
        if (!_index.TryGetValue(key, out var dict))
            return;
        var remove = new List<long>();
        foreach (var kv in dict)
        {
            if (kv.Key <= upto || !kv.Value.TryGetTarget(out var target))
            {
                if (kv.Value.TryGetTarget(out var ent))
                    _meta.Remove(ent);
                remove.Add(kv.Key);
            }
        }
        foreach (var k in remove)
            dict.Remove(k);
        if (dict.Count == 0)
            _index.Remove(key);
    }
}
