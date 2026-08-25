namespace Icod.DCurses;

using Icod.Terminal;
using Icod.TermInfo;

/// <summary>Selects the preferred terminal alert presentation.</summary>
public enum CursesAlertKind {
	/// <summary>Prefer the audible bell and fall back to a visual alert.</summary>
	Audible,

	/// <summary>Prefer a visual alert and fall back to the audible bell.</summary>
	Visual
}

/// <summary>Selects the requested physical cursor presentation.</summary>
public enum CursesCursorVisibility {
	/// <summary>Hide the physical cursor.</summary>
	Hidden,

	/// <summary>Use the terminal's normal cursor presentation.</summary>
	Normal,

	/// <summary>Use the terminal's most visible cursor presentation.</summary>
	VeryVisible
}

/// <summary>Essential curses presentation policy layered over Terminal presentation leases.</summary>
public sealed partial class CursesSession {
	private readonly SemaphoreSlim presentationGate = new( 1, 1 );
	private TerminalPresentationLease? alternateScreenLease;
	private TerminalPresentationLease? keypadLease;
	private TerminalPresentationLease? cursorLease;

	/// <summary>Produces an audible or visual alert through the best available capability.</summary>
	public async ValueTask<bool> AlertAsync(
		CursesAlertKind alertKind = CursesAlertKind.Audible,
		CancellationToken cancellationToken = default
	) {
		if ( !Enum.IsDefined( alertKind ) ) {
			throw new ArgumentOutOfRangeException( nameof( alertKind ) );
		}
		cancellationToken.ThrowIfCancellationRequested();

		StringCapability preferred = CursesAlertKind.Audible == alertKind
			? StringCapability.Bell
			: StringCapability.FlashScreen
		;
		StringCapability fallback = CursesAlertKind.Audible == alertKind
			? StringCapability.FlashScreen
			: StringCapability.Bell
		;
		string? capability = this.Terminal.GetString( preferred )
			?? this.Terminal.GetString( fallback );
		if ( capability is null ) {
			return false;
		}

		using IDisposable activity = await this.AcquireTerminalActivityAsync(
			cancellationToken
		).ConfigureAwait( false );
		await this.GetRefreshEngine().WriteControlAsync(
			capability,
			invalidatePhysicalScreen: false,
			cancellationToken
		).ConfigureAwait( false );
		return true;
	}

	/// <summary>Requests physical cursor visibility through a reversible Terminal lease.</summary>
	public async ValueTask<bool> SetCursorVisibilityAsync(
		CursesCursorVisibility visibility,
		CancellationToken cancellationToken = default
	) {
		if ( !Enum.IsDefined( visibility ) ) {
			throw new ArgumentOutOfRangeException( nameof( visibility ) );
		}
		cancellationToken.ThrowIfCancellationRequested();

		using IDisposable activity = await this.AcquireTerminalActivityAsync(
			cancellationToken
		).ConfigureAwait( false );
		await this.presentationGate.WaitAsync( cancellationToken ).ConfigureAwait( false );
		try {
			TerminalPresentationLease? replacement = await this.TryAcquirePresentationLeaseAsync(
				new TerminalPresentationOptions {
					CursorVisibility = ToTerminalCursorVisibility( visibility )
				},
				cancellationToken
			).ConfigureAwait( false );
			if ( replacement is null ) {
				return false;
			}

			TerminalPresentationLease? prior = this.cursorLease;
			this.cursorLease = replacement;
			if ( prior is not null ) {
				await prior.DisposeAsync().ConfigureAwait( false );
			}
			return true;
		} finally {
			this.presentationGate.Release();
		}
	}

	/// <summary>Moves the logical and physical cursor when addressing is available.</summary>
	public async ValueTask<bool> SetCursorPositionAsync(
		int row,
		int column,
		CancellationToken cancellationToken = default
	) {
		if ( 0 > row ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( 0 > column ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		cancellationToken.ThrowIfCancellationRequested();

		if ( this.Terminal.GetString( StringCapability.CursorAddress ) is null ) {
			return false;
		}

		using IDisposable activity = await this.AcquireTerminalActivityAsync(
			cancellationToken
		).ConfigureAwait( false );
		_ = this.SynchronizeDimensions();
		this.StandardScreen.Move( row, column );
		await this.GetRefreshEngine().SetCursorPositionAsync(
			row,
			column,
			cancellationToken
		).ConfigureAwait( false );
		return true;
	}

	/// <summary>Resets terminal rendition through available terminfo capabilities.</summary>
	public async ValueTask<bool> ResetRenditionAsync(
		CancellationToken cancellationToken = default
	) {
		cancellationToken.ThrowIfCancellationRequested();
		if (
			this.Terminal.GetString( StringCapability.ExitAttributeMode ) is null
			&& this.Terminal.GetString( StringCapability.OriginalColorPair ) is null
		) {
			return false;
		}

		using IDisposable activity = await this.AcquireTerminalActivityAsync(
			cancellationToken
		).ConfigureAwait( false );
		await this.GetRefreshEngine().ResetRenditionAsync(
			cancellationToken
		).ConfigureAwait( false );
		return true;
	}

	/// <summary>Enters or releases Terminal-owned alternate-screen state.</summary>
	public ValueTask<bool> SetAlternateScreenAsync(
		bool enabled,
		CancellationToken cancellationToken = default
	) {
		return this.SetBooleanPresentationAsync(
			enabled,
			alternateScreen: true,
			cancellationToken
		);
	}

	/// <summary>Enters or releases Terminal-owned keypad/application mode.</summary>
	public ValueTask<bool> SetKeypadModeAsync(
		bool enabled,
		CancellationToken cancellationToken = default
	) {
		return this.SetBooleanPresentationAsync(
			enabled,
			alternateScreen: false,
			cancellationToken
		);
	}

	private async ValueTask InitializePresentationAsync(
		CancellationToken cancellationToken
	) {
		if ( this.Options.UseAlternateScreen ) {
			_ = await this.SetAlternateScreenAsync(
				true,
				cancellationToken
			).ConfigureAwait( false );
		}
		if ( this.Options.EnableKeypad ) {
			_ = await this.SetKeypadModeAsync(
				true,
				cancellationToken
			).ConfigureAwait( false );
		}
		if ( this.Options.HideCursor ) {
			_ = await this.SetCursorVisibilityAsync(
				CursesCursorVisibility.Hidden,
				cancellationToken
			).ConfigureAwait( false );
		}
	}

	private async ValueTask<bool> SetBooleanPresentationAsync(
		bool enabled,
		bool alternateScreen,
		CancellationToken cancellationToken
	) {
		cancellationToken.ThrowIfCancellationRequested();
		using IDisposable activity = await this.AcquireTerminalActivityAsync(
			cancellationToken
		).ConfigureAwait( false );
		await this.presentationGate.WaitAsync( cancellationToken ).ConfigureAwait( false );
		try {
			TerminalPresentationLease? current = alternateScreen
				? this.alternateScreenLease
				: this.keypadLease
			;
			if ( enabled ) {
				if ( current is not null ) {
					return true;
				}

				TerminalPresentationLease? acquired = await this.TryAcquirePresentationLeaseAsync(
					new TerminalPresentationOptions {
						AlternateScreen = alternateScreen,
						KeypadMode = !alternateScreen
					},
					cancellationToken
				).ConfigureAwait( false );
				if ( acquired is null ) {
					return false;
				}

				if ( alternateScreen ) {
					this.alternateScreenLease = acquired;
					this.InvalidatePhysicalScreen();
				} else {
					this.keypadLease = acquired;
				}
				return true;
			}

			if ( current is null ) {
				return true;
			}
			if ( alternateScreen ) {
				this.alternateScreenLease = null;
			} else {
				this.keypadLease = null;
			}

			await current.DisposeAsync().ConfigureAwait( false );
			if ( alternateScreen ) {
				this.InvalidatePhysicalScreen();
			}
			return true;
		} finally {
			this.presentationGate.Release();
		}
	}

	private async ValueTask<TerminalPresentationLease?> TryAcquirePresentationLeaseAsync(
		TerminalPresentationOptions options,
		CancellationToken cancellationToken
	) {
		ArgumentNullException.ThrowIfNull( options );
		TerminalControlResult<TerminalPresentationLease> result =
			await this.terminalSession.AcquirePresentationAsync(
				options,
				cancellationToken
			).ConfigureAwait( false );
		if ( result.IsAvailable ) {
			return result.GetRequiredValue();
		}
		if (
			result.Status is TerminalControlStatus.Unavailable
				or TerminalControlStatus.Unsupported
		) {
			return null;
		}

		throw new InvalidOperationException(
			result.Message ?? "The requested terminal presentation state could not be acquired."
		);
	}

	private async ValueTask ReleasePresentationLeasesForDisposalAsync(
		ICollection<Exception> exceptions
	) {
		ArgumentNullException.ThrowIfNull( exceptions );
		await ReleaseLeaseAsync( this.cursorLease, exceptions ).ConfigureAwait( false );
		this.cursorLease = null;
		await ReleaseLeaseAsync( this.keypadLease, exceptions ).ConfigureAwait( false );
		this.keypadLease = null;
		await ReleaseLeaseAsync( this.alternateScreenLease, exceptions ).ConfigureAwait( false );
		this.alternateScreenLease = null;
	}

	private static async ValueTask ReleaseLeaseAsync(
		TerminalPresentationLease? lease,
		ICollection<Exception> exceptions
	) {
		ArgumentNullException.ThrowIfNull( exceptions );
		if ( lease is null ) {
			return;
		}

		try {
			await lease.DisposeAsync().ConfigureAwait( false );
		} catch ( Exception exception ) {
			exceptions.Add( exception );
		}
	}

	private static TerminalCursorVisibility ToTerminalCursorVisibility(
		CursesCursorVisibility visibility
	) {
		return visibility switch {
			CursesCursorVisibility.Hidden => TerminalCursorVisibility.Hidden,
			CursesCursorVisibility.Normal => TerminalCursorVisibility.Normal,
			CursesCursorVisibility.VeryVisible => TerminalCursorVisibility.VeryVisible,
			_ => throw new ArgumentOutOfRangeException( nameof( visibility ) )
		};
	}
}
