namespace TurnerSoftware.DinoDNS;

public ref struct DnsHostsTokenReader
{
	private const char EndOfFile = char.MinValue;

	private readonly ReadOnlySpan<char> Value;
	private int Index;

	public DnsHostsTokenReader(ReadOnlySpan<char> value)
	{
		Value = value;
		Index = 0;
	}

	private char Current
	{
		get
		{
			if (Index < Value.Length)
			{
				return Value[Index];
			}

			return EndOfFile;
		}
	}

	private char Peek()
	{
		if (Index + 1 < Value.Length)
		{
			return Value[Index + 1];
		}
		return EndOfFile;
	}

	private void ReadNext() => Index++;

	public bool NextToken(out DnsHostsToken token)
	{
		if (Current == EndOfFile)
		{
			token = default;
			return false;
		}

		token = Current switch
		{
			'#' => ReadComment(),
			' ' or '\t' => ReadWhitespace(),
			'\r' or '\n' => ReadNewLine(),
			_ => ReadIdentifier(),
		};
		return true;
	}

	private DnsHostsToken CreateToken(HostsTokenType tokenType, int startIndex)
	{
		var value = Value[startIndex..Index];
		var token = new DnsHostsToken(tokenType, value);
		return token;
	}

	private DnsHostsToken ReadComment()
	{
		var startIndex = Index;
		while (true)
		{
			ReadNext();
			switch (Current)
			{
				case EndOfFile:
				case '\r':
				case '\n':
					return CreateToken(HostsTokenType.Comment, startIndex);
			}
		}
	}

	private DnsHostsToken ReadWhitespace()
	{
		var startIndex = Index;
		while (true)
		{
			ReadNext();
			switch (Current)
			{
				case ' ':
				case '\t':
					ReadNext();
					continue;
				default:
					return CreateToken(HostsTokenType.Whitespace, startIndex);
			}
		}
	}

	private DnsHostsToken ReadNewLine()
	{
		var startIndex = Index;
		if (Current == '\r' && Peek() == '\n')
		{
			ReadNext();
		}
		ReadNext();
		return CreateToken(HostsTokenType.NewLine, startIndex);
	}

	private DnsHostsToken ReadIdentifier()
	{
		var startIndex = Index;
		while (true)
		{
			ReadNext();
			switch (Current)
			{
				case EndOfFile:
				case '\r':
				case '\n':
				case ' ':
				case '\t':
				case '#':
					return CreateToken(HostsTokenType.Identifier, startIndex);
			}
		}
	}
}

public readonly ref struct DnsHostsToken
{
	public readonly HostsTokenType TokenType;
	public readonly ReadOnlySpan<char> Value;

	public DnsHostsToken(HostsTokenType tokenType, ReadOnlySpan<char> value)
	{
		TokenType = tokenType;
		Value = value;
	}
}

public enum HostsTokenType
{
	NewLine,
	Identifier,
	Whitespace,
	Comment
}
