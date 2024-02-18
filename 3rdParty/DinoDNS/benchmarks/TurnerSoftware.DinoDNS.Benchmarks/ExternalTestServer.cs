using System.Diagnostics;

namespace TurnerSoftware.DinoDNS.Benchmarks;

internal static class ExternalTestServer
{
	private readonly static TimeSpan StartWait = TimeSpan.FromSeconds(1);
	private static Process? Instance;

	public static void StartUdp() => Start("udp");
	public static void StartTcp() => Start("tcp");
	public static void StartTls() => Start("tls");
	public static void StartHttps() => Start("https");

	private static void Start(string arguments)
	{
		Instance = Process.Start(new ProcessStartInfo("StaticDnsServer.exe")
		{
			Arguments = arguments,
			RedirectStandardInput = true
		}) ?? throw new Exception("Test server could not start");
		Thread.Sleep(StartWait);
	}

	public static void Stop()
	{
		if (Instance is not null && !Instance.HasExited)
		{
			Instance.StandardInput.Write("0");
		}
	}
}
