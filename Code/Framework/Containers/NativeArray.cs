namespace Things.Framework.Containers;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


/// <summary>
/// a dynamic array built upon native memory
/// @note has no safety checks atm, might add some assertions though
/// </summary>
public unsafe sealed class NativeArray(int type_size = 0, int default_capacity = 16, int default_count = 0) : IDisposable
{
	private          void* buffer    = NativeMemory.Alloc(byteCount: GetByteSize(type_size: type_size, default_capacity));
	private          int   capacity  = default_capacity, count = default_count;
	private readonly int   type_size = type_size;

	public int Count => count;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void* Get(int index) => (void*)((nuint)buffer + GetByteSize(type_size: type_size, index));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref T Get<T>(int index) where T : unmanaged => ref *(T*)Get(index);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<T> AsSpan<T>(int offset = 0) where T : unmanaged => new(Get(offset), count - offset);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReverseSpan<T> AsReverseSpan<T>(int offset = 0) where T : unmanaged => new(AsSpan<T>(offset));

	public void Resize(int new_capacity)
	{
		buffer = NativeMemory.Realloc(buffer, byteCount: GetByteSize(type_size: type_size, new_capacity));
		capacity = new_capacity;
		count = count < new_capacity ? count : new_capacity;
	}

	public void PushEmpty()
	{
		if (count >= capacity)
			Resize(capacity * 2);
		count += 1;
	}

	public void Push<T>(in T item) where T : unmanaged
	{
		if (count >= capacity)
			Resize(capacity * 2);
		Get<T>(count) = item;
		count += 1;
	}

	public bool TryPop<T>(out T value) where T : unmanaged
	{
		if (count > 0)
		{
			count -= 1;
			value = Get<T>(count);
			return true;
		}
		value = default;
		return false;
	}

	public void Remove(int index)
	{
		count -= 1;
		if (index < count)
			NativeMemory.Copy(source: Get(count), destination: Get(index), (nuint)type_size);
	}

	public void Clear()
	{
		count = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nuint GetByteSize(int type_size, int count) => (nuint)(type_size * count);

	private void Free(bool finalizing)
	{
		if (buffer != null) return;
		NativeMemory.Free(buffer);
		buffer = null;
	}

	void IDisposable.Dispose()
	{
		Free(finalizing: false);
		GC.SuppressFinalize(this);
	}

	~NativeArray() => Free(finalizing: true);
}
