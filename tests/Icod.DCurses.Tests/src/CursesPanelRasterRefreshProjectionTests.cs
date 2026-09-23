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

using System.Reflection;
using System.Runtime.CompilerServices;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Drives the T1606 panel-composition to physical-refresh raster projection seam.</summary>
public sealed class CursesPanelRasterRefreshProjectionTests {
	[Fact]
	public void InitialPanelRefreshProjectionCarriesRasterAxis() {
		CursesSession session = CreateProjectionOnlySession();
		CursesScreen source = new( 3, 1 );
		CursesVirtualScreen composed = new( 3, 1 );
		CursesRasterCell token = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		composed.SetRasterCell( 0, 1, token );

		CursesScreen projection = SynchronizeProjection(
			session,
			source,
			composed
		);

		Assert.Equal( token, projection.VirtualScreen.GetRasterCell( 0, 1 ) );
	}

	[Fact]
	public void IncrementalPanelRefreshProjectionAddsRasterWithoutChangingVisualCell() {
		CursesSession session = CreateProjectionOnlySession();
		CursesScreen source = new( 3, 1 );
		CursesVirtualScreen composed = new( 3, 1 );
		_ = SynchronizeProjection(
			session,
			source,
			composed
		);
		composed.MarkClean();
		CursesRasterCell token = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		composed.SetRasterCell( 0, 2, token );

		CursesScreen projection = SynchronizeProjection(
			session,
			source,
			composed
		);

		Assert.Equal( token, projection.VirtualScreen.GetRasterCell( 0, 2 ) );
	}

	[Fact]
	public void IncrementalPanelRefreshProjectionRemovesStaleRaster() {
		CursesSession session = CreateProjectionOnlySession();
		CursesScreen source = new( 3, 1 );
		CursesVirtualScreen composed = new( 3, 1 );
		CursesScreen projection = SynchronizeProjection(
			session,
			source,
			composed
		);
		CursesRasterCell token = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		projection.VirtualScreen.SetRasterCell( 0, 1, token );
		projection.VirtualScreen.MarkClean();
		composed.MarkClean();
		composed.TouchCell( 0, 1 );

		projection = SynchronizeProjection(
			session,
			source,
			composed
		);

		Assert.Null( projection.VirtualScreen.GetRasterCell( 0, 1 ) );
	}

	[Fact]
	public async Task RasterInsideLineShiftFootprintForcesSemanticRepaint() {
		RecordingTerminalOutput output = new();
		TerminalDescription terminal = new TerminalDescriptionBuilder(
			"raster-line-shift-safety"
		)
			.SetString( StringCapability.CursorAddress, "C" )
			.SetString( StringCapability.DeleteLines, "D%p1%d" )
			.Build();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync( terminal, output );
		CursesScreen screen = new( 8, 3 );
		for ( int row = 0; row < screen.Rows; row++ ) {
			for ( int column = 0; column < screen.Columns; column++ ) {
				screen.VirtualScreen[ row, column ] = new CursesCell(
					((char)( 'A' + row )).ToString()
				);
			}
		}
		CursesRasterCell raster =
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell(
				context.Session
			);
		screen.VirtualScreen.SetRasterCell( 1, 3, raster );
		await context.Engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		screen.StandardWindow.DeleteLines();
		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.DoesNotContain( "D1", output.Text, StringComparison.Ordinal );
		Assert.Contains( "\U0010EEEE", output.Text, StringComparison.Ordinal );
		Assert.Equal( raster, screen.VirtualScreen.GetRasterCell( 0, 3 ) );
		Assert.Equal( 1, output.FlushCount );
	}

	private static CursesSession CreateProjectionOnlySession() {
		return (CursesSession)RuntimeHelpers.GetUninitializedObject(
			typeof( CursesSession )
		);
	}

	private static CursesScreen SynchronizeProjection(
		CursesSession session,
		CursesScreen source,
		CursesVirtualScreen composed
	) {
		MethodInfo method = Assert.IsAssignableFrom<MethodInfo>(
			typeof( CursesSession ).GetMethod(
				"SynchronizePanelRefreshProjection",
				BindingFlags.Instance | BindingFlags.NonPublic
			)
		);
		object? result = method.Invoke(
			session,
			[ source, composed ]
		);
		return Assert.IsType<CursesScreen>( result );
	}
}
