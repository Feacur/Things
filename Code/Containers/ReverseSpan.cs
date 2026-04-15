namespace Things.Code.Containers;

using System;
using System.Runtime.CompilerServices;

public readonly ref struct ReverseSpan<T>(Span<T> buffer) where T : unmanaged
{
	private readonly Span<T> buffer = buffer;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator ReadOnlyReverseSpan<T>(ReverseSpan<T> value) => new(value.buffer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Enumerator GetEnumerator() => new(buffer);

	public ref struct Enumerator(Span<T> buffer)
	{
		private readonly Span<T> buffer = buffer;
		private          int     index  = buffer.Length;

		public readonly ref T Current => ref buffer[index];

		public bool MoveNext()
		{
			if (index > 0)
			{
				index -= 1;
				return true;
			}
			return false;
		}
	}
}

public readonly ref struct ReadOnlyReverseSpan<T>(ReadOnlySpan<T> buffer) where T : unmanaged
{
	private readonly ReadOnlySpan<T> buffer = buffer;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Enumerator GetEnumerator() => new(buffer);

	public ref struct Enumerator(ReadOnlySpan<T> buffer)
	{
		private readonly ReadOnlySpan<T> buffer = buffer;
		private          int             index  = buffer.Length;

		public readonly T Current => buffer[index];

		public bool MoveNext()
		{
			if (index > 0)
			{
				index -= 1;
				return true;
			}
			return false;
		}
	}
}
