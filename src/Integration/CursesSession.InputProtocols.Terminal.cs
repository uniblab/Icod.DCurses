namespace Icod.DCurses;

using Icod.Terminal;

/// <summary>
/// Delegates reversible rich-input protocol ownership to the canonical Terminal session.
/// </summary>
public sealed partial class CursesSession {
	/// <summary>
	/// Acquires one reversible set of rich-input protocol reporting requirements.
	/// </summary>
	public async ValueTask<TerminalControlResult<CursesInputProtocolLease>> AcquireInputProtocolsAsync(
		CursesInputProtocolOptions options,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( options );
		options.Validate();
		cancellationToken.ThrowIfCancellationRequested();

		using IDisposable activity = await this.AcquireTerminalActivityAsync(
			cancellationToken
		).ConfigureAwait( false );

		TerminalControlResult<TerminalInputProtocolLease> result =
			await this.terminalSession.AcquireInputProtocolsAsync(
				options.ToTerminalOptions(),
				cancellationToken
			).ConfigureAwait( false );

		return result.Status switch {
			TerminalControlStatus.Available =>
				TerminalControlResult<CursesInputProtocolLease>.Available(
					new CursesInputProtocolLease(
						result.GetRequiredValue(),
						options
					)
				),
			TerminalControlStatus.Unavailable =>
				TerminalControlResult<CursesInputProtocolLease>.Unavailable(
					result.Message,
					result.NativeErrorCode
				),
			TerminalControlStatus.Unsupported =>
				TerminalControlResult<CursesInputProtocolLease>.Unsupported(
					result.Message
				),
			TerminalControlStatus.Failed =>
				TerminalControlResult<CursesInputProtocolLease>.Failed(
					result.Message,
					result.NativeErrorCode
				),
			_ => throw new ArgumentOutOfRangeException(
				nameof( result ),
				result.Status,
				"The Terminal input-protocol result status is not recognized."
			)
		};
	}
}
