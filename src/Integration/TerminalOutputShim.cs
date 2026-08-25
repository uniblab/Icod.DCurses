namespace Icod.DCurses.Terminal;

using Icod.Terminal;

/// <summary>
/// Internal refresh-output boundary retained during the Terminal T10 validation cycle.
/// </summary>
/// <remarks>
/// This is not a live-terminal ownership abstraction. The implementation delegates application
/// text, terminfo protocol strings, and flushing to the owning <see cref="TerminalSession"/>.
/// </remarks>
internal interface ITerminalOutput {
	ValueTask WriteTextAsync(
		string value,
		CancellationToken cancellationToken = default
	);

	ValueTask WriteTerminalStringAsync(
		string value,
		int affectedLines = 1,
		CancellationToken cancellationToken = default
	);

	ValueTask FlushAsync(
		CancellationToken cancellationToken = default
	);
}

/// <summary>Routes DCurses refresh output through the canonical Terminal session.</summary>
internal sealed class TerminalSessionCursesOutput : ITerminalOutput {
	private readonly TerminalSession session;

	internal TerminalSessionCursesOutput(
		TerminalSession session
	) {
		ArgumentNullException.ThrowIfNull( session );
		this.session = session;
	}

	public ValueTask WriteTextAsync(
		string value,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( value );
		return this.session.WriteTextAsync( value, cancellationToken );
	}

	public ValueTask WriteTerminalStringAsync(
		string value,
		int affectedLines = 1,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( value );
		if ( 0 >= affectedLines ) {
			throw new ArgumentOutOfRangeException( nameof( affectedLines ) );
		}

		return this.session.WriteTerminalStringAsync(
			value,
			affectedLines,
			cancellationToken
		);
	}

	public ValueTask FlushAsync(
		CancellationToken cancellationToken = default
	) {
		return this.session.Output.FlushAsync( cancellationToken );
	}
}
