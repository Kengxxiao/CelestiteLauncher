namespace TurnerSoftware.DinoDNS.Tests;

internal static class DnsProtocolWriterExtensions
{
	public static byte[] EndLabelSequence(this DnsProtocolWriter protocolWriter)
	{
		return protocolWriter.AppendLabelSequenceEnd().GetWrittenBytes().ToArray();
	}
}
