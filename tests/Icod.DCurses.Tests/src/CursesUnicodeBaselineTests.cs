using System.Text;
using Icod.DCurses;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class CursesUnicodeBaselineTests {
	[Fact]
	public void AsciiStillConsumesOneCellPerTextElement() {
		CursesScreen screen = new( 4, 1 );

		screen.StandardWindow.Write( "AB" );

		Assert.Equal( "A", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( 1, screen.VirtualScreen[ 0, 0 ].DisplayWidth );
		Assert.Equal( "B", screen.VirtualScreen[ 0, 1 ].Content );
		Assert.Equal( 2, screen.StandardWindow.CursorColumn );
	}

	[Fact]
	public void SupplementaryScalarIsNotSplitAcrossCells() {
		CursesScreen screen = new(
			4,
			1,
			new FixedWidthProvider( 1 )
		);
		string scalar = char.ConvertFromUtf32( 0x1D11E );

		screen.StandardWindow.Write( scalar );

		Assert.Equal( scalar, screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( 1, screen.VirtualScreen[ 0, 0 ].DisplayWidth );
		Assert.True( screen.VirtualScreen[ 0, 1 ].IsBlank );
		Assert.Equal( 1, screen.StandardWindow.CursorColumn );
	}

	[Fact]
	public void CombiningMarkAttachesToPreviousVisibleCell() {
		CursesScreen screen = new( 4, 1 );
		string text = "e\u0301";

		screen.StandardWindow.Write( text );

		Assert.Equal( text, screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( 1, screen.VirtualScreen[ 0, 0 ].DisplayWidth );
		Assert.True( screen.VirtualScreen[ 0, 1 ].IsBlank );
		Assert.Equal( 1, screen.StandardWindow.CursorColumn );
	}

	[Fact]
	public void WideElementUsesContinuationCell() {
		CursesScreen screen = new( 4, 1 );

		screen.StandardWindow.Write( "界X" );

		Assert.Equal( "界", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( 2, screen.VirtualScreen[ 0, 0 ].DisplayWidth );
		Assert.True( screen.VirtualScreen[ 0, 1 ].IsContinuation );
		Assert.Equal( "X", screen.VirtualScreen[ 0, 2 ].Content );
		Assert.Equal( 3, screen.StandardWindow.CursorColumn );
	}

	[Fact]
	public void ClipModeDoesNotEmitHalfOfWideElement() {
		CursesScreen screen = new( 3, 1 );
		CursesWindow window = screen.StandardWindow;
		window.WrapMode = CursesWrapMode.Clip;
		window.Move( 0, 2 );

		window.Write( "界" );

		Assert.True( screen.VirtualScreen[ 0, 2 ].IsBlank );
		Assert.Equal( 2, window.CursorColumn );
	}

	[Fact]
	public void WideElementWrapsAsWholeUnit() {
		CursesScreen screen = new( 3, 2 );
		CursesWindow window = screen.StandardWindow;
		window.Move( 0, 2 );

		window.Write( "界" );

		Assert.True( screen.VirtualScreen[ 0, 2 ].IsBlank );
		Assert.Equal( "界", screen.VirtualScreen[ 1, 0 ].Content );
		Assert.True( screen.VirtualScreen[ 1, 1 ].IsContinuation );
		Assert.Equal( 1, window.CursorRow );
		Assert.Equal( 2, window.CursorColumn );
	}

	[Fact]
	public void OverwritingWideContinuationRepairsWholeOldGlyph() {
		CursesScreen screen = new( 4, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Write( "界" );
		window.Move( 0, 1 );

		window.Write( "X" );

		Assert.True( screen.VirtualScreen[ 0, 0 ].IsBlank );
		Assert.Equal( "X", screen.VirtualScreen[ 0, 1 ].Content );
		Assert.False( screen.VirtualScreen[ 0, 1 ].IsContinuation );
	}

	[Fact]
	public void OverwritingWideLeaderRepairsContinuation() {
		CursesScreen screen = new( 4, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Write( "界" );
		window.Move( 0, 0 );

		window.Write( "X" );

		Assert.Equal( "X", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.True( screen.VirtualScreen[ 0, 1 ].IsBlank );
	}

	[Fact]
	public void MalformedUtf16BecomesReplacementCharacter() {
		CursesScreen screen = new(
			4,
			1,
			new FixedWidthProvider( 1 )
		);
		string malformed = new( new[] { '\uD800' } );

		screen.StandardWindow.Write( malformed );

		Assert.Equal(
			Rune.ReplacementChar.ToString(),
			screen.VirtualScreen[ 0, 0 ].Content
		);
		Assert.Equal( 1, screen.StandardWindow.CursorColumn );
	}

	[Fact]
	public void WidthProviderIsReplaceableWithoutChangingWindowApi() {
		CursesScreen screen = new(
			4,
			1,
			new FixedWidthProvider( 2 )
		);

		screen.StandardWindow.Write( "A" );

		Assert.Equal( 2, screen.VirtualScreen[ 0, 0 ].DisplayWidth );
		Assert.True( screen.VirtualScreen[ 0, 1 ].IsContinuation );
		Assert.Equal( 2, screen.StandardWindow.CursorColumn );
	}

	private sealed class FixedWidthProvider
		: ICursesTextWidthProvider {
		private readonly int width;

		internal FixedWidthProvider( int width ) {
			this.width = width;
		}

		public int GetWidth( string textElement ) {
			ArgumentException.ThrowIfNullOrEmpty( textElement );
			return width;
		}
	}
}
