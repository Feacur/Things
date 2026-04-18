namespace Things.Framework.ECS;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Things.Framework.Containers;
using Connetions = Containers.SparseSet<Entity>;
using EntityConnections = System.Collections.Generic.Dictionary<Entity, Containers.SparseSet<Entity>>;


public sealed class Relation(int type_size = 0) : IDisposable
{
	private readonly SparseSet<Pair>   data              = new(type_size: type_size);
	private readonly EntityConnections source_to_targets = [];
	private readonly EntityConnections target_to_sources = [];
	private readonly Stack<Connetions> free_list         = [];
	private bool is_disposed;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<Pair> GetPairs() => data.GetKeys();

	public bool Has(in Entity source, in Entity target)
	{
		var pair = new Pair(source, target);
		return data.Has(in pair);
	}

	public ref T Get<T>(in Entity source, in Entity target) where T : unmanaged
	{
		// @note compiler don't trust the dev using `in` here
		// even if `T` can't be `Pair` ever; so, pass by value.
		// alternatively, pass a compound key from the outside, but
		// it would make API more ugly; @todo evaluate
		var pair = new Pair(source, target);
		return ref data.Get<T>(pair);
	}

	public (bool source, bool target) Set<T>(in Entity source, in Entity target, in T value) where T : unmanaged
	{
		var ret = (source: false, target: false);
		var pair = new Pair(source, target);
		if (!data.Set(in pair, in value))
		{
			if (!source_to_targets.TryGetValue(source, out var target_entities))
				source_to_targets.Add(source, target_entities = Acquire());
			ret.source = target_entities.SetKey(target);

			if (!target_to_sources.TryGetValue(target, out var source_entities))
				target_to_sources.Add(target, source_entities = Acquire());
			ret.target = source_entities.SetKey(source);
		}
		return ret;
	}

	public (bool source, bool target) Remove(in Entity source, in Entity target)
	{
		var ret = (source: false, target: false);
		var pair = new Pair(source, target);
		if (data.Remove(in pair))
		{
			if (source_to_targets.TryGetValue(source, out var target_entities))
			{
				target_entities.Remove(target);
				if (target_entities.Count == 0)
				{
					source_to_targets.Remove(source);
					Release(target_entities);
					ret.source = true;
				}
			}
			if (target_to_sources.TryGetValue(target, out var source_entities))
			{
				source_entities.SetKey(source);
				if (target_to_sources.Count == 0)
				{
					target_to_sources.Remove(target);
					Release(source_entities);
					ret.target = true;
				}
			}
		}
		return ret;
	}

	public void Remove(in Entity entity)
	{
		if (source_to_targets.TryGetValue(entity, out var target_entities))
		{
			source_to_targets.Remove(entity);
			foreach (var target in target_entities.GetKeys())
			{
				var pair = new Pair(entity, target);
				data.Remove(in pair);
			}
			target_entities.Clear();
			Release(target_entities);
		}

		if (target_to_sources.TryGetValue(entity, out var source_entities))
		{
			target_to_sources.Remove(entity);
			foreach (var source in source_entities.GetKeys())
			{
				var pair = new Pair(source, entity);
				data.Remove(in pair);
			}
			source_entities.Clear();
			Release(source_entities);
		}
	}

	public ReadOnlySpan<Entity> GetTargets(in Entity entity)
	{
		if (source_to_targets.TryGetValue(entity, out var target_entities))
			return target_entities.GetKeys();
		return [];
	}

	public ReadOnlySpan<Entity> GetSources(in Entity entity)
	{
		if (target_to_sources.TryGetValue(entity, out var source_entities))
			return source_entities.GetKeys();
		return [];
	}

	private Connetions Acquire()           => free_list.TryPop(out var ret) ? ret : new();
	private void Release(Connetions value) => free_list.Push(value);

	void IDisposable.Dispose()
	{
		if (!is_disposed) return;
		((IDisposable)data).Dispose();
		foreach (IDisposable it in source_to_targets.Values)
			it.Dispose();
		foreach (IDisposable it in target_to_sources.Values)
			it.Dispose();
		foreach (IDisposable it in free_list)
			it.Dispose();
		is_disposed = true;
		GC.SuppressFinalize(this);
	}

	public readonly record struct Pair(Entity Source, Entity Target);
}
