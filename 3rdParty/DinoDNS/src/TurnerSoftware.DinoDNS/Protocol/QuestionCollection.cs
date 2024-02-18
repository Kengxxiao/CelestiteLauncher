using System.Collections;
using TurnerSoftware.DinoDNS.Internal;

namespace TurnerSoftware.DinoDNS.Protocol;

public readonly struct QuestionCollection : IEquatable<QuestionCollection>
{
	private readonly int ItemCount;
	private readonly SeekableReadOnlyMemory<byte> ByteValue;
	private readonly Question[] ArrayValue;

	public QuestionCollection(Question[] value)
	{
		ItemCount = value.Length;
		ByteValue = ReadOnlyMemory<byte>.Empty;
		ArrayValue = value;
	}

	public QuestionCollection(SeekableReadOnlyMemory<byte> value, int itemCount)
	{
		ItemCount = itemCount;
		ByteValue = value;
		ArrayValue = Array.Empty<Question>();
	}

	public int Count => ItemCount;

	public bool Equals(QuestionCollection other)
	{
		if (Count != other.Count)
		{
			return false;
		}

		var enumerator = GetEnumerator();
		var otherEnumerator = other.GetEnumerator();

		while (enumerator.MoveNext() && otherEnumerator.MoveNext())
		{
			if (!enumerator.Current.Equals(otherEnumerator.Current))
			{
				return false;
			}
		}

		return true;
	}

	public override bool Equals(object? obj) => obj is QuestionCollection collection && Equals(collection);

	public override int GetHashCode() => ItemCount.GetHashCode();

	public Enumerator GetEnumerator() => new(this);

	public static implicit operator QuestionCollection(Question[] value) => new(value);
	public static bool operator ==(in QuestionCollection left, in QuestionCollection right) => left.Equals(right);
	public static bool operator !=(in QuestionCollection left, in QuestionCollection right) => !(left == right);

	public struct Enumerator : IEnumerator<Question>
	{
		private readonly QuestionCollection Value;
		private int Index;
		private DnsProtocolReader Reader;

		internal Enumerator(QuestionCollection collection)
		{
			Value = collection;
			Index = 0;
			Current = default;
			Reader = !Value.ByteValue.EndOfData ? new DnsProtocolReader(Value.ByteValue) : default;
		}

		public Question Current { get; private set; }

		readonly object IEnumerator.Current => Current;

		public bool MoveNext()
		{
			if (Index == Value.ItemCount)
			{
				return false;
			}

			if (!Value.ByteValue.EndOfData)
			{
				Reader = Reader.ReadQuestion(out var question);
				Current = question;
			}
			else
			{
				Current = Value.ArrayValue[Index];
			}

			Index++;
			return true;
		}

		public void Reset()
		{
			Index = 0;
			Current = default;
			Reader = !Value.ByteValue.EndOfData ? new DnsProtocolReader(Value.ByteValue) : default;
		}

		public readonly void Dispose()
		{
		}
	}
}
