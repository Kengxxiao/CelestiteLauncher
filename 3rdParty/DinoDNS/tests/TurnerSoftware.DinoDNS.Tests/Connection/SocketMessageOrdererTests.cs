using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurnerSoftware.DinoDNS.Connection;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Tests.Connection;

[TestClass]
public class SocketMessageOrdererTests
{
	[TestMethod]
	public void CheckMessageId_Mixed()
	{
		var requestBytes = new DnsProtocolWriter(new byte[512].AsMemory())
			.AppendMessage(DnsMessage.CreateQuery(1)
				.WithQuestions(new Question[]
				{
					new()
					{
						Query = new LabelSequence("test.www.example.org"),
						Type = DnsQueryType.A,
						Class = DnsClass.IN
					}
				})
			)
			.GetWrittenBytes();

		var responseBytes = new DnsProtocolWriter(new byte[512].AsMemory())
			.AppendMessage(
				DnsMessage.CreateResponse(
					DnsMessage.CreateQuery(2).WithQuestions(new Question[]
					{
						new()
						{
							Query = new LabelSequence("www.example.org"),
							Type = DnsQueryType.A,
							Class = DnsClass.IN
						}
					}),
					ResponseCode.NOERROR
				)
			)
			.GetWrittenBytes();

		var result = SocketMessageOrderer.CheckMessageId(requestBytes, responseBytes);
		Assert.AreEqual(MessageIdResult.Mixed, result);
	}

	[TestMethod]
	public void CheckMessageId_Matched()
	{
		var requestBytes = new DnsProtocolWriter(new byte[512].AsMemory())
			.AppendMessage(DnsMessage.CreateQuery(1)
				.WithQuestions(new Question[]
				{
					new()
					{
						Query = new LabelSequence("test.www.example.org"),
						Type = DnsQueryType.A,
						Class = DnsClass.IN
					}
				})
			)
			.GetWrittenBytes();

		var responseBytes = new DnsProtocolWriter(new byte[512].AsMemory())
			.AppendMessage(
				DnsMessage.CreateResponse(
					DnsMessage.CreateQuery(1).WithQuestions(new Question[]
					{
						new()
						{
							Query = new LabelSequence("test.www.example.org"),
							Type = DnsQueryType.A,
							Class = DnsClass.IN
						}
					}),
					ResponseCode.NOERROR
				)
			)
			.GetWrittenBytes();

		var result = SocketMessageOrderer.CheckMessageId(requestBytes, responseBytes);
		Assert.AreEqual(MessageIdResult.Matched, result);
	}


	[TestMethod]
	public async Task Exchange_Success()
	{
		var request1 = DnsMessage.CreateQuery(1)
			.WithQuestions(new Question[]
			{
				new()
				{
					Query = new LabelSequence("ww1.example.org"),
					Type = DnsQueryType.A,
					Class = DnsClass.IN
				}
			});
		var request1Buffer = new DnsProtocolWriter(new byte[512].AsMemory()).AppendMessage(request1).GetWrittenBytes();
		var response1 = DnsMessage.CreateResponse(request1, ResponseCode.NOERROR);
		var response1Buffer = new byte[512].AsMemory();
		var response1Length = new DnsProtocolWriter(response1Buffer).AppendMessage(response1).BytesWritten;

		var request2 = DnsMessage.CreateQuery(2)
			.WithQuestions(new Question[]
			{
				new()
				{
					Query = new LabelSequence("ww2.example.org"),
					Type = DnsQueryType.A,
					Class = DnsClass.IN
				}
			});
		var request2Buffer = new DnsProtocolWriter(new byte[512].AsMemory()).AppendMessage(request2).GetWrittenBytes();
		var response2 = DnsMessage.CreateResponse(request2, ResponseCode.NOERROR);
		var response2Buffer = new byte[512].AsMemory();
		var response2Length = new DnsProtocolWriter(response2Buffer).AppendMessage(response2).BytesWritten;

		using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		var request1Task = Task.Run(() => SocketMessageOrderer.Exchange(socket, request1Buffer, response2Buffer, response2Length, default));
		var request2ExchangedLength = SocketMessageOrderer.Exchange(socket, request2Buffer, response1Buffer, response1Length, default);
		var request1ExchangedLength = await request1Task;

		Assert.AreEqual(response1Length, request1ExchangedLength);
		Assert.AreEqual(response2Length, request2ExchangedLength);

		new DnsProtocolReader(response2Buffer).ReadMessage(out var actualResponse1);
		Assert.AreEqual(response1, actualResponse1);

		new DnsProtocolReader(response1Buffer).ReadMessage(out var actualResponse2);
		Assert.AreEqual(response2, actualResponse2);
	}
}
