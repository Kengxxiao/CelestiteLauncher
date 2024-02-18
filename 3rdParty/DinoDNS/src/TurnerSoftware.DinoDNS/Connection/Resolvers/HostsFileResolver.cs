using System.Net;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Connection.Resolvers;

public class HostsFileResolver : IDnsResolver
{
	private readonly DnsHostsFile HostsFile;
	private readonly uint TimeToLive = 3600;

	public HostsFileResolver(DnsHostsFile hostsFile)
	{
		HostsFile = hostsFile;
	}

	public ValueTask<int> SendMessageAsync(IPEndPoint endPoint, ReadOnlyMemory<byte> requestBuffer, Memory<byte> responseBuffer, CancellationToken cancellationToken)
	{
		new DnsProtocolReader(requestBuffer)
			.ReadMessage(out var request);

		int bytesWritten;

		if (request.Header.Flags.Opcode == Opcode.Query)
		{
			var questions = request.Questions.GetEnumerator();
			if (questions.MoveNext())
			{
				var question = questions.Current;
				if (question.Type == DnsQueryType.A || question.Type == DnsQueryType.AAAA)
				{
					if (HostsFile.TryGetAddress(question.Query, out var address))
					{
						DnsType addressType = address.Length switch
						{
							4 => DnsType.A,
							16 => DnsType.AAAA,
							_ => 0,
						};

						if ((int)question.Type == (int)addressType)
						{
							var answer = new ResourceRecord
							{
								DomainName = question.Query,
								Type = addressType,
								Class = DnsClass.IN,
								TimeToLive = TimeToLive,
								ResourceDataLength = (ushort)address.Length,
								Data = address
							};

							bytesWritten = new DnsProtocolWriter(responseBuffer)
								.AppendHeader(
									Header.CreateResponseHeader(
										request.Header,
										ResponseCode.NOERROR,
										authoritativeAnswer: AuthoritativeAnswer.Yes,
										answerRecordCount: 1
									)
								)
								.AppendQuestion(in question)
								.AppendResourceRecord(in answer)
								.BytesWritten;
							return ValueTask.FromResult(bytesWritten);
						}
					}

					bytesWritten = DnsMessage.CreateResponse(
						in request,
						ResponseCode.NOERROR,
						RecursionAvailable.No
					).WriteTo(responseBuffer);
					return ValueTask.FromResult(bytesWritten);
				}
			}
		}

		bytesWritten = DnsMessage.CreateResponse(
			in request,
			ResponseCode.NOTIMP,
			RecursionAvailable.No
		).WriteTo(responseBuffer);
		return ValueTask.FromResult(bytesWritten);
	}
}
