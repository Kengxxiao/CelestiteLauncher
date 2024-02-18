using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS;

public sealed class DnsHostsFile
{
	private readonly ConcurrentDictionary<DnsRawValue, ReadOnlyMemory<byte>> HostLookup = new(DnsRawValue.CaseInsensitiveComparer);

	public int Count => HostLookup.Count;

	public void Add(DnsRawValue host, ReadOnlyMemory<byte> address)
	{
		HostLookup[host] = address;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(DnsRawValue host, ReadOnlySpan<char> address)
	{
		//TODO: Look to reduce allocations here
		//		It will need to allocate _something_ but ideally not much
		HostLookup[host] = IPAddress.Parse(address).GetAddressBytes();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryGetAddress(DnsRawValue host, [MaybeNullWhen(false)] out ReadOnlyMemory<byte> address) => HostLookup.TryGetValue(host, out address);

	public bool TryGetAddress(in LabelSequence host, [MaybeNullWhen(false)] out ReadOnlyMemory<byte> address)
	{
		var rentedBuffer = ArrayPool<byte>.Shared.Rent(host.GetSequenceByteLength());
		try
		{
			var buffer = rentedBuffer.AsMemory();
			if (host.TryWriteUnencodedBytes(buffer.Span, out var bytesWritten))
			{
				//Trim the trailing dot as hosts file entries don't contain it
				var parsedHost = new DnsRawValue(buffer[..(bytesWritten - 1)]);
				return TryGetAddress(parsedHost, out address);
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(rentedBuffer);
		}
		address = default;
		return false;
	}

	public static DnsHostsFile FromString(ReadOnlySpan<char> hostsFileContent)
	{
		var result = new DnsHostsFile();
		var tokenReader = new DnsHostsTokenReader(hostsFileContent);
		var state = ReadState.None;
		ReadOnlySpan<char> address = default;
		while (tokenReader.NextToken(out var token))
		{
			switch (state)
			{
				case ReadState.None:
					if (token.TokenType == HostsTokenType.Identifier)
					{
						address = token.Value;
						state = ReadState.Hosts;
					}
					continue;
				case ReadState.Hosts:
					if (token.TokenType == HostsTokenType.Identifier)
					{
						result.Add(token.Value.ToString(), address);
					}
					else if (token.TokenType != HostsTokenType.Whitespace)
					{
						state = ReadState.None;
					}
					continue;
			}
		}
		return result;
	}

	private enum ReadState
	{
		None,
		Hosts
	}
}