namespace Icod.DCurses.Internal;

using System.Text;
using Icod.DCurses.Terminal;
using Icod.TermInfo;

/// <summary>Writes expanded terminfo capability data to a terminal output service.</summary>
internal static class TerminalCapabilityWriter
{
	/// <summary>Writes one terminal capability using terminfo output semantics.</summary>
	/// <param name="output">The destination terminal output service.</param>
	/// <param name="capability">The expanded capability data.</param>
	/// <param name="cancellationToken">Cancellation for the write operation.</param>
	/// <returns>A value task representing the asynchronous write.</returns>
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
