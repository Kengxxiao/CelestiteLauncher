using System.Runtime.CompilerServices;

namespace TurnerSoftware.DinoDNS.Internal;

/// <summary>
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct SeekableMemory<T>
{
	public readonly Memory<T> Source;
	public readonly int Offset;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public SeekableMemory(in Memory<T> source)
	{
		Source = source;
		Offset = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public SeekableMemory(in Memory<T> source, int offset)
	{
		Source = source;
		Offset = offset;
	}

	public bool EndOfData => Offset == Source.Length;
	public int Remaining => Source.Length - Offset;

	public Span<T> Span => Source.Span[Offset..];
	public Memory<T> Memory => Source[Offset..];
	public ref T this[Index index] => ref Span[index];
	public Memory<T> this[Range range] => Memory[range];
	public ref T Current => ref this[0];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public SeekableMemory<T> Seek(int offset) => new(Source, offset);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public SeekableMemory<T> SeekRelative(int offsetAdjustment) => new(Source, Offset + offsetAdjustment);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public SeekableMemory<T> Read(out T value)
	{
		value = Current;
		return SeekRelative(1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public SeekableMemory<T> ReadNext(int count, out Memory<T> value)
	{
		value = this[..count];
		return SeekRelative(count);
	}

	public static implicit operator SeekableMemory<T>(in Memory<T> source) => new(in source);
}

public readonly struct SeekableReadOnlyMemory<T>
{
	public readonly ReadOnlyMemory<T> Source;
	public readonly int Offset;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public SeekableReadOnlyMemory(in ReadOnlyMemory<T> source)
	{
		Source = source;
		Offset = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public SeekableReadOnlyMemory(in ReadOnlyMemory<T> source, int offset)
	{
		Source = source;
		Offset = offset;
	}

	public bool EndOfData => Offset == Source.Length;
	public int Remaining => Source.Length - Offset;

	public ReadOnlySpan<T> Span => Source.Span[Offset..];
	public ReadOnlyMemory<T> Memory => Source[Offset..];
	public ref readonly T this[Index index] => ref Span[index];
	public ReadOnlyMemory<T> this[Range range] => Memory[range];
	public ref readonly T Current => ref this[0];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly SeekableReadOnlyMemory<T> Seek(int offset) => new(in Source, offset);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly SeekableReadOnlyMemory<T> SeekRelative(int offsetAdjustment) => new(in Source, Offset + offsetAdjustment);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public SeekableReadOnlyMemory<T> Read(out T value)
	{
		value = Current;
		return SeekRelative(1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public SeekableReadOnlyMemory<T> ReadNext(int count, out ReadOnlyMemory<T> value)
	{
		value = this[..count];
		return SeekRelative(count);
	}

	public static implicit operator SeekableReadOnlyMemory<T>(Memory<T> source) => new(source);
	public static implicit operator SeekableReadOnlyMemory<T>(in ReadOnlyMemory<T> source) => new(in source);
	public static implicit operator ReadOnlySpan<T>(in SeekableReadOnlyMemory<T> source) => source.Span;
	public static implicit operator ReadOnlyMemory<T>(in SeekableReadOnlyMemory<T> source) => source.Memory;
}