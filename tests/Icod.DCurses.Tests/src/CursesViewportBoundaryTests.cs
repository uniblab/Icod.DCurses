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

using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Specifies viewport boundaries and validation.</summary>
public sealed class CursesViewportBoundaryTests {
	[Fact]
	public void PositionsAndOutOfContentTargetsAreValidated() {
		Assert.Equal( "row", Assert.Throws<ArgumentOutOfRangeException>( () => new CursesCellPosition( -1, 0 ) ).ParamName );
		Assert.Equal( "column", Assert.Throws<ArgumentOutOfRangeException>( () => new CursesCellPosition( 0, -1 ) ).ParamName );
		CursesViewport viewport = new CursesViewport( 100, 200, 10, 20 );
		Assert.Equal( "position", Assert.Throws<ArgumentOutOfRangeException>( () => viewport.EnsureVisible( new CursesCellPosition( 100, 0 ) ) ).ParamName );
		Assert.Equal( "rectangle", Assert.Throws<ArgumentOutOfRangeException>( () => viewport.EnsureVisible( new CursesRectangle( 90, 0, 11, 1 ) ) ).ParamName );
		Assert.Equal( "overscanColumns", Assert.Throws<ArgumentOutOfRangeException>( () => viewport.GetVisibleContent( 0, -1 ) ).ParamName );
	}

	[Fact]
	public void EmptyAndZeroSizedAxesCannotExposeCells() {
		CursesViewport empty = new CursesViewport( 0, 0, 10, 20, 12, 18 );
		Assert.Equal( new CursesRectangle( 0, 0, 0, 0 ), empty.VisibleContent );
		Assert.False( empty.TryContentToViewport( default, out CursesCellPosition absent ) );
		Assert.Equal( default, absent );
		CursesViewport zeroRows = new CursesViewport( 100, 200, 0, 20, 30, 40 );
		Assert.Equal( new CursesViewport( 100, 200, 0, 20, 30, 41 ), zeroRows.EnsureVisible( new CursesCellPosition( 70, 60 ) ) );
		Assert.False( zeroRows.TryViewportToContent( default, out absent ) );
		Assert.Equal( default, absent );
	}

	[Fact]
	public void OverscanAndTranslationsAreSafeAtNumericLimits() {
		CursesViewport viewport = new CursesViewport( int.MaxValue, int.MaxValue, 10, 20, int.MaxValue, int.MaxValue );
		Assert.Equal( new CursesRectangle( int.MaxValue - 10, int.MaxValue - 20, 10, 20 ), viewport.VisibleContent );
		Assert.Equal( new CursesRectangle( 0, 0, int.MaxValue, int.MaxValue ), viewport.GetVisibleContent( int.MaxValue, int.MaxValue ) );
		Assert.True( viewport.TryViewportToContent( new CursesCellPosition( 9, 19 ), out CursesCellPosition finalCell ) );
		Assert.Equal( new CursesCellPosition( int.MaxValue - 1, int.MaxValue - 1 ), finalCell );
		Assert.False( viewport.TryContentToViewport( new CursesCellPosition( int.MaxValue - 11, 0 ), out CursesCellPosition absent ) );
		Assert.Equal( default, absent );
	}
}
