using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace TurnerSoftware.DinoDNS.Protocol;

public readonly struct DnsRawValue : IEquatable<DnsRawValue>
{
	private const int MaxStackSize = 256;

	internal readonly ReadOnlyMemory<byte> ByteValue;
	internal readonly ReadOnlyMemory<char> CharValue;

	public DnsRawValue(ReadOnlyMemory<byte> value)
	{
		ByteValue = value;
		CharValue = default;
	}

	public DnsRawValue(ReadOnlyMemory<char> value)
	{
		ByteValue = default;
		CharValue = value;
	}

	//Only one value will have a length so adding the length values together skips a branch.
	public int Length => ByteValue.Length + CharValue.Length;

	public bool TryWriteBytes(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length < Length)
		{
			bytesWritten = 0;
			return false;
		}

		if (!ByteValue.IsEmpty)
		{
			ByteValue.Span.CopyTo(destination);
			bytesWritten = ByteValue.Length;
			return true;
		}

		bytesWritten = Encoding.ASCII.GetBytes(CharValue.Span, destination);
		return true;
	}

	public bool Equals(ReadOnlySpan<char> other) => EqualsHelper(other, false);
	[SkipLocalsInit]
	private bool EqualsHelper(ReadOnlySpan<char> other, bool ignoreCase)
	{
		if (!ByteValue.IsEmpty)
		{
			var length = Length;
			byte[]? rentedArray = null;
			Span<byte> buffer = length <= MaxStackSize ?
				stackalloc byte[MaxStackSize] :
				(rentedArray = ArrayPool<byte>.Shared.Rent(length));

			try
			{
				var asciiBytes = Encoding.ASCII.GetBytes(other, buffer);
				return ignoreCase ? ByteValue.Span.SequenceEqual(buffer[..asciiBytes], CaseInsensitiveByteComparer.Instance)
					: ByteValue.Span.SequenceEqual(buffer[..asciiBytes]);
			}
			finally
			{
				if (rentedArray is not null)
				{
					ArrayPool<byte>.Shared.Return(rentedArray);
				}
			}
		}

		return CharValue.Span.Equals(
			other,
			ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal
		);
	}

	public bool Equals(ReadOnlySpan<byte> other) => EqualsHelper(other, false);
	[SkipLocalsInit]
	private bool EqualsHelper(ReadOnlySpan<byte> other, bool ignoreCase)
	{
		if (!CharValue.IsEmpty)
		{
			var length = Length;
			byte[]? rentedArray = null;
			Span<byte> buffer = length <= MaxStackSize ?
				stackalloc byte[MaxStackSize] :
				(rentedArray = ArrayPool<byte>.Shared.Rent(length));

			try
			{
				var asciiBytes = Encoding.ASCII.GetBytes(CharValue.Span, buffer);
				return ignoreCase ? buffer[..asciiBytes].SequenceEqual(other, CaseInsensitiveByteComparer.Instance)
					: buffer[..asciiBytes].SequenceEqual(other);
			}
			finally
			{
				if (rentedArray is not null)
				{
					ArrayPool<byte>.Shared.Return(rentedArray);
				}
			}
		}

		return ignoreCase ? ByteValue.Span.SequenceEqual(other, CaseInsensitiveByteComparer.Instance)
			: ByteValue.Span.SequenceEqual(other);
	}

	public bool Equals(DnsRawValue other) => EqualsHelper(other, false);
	private bool EqualsHelper(DnsRawValue other, bool ignoreCase)
	{
		if (!other.ByteValue.IsEmpty)
		{
			return EqualsHelper(other.ByteValue.Span, ignoreCase);
		}
		else if (!other.CharValue.IsEmpty)
		{
			return EqualsHelper(other.CharValue.Span, ignoreCase);
		}
		return Length == 0;
	}

	public override bool Equals(object? obj) => obj is DnsRawValue value && Equals(value);

	public override int GetHashCode() => GetHashCode(false);
	[SkipLocalsInit]
	private int GetHashCode(bool ignoreCase)
	{
		if (!ByteValue.IsEmpty)
		{
			var length = Length;
			char[]? rentedArray = null;
			Span<char> buffer = length <= MaxStackSize ?
				stackalloc char[MaxStackSize] :
				(rentedArray = ArrayPool<char>.Shared.Rent(length));

			try
			{
				var asciiBytes = Encoding.ASCII.GetChars(ByteValue.Span, buffer);
				return string.GetHashCode(
					buffer[..asciiBytes],
					ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal
				);
			}
			finally
			{
				if (rentedArray is not null)
				{
					ArrayPool<char>.Shared.Return(rentedArray);
				}
			}
		}

		return string.GetHashCode(
			CharValue.Span,
			ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal
		);
	}

	public ReadOnlyMemory<byte> ToBytes()
	{
		if (!ByteValue.IsEmpty)
		{
			return ByteValue;
		}
		else if (!CharValue.IsEmpty)
		{
			var result = new byte[Length];
			Encoding.ASCII.GetBytes(CharValue.Span, result);
			return result;
		}
		return ReadOnlyMemory<byte>.Empty;
	}

	public override string ToString()
	{
		if (!CharValue.IsEmpty)
		{
			return CharValue.ToString();
		}
		else if (!ByteValue.IsEmpty)
		{
			return Encoding.ASCII.GetString(ByteValue.Span);
		}

		return string.Empty;
	}

	public static implicit operator DnsRawValue(string value) => new(value.AsMemory());
	public static implicit operator DnsRawValue(ReadOnlyMemory<char> value) => new(value);
	public static implicit operator DnsRawValue(ReadOnlyMemory<byte> value) => new(value);

	public static bool operator ==(in DnsRawValue left, in DnsRawValue right) => left.Equals(right);
	public static bool operator !=(in DnsRawValue left, in DnsRawValue right) => !(left == right);

	private class CaseInsensitiveByteComparer : IEqualityComparer<byte>
	{
		public static readonly CaseInsensitiveByteComparer Instance = new();

		public bool Equals(byte x, byte y) => x == y ||
			//Check if x is lowercase, making it upper case for a second comparison
			//"a" = 97, "A" = 65
			(x >= 'a' && x <= 'z' && (x - ('a' - 'A') == y));

		public int GetHashCode([DisallowNull] byte obj) => (obj >= 'a' && obj <= 'z') ? obj - ('a' - 'A') : obj;
	}

	public static readonly IEqualityComparer<DnsRawValue> DefaultComparer = new DefaultComparerImpl();
	public static readonly IEqualityComparer<DnsRawValue> CaseInsensitiveComparer = new CaseInsensitiveComparerImpl();
	private class DefaultComparerImpl : IEqualityComparer<DnsRawValue>
	{
		public bool Equals(DnsRawValue x, DnsRawValue y) => x.Equals(y);
		public int GetHashCode([DisallowNull] DnsRawValue obj) => obj.GetHashCode();
	}
	private class CaseInsensitiveComparerImpl : IEqualityComparer<DnsRawValue>
	{
		public bool Equals(DnsRawValue x, DnsRawValue y) => x.EqualsHelper(y, true);

		public int GetHashCode([DisallowNull] DnsRawValue obj) => obj.GetHashCode(true);
	}
}
