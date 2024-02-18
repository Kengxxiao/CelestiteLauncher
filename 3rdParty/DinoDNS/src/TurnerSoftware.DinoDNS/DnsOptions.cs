using System.Diagnostics.CodeAnalysis;

namespace TurnerSoftware.DinoDNS;

public readonly record struct DnsMessageOptions(
	int MaximumMessageSize
)
{
	/// <summary>
	/// A reasonable default size for sending DNS messages.
	/// If you encounter issues using this, try <see cref="DefaultCompatibleMessageSize"/> instead.
	/// </summary>
	/// <remarks>
	/// See: https://tools.ietf.org/id/draft-madi-dnsop-udp4dns-00.html
	/// </remarks>
	public const int DefaultMessageSize = 1232;
	public const int DefaultCompatibleMessageSize = 512;
	public const int MinimumMessageSize = 64;


	public static readonly DnsMessageOptions Default = new()
	{
		MaximumMessageSize = DefaultMessageSize
	};


	public bool Validate([NotNullWhen(false)] out string? errorMessage)
	{
		if (MaximumMessageSize < MinimumMessageSize)
		{
			errorMessage = "Message size is too small.";
			return false;
		}

		errorMessage = default;
		return true;
	}
}
