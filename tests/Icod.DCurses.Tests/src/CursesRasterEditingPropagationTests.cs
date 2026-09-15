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
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes T1604 raster propagation through existing window editing and composition paths.</summary>
public sealed class CursesRasterEditingPropagationTests {
	[Fact]
	public void InsertAndDeleteCellsMoveRasterWithLogicalState() {
		CursesScreen screen = new( 6, 1 );
		CursesWindow window = screen.StandardWindow;
		CursesRasterCell token = CreateToken();
		window.SetRasterCell( 0, 2, token );

		window.Move( 0, 1 );
		window.InsertCells();
		Assert.Null( window.GetRasterCell( 0, 2 ) );
		AssertSameToken( token, window.GetRasterCell( 0, 3 ) );

		window.Move( 0, 1 );
		window.DeleteCells();
		Assert.Null( window.GetRasterCell( 0, 3 ) );
		AssertSameToken( token, window.GetRasterCell( 0, 2 ) );
	}

	[Fact]
	public void InsertAndDeleteLinesMoveRasterWithLogicalState() {
		CursesScreen screen = new( 4, 4 );
		CursesWindow window = screen.StandardWindow;
		CursesRasterCell token = CreateToken();
		window.SetRasterCell( 1, 2, token );

		window.Move( 0, 0 );
		window.InsertLines();
		Assert.Null( window.GetRasterCell( 1, 2 ) );
		AssertSameToken( token, window.GetRasterCell( 2, 2 ) );

		window.Move( 0, 0 );
		window.DeleteLines();
		Assert.Null( window.GetRasterCell( 2, 2 ) );
		AssertSameToken( token, window.GetRasterCell( 1, 2 ) );
	}

	[Fact]
	public void ExplicitScrollMovesRasterAndClearsVacatedRows() {
		CursesScreen screen = new( 4, 3 );
		CursesWindow window = screen.StandardWindow;
		CursesRasterCell token = CreateToken();
		window.SetRasterCell( 1, 1, token );

		window.ScrollUp();
		AssertSameToken( token, window.GetRasterCell( 0, 1 ) );
		Assert.Null( window.GetRasterCell( 1, 1 ) );
		Assert.Null( window.GetRasterCell( 2, 1 ) );

		window.ScrollDown();
		Assert.Null( window.GetRasterCell( 0, 1 ) );
		AssertSameToken( token, window.GetRasterCell( 1, 1 ) );
	}

	[Fact]
	public void ClearAndRegionReplacementRemoveRasterState() {
		CursesScreen screen = new( 5, 2 );
		CursesWindow window = screen.StandardWindow;
		CursesRasterCell first = CreateToken();
		CursesRasterCell second = CreateToken();
		window.SetRasterCell( 0, 1, first );
		window.SetRasterCell( 0, 3, second );

		window.Move( 0, 2 );
		window.ClearToBeginningOfLine();
		Assert.Null( window.GetRasterCell( 0, 1 ) );
		AssertSameToken( second, window.GetRasterCell( 0, 3 ) );

		window.FillRectangle(
			0,
			3,
			1,
			1,
			CursesCell.Blank()
		);
		Assert.Null( window.GetRasterCell( 0, 3 ) );
	}

	[Fact]
	public void SubwindowEditingMovesRasterInsideSharedProjection() {
		CursesScreen screen = new( 7, 1 );
		CursesWindow subwindow = screen.StandardWindow.CreateSubwindow( 0, 1, 1, 5 );
		CursesRasterCell token = CreateToken();
		subwindow.SetRasterCell( 0, 1, token );

		subwindow.Move( 0, 0 );
		subwindow.InsertCells();

		Assert.Null( subwindow.GetRasterCell( 0, 1 ) );
		AssertSameToken( token, subwindow.GetRasterCell( 0, 2 ) );
		AssertSameToken( token, screen.VirtualScreen.GetRasterCell( 0, 3 ) );
	}

	[Fact]
	public void DestructiveCopyTransfersBlankRasterCoordinate() {
		CursesScreen sourceScreen = new( 4, 1 );
		CursesScreen destinationScreen = new( 4, 1 );
		CursesWindow source = sourceScreen.StandardWindow;
		CursesWindow destination = destinationScreen.StandardWindow;
		CursesRasterCell token = CreateToken();
		source.SetRasterCell( 0, 1, token );
		destinationScreen.VirtualScreen.SetCell( 0, 1, new CursesCell( "X", default ) );

		source.CopyRectangleTo(
			destination,
			0,
			1,
			1,
			1,
			0,
			1
		);

		Assert.True( destination.GetCell( 0, 1 ).IsBlank );
		AssertSameToken( token, destination.GetRasterCell( 0, 1 ) );
	}

	[Fact]
	public void OverlayTreatsBlankRasterCoordinateAsPresent() {
		CursesScreen sourceScreen = new( 3, 1 );
		CursesScreen destinationScreen = new( 3, 1 );
		CursesWindow source = sourceScreen.StandardWindow;
		CursesWindow destination = destinationScreen.StandardWindow;
		CursesRasterCell token = CreateToken();
		source.SetRasterCell( 0, 1, token );
		destinationScreen.VirtualScreen.SetCell( 0, 1, new CursesCell( "D", default ) );

		source.OverlayRectangleTo(
			destination,
			0,
			1,
			1,
			1,
			0,
			1
		);

		Assert.True( destination.GetCell( 0, 1 ).IsBlank );
		AssertSameToken( token, destination.GetRasterCell( 0, 1 ) );
	}

	[Fact]
	public void OverlappingCopyUsesRasterSnapshotRatherThanMutationOrder() {
		CursesScreen screen = new( 5, 1 );
		CursesWindow window = screen.StandardWindow;
		CursesRasterCell first = CreateToken();
		CursesRasterCell second = CreateToken();
		window.SetRasterCell( 0, 0, first );
		window.SetRasterCell( 0, 1, second );

		window.CopyRectangleTo(
			window,
			0,
			0,
			1,
			2,
			0,
			1
		);

		AssertSameToken( first, window.GetRasterCell( 0, 1 ) );
		AssertSameToken( second, window.GetRasterCell( 0, 2 ) );
	}

	[Fact]
	public void SameSessionTransferAcceptsOwnedRasterReference() {
		CursesSession owner = CreateUninitializedSession();
		CursesScreen sourceScreen = new( 2, 1 );
		CursesScreen destinationScreen = new( 2, 1 );
		BindSessionOwner( destinationScreen, owner );
		CursesRasterCell token = CreateToken( owner );
		sourceScreen.StandardWindow.SetRasterCell( 0, 0, token );

		sourceScreen.StandardWindow.CopyRectangleTo(
			destinationScreen.StandardWindow,
			0,
			0,
			1,
			1,
			0,
			0
		);

		AssertSameToken( token, destinationScreen.StandardWindow.GetRasterCell( 0, 0 ) );
	}

	[Fact]
	public void ForeignSessionTransferIsRejectedBeforeDestinationMutation() {
		CursesSession sourceOwner = CreateUninitializedSession();
		CursesSession destinationOwner = CreateUninitializedSession();
		CursesScreen sourceScreen = new( 2, 1 );
		CursesScreen destinationScreen = new( 2, 1 );
		BindSessionOwner( destinationScreen, destinationOwner );
		CursesRasterCell foreign = CreateToken( sourceOwner );
		sourceScreen.StandardWindow.SetRasterCell( 0, 0, foreign );
		CursesCell sentinel = new( "D", default );
		destinationScreen.VirtualScreen.SetCell( 0, 0, sentinel );

		Assert.Throws<InvalidOperationException>(
			() => sourceScreen.StandardWindow.CopyRectangleTo(
				destinationScreen.StandardWindow,
				0,
				0,
				1,
				1,
				0,
				0
			)
		);

		Assert.Equal( sentinel, destinationScreen.StandardWindow.GetCell( 0, 0 ) );
		Assert.Null( destinationScreen.StandardWindow.GetRasterCell( 0, 0 ) );
	}

	private static CursesRasterCell CreateToken() {
		return CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
	}

	private static CursesRasterCell CreateToken( CursesSession owner ) {
		ArgumentNullException.ThrowIfNull( owner );
		CursesRasterCell token = CreateToken();
		FieldInfo? ownerField = typeof( CursesRasterPlaceholder ).GetField(
			"owner",
			BindingFlags.Instance | BindingFlags.NonPublic
		);
		Assert.NotNull( ownerField );
		ownerField!.SetValue(
			token.Placeholder,
			owner
		);
		return token;
	}

	private static CursesSession CreateUninitializedSession() {
		return (CursesSession)RuntimeHelpers.GetUninitializedObject( typeof( CursesSession ) );
	}

	private static void BindSessionOwner(
		CursesScreen screen,
		CursesSession owner
	) {
		ArgumentNullException.ThrowIfNull( screen );
		ArgumentNullException.ThrowIfNull( owner );
		MethodInfo? bind = typeof( CursesScreen ).GetMethod(
			"BindRasterSessionOwner",
			BindingFlags.Instance | BindingFlags.NonPublic
		);
		Assert.True(
			bind is not null,
			"T1604 requires a single internal CursesScreen raster-session ownership seam."
		);
		_ = bind!.Invoke(
			screen,
			[ owner ]
		);
	}

	private static void AssertSameToken(
		CursesRasterCell expected,
		CursesRasterCell? actual
	) {
		Assert.True( actual.HasValue );
		Assert.True( actual.Value.IsValid );
		Assert.Same(
			expected.Placeholder,
			actual.Value.Placeholder
		);
	}
}
