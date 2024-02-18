using System.Buffers;

namespace TurnerSoftware.DinoDNS.Connection;

public readonly struct TransitData
{
	private readonly int MessageSize;
	private readonly byte[] RentedBytes;

	private TransitData(int messageSize, byte[] rentedBytes)
	{
		MessageSize = messageSize;
		RentedBytes = rentedBytes;
	}

	public static TransitData Rent(DnsMessageOptions options)
	{
		var messageSize = options.MaximumMessageSize;
		var rentedBytes = ArrayPool<byte>.Shared.Rent(messageSize * 2);
		return new(messageSize, rentedBytes);
	}

	public static void Return(TransitData transitData)
	{
		ArrayPool<byte>.Shared.Return(transitData.RentedBytes);
	}

	public Memory<byte> RequestBuffer => RentedBytes.AsMemory(0, MessageSize);
	public Memory<byte> ResponseBuffer => RentedBytes.AsMemory(MessageSize, MessageSize);

	public void Deconstruct(out Memory<byte> requestBuffer, out Memory<byte> responseBuffer)
	{
		requestBuffer = RequestBuffer;
		responseBuffer = ResponseBuffer;
	}
}
