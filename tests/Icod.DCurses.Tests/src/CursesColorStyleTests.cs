using Icod.DCurses;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class CursesColorStyleTests {
	[Fact]
	public void DefaultColorRepresentsTerminalDefaultWithoutEncodedOutput() {
		CursesColor color = CursesColor.Default;

		Assert.True( color.IsDefault );
		Assert.Equal( CursesColorKind.Default, color.Kind );
		Assert.Null( color.Index );
		Assert.Null( color.Red );
		Assert.Null( color.Green );
		Assert.Null( color.Blue );
	}

	[Fact]
	public void IndexedAndRgbColorsPreserveSemanticValues() {
		CursesColor indexed = CursesColor.Indexed( 237 );
		CursesColor rgb = CursesColor.Rgb( 12, 34, 56 );

		Assert.Equal( CursesColorKind.Indexed, indexed.Kind );
		Assert.Equal( (int?)237, indexed.Index );
		Assert.Equal( CursesColorKind.Rgb, rgb.Kind );
		Assert.Equal( (byte?)12, rgb.Red );
		Assert.Equal( (byte?)34, rgb.Green );
		Assert.Equal( (byte?)56, rgb.Blue );
	}

	[Fact]
	public void IndexedColorRejectsNegativeIndex() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesColor.Indexed( -1 )
		);
	}

	[Fact]
	public void StyleCombinesColorsAndSemanticAttributes() {
		CursesStyle style = new(
			CursesColor.Indexed( 15 ),
			CursesColor.Rgb( 1, 2, 3 ),
			CursesTextAttributes.Bold
				| CursesTextAttributes.Dim
				| CursesTextAttributes.Underline
				| CursesTextAttributes.Reverse
				| CursesTextAttributes.Standout
		);

		Assert.Equal( (int?)15, style.Foreground.Index );
		Assert.Equal( (byte?)1, style.Background.Red );
		Assert.True( style.Attributes.HasFlag( CursesTextAttributes.Bold ) );
		Assert.True( style.Attributes.HasFlag( CursesTextAttributes.Dim ) );
		Assert.True( style.Attributes.HasFlag( CursesTextAttributes.Underline ) );
		Assert.True( style.Attributes.HasFlag( CursesTextAttributes.Reverse ) );
		Assert.True( style.Attributes.HasFlag( CursesTextAttributes.Standout ) );
	}

	[Fact]
	public void StyleRejectsUnknownAttributeFlags() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesStyle(
				CursesColor.Default,
				CursesColor.Default,
				(CursesTextAttributes)0x4000
			)
		);
	}

	[Fact]
	public void CellRejectsTerminalControlCharactersInVisibleContent() {
		Assert.Throws<ArgumentException>(
			() => new CursesCell( "\u001b[31mred" )
		);
		Assert.Throws<ArgumentException>(
			() => new CursesCell( "line\nfeed" )
		);
	}

	[Fact]
	public void ContinuationCellCarriesStyleWithoutVisibleContent() {
		CursesStyle style = new(
			CursesColor.Indexed( 2 ),
			CursesColor.Default,
			CursesTextAttributes.Bold
		);

		CursesCell cell = CursesCell.Continuation( style );

		Assert.True( cell.IsContinuation );
		Assert.False( cell.IsBlank );
		Assert.Empty( cell.Content );
		Assert.Equal( style, cell.Style );
	}

	[Fact]
	public void DefaultCellAndBlankCellAreSemanticallyEqual() {
		Assert.Equal(
			default( CursesCell ),
			CursesCell.Blank()
		);
	}
}
