namespace Icod.DCurses.Terminal;

using Icod.Terminal;

/// <summary>
/// Internal refresh-output boundary between DCurses rendition and the owning Terminal session.
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

/// <summary>Optional semantic hyperlink output implemented by Terminal-backed refresh output.</summary>
internal interface ITerminalHyperlinkOutput {
	/// <summary>Writes one bounded application-text run through Terminal's typed hyperlink ownership.</summary>
	/// <param name="value">The application text to write.</param>
	/// <param name="hyperlink">The terminal-independent hyperlink semantic.</param>
	/// <param name="cancellationToken">Cancellation observed before Terminal begins hyperlink transmission.</param>
	ValueTask WriteHyperlinkTextAsync(
		string value,
		CursesHyperlink hyperlink,
		CancellationToken cancellationToken = default
	);
}

/// <summary>Routes DCurses refresh output through the canonical Terminal session.</summary>
internal sealed class TerminalSessionCursesOutput
	: ITerminalOutput,
	  ITerminalHyperlinkOutput {
	private readonly TerminalSession session;
	private Exception? semanticOutputFailure;

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
		ThrowIfSemanticOutputIsUncertain();
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

	public async ValueTask WriteHyperlinkTextAsync(
		string value,
		CursesHyperlink hyperlink,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( value );
		ArgumentNullException.ThrowIfNull( hyperlink );
		ThrowIfSemanticOutputIsUncertain();

		try {
			await this.session.WriteHyperlinkAsync(
				value,
				hyperlink.Uri,
				hyperlink.Identifier,
				cancellationToken
			).ConfigureAwait( false );
		} catch ( OperationCanceledException ) when (
			cancellationToken.IsCancellationRequested
		) {
			throw;
		} catch ( Exception exception ) {
			_ = Interlocked.CompareExchange(
				ref this.semanticOutputFailure,
				exception,
				null
			);
			throw;
		}
	}

	public ValueTask FlushAsync(
		CancellationToken cancellationToken = default
	) {
		return this.session.Output.FlushAsync( cancellationToken );
	}

	private void ThrowIfSemanticOutputIsUncertain() {
		Exception? failure = Volatile.Read( ref this.semanticOutputFailure );
		if ( failure is null ) {
			return;
		}

		throw new InvalidOperationException(
			"A prior Terminal hyperlink operation failed and may still require Terminal-owned cleanup. "
				+ "Dispose the owning CursesSession before emitting further application text.",
			failure
		);
	}
}
