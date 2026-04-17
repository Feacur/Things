namespace Things.Framework.ECS;

using System;
using System.Runtime.CompilerServices;
using Things.Framework.Containers;
using EntityId = uint;


public readonly record struct Entity(EntityId Id)
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator EntityId(in Entity value) => value.Id;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int AsIndex() => (int)Id - 1; // @note zero is nil

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsValid() => Id > 0; // @note zero is nil

	public sealed class Pool : IDisposable
	{
		private readonly NativeArray<Entity> free_list = new();
		private          EntityId            counter   = 0; // @note zero is nil
		private bool is_disposed;

		public Entity Acquire()
		{
			if (free_list.TryPop(out var free_entity))
				return free_entity;
			return new Entity(counter += 1); // @note zero is nil
		}

		public void Release(in Entity entity)
		{
			free_list.Push(entity);
		}

		void IDisposable.Dispose()
		{
			if (!is_disposed) return;
			((IDisposable)free_list).Dispose();
			is_disposed = true;
			GC.SuppressFinalize(this);
		}
	}
}
