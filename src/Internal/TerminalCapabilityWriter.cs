namespace Icod.DCurses.Internal;

using Icod.DCurses.Terminal;

/// <summary>Writes expanded terminfo capability data to a terminal output service.</summary>
internal static class TerminalCapabilityWriter {
	/// <summary>Writes one terminal capability using terminfo output semantics.</summary>
	/// <param name="output">The destination terminal output service.</param>
	/// <param name="capability">The expanded capability data.</param>
	/// <param name="cancellationToken">Cancellation for the write operation.</param>
	/// <returns>A value task representing the asynchronous write.</returns>
	internal static ValueTask WriteAsync(
		ITerminalOutput output,
		string capability,
		CancellationToken cancellationToken
	) {
		return WriteAsync(
			output,
			capability,
			affectedLines: 1,
			cancellationToken
		);
	}

	/// <summary>Writes one terminal capability using terminfo output semantics.</summary>
	/// <param name="output">The destination terminal output service.</param>
	/// <param name="capability">The expanded capability data.</param>
	/// <param name="affectedLines">The positive number of lines affected for padding semantics.</param>
	/// <param name="cancellationToken">Cancellation for the write operation.</param>
	/// <returns>A value task representing the asynchronous write.</returns>
	internal static ValueTask WriteAsync(
		ITerminalOutput output,
		string capability,
		int affectedLines,
		CancellationToken cancellationToken
	) {
		ArgumentNullException.ThrowIfNull( output );
		ArgumentNullException.ThrowIfNull( capability );
		if ( 0 >= affectedLines ) {
			throw new ArgumentOutOfRangeException( nameof( affectedLines ) );
		}

		return output.WriteTerminalStringAsync(
			capability,
			affectedLines,
			cancellationToken
		);
	}
}
