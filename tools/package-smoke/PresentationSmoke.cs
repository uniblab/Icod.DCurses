using System.Runtime.CompilerServices;
using Icod.DCurses;

internal static class PresentationSmoke {
	[ModuleInitializer]
	internal static void VerifyPresentationSurface() {
		if ( 16 != (int)CursesTextAttributes.Standout
			|| 32 != (int)CursesTextAttributes.Italic
			|| 64 != (int)CursesTextAttributes.Blink
			|| 128 != (int)CursesTextAttributes.Conceal
			|| 256 != (int)CursesTextAttributes.Strikeout ) {
			throw new InvalidOperationException(
				"DCurses package-only rendition enum compatibility validation failed."
			);
		}

		CursesStyle allAttributes = new(
			CursesColor.Default,
			CursesColor.Default,
			CursesTextAttributes.Bold
				| CursesTextAttributes.Dim
				| CursesTextAttributes.Underline
				| CursesTextAttributes.Reverse
				| CursesTextAttributes.Standout
				| CursesTextAttributes.Italic
				| CursesTextAttributes.Blink
				| CursesTextAttributes.Conceal
				| CursesTextAttributes.Strikeout
		);
		if ( CursesTextAttributes.Strikeout !=
			( allAttributes.Attributes & CursesTextAttributes.Strikeout ) ) {
			throw new InvalidOperationException(
				"DCurses package-only complete rendition vocabulary is unavailable."
			);
		}

		CursesCell line = CursesCell.Line(
			CursesLineGlyph.Crossing,
			allAttributes.WithAttributes( CursesTextAttributes.Bold )
		);
		if ( !line.IsLineGlyph
			|| CursesLineGlyph.Crossing != line.LineGlyph
			|| "┼" != line.Content
			|| 1 != line.DisplayWidth ) {
			throw new InvalidOperationException(
				"DCurses package-only semantic line-cell surface failed validation."
			);
		}

		CursesScreen logical = new(
			8,
			4
		);
		logical.StandardWindow.DrawBorder();
		logical.StandardWindow.DrawHorizontalLine(
			1,
			2,
			4
		);
		logical.StandardWindow.DrawVerticalLine(
			1,
			6,
			2
		);
		if ( CursesLineGlyph.UpperLeftCorner != logical.StandardWindow.GetCell( 0, 0 ).LineGlyph
			|| CursesLineGlyph.Horizontal != logical.StandardWindow.GetCell( 1, 3 ).LineGlyph
			|| CursesLineGlyph.Vertical != logical.StandardWindow.GetCell( 2, 6 ).LineGlyph ) {
			throw new InvalidOperationException(
				"DCurses package-only semantic drawing surface failed validation."
			);
		}

		if ( typeof( CursesPresentationCapabilities ) !=
			typeof( CursesSession ).GetProperty(
				nameof( CursesSession.PresentationCapabilities )
			)?.PropertyType ) {
			throw new InvalidOperationException(
				"DCurses package-only presentation-capabilities surface is unavailable."
			);
		}
	}
}
