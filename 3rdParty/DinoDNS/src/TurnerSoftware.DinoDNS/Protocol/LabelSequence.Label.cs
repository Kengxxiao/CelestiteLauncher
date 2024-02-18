namespace TurnerSoftware.DinoDNS.Protocol;

public readonly partial struct LabelSequence
{
	public readonly struct Label : IEquatable<Label>
	{
		public const int MaxLength = 63;

		public readonly DnsRawValue Value;
		public readonly bool FromPointerOffset;

		public Label(DnsRawValue value, bool fromPointerOffset)
		{
			if (value.Length > MaxLength)
			{
				throw new ArgumentException($"The max length of a label is {MaxLength} characters.");
			}

			Value = value;
			FromPointerOffset = fromPointerOffset;
		}

		public int Length => Value.Length;

		public bool Equals(Label other) => DnsRawValue.CaseInsensitiveComparer.Equals(Value, other.Value) &&
			FromPointerOffset == other.FromPointerOffset;

		public override bool Equals(object? obj) => obj is Label label && Equals(label);

		public override int GetHashCode() => HashCode.Combine(
			DnsRawValue.CaseInsensitiveComparer.GetHashCode(Value),
			FromPointerOffset
		);

		public override string ToString() => Value.ToString();

		public static implicit operator Label(string source) => new(source.AsMemory(), false);
		public static implicit operator Label(ReadOnlyMemory<char> source) => new(source, false);
		public static bool operator ==(in Label left, in Label right) => left.Equals(right);
		public static bool operator !=(in Label left, in Label right) => !(left == right);
	}
}
