namespace Icod.DCurses.Internal;

using System.Text;
using Icod.DCurses.Terminal;
using Icod.TermInfo;

internal static class TerminalCapabilityWriter
{
	internal static async ValueTask WriteAsync(
		ITerminalOutput output,
		string capability,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(output);
		ArgumentNullException.ThrowIfNull(capability);

		using MemoryStream stream = new();

		TermInfoOutput.TPuts(
			capability,
			1,
			stream,
			Encoding.Latin1,
			PaddingMode.Ignore);

		await output.WriteAsync(
			stream.GetBuffer().AsMemory(0, checked((int)stream.Length)),
			cancellationToken).ConfigureAwait(false);
	}
}
