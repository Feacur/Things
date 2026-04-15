namespace Things.Code.ECS;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TypeId = ushort;
using Size = int;


public readonly record struct Type(TypeId Id)
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TypeId(in Type value) => value.Id;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int AsIndex() => (int)Id - 1; // @note zero is nil

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsValid() => Id > 0; // @note zero is nil

	public sealed class Identity<TKind> where TKind : unmanaged
	{
		private static readonly List<Size> sizes   = [0]; // @note zero is nil
		private static          TypeId     counter = 0; // @note zero is nil

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Type Get<TInstance>() where TInstance : unmanaged => Generic<TInstance>.Type;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Size SizeOf<TInstance>() where TInstance : unmanaged => Generic<TInstance>.Size;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Size SizeOf(in Type type) => sizes[type];

		private Identity() { /*is not meant to be*/ }

		private sealed class Generic<TInstance> where TInstance : unmanaged
		{
			public readonly static Type Type;
			public readonly static Size Size;

			private Generic() { /*is not meant to be*/ }

			static Generic()
			{
				Type = new Type(counter += 1); // @note zero is nil
				Size = Unsafe.SizeOf<TInstance>();
				sizes.Add(Size);
			}
		}
	}
}
