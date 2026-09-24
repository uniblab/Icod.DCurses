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

/// <summary>Specifies pure large-content viewport geometry.</summary>
public sealed class CursesViewportTests {
	[Fact]
	public void ConstructorClampsOriginAndReportsVisibleContent() {
		CursesViewport viewport = new( 100, 200, 20, 40, 99, 199 );

		Assert.Equal( 80, viewport.OriginRow );
		Assert.Equal( 160, viewport.OriginColumn );
		Assert.Equal(
			new CursesRectangle( 80, 160, 20, 40 ),
			viewport.VisibleContent
		);
	}

	[Fact]
	public void ResizingPreservesTheValidOriginAndClampsWhenNecessary() {
		CursesViewport viewport = new CursesViewport( 100, 200, 20, 40, 50, 60 );

		Assert.Equal( new CursesViewport( 60, 80, 20, 40, 40, 40 ), viewport.WithContentExtent( 60, 80 ) );
		Assert.Equal( new CursesViewport( 100, 200, 80, 160, 20, 40 ), viewport.WithViewportExtent( 80, 160 ) );
		Assert.Equal( new CursesViewport( 0, 0, 20, 40 ), viewport.WithContentExtent( 0, 0 ) );
		Assert.Equal( new CursesRectangle( 0, 0, 0, 0 ), viewport.WithContentExtent( 0, 0 ).VisibleContent );
		Assert.Equal( new CursesViewport( 100, 200, 200, 300 ), viewport.WithViewportExtent( 200, 300 ) );
	}

	[Fact]
	public void MovementAndPagesClampIndependentlyOnBothAxes() {
		CursesViewport viewport = new CursesViewport( 100, 200, 20, 40, 50, 60 );

		Assert.Equal( new CursesViewport( 100, 200, 20, 40, 0, 160 ), viewport.MoveTo( 0, int.MaxValue ) );
		Assert.Equal( new CursesViewport( 100, 200, 20, 40, 0, 0 ), viewport.MoveToStart() );
		Assert.Equal( new CursesViewport( 100, 200, 20, 40, 80, 160 ), viewport.MoveToEnd() );
		Assert.Equal( new CursesViewport( 100, 200, 20, 40, 48, 63 ), viewport.PanBy( -2, 3 ) );
		Assert.Equal( new CursesViewport( 100, 200, 20, 40, 70, 20 ), viewport.PageBy( 1, -1 ) );
	}

	[Fact]
	public void ZeroSizedViewportAndExtremeMovementNeverWrap() {
		CursesViewport viewport = new CursesViewport( int.MaxValue, int.MaxValue, 0, 0, 100, 100 );

		Assert.Equal( new CursesRectangle( 100, 100, 0, 0 ), viewport.VisibleContent );
		Assert.Equal( viewport, viewport.PageBy( int.MaxValue, int.MinValue ) );
		Assert.Equal( int.MaxValue, viewport.PanBy( int.MaxValue, int.MaxValue ).OriginRow );
		Assert.Equal( 0, viewport.PanBy( int.MinValue, int.MinValue ).OriginColumn );
		CursesViewport paged = new CursesViewport( int.MaxValue, int.MaxValue, int.MaxValue - 1, int.MaxValue - 1 );
		Assert.Equal( new CursesViewport( int.MaxValue, int.MaxValue, int.MaxValue - 1, int.MaxValue - 1, 1, 0 ), paged.PageBy( int.MaxValue, int.MinValue ) );
	}

	[Fact]
	public void InvalidExtentsAndNegativeMoveCoordinatesAreRejected() {
		Assert.Equal( "contentRows", Assert.Throws<ArgumentOutOfRangeException>( () => new CursesViewport( -1, 2, 1, 1 ) ).ParamName );
		Assert.Equal( "originColumn", Assert.Throws<ArgumentOutOfRangeException>( () => new CursesViewport( 2, 2, 1, 1, 0, -1 ) ).ParamName );
		CursesViewport viewport = new CursesViewport( 2, 2, 1, 1 );
		Assert.Equal( "rows", Assert.Throws<ArgumentOutOfRangeException>( () => viewport.WithViewportExtent( -1, 1 ) ).ParamName );
		Assert.Equal( "column", Assert.Throws<ArgumentOutOfRangeException>( () => viewport.MoveTo( 0, -1 ) ).ParamName );
	}
}
