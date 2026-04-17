namespace Things.Framework.ECS;

using System;
using Things.Framework.Containers;


public sealed class Filter(World world, in Filter.Signature signature) : IDisposable
{
	private readonly World             world     = world;
	private readonly Signature         signature = signature;
	private readonly SparseSet<Entity> entities  = new();
	private bool is_disposed;

	public ReadOnlySpan<Entity> AsSpan() => entities.GetKeys();

	public ReadOnlySpan<Type> DebugIncludes() => signature.Include.GetKeys();
	public ReadOnlySpan<Type> DebugExcludes() => signature.Exclude.GetKeys();

	public bool Check(in Entity entity)
	{
		foreach (var type in signature.Include.GetKeys())
			if (!world.HasComponent(in entity, in type))
				return entities.Remove(in entity);

		foreach (var type in signature.Exclude.GetKeys())
			if (world.HasComponent(in entity, in type))
				return entities.Remove(in entity);

		return entities.SetKey(in entity);
	}

	public bool Remove(in Entity entity)
		=> entities.Remove(in entity);

	void IDisposable.Dispose()
	{
		if (!is_disposed) return;
		((IDisposable)signature.Include).Dispose();
		((IDisposable)signature.Exclude).Dispose();
		is_disposed = true;
		GC.SuppressFinalize(this);
	}

	public readonly struct Signature() : IEquatable<Signature>
	{
		public readonly SparseSet<Type> Include = new();
		public readonly SparseSet<Type> Exclude = new();

		public bool Equals(Signature other)
		{
			foreach (var it in Include.GetKeys())
				if (!other.Include.Has(it))
					return false;
			foreach (var it in Exclude.GetKeys())
				if (other.Exclude.Has(it))
					return false;
			return true;
		}

		public override bool Equals(object? obj)
			=> obj is Signature signature && Equals(signature);

		public override int GetHashCode()
		{
			int ret = HashCode.Combine(0);
			foreach (var it in Include.GetKeys())
				ret = HashCode.Combine(ret, it);
			foreach (var it in Exclude.GetKeys())
				ret = HashCode.Combine(ret, it);
			return ret;
		}

		public static bool operator ==(Signature left, Signature right)
			=> left.Equals(right);

		public static bool operator !=(Signature left, Signature right)
			=> !(left == right);
	}

	public readonly struct Builder(World world)
	{
		private readonly World world = world;
		private readonly Signature signature = new();

		public Builder Include<T>() where T : unmanaged
		{
			var component_type = world.EnsureComponentStorage<T>();
			signature.Include.SetKey(component_type);
			return this;
		}

		public Builder Exclude<T>() where T : unmanaged
		{
			var component_type = world.EnsureComponentStorage<T>();
			signature.Exclude.SetKey(component_type);
			return this;
		}

		public Filter Build()
		{
			return world.GetFilter(in signature);
		}
	}
}
