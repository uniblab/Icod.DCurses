/*
	Icod.DCurses.Tests
	Automated test suite for Icod.DCurses.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Text;
using Icod.DCurses.Internal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises application-shaped workloads through the T2004 editing transaction path.</summary>
public sealed class CursesVerticalCutoverApplicationTests {
	private const string RasterPlaceholder = "\U0010EEEE";

	[Fact]
	public async Task RoguelikeFrameCommitsSparseStyledUnicodeAndAcsContentAtomically() {
		RecordingTerminalOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesScreen screen = new( 12, 5 );
		CursesStyle bold = new(
			CursesColor.Indexed( 2 ),
			CursesColor.Default,
			CursesTextAttributes.Bold
		);
		screen.VirtualScreen[ 0, 0 ] = CursesCell.Line(
			CursesLineGlyph.Horizontal,
			bold
		);
		screen.StandardWindow.Move( 1, 1 );
		screen.StandardWindow.Write( "界", bold );
		screen.StandardWindow.Move( 2, 5 );
		screen.StandardWindow.WriteWithMetadata(
			"@",
			new CursesCellMetadata(
				new CursesHyperlink(
					"https://example.test/rogue",
					"rogue"
				)
			)
		);
		CursesWindow messageLog = screen.CreateWindow( 3, 0, 2, 12 );
		messageLog.Write( "old message" );
		messageLog.Move( 1, 0 );
		messageLog.Write( "new message" );
		output.Clear();

		Assert.Equal( string.Empty, output.Text );
		await context.Engine.RefreshAsync( screen, 3, 0 );

		string text = output.Text;
		Assert.Contains( "<smacs>=<rmacs>", text );
		Assert.Contains( "界", text );
		Assert.Contains( "\u001b]8;id=rogue;https://example.test/rogue\u001b\\", text );
		Assert.EndsWith( "<cup:3,0>", text );
		Assert.DoesNotContain( "<ich:", text );
		Assert.DoesNotContain( "<dl:", text );
		Assert.DoesNotContain( "<el>", text );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		messageLog.Move( 0, 0 );
		messageLog.DeleteLines();
		await context.Engine.RefreshAsync( screen, 3, 0 );

		Assert.Equal( "<dl:1>", output.Text );
		Assert.Equal( "new message", ReadText( screen, 3, 0, 11 ) );
		Assert.DoesNotContain( "<smacs>", output.Text, StringComparison.Ordinal );
		Assert.DoesNotContain( "界", output.Text, StringComparison.Ordinal );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await context.Engine.RefreshAsync( screen, 3, 0 );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public async Task PixelArtFrameOrdersTextPanelsAndRasterPlaceholdersInOneCommit() {
		RecordingTerminalOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesScreen screen = new( 8, 3 );
		screen.StandardWindow.Move( 0, 0 );
		screen.StandardWindow.Write( "HUD" );
		screen.VirtualScreen.SetRasterCell(
			1,
			0,
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell(
				context.Session
			)
		);
		screen.StandardWindow.Move( 1, 1 );
		screen.StandardWindow.Write( "PIX" );
		screen.StandardWindow.Move( 2, 0 );
		screen.StandardWindow.Write( "STATUS" );
		output.Clear();

		Assert.Equal( string.Empty, output.Text );
		await context.Engine.RefreshAsync( screen, 2, 6 );

		string text = output.Text;
		int hud = text.IndexOf( "HUD", StringComparison.Ordinal );
		int raster = text.IndexOf( RasterPlaceholder, StringComparison.Ordinal );
		int pixels = text.IndexOf( "PIX", StringComparison.Ordinal );
		int status = text.IndexOf( "STATUS", StringComparison.Ordinal );
		Assert.True( 0 <= hud );
		Assert.True( hud < raster );
		Assert.True( raster < pixels );
		Assert.True( pixels < status );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task SpriteLikeCellAlignedRasterMovementClipsAndRepaintsDeterministically() {
		RecordingTerminalOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesScreen screen = new( 40, 2 );
		CursesRasterCell sprite =
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell(
				context.Session
			);
		screen.StandardWindow.Write( "HUD:ABCDEFGHIJKLMNOPQRSTUVWXYZ" );
		screen.VirtualScreen.SetRasterCell( 1, 0, sprite );
		await context.Engine.RefreshAsync( screen, 0, 1 );
		output.Clear();

		screen.StandardWindow.Move( 0, 1 );
		screen.StandardWindow.InsertCells( 2 );
		screen.VirtualScreen.SetRasterCell( 1, 0, null );
		screen.VirtualScreen.SetRasterCell( 1, 30, sprite );
		await context.Engine.RefreshAsync( screen, 0, 1 );

		string text = output.Text;
		int hudShift = text.IndexOf( "<ich:2>", StringComparison.Ordinal );
		int spriteMove = text.IndexOf( "<cup:1,30>", StringComparison.Ordinal );
		int spriteCell = text.IndexOf( RasterPlaceholder, StringComparison.Ordinal );
		Assert.True( 0 <= hudShift );
		Assert.True( 0 <= spriteMove );
		Assert.True( hudShift < spriteMove );
		Assert.True( spriteMove < spriteCell );
		Assert.Equal( 1, CountOccurrences( text, RasterPlaceholder ) );
		Assert.DoesNotContain( "<dl:", text );
		Assert.DoesNotContain( "<el>", text );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await context.Engine.RefreshAsync( screen, 0, 1 );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public async Task TileMapRejectsLineShiftAcrossRasterWhileRepaintingTextTiles() {
		RecordingTerminalOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesScreen screen = new( 12, 3 );
		screen.StandardWindow.Move( 0, 0 );
		screen.StandardWindow.Write( "TOP" );
		screen.StandardWindow.Move( 1, 0 );
		screen.StandardWindow.Write( "T界" );
		screen.VirtualScreen[ 1, 4 ] = CursesCell.Line( CursesLineGlyph.Horizontal );
		CursesRasterCell rasterTile =
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell(
				context.Session
			);
		screen.VirtualScreen.SetRasterCell(
			1,
			6,
			rasterTile
		);
		screen.StandardWindow.Move( 2, 0 );
		screen.StandardWindow.Write( "BOTTOM" );
		await context.Engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		screen.StandardWindow.Move( 0, 0 );
		screen.StandardWindow.DeleteLines();
		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "T界", output.Text, StringComparison.Ordinal );
		Assert.Contains( "<smacs>=<rmacs>", output.Text, StringComparison.Ordinal );
		Assert.Contains( RasterPlaceholder, output.Text, StringComparison.Ordinal );
		Assert.DoesNotContain( "<dl:", output.Text, StringComparison.Ordinal );
		Assert.DoesNotContain( "<ich:", output.Text, StringComparison.Ordinal );
		Assert.Equal( rasterTile, screen.VirtualScreen.GetRasterCell( 0, 6 ) );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task EditorFrameUsesCharacterLineEraseAndInteriorRegionPlans() {
		RecordingTerminalOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateEditingTerminal(),
				output
			);
		CursesScreen screen = new( 24, 6 );
		CursesWindow editor = screen.StandardWindow;
		editor.Move( 1, 0 );
		editor.Write( "abcdefghijklmnopqrst" );
		for ( int row = 2; row < screen.Rows; row++ ) {
			editor.Move( row, 0 );
			editor.Write( $"editor-line-{row:D2}" );
		}
		await context.Engine.RefreshAsync( screen, 1, 2 );
		output.Clear();

		editor.Move( 1, 2 );
		editor.InsertCells( 2 );
		await context.Engine.RefreshAsync( screen, 1, 2 );
		Assert.Equal( "I2", output.Text );
		output.Clear();

		editor.DeleteCells( 2 );
		await context.Engine.RefreshAsync( screen, 1, 2 );
		Assert.Equal( "X2", output.Text );
		output.Clear();

		editor.Move( 1, 8 );
		editor.ClearToEndOfLine();
		await context.Engine.RefreshAsync( screen, 2, 0 );
		Assert.Contains( "E", output.Text, StringComparison.Ordinal );
		output.Clear();

		editor.Move( 2, 0 );
		editor.InsertLines();
		await context.Engine.RefreshAsync( screen, 2, 0 );
		Assert.Equal( "N1", output.Text );
		output.Clear();

		editor.DeleteLines();
		await context.Engine.RefreshAsync( screen, 2, 0 );
		Assert.Equal( "D1", output.Text );
		output.Clear();

		CursesWindow region = screen.CreateWindow( 2, 0, 2, screen.Columns );
		region.DeleteLines();
		await context.Engine.RefreshAsync( screen, 0, 0 );
		Assert.Equal( "RCLRC", output.Text );
		output.Clear();

		editor.Move( 0, 0 );
		CursesStyle underline = CursesStyle.Default.WithAttributes(
			CursesTextAttributes.Underline
		);
		editor.Write( "++", underline );
		await context.Engine.RefreshAsync( screen, 0, 2 );
		Assert.Contains( "<u>", output.Text, StringComparison.Ordinal );
		Assert.Equal( underline, editor.GetCell( 0, 0 ).Style );
		Assert.Equal( "++", ReadText( screen, 0, 0, 2 ) );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await context.Engine.RefreshAsync( screen, 0, 2 );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.FlushCount );
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "vertical-cutover-applications" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetNumber( NumericCapability.Colors, 8 )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.SetString( StringCapability.EnterUnderlineMode, "<underline>" )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.SetString( StringCapability.EnterAlternateCharacterSetMode, "<smacs>" )
			.SetString( StringCapability.ExitAlternateCharacterSetMode, "<rmacs>" )
			.SetString( StringCapability.AlternateCharacterSet, "q=" )
			.SetString( StringCapability.ClearToEndOfLine, "<el>" )
			.SetString( StringCapability.InsertCharacters, "<ich:%p1%d>" )
			.SetString( StringCapability.DeleteLines, "<dl:%p1%d>" )
			.Build();
	}

	private static TerminalDescription CreateEditingTerminal() {
		return new TerminalDescriptionBuilder( "vertical-cutover-editor" )
			.SetString( StringCapability.CursorAddress, "C" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetString( StringCapability.EnterUnderlineMode, "<u>" )
			.SetString( StringCapability.ClearToEndOfLine, "E" )
			.SetString( StringCapability.InsertCharacters, "I%p1%d" )
			.SetString( StringCapability.DeleteCharacters, "X%p1%d" )
			.SetString( StringCapability.InsertLines, "N%p1%d" )
			.SetString( StringCapability.DeleteLines, "D%p1%d" )
			.SetString( StringCapability.ChangeScrollRegion, "R" )
			.SetString( StringCapability.DeleteLine, "L" )
			.Build();
	}

	private static string ReadText(
		CursesScreen screen,
		int row,
		int startColumn,
		int length
	) {
		StringBuilder result = new();
		for ( int column = startColumn; column < startColumn + length; column++ ) {
			CursesCell cell = screen.VirtualScreen[ row, column ];
			if ( !cell.IsContinuation ) {
				result.Append( cell.IsBlank ? " " : cell.Content );
			}
		}
		return result.ToString();
	}

	private static int CountOccurrences(
		string source,
		string value
	) {
		int count = 0;
		int offset = 0;
		while ( true ) {
			int match = source.IndexOf( value, offset, StringComparison.Ordinal );
			if ( 0 > match ) {
				return count;
			}
			count++;
			offset = match + value.Length;
		}
	}
}
