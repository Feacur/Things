namespace Things.Framework.Containers;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


/// <summary>
/// a dynamic array built upon native memory
/// @note has no safety checks atm, might add some assertions though
/// also, there's no proven reason to use this over a `T[]` and a `List<T>`
/// performance wise in this particular case; I can imagine it being less GC-heavy maybe
/// and the lack of safety somewhat helps too, I dunno - otherwise it's just a study.
/// one thing definite is that the indexer return by a ref
/// </summary>
public unsafe sealed class NativeArray<T>(int default_capacity = 16) : IDisposable where T : unmanaged
{
	private T*  buffer = (T*)NativeMemory.Alloc(byteCount: GetByteSize(default_capacity));
	private int capacity = default_capacity, count = 0;

	public int Count => count;
	public ref T this[int index] => ref buffer[index];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<T> AsSpan() => new(buffer, count);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReverseSpan<T> AsReverseSpan() => new(AsSpan());

	public void Resize(int new_capacity)
	{
		buffer = (T*)NativeMemory.Realloc(buffer, byteCount: GetByteSize(new_capacity));
		capacity = new_capacity;
		count = count < new_capacity ? count : new_capacity;
	}

	public void PushEmpty()
	{
		if (count >= capacity)
			Resize(capacity * 2);
		count += 1;
	}

	public void Push(in T item)
	{
		if (count >= capacity)
			Resize(capacity * 2);
		buffer[count] = item;
		count += 1;
	}

	public bool TryPop(out T value)
	{
		if (count > 0)
		{
			count -= 1;
			value = buffer[count];
			return true;
		}
		value = default;
		return false;
	}

	public void Remove(int index)
	{
		count -= 1;
		if (index < count)
			buffer[index] = buffer[count];
	}

	public void Clear()
	{
		count = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int TypeSize() => Unsafe.SizeOf<T>();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nuint GetByteSize(int count) => (nuint)(TypeSize() * count);

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
