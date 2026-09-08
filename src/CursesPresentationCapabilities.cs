namespace Icod.DCurses;

using Icod.TermInfo;

/// <summary>
/// Describes terminal presentation capabilities through curses-shaped semantic observations.
/// </summary>
/// <remarks>
/// This value contains no terminal escape sequences. It reports immutable facts derived from the
/// session's selected terminal description so ordinary applications do not need to inspect raw
/// terminfo capability strings merely to make common presentation decisions.
/// </remarks>
public readonly record struct CursesPresentationCapabilities {
	private const int NoColorVideoStandout = 1;
	private const int NoColorVideoUnderline = 2;
	private const int NoColorVideoReverse = 4;
	private const int NoColorVideoBlink = 8;
	private const int NoColorVideoDim = 16;
	private const int NoColorVideoBold = 32;
	private const int NoColorVideoInvisible = 64;
	private const int NoColorVideoItalic = 2048;

	internal CursesPresentationCapabilities(
		int indexedColorCount,
		bool supportsDirectRgb,
		bool supportsForegroundColor,
		bool supportsBackgroundColor,
		bool supportsDefaultColorRestoration,
		CursesTextAttributes supportedAttributes,
		CursesTextAttributes colorRestrictedAttributes,
		bool supportsAlternateCharacterSet,
		bool supportsCursorHidden,
		bool supportsCursorNormal,
		bool supportsCursorVeryVisible
	) {
		IndexedColorCount = indexedColorCount;
		SupportsDirectRgb = supportsDirectRgb;
		SupportsForegroundColor = supportsForegroundColor;
		SupportsBackgroundColor = supportsBackgroundColor;
		SupportsDefaultColorRestoration = supportsDefaultColorRestoration;
		SupportedAttributes = supportedAttributes;
		ColorRestrictedAttributes = colorRestrictedAttributes;
		SupportsAlternateCharacterSet = supportsAlternateCharacterSet;
		SupportsCursorHidden = supportsCursorHidden;
		SupportsCursorNormal = supportsCursorNormal;
		SupportsCursorVeryVisible = supportsCursorVeryVisible;
	}

	/// <summary>Gets the number of safely addressable indexed terminal colors.</summary>
	public int IndexedColorCount {
		get;
	}

	/// <summary>Gets whether the terminal advertises direct RGB color semantics.</summary>
	public bool SupportsDirectRgb {
		get;
	}

	/// <summary>Gets whether a usable foreground-color selector is available.</summary>
	public bool SupportsForegroundColor {
		get;
	}

	/// <summary>Gets whether a usable background-color selector is available.</summary>
	public bool SupportsBackgroundColor {
		get;
	}

	/// <summary>Gets whether rendition can restore terminal-default colors after explicit color use.</summary>
	public bool SupportsDefaultColorRestoration {
		get;
	}

	/// <summary>Gets the text attributes with a directly advertised terminal representation.</summary>
	public CursesTextAttributes SupportedAttributes {
		get;
	}

	/// <summary>
	/// Gets the supported text attributes that the terminal reports as unavailable while color is active.
	/// </summary>
	public CursesTextAttributes ColorRestrictedAttributes {
		get;
	}

	/// <summary>Gets whether a complete alternate-character-set mapping/enter/exit contract is available.</summary>
	public bool SupportsAlternateCharacterSet {
		get;
	}

	/// <summary>Gets whether the physical cursor can be hidden through the selected terminal profile.</summary>
	public bool SupportsCursorHidden {
		get;
	}

	/// <summary>Gets whether the normal physical cursor presentation is explicitly available.</summary>
	public bool SupportsCursorNormal {
		get;
	}

	/// <summary>Gets whether the terminal advertises a very-visible cursor presentation.</summary>
	public bool SupportsCursorVeryVisible {
		get;
	}

	/// <summary>Gets whether at least one indexed or direct color representation is available.</summary>
	public bool SupportsColor => 0 < IndexedColorCount || SupportsDirectRgb;

	/// <summary>Gets whether bold rendition has a directly advertised representation.</summary>
	public bool SupportsBold => 0 != ( SupportedAttributes & CursesTextAttributes.Bold );

	/// <summary>Gets whether dim rendition has a directly advertised representation.</summary>
	public bool SupportsDim => 0 != ( SupportedAttributes & CursesTextAttributes.Dim );

	/// <summary>Gets whether underline rendition has a directly advertised representation.</summary>
	public bool SupportsUnderline => 0 != ( SupportedAttributes & CursesTextAttributes.Underline );

	/// <summary>Gets whether reverse-video rendition has a directly advertised representation.</summary>
	public bool SupportsReverse => 0 != ( SupportedAttributes & CursesTextAttributes.Reverse );

	/// <summary>Gets whether standout rendition has a directly advertised representation.</summary>
	public bool SupportsStandout => 0 != ( SupportedAttributes & CursesTextAttributes.Standout );

	/// <summary>Gets whether italic rendition has a directly advertised representation.</summary>
	public bool SupportsItalic => 0 != ( SupportedAttributes & CursesTextAttributes.Italic );

	/// <summary>Gets whether blink rendition has a directly advertised representation.</summary>
	public bool SupportsBlink => 0 != ( SupportedAttributes & CursesTextAttributes.Blink );

	/// <summary>Gets whether conceal/invisible rendition has a directly advertised representation.</summary>
	public bool SupportsConceal => 0 != ( SupportedAttributes & CursesTextAttributes.Conceal );

	/// <summary>Gets whether strikeout rendition has a directly advertised representation.</summary>
	public bool SupportsStrikeout => 0 != ( SupportedAttributes & CursesTextAttributes.Strikeout );

	/// <summary>Derives a curses-shaped presentation view from one immutable terminal description.</summary>
	internal static CursesPresentationCapabilities Create( TerminalDescription terminal ) {
		ArgumentNullException.ThrowIfNull( terminal );

		TerminalColorSupport colors = TerminalColors.GetColorSupport( terminal );
		bool hasSetAttributes = null != terminal.GetString( StringCapability.SetAttributes );
		CursesTextAttributes supportedAttributes = CursesTextAttributes.None;

		if ( hasSetAttributes
			|| null != terminal.GetString( StringCapability.EnterBoldMode ) ) {
			supportedAttributes |= CursesTextAttributes.Bold;
		}
		if ( hasSetAttributes
			|| null != terminal.GetString( StringCapability.EnterDimMode ) ) {
			supportedAttributes |= CursesTextAttributes.Dim;
		}
		if ( hasSetAttributes
			|| null != terminal.GetString( StringCapability.EnterUnderlineMode ) ) {
			supportedAttributes |= CursesTextAttributes.Underline;
		}
		if ( hasSetAttributes
			|| null != terminal.GetString( StringCapability.EnterReverseMode ) ) {
			supportedAttributes |= CursesTextAttributes.Reverse;
		}
		if ( hasSetAttributes
			|| null != terminal.GetString( StringCapability.EnterStandoutMode ) ) {
			supportedAttributes |= CursesTextAttributes.Standout;
		}
		if ( null != terminal.GetString( StringCapability.EnterItalicMode ) ) {
			supportedAttributes |= CursesTextAttributes.Italic;
		}
		if ( hasSetAttributes
			|| null != terminal.GetString( StringCapability.EnterBlinkMode ) ) {
			supportedAttributes |= CursesTextAttributes.Blink;
		}
		if ( hasSetAttributes
			|| null != terminal.GetString( StringCapability.EnterInvisibleMode ) ) {
			supportedAttributes |= CursesTextAttributes.Conceal;
		}
		if ( terminal.TryGetExtendedString(
			"smxx",
			out _
		) ) {
			supportedAttributes |= CursesTextAttributes.Strikeout;
		}

		bool supportsAlternateCharacterSet =
			null != terminal.GetString( StringCapability.AlternateCharacterSet )
			&& null != terminal.GetString( StringCapability.EnterAlternateCharacterSetMode )
			&& null != terminal.GetString( StringCapability.ExitAlternateCharacterSetMode );

		return new CursesPresentationCapabilities(
			colors.IndexedColorCount,
			TerminalColorModel.DirectRgb == colors.Model,
			colors.HasForegroundSelector,
			colors.HasBackgroundSelector,
			colors.HasOriginalColorPair
				|| null != terminal.GetString( StringCapability.ExitAttributeMode ),
			supportedAttributes,
			TranslateNoColorVideoMask( colors.NoColorVideoMask ),
			supportsAlternateCharacterSet,
			null != terminal.GetString( StringCapability.CursorInvisible ),
			null != terminal.GetString( StringCapability.CursorNormal ),
			null != terminal.GetString( StringCapability.CursorVeryVisible )
		);
	}

	private static CursesTextAttributes TranslateNoColorVideoMask( int? mask ) {
		if ( !mask.HasValue ) {
			return CursesTextAttributes.None;
		}

		int value = mask.Value;
		CursesTextAttributes result = CursesTextAttributes.None;
		if ( 0 != ( value & NoColorVideoStandout ) ) {
			result |= CursesTextAttributes.Standout;
		}
		if ( 0 != ( value & NoColorVideoUnderline ) ) {
			result |= CursesTextAttributes.Underline;
		}
		if ( 0 != ( value & NoColorVideoReverse ) ) {
			result |= CursesTextAttributes.Reverse;
		}
		if ( 0 != ( value & NoColorVideoBlink ) ) {
			result |= CursesTextAttributes.Blink;
		}
		if ( 0 != ( value & NoColorVideoDim ) ) {
			result |= CursesTextAttributes.Dim;
		}
		if ( 0 != ( value & NoColorVideoBold ) ) {
			result |= CursesTextAttributes.Bold;
		}
		if ( 0 != ( value & NoColorVideoInvisible ) ) {
			result |= CursesTextAttributes.Conceal;
		}
		if ( 0 != ( value & NoColorVideoItalic ) ) {
			result |= CursesTextAttributes.Italic;
		}
		return result;
	}
}
