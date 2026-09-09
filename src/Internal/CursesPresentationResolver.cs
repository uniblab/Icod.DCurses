namespace Icod.DCurses.Internal;

using Icod.TermInfo;

/// <summary>Resolves logical curses style intent to a safe physical terminal rendition.</summary>
internal sealed class CursesPresentationResolver {
	private readonly TerminalDescription terminal;
	private readonly TerminalColorSupport colorSupport;
	private readonly CursesPresentationCapabilities capabilities;
	private readonly CursesTextAttributes reversibleAttributes;

	internal CursesPresentationResolver( TerminalDescription terminal ) {
		ArgumentNullException.ThrowIfNull( terminal );

		this.terminal = terminal;
		this.colorSupport = TerminalColors.GetColorSupport( terminal );
		this.capabilities = CursesPresentationCapabilities.Create( terminal );
		this.reversibleAttributes = GetReversibleAttributes(
			terminal,
			this.capabilities.SupportedAttributes
		);
	}

	internal CursesStyle Resolve( CursesStyle requested ) {
		CursesColor foreground = ResolveColor(
			requested.Foreground,
			foreground: true
		);
		CursesColor background = ResolveColor(
			requested.Background,
			foreground: false
		);

		CursesTextAttributes attributes = requested.Attributes
			& reversibleAttributes;
		if ( 0 != ( requested.Attributes & CursesTextAttributes.Standout )
			&& 0 == ( attributes & CursesTextAttributes.Standout )
			&& 0 != ( reversibleAttributes & CursesTextAttributes.Reverse ) ) {
			attributes |= CursesTextAttributes.Reverse;
		}

		if ( !foreground.IsDefault || !background.IsDefault ) {
			attributes &= ~capabilities.ColorRestrictedAttributes;
		}

		return new CursesStyle(
			foreground,
			background,
			attributes
		);
	}

	private CursesColor ResolveColor(
		CursesColor requested,
		bool foreground
	) {
		if ( CursesColorKind.Default == requested.Kind ) {
			return CursesColor.Default;
		}

		bool hasSelector = foreground
			? colorSupport.HasForegroundSelector
			: colorSupport.HasBackgroundSelector;
		if ( !hasSelector || !colorSupport.HasOriginalColorPair ) {
			return CursesColor.Default;
		}

		switch ( requested.Kind ) {
			case CursesColorKind.Indexed:
				int index = requested.Index
					?? throw new InvalidOperationException(
						"An indexed curses color does not contain an index."
					);
				return 0 <= index && index < colorSupport.IndexedColorCount
					? requested
					: CursesColor.Default;

			case CursesColorKind.Rgb:
				if ( TerminalColorModel.DirectRgb != colorSupport.Model
					|| !requested.Red.HasValue
					|| !requested.Green.HasValue
					|| !requested.Blue.HasValue
					|| !colorSupport.RgbLayout.HasValue
					|| !colorSupport.ColorCount.HasValue ) {
					return CursesColor.Default;
				}

				TerminalRgbColor rgb = new(
					requested.Red.Value,
					requested.Green.Value,
					requested.Blue.Value
				);
				int packed = colorSupport.RgbLayout.Value.Pack( rgb );
				if ( packed >= colorSupport.ColorCount.Value
					|| ( 0 < packed && packed < colorSupport.IndexedColorCount ) ) {
					return CursesColor.Default;
				}
				return requested;

			default:
				throw new ArgumentOutOfRangeException(
					nameof( requested ),
					requested.Kind,
					"Unknown curses color kind."
				);
		}
	}

	private static CursesTextAttributes GetReversibleAttributes(
		TerminalDescription terminal,
		CursesTextAttributes supportedAttributes
	) {
		ArgumentNullException.ThrowIfNull( terminal );

		if ( null != terminal.GetString( StringCapability.ExitAttributeMode ) ) {
			return supportedAttributes;
		}

		CursesTextAttributes result = CursesTextAttributes.None;
		if ( 0 != ( supportedAttributes & CursesTextAttributes.Underline )
			&& null != terminal.GetString( StringCapability.ExitUnderlineMode ) ) {
			result |= CursesTextAttributes.Underline;
		}
		if ( 0 != ( supportedAttributes & CursesTextAttributes.Standout )
			&& null != terminal.GetString( StringCapability.ExitStandoutMode ) ) {
			result |= CursesTextAttributes.Standout;
		}
		if ( 0 != ( supportedAttributes & CursesTextAttributes.Italic )
			&& null != terminal.GetString( StringCapability.ExitItalicMode ) ) {
			result |= CursesTextAttributes.Italic;
		}
		if ( 0 != ( supportedAttributes & CursesTextAttributes.Strikeout )
			&& terminal.TryGetExtendedString(
				"rmxx",
				out _
			) ) {
			result |= CursesTextAttributes.Strikeout;
		}
		return result;
	}
}
