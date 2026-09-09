using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies semantic line-drawing identity independently of physical presentation.</summary>
public sealed class CursesLineGlyphTests {
	[Fact]
	public void SemanticLineCellsExposeCanonicalOneColumnContent() {
		( CursesLineGlyph Glyph, string Content )[] cases = [
			( CursesLineGlyph.Horizontal, "─" ),
			( CursesLineGlyph.Vertical, "│" ),
			( CursesLineGlyph.UpperLeftCorner, "┌" ),
			( CursesLineGlyph.UpperRightCorner, "┐" ),
			( CursesLineGlyph.LowerLeftCorner, "└" ),
			( CursesLineGlyph.LowerRightCorner, "┘" ),
			( CursesLineGlyph.TeeUp, "┴" ),
			( CursesLineGlyph.TeeDown, "┬" ),
			( CursesLineGlyph.TeeLeft, "┤" ),
			( CursesLineGlyph.TeeRight, "├" ),
			( CursesLineGlyph.Crossing, "┼" )
		];

		foreach ( ( CursesLineGlyph glyph, string content ) in cases ) {
			CursesCell cell = CursesCell.Line( glyph );

			Assert.True( cell.IsLineGlyph );
			Assert.Equal( (CursesLineGlyph?)glyph, cell.LineGlyph );
			Assert.Equal( content, cell.Content );
			Assert.Equal( 1, cell.DisplayWidth );
			Assert.False( cell.IsContinuation );
			Assert.False( cell.IsBlank );
		}
	}

	[Fact]
	public void SemanticLineIdentityParticipatesInCellEquality() {
		CursesCell semantic = CursesCell.Line( CursesLineGlyph.Horizontal );
		CursesCell ordinary = new( "─" );

		Assert.Equal( semantic.Content, ordinary.Content );
		Assert.NotEqual( semantic, ordinary );
	}

	[Fact]
	public void SemanticLineFactoryPreservesStyleAndRejectsUnknownGlyph() {
		CursesStyle style = new(
			CursesColor.Indexed( 3 ),
			CursesColor.Default,
			CursesTextAttributes.Bold
		);
		CursesCell cell = CursesCell.Line(
			CursesLineGlyph.Crossing,
			style
		);

		Assert.Equal( style, cell.Style );
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesCell.Line( (CursesLineGlyph)99 )
		);
	}

	[Fact]
	public void WindowCompositionPreservesSemanticLineIdentity() {
		CursesScreen sourceScreen = new( 3, 1 );
		CursesScreen destinationScreen = new( 3, 1 );
		CursesWindow source = sourceScreen.StandardWindow;
		CursesWindow destination = destinationScreen.StandardWindow;
		source.WriteCell( CursesCell.Line( CursesLineGlyph.TeeRight ) );

		source.CopyRectangleTo(
			destination,
			0,
			0,
			1,
			1,
			0,
			1
		);

		CursesCell copied = destination.GetCell( 0, 1 );
		Assert.True( copied.IsLineGlyph );
		Assert.Equal( CursesLineGlyph.TeeRight, copied.LineGlyph );
		Assert.Equal( "├", copied.Content );
	}
}
