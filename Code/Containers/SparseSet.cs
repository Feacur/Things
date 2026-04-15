namespace Things.Code.Containers;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Size = int;


public sealed class SparseSet<TKey>(Size type_size = 0) : IDisposable where TKey : unmanaged
{
	private readonly NativeArray           payload = new(type_size: type_size);
	private readonly NativeArray<TKey>     packed  = new();
	private readonly Dictionary<TKey, int> sparse  = []; // @note array with gaps is possible but can be wasteful
	private bool is_disposed;

	public int Count => packed.Count;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<TKey> GetKeys() => packed.AsSpan();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlyReverseSpan<TKey> GetReverseKeys() => packed.AsReverseSpan();

	public bool Has(in TKey key) => sparse.ContainsKey(key);

	public ref TValue Get<TValue>(TKey key) where TValue : unmanaged
	{
		var index = sparse[key];
		return ref payload.Get<TValue>(index);
	}

	public ref TValue Get<TValue>(in TKey key) where TValue : unmanaged
	{
		var index = sparse[key];
		return ref payload.Get<TValue>(index);
	}

	public bool SetKey(in TKey key)
	{
		if (sparse.TryGetValue(key, out _))
			return true;
		var set_index = Count;
		payload.PushEmpty();
		packed.Push(in key);
		sparse.Add(key, set_index);
		return false;
	}

	public bool Set<TValue>(in TKey key, in TValue value) where TValue : unmanaged
	{
		if (sparse.TryGetValue(key, out var sparse_index))
		{
			payload.Get<TValue>(sparse_index) = value;
			return true;
		}
		var set_index = Count;
		payload.Push(in value);
		packed.Push(in key);
		sparse.Add(key, set_index);
		return false;
	}

	public bool Remove(in TKey key)
	{
		if (sparse.TryGetValue(key, out var remove_index))
		{
			payload.Remove(remove_index);
			packed.Remove(remove_index);
			sparse.Remove(key);
			var last_index = Count;
			if (remove_index < last_index)
			{ // @note `NativeArray` moves the last element in place of the removed one
				var last_key = packed[last_index];
				sparse[last_key] = remove_index;
			}
			return true;
		}
		return false;
	}

	public void Clear()
	{
		payload.Clear();
		packed.Clear();
		sparse.Clear();
	}

	void IDisposable.Dispose()
	{
		if (!is_disposed) return;
		((IDisposable)payload).Dispose();
		((IDisposable)packed).Dispose();
		is_disposed = true;
		GC.SuppressFinalize(this);
	}
}
