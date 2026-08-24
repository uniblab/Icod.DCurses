namespace Icod.DCurses;

using Icod.TermInfo;

/// <summary>
/// Selects the preferred terminal alert presentation.
/// </summary>
public enum CursesAlertKind {
	/// <summary>Prefer the audible bell and fall back to a visual alert.</summary>
	Audible,

	/// <summary>Prefer a visual alert and fall back to the audible bell.</summary>
	Visual
}

/// <summary>
/// Selects the requested physical cursor presentation.
/// </summary>
public enum CursesCursorVisibility {
	/// <summary>Hide the physical cursor.</summary>
	Hidden,

	/// <summary>Use the terminal's normal cursor presentation.</summary>
	Normal,

	/// <summary>Use the terminal's most visible cursor presentation.</summary>
	VeryVisible
}

/// <summary>
/// Essential terminal presentation operations for <see cref="CursesSession"/>.
/// </summary>
public sealed partial class CursesSession {
	private bool alternateScreenRequested;
	private bool keypadRequested;
	private CursesCursorVisibility? cursorVisibilityRequested;

	/// <summary>
	/// Produces an audible or visual alert, falling back to the other form when necessary.
	/// </summary>
	/// <param name="alertKind">The preferred alert form.</param>
	/// <param name="cancellationToken">Cancellation for the presentation operation.</param>
	/// <returns>
	/// <see langword="true"/> when an alert capability was emitted; otherwise <see langword="false"/>.
	/// </returns>
	public async ValueTask<bool> AlertAsync(
		CursesAlertKind alertKind = CursesAlertKind.Audible,
		CancellationToken cancellationToken = default ) {
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
		string? capability = Terminal.GetString( preferred )
			?? Terminal.GetString( fallback );

		if ( null == capability ) {
			return false;
		}

		await GetRefreshEngine().WriteControlAsync(
			capability,
			invalidatePhysicalScreen: false,
			cancellationToken
		).ConfigureAwait( false );
		return true;
	}

	/// <summary>
	/// Requests a physical cursor presentation using the best matching available capability.
	/// </summary>
	/// <param name="visibility">The requested cursor presentation.</param>
	/// <param name="cancellationToken">Cancellation for the presentation operation.</param>
	/// <returns>
	/// <see langword="true"/> when a cursor capability was emitted; otherwise <see langword="false"/>.
	/// </returns>
	public async ValueTask<bool> SetCursorVisibilityAsync(
		CursesCursorVisibility visibility,
		CancellationToken cancellationToken = default ) {
		if ( !Enum.IsDefined( visibility ) ) {
			throw new ArgumentOutOfRangeException( nameof( visibility ) );
		}
		cancellationToken.ThrowIfCancellationRequested();

		if ( !TryGetCursorVisibilityCapabilities(
			visibility,
			out string capability,
			out string restoreCapability
		) ) {
			return false;
		}

		await GetRefreshEngine().WriteControlAsync(
			capability,
			invalidatePhysicalScreen: false,
			cancellationToken
		).ConfigureAwait( false );

		cursorVisibilityRequested = visibility;
		cursorRestore = restoreCapability;
		return true;
	}

	/// <summary>
	/// Moves the logical standard-screen cursor and, when supported, positions the physical cursor immediately.
	/// </summary>
	/// <param name="row">Zero-based row.</param>
	/// <param name="column">Zero-based column.</param>
	/// <param name="cancellationToken">Cancellation for the presentation operation.</param>
	/// <returns>
	/// <see langword="true"/> when cursor addressing is available; otherwise <see langword="false"/>.
	/// </returns>
	public async ValueTask<bool> SetCursorPositionAsync(
		int row,
		int column,
		CancellationToken cancellationToken = default ) {
		if ( 0 > row ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( 0 > column ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		cancellationToken.ThrowIfCancellationRequested();

		if ( null == Terminal.GetString( StringCapability.CursorAddress ) ) {
			return false;
		}

		_ = SynchronizeDimensions();
		StandardScreen.Move(
			row,
			column
		);

		await GetRefreshEngine().SetCursorPositionAsync(
			row,
			column,
			cancellationToken
		).ConfigureAwait( false );
		return true;
	}

	/// <summary>
	/// Resets terminal rendition through the available attribute/color reset capabilities.
	/// </summary>
	/// <param name="cancellationToken">Cancellation for the presentation operation.</param>
	/// <returns>
	/// <see langword="true"/> when at least one rendition-reset capability is available.
	/// </returns>
	public async ValueTask<bool> ResetRenditionAsync(
		CancellationToken cancellationToken = default ) {
		cancellationToken.ThrowIfCancellationRequested();

		if ( null == Terminal.GetString( StringCapability.ExitAttributeMode )
			&& null == Terminal.GetString( StringCapability.OriginalColorPair ) ) {
			return false;
		}

		await GetRefreshEngine().ResetRenditionAsync(
			cancellationToken
		).ConfigureAwait( false );
		return true;
	}

	/// <summary>
	/// Enters or leaves the terminal's cursor-addressed/alternate-screen presentation mode.
	/// </summary>
	/// <param name="enabled">Whether cursor-addressed presentation mode is requested.</param>
	/// <param name="cancellationToken">Cancellation for the presentation operation.</param>
	/// <returns>
	/// <see langword="true"/> when the requested transition capability was emitted.
	/// </returns>
	public async ValueTask<bool> SetAlternateScreenAsync(
		bool enabled,
		CancellationToken cancellationToken = default ) {
		cancellationToken.ThrowIfCancellationRequested();

		string? enter = Terminal.GetString(
			StringCapability.EnterCursorAddressingMode
		);
		string? exit = Terminal.GetString(
			StringCapability.ExitCursorAddressingMode
		);

		if ( enabled && ( null == enter || null == exit ) ) {
			return false;
		}

		string? capability = enabled
			? enter
			: exit
		;
		if ( null == capability ) {
			return false;
		}

		await GetRefreshEngine().WriteControlAsync(
			capability,
			invalidatePhysicalScreen: true,
			cancellationToken
		).ConfigureAwait( false );

		alternateScreenRequested = enabled;
		if ( enabled ) {
			alternateScreenRestore = exit;
		}
		return true;
	}

	/// <summary>
	/// Enters or leaves terminal keypad/application transmit mode.
	/// </summary>
	/// <param name="enabled">Whether keypad/application mode is requested.</param>
	/// <param name="cancellationToken">Cancellation for the presentation operation.</param>
	/// <returns>
	/// <see langword="true"/> when the requested transition capability was emitted.
	/// </returns>
	public async ValueTask<bool> SetKeypadModeAsync(
		bool enabled,
		CancellationToken cancellationToken = default ) {
		cancellationToken.ThrowIfCancellationRequested();

		string? enter = Terminal.GetString(
			StringCapability.EnterKeypadMode
		);
		string? exit = Terminal.GetString(
			StringCapability.ExitKeypadMode
		);

		if ( enabled && ( null == enter || null == exit ) ) {
			return false;
		}

		string? capability = enabled
			? enter
			: exit
		;
		if ( null == capability ) {
			return false;
		}

		await GetRefreshEngine().WriteControlAsync(
			capability,
			invalidatePhysicalScreen: false,
			cancellationToken
		).ConfigureAwait( false );

		keypadRequested = enabled;
		if ( enabled ) {
			keypadRestore = exit;
		}
		return true;
	}

	private bool TryGetCursorVisibilityCapabilities(
		CursesCursorVisibility visibility,
		out string capability,
		out string restoreCapability ) {
		string? selected;
		string? restore;

		switch ( visibility ) {
			case CursesCursorVisibility.Hidden:
				selected = Terminal.GetString(
					StringCapability.CursorInvisible
				);
				restore = Terminal.GetString(
					StringCapability.CursorNormal
				) ?? Terminal.GetString(
					StringCapability.CursorVeryVisible
				);
				break;

			case CursesCursorVisibility.Normal:
				selected = Terminal.GetString(
					StringCapability.CursorNormal
				) ?? Terminal.GetString(
					StringCapability.CursorVeryVisible
				);
				restore = selected;
				break;

			case CursesCursorVisibility.VeryVisible:
				selected = Terminal.GetString(
					StringCapability.CursorVeryVisible
				) ?? Terminal.GetString(
					StringCapability.CursorNormal
				);
				restore = Terminal.GetString(
					StringCapability.CursorNormal
				) ?? selected;
				break;

			default:
				throw new ArgumentOutOfRangeException(
					nameof( visibility ),
					visibility,
					"Unknown cursor visibility."
				);
		}

		if ( null == selected || null == restore ) {
			capability = string.Empty;
			restoreCapability = string.Empty;
			return false;
		}

		capability = selected;
		restoreCapability = restore;
		return true;
	}
}
