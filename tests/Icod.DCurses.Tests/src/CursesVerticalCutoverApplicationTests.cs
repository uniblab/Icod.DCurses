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

using Icod.DCurses.Internal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises application-shaped workloads through the T2003 Terminal transaction path.</summary>
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
		CursesScreen screen = new( 12, 3 );
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
		output.Clear();

		Assert.Equal( string.Empty, output.Text );
		await context.Engine.RefreshAsync( screen, 2, 6 );

		string text = output.Text;
		Assert.Contains( "<smacs>=<rmacs>", text );
		Assert.Contains( "界", text );
		Assert.Contains( "\u001b]8;id=rogue;https://example.test/rogue\u001b\\", text );
		Assert.EndsWith( "<cup:2,6>", text );
		Assert.DoesNotContain( "<ich:", text );
		Assert.DoesNotContain( "<dl:", text );
		Assert.DoesNotContain( "<el>", text );
		Assert.Equal( 1, output.FlushCount );
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
		CursesScreen screen = new( 6, 1 );
		CursesRasterCell sprite =
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell(
				context.Session
			);
		screen.VirtualScreen.SetRasterCell( 0, 0, sprite );
		await context.Engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		screen.VirtualScreen.SetRasterCell( 0, 0, null );
		screen.VirtualScreen.SetRasterCell( 0, 4, sprite );
		await context.Engine.RefreshAsync( screen, 0, 5 );

		string text = output.Text;
		int spriteMove = text.IndexOf( "<cup:0,4>", StringComparison.Ordinal );
		int spriteCell = text.IndexOf( RasterPlaceholder, StringComparison.Ordinal );
		Assert.StartsWith( " ", text, StringComparison.Ordinal );
		Assert.True( 0 <= spriteMove );
		Assert.True( spriteMove < spriteCell );
		Assert.Equal( 1, CountOccurrences( text, RasterPlaceholder ) );
		Assert.DoesNotContain( "<ich:", text );
		Assert.DoesNotContain( "<dl:", text );
		Assert.DoesNotContain( "<el>", text );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await context.Engine.RefreshAsync( screen, 0, 5 );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public async Task EditorFramePreservesSparseTextCursorAndRenditionWithRewriteFallback() {
		RecordingTerminalOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesScreen screen = new( 12, 3 );
		CursesWindow editor = screen.StandardWindow;
		editor.Move( 1, 0 );
		editor.Write( "alpha界" );
		await context.Engine.RefreshAsync( screen, 1, 7 );
		output.Clear();

		editor.Move( 1, 2 );
		editor.InsertCells( 2 );
		editor.Write(
			"++",
			new CursesStyle(
				CursesColor.Default,
				CursesColor.Default,
				CursesTextAttributes.Underline
			)
		);
		await context.Engine.RefreshAsync( screen, 1, 4 );

		Assert.Equal( "++", editor.GetCell( 1, 2 ).Content + editor.GetCell( 1, 3 ).Content );
		Assert.Contains( "<underline>", output.Text );
		Assert.Contains( "++", output.Text );
		Assert.Contains( "pha界", output.Text );
		Assert.DoesNotContain( "<ich:", output.Text );
		Assert.DoesNotContain( "<dl:", output.Text );
		Assert.DoesNotContain( "<el>", output.Text );
		Assert.EndsWith( "<cup:1,4>", output.Text );
		Assert.Equal( 1, output.FlushCount );
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
