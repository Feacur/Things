namespace Things.Framework.ECS;

using System;
using System.Collections.Generic;
using Things.Framework.Containers;


public sealed partial class World : IDisposable
{
	private readonly Dictionary<Filter.Signature, Filter> filter_instances            = [];
	private readonly Entity.Pool                          entity_pool                 = new();
	private readonly SparseSet<Entity>                    entity_instances            = new();
	private readonly List<SparseSet<Type>>                entity_to_component_types   = [];
	private readonly List<SparseSet<Type>>                entity_to_relation_types    = [];
	private readonly List<SparseSet<Entity>>              component_type_to_instances = [];
	private readonly List<List<Filter>>                   component_type_to_filters   = [];
	private readonly List<NativeArray>                    message_type_to_instances   = [];
	private readonly List<Relation>                       relation_type_to_instances  = [];
	private bool is_disposed;

	public Filter.Builder FilterBuilder => new(this);
	public Filter GetFilter(in Filter.Signature signature)
	{
		if (!filter_instances.TryGetValue(signature, out var component_filter))
		{
			component_filter = new Filter(this, in signature);
			foreach (var it in signature.Include.GetKeys())
				component_type_to_filters[it.AsIndex()].Add(component_filter);
			foreach (var it in signature.Exclude.GetKeys())
				component_type_to_filters[it.AsIndex()].Add(component_filter);
			for (uint i = 0; i < entity_to_component_types.Count; i++)
				component_filter.Check(new Entity(Id: i + 1)); // @note zero is nil
			filter_instances.Add(signature, component_filter);
		}
		return component_filter;
	}

	public Entity CreateEntity()
	{
		var entity = entity_pool.Acquire();
		if (entity >= entity_to_component_types.Count)
			entity_to_component_types.Add(new SparseSet<Type>());
		if (entity >= entity_to_relation_types.Count)
			entity_to_relation_types.Add(new SparseSet<Type>());
		entity_instances.SetKey(in entity);
		return entity;
	}

	public void DestroyEntity(in Entity entity)
	{
		if (!entity_instances.Remove(in entity))
			return;

		var component_types = entity_to_component_types[entity.AsIndex()];
		foreach (var component_type in component_types.GetReverseKeys())
		{
			var component_storage = component_type_to_instances[component_type.AsIndex()];
			component_storage.Remove(entity);

			var filters = component_type_to_filters[component_type.AsIndex()];
			foreach (var filter in filters)
				filter.Remove(in entity);
		}
		component_types.Clear();

		var realation_types = entity_to_relation_types[entity.AsIndex()];
		foreach (var relation_type in realation_types.GetReverseKeys())
		{
			var relation_storage = relation_type_to_instances[relation_type.AsIndex()];
			relation_storage.Remove(entity);
		}
		realation_types.Clear();

		entity_pool.Release(entity);
	}

	public bool HasComponent<T>(in Entity entity) where T : unmanaged
	{
		var component_type = EnsureComponentStorage<T>();
		var component_storage = component_type_to_instances[component_type.AsIndex()];
		return component_storage.Has(in entity);
	}

	public bool HasComponent(in Entity entity, in Type component_type)
	{
		var component_types = entity_to_component_types[entity.AsIndex()];
		return component_types.Has(component_type);
	}

	public ref T GetComponent<T>(in Entity entity) where T : unmanaged
	{
		var component_type = EnsureComponentStorage<T>();
		var component_storage = component_type_to_instances[component_type.AsIndex()];
		return ref component_storage.Get<T>(in entity);
	}

	public void SetComponent<T>(in Entity entity, in T value) where T : unmanaged
	{
		var component_type = EnsureComponentStorage<T>();
		var component_storage = component_type_to_instances[component_type.AsIndex()];
		if (!component_storage.Set<T>(in entity, in value))
		{
			var component_types = entity_to_component_types[entity.AsIndex()];
			component_types.SetKey(component_type);
			var filters = component_type_to_filters[component_type.AsIndex()];
			foreach (var filter in filters)
				filter.Check(in entity);
		}
	}

	public void RemoveComponent<T>(in Entity entity) where T : unmanaged
	{
		var component_type = EnsureComponentStorage<T>();
		var component_storage = component_type_to_instances[component_type.AsIndex()];
		if (component_storage.Remove(in entity))
		{
			var component_types = entity_to_component_types[entity.AsIndex()];
			component_types.Remove(component_type);
			var filters = component_type_to_filters[component_type.AsIndex()];
			foreach (var filter in filters)
				filter.Check(in entity);
		}
	}

	public Type EnsureComponentStorage<T>() where T : unmanaged
	{
		var component_type = Type.Identity<KindComponent>.Get<T>();
		if (component_type >= component_type_to_instances.Count)
		{
			var component_size = Type.Identity<KindComponent>.SizeOf(component_type);
			component_type_to_instances.Add(new SparseSet<Entity>(type_size: component_size));
			component_type_to_filters.Add([]);
		}
		return component_type;
	}

	public void Send<T>(in T value) where T : unmanaged
	{
		var message_type = EnsureMessageStorage<T>();
		var message_storage = message_type_to_instances[message_type.AsIndex()];
		message_storage.Push(in value);
	}

	public ReadOnlySpan<T> Read<T>() where T : unmanaged
	{
		var message_type = EnsureMessageStorage<T>();
		var message_storage = message_type_to_instances[message_type.AsIndex()];
		return message_storage.AsSpan<T>();
	}

	public void ClearMessages()
	{
		foreach (var message_storage in message_type_to_instances)
			message_storage.Clear();
	}

	public Type EnsureMessageStorage<T>() where T : unmanaged
	{
		var message_type = Type.Identity<KindMessage>.Get<T>();
		if (message_type >= message_type_to_instances.Count)
		{
			var message_size = Type.Identity<KindMessage>.SizeOf(message_type);
			message_type_to_instances.Add(new NativeArray(type_size: message_size));
		}
		return message_type;
	}

	public ReadOnlySpan<Relation.Pair> GetRelations<T>() where T : unmanaged
	{
		var relation_type = EnsureRelationStorage<T>();
		var relation_storage = relation_type_to_instances[relation_type.AsIndex()];
		return relation_storage.GetPairs();
	}

	public bool HasRelation<T>(in Entity source, in Entity target) where T : unmanaged
	{
		var relation_type = EnsureRelationStorage<T>();
		var relation_storage = relation_type_to_instances[relation_type.AsIndex()];
		return relation_storage.Has(in source, in target);
	}

	public ref T GetRelation<T>(in Entity source, in Entity target) where T : unmanaged
	{
		var relation_type = EnsureRelationStorage<T>();
		var relation_storage = relation_type_to_instances[relation_type.AsIndex()];
		return ref relation_storage.Get<T>(in source, in target);
	}

	public void SetRelation<T>(in Entity source, in Entity target, in T value) where T : unmanaged
	{
		var relation_type = EnsureRelationStorage<T>();
		var relation_storage = relation_type_to_instances[relation_type.AsIndex()];
		var result = relation_storage.Set(in source, in target, in value);
		if (!result.source)
		{
			var source_types = entity_to_relation_types[source.AsIndex()];
			source_types.SetKey(relation_type);
		}
		if (!result.target)
		{
			var target_types = entity_to_relation_types[target.AsIndex()];
			target_types.SetKey(relation_type);
		}
	}

	public void RemoveRelation<T>(in Entity source, in Entity target) where T : unmanaged
	{
		var relation_type = EnsureRelationStorage<T>();
		var relation_storage = relation_type_to_instances[relation_type.AsIndex()];
		var result = relation_storage.Remove(in source, in target);
		if (result.source)
		{
			var source_types = entity_to_relation_types[source.AsIndex()];
			source_types.Remove(relation_type);
		}
		if (result.target)
		{
			var target_types = entity_to_relation_types[target.AsIndex()];
			target_types.Remove(relation_type);
		}
	}

	public ReadOnlySpan<Entity> GetTargetRelations<T>(in Entity entity) where T : unmanaged
	{
		var relation_type = EnsureRelationStorage<T>();
		var relation_storage = relation_type_to_instances[relation_type.AsIndex()];
		return relation_storage.GetTargets(in entity);
	}

	public ReadOnlySpan<Entity> GetSourceRelations<T>(in Entity entity) where T : unmanaged
	{
		var relation_type = EnsureRelationStorage<T>();
		var relation_storage = relation_type_to_instances[relation_type.AsIndex()];
		return relation_storage.GetSources(in entity);
	}

	public Type EnsureRelationStorage<T>() where T : unmanaged
	{
		var relation_type = Type.Identity<KindRelation>.Get<T>();
		if (relation_type >= relation_type_to_instances.Count)
		{
			var relation_size = Type.Identity<KindRelation>.SizeOf(relation_type);
			relation_type_to_instances.Add(new(type_size: relation_size));
		}
		return relation_type;
	}

	void IDisposable.Dispose()
	{
		if (!is_disposed) return;
		foreach (IDisposable it in filter_instances.Values)
			it.Dispose();
		((IDisposable)entity_pool).Dispose();
		((IDisposable)entity_instances).Dispose();
		foreach (IDisposable it in entity_to_component_types)
			it.Dispose();
		foreach (IDisposable it in entity_to_relation_types)
			it.Dispose();
		foreach (IDisposable it in component_type_to_instances)
			it.Dispose();
		foreach (IDisposable it in message_type_to_instances)
			it.Dispose();
		foreach (IDisposable it in relation_type_to_instances)
			it.Dispose();
		is_disposed = true;
		GC.SuppressFinalize(this);
	}

	private readonly record struct KindComponent();
	private readonly record struct KindMessage();
	private readonly record struct KindRelation();
}

public sealed partial class World
{
	public ReadOnlySpan<Entity> DebugEntities()
		=> entity_instances.GetKeys();

	public Dictionary<Filter.Signature, Filter>.ValueCollection DebugFilters()
		=> filter_instances.Values;

	public ReadOnlySpan<Type> DebugComponentTypes(in Entity entity)
	{
		var component_types = entity_to_component_types[entity.AsIndex()];
		return component_types.GetKeys();
	}

	public ReadOnlySpan<Type> DebugRelationTypes(in Entity entity)
	{
		var relation_types = entity_to_relation_types[entity.AsIndex()];
		return relation_types.GetKeys();
	}
}
