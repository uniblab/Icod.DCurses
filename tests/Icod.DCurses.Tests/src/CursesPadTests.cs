using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies the core off-screen pad backing surface.</summary>
public sealed class CursesPadTests {
	[Fact]
	public void PadDimensionsAreIndependentOfTerminalSizedScreens() {
		CursesPad pad = new(
			240,
			120
		);

		Assert.Equal( 240, pad.Columns );
		Assert.Equal( 120, pad.Rows );
		Assert.Equal( 240, pad.ContentWindow.Columns );
		Assert.Equal( 120, pad.ContentWindow.Rows );
		Assert.True( pad.ContentWindow.IsStandardWindow );
		Assert.Same(
			UnicodeCursesTextWidthProvider.Instance,
			pad.TextWidthProvider
		);
	}

	[Fact]
	public void ContentWindowReusesUnicodeAndEditingSemantics() {
		CursesPad pad = new(
			20,
			6
		);
		CursesWindow content = pad.ContentWindow;
		content.Move(
			2,
			3
		);
		content.Write( "A\u754CB" );

		Assert.Equal( "A", content.GetCell( 2, 3 ).Content );
		Assert.Equal( "\u754C", content.GetCell( 2, 4 ).Content );
		Assert.Equal( 2, content.GetCell( 2, 4 ).DisplayWidth );
		Assert.True( content.GetCell( 2, 5 ).IsContinuation );
		Assert.Equal( "B", content.GetCell( 2, 6 ).Content );

		content.Move(
			2,
			3
		);
		content.InsertCells( 2 );

		Assert.True( content.GetCell( 2, 3 ).IsBlank );
		Assert.True( content.GetCell( 2, 4 ).IsBlank );
		Assert.Equal( "A", content.GetCell( 2, 5 ).Content );
		Assert.Equal( "\u754C", content.GetCell( 2, 6 ).Content );
		Assert.True( content.GetCell( 2, 7 ).IsContinuation );
		Assert.Equal( "B", content.GetCell( 2, 8 ).Content );
	}

	[Fact]
	public void SubwindowSharesPadBackingStorage() {
		CursesPad pad = new(
			30,
			20
		);
		CursesWindow view = pad.ContentWindow.CreateSubwindow(
			7,
			11,
			3,
			6
		);

		view.Write( "HELLO" );

		Assert.Equal( "H", pad.ContentWindow.GetCell( 7, 11 ).Content );
		Assert.Equal( "E", pad.ContentWindow.GetCell( 7, 12 ).Content );
		Assert.Equal( "L", pad.ContentWindow.GetCell( 7, 13 ).Content );
		Assert.Equal( "L", pad.ContentWindow.GetCell( 7, 14 ).Content );
		Assert.Equal( "O", pad.ContentWindow.GetCell( 7, 15 ).Content );
	}

	[Fact]
	public void PadUsesCallerSuppliedWidthProvider() {
		FixedWidthProvider provider = new( 2 );
		CursesPad pad = new(
			8,
			2,
			provider
		);

		pad.ContentWindow.Write( "x" );

		Assert.Same( provider, pad.TextWidthProvider );
		Assert.Equal( "x", pad.ContentWindow.GetCell( 0, 0 ).Content );
		Assert.Equal( 2, pad.ContentWindow.GetCell( 0, 0 ).DisplayWidth );
		Assert.True( pad.ContentWindow.GetCell( 0, 1 ).IsContinuation );
	}

	[Fact]
	public void PadRejectsInvalidOrUnaddressableDimensions() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesPad(
				0,
				1
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesPad(
				1,
				0
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesPad(
				int.MaxValue,
				2
			)
		);
	}

	private sealed class FixedWidthProvider
		: ICursesTextWidthProvider {
		private readonly int width;

		internal FixedWidthProvider( int width ) {
			if ( 0 > width || 2 < width ) {
				throw new ArgumentOutOfRangeException( nameof( width ) );
			}

			this.width = width;
		}

		public int GetWidth( string textElement ) {
			ArgumentException.ThrowIfNullOrEmpty( textElement );
			return width;
		}
	}
}
