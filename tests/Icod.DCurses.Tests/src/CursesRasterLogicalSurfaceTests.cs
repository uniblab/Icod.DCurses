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
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes T1603 retained-raster logical-screen and window behavior.</summary>
public sealed class CursesRasterLogicalSurfaceTests {
	[Fact]
	public void LogicalRasterApiMatchesFrozenT1601Candidate() {
		Type nullableRasterCell = typeof( CursesRasterCell? );
		Assert.Equal(
			nullableRasterCell,
			RequireMethod(
				typeof( CursesVirtualScreen ),
				"GetRasterCell",
				[ typeof( int ), typeof( int ) ]
			).ReturnType
		);
		Assert.Equal(
			typeof( void ),
			RequireMethod(
				typeof( CursesVirtualScreen ),
				"SetRasterCell",
				[ typeof( int ), typeof( int ), nullableRasterCell ]
			).ReturnType
		);
		Assert.Equal(
			nullableRasterCell,
			RequireMethod(
				typeof( CursesWindow ),
				"GetRasterCell",
				[ typeof( int ), typeof( int ) ]
			).ReturnType
		);
		Assert.Equal(
			typeof( void ),
			RequireMethod(
				typeof( CursesWindow ),
				"SetRasterCell",
				[ typeof( int ), typeof( int ), nullableRasterCell ]
			).ReturnType
		);
		Assert.Equal(
			typeof( void ),
			RequireMethod(
				typeof( CursesWindow ),
				"WriteRasterCell",
				[ typeof( CursesRasterCell ) ]
			).ReturnType
		);
	}

	[Fact]
	public void VirtualScreenRasterSetRemoveTracksDamageWithoutMutatingOtherAxes() {
		CursesVirtualScreen screen = new( 4, 2 );
		CursesStyle style = new(
			CursesColor.Indexed( 2 ),
			CursesColor.Default,
			CursesTextAttributes.Bold
		);
		CursesCell visual = new( "X", style );
		CursesCellMetadata metadata = new(
			new CursesHyperlink(
				"https://example.test/raster",
				"raster"
			)
		);
		screen.SetCell( 0, 1, visual );
		screen.SetMetadata( 0, 1, metadata );
		screen.MarkClean();
		CursesRasterCell token = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		MethodInfo set = RequireMethod(
			typeof( CursesVirtualScreen ),
			"SetRasterCell",
			[ typeof( int ), typeof( int ), typeof( CursesRasterCell? ) ]
		);
		MethodInfo get = RequireMethod(
			typeof( CursesVirtualScreen ),
			"GetRasterCell",
			[ typeof( int ), typeof( int ) ]
		);

		_ = set.Invoke( screen, [ 0, 1, token ] );

		CursesRasterCell stored = Assert.IsType<CursesRasterCell>( get.Invoke( screen, [ 0, 1 ] ) );
		Assert.True( stored.IsValid );
		Assert.True( screen.IsDirty( 0, 1 ) );
		Assert.Equal( visual, screen.GetCell( 0, 1 ) );
		Assert.Equal( metadata, screen.GetMetadata( 0, 1 ) );

		screen.MarkClean();
		_ = set.Invoke( screen, [ 0, 1, null ] );

		Assert.Null( get.Invoke( screen, [ 0, 1 ] ) );
		Assert.True( screen.IsDirty( 0, 1 ) );
		Assert.Equal( visual, screen.GetCell( 0, 1 ) );
		Assert.Equal( metadata, screen.GetMetadata( 0, 1 ) );
	}

	[Fact]
	public void DefaultRasterCellIsRejectedWithoutMutation() {
		CursesVirtualScreen screen = new( 3, 1 );
		screen.MarkClean();
		MethodInfo set = RequireMethod(
			typeof( CursesVirtualScreen ),
			"SetRasterCell",
			[ typeof( int ), typeof( int ), typeof( CursesRasterCell? ) ]
		);

		TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
			() => set.Invoke(
				screen,
				[ 0, 1, default( CursesRasterCell ) ]
			)
		);
		Assert.IsType<ArgumentException>( exception.InnerException );
		Assert.False( screen.IsDirty( 0, 1 ) );
	}

	[Fact]
	public void OrdinaryReplacementAndFillRemoveRetainedRasterState() {
		CursesVirtualScreen screen = new( 3, 2 );
		CursesRasterCell token = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		MethodInfo set = RequireMethod(
			typeof( CursesVirtualScreen ),
			"SetRasterCell",
			[ typeof( int ), typeof( int ), typeof( CursesRasterCell? ) ]
		);
		MethodInfo get = RequireMethod(
			typeof( CursesVirtualScreen ),
			"GetRasterCell",
			[ typeof( int ), typeof( int ) ]
		);

		_ = set.Invoke( screen, [ 0, 1, token ] );
		CursesCell sameVisual = screen.GetCell( 0, 1 );
		screen.SetCell( 0, 1, sameVisual );
		Assert.Null( get.Invoke( screen, [ 0, 1 ] ) );

		_ = set.Invoke( screen, [ 1, 2, token ] );
		screen.Fill( CursesCell.Blank() );
		Assert.Null( get.Invoke( screen, [ 1, 2 ] ) );
	}

	[Fact]
	public void WindowCoordinatesProjectRasterStateIntoOwningScreen() {
		CursesScreen screen = new( 5, 2 );
		CursesWindow window = screen.CreateWindow( 0, 1, 2, 3 );
		CursesRasterCell token = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		MethodInfo setWindow = RequireMethod(
			typeof( CursesWindow ),
			"SetRasterCell",
			[ typeof( int ), typeof( int ), typeof( CursesRasterCell? ) ]
		);
		MethodInfo getWindow = RequireMethod(
			typeof( CursesWindow ),
			"GetRasterCell",
			[ typeof( int ), typeof( int ) ]
		);
		MethodInfo getScreen = RequireMethod(
			typeof( CursesVirtualScreen ),
			"GetRasterCell",
			[ typeof( int ), typeof( int ) ]
		);

		_ = setWindow.Invoke( window, [ 1, 2, token ] );

		Assert.IsType<CursesRasterCell>( getWindow.Invoke( window, [ 1, 2 ] ) );
		Assert.IsType<CursesRasterCell>( getScreen.Invoke( screen.VirtualScreen, [ 1, 3 ] ) );
		Assert.Null( getScreen.Invoke( screen.VirtualScreen, [ 1, 2 ] ) );
	}

	[Fact]
	public void WriteRasterCellAdvancesOneColumnAndWrapsUsingExistingWindowPolicy() {
		CursesScreen screen = new( 3, 2 );
		CursesWindow window = screen.StandardWindow;
		CursesRasterCell token = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		MethodInfo write = RequireMethod(
			typeof( CursesWindow ),
			"WriteRasterCell",
			[ typeof( CursesRasterCell ) ]
		);
		MethodInfo getScreen = RequireMethod(
			typeof( CursesVirtualScreen ),
			"GetRasterCell",
			[ typeof( int ), typeof( int ) ]
		);

		window.Move( 0, 1 );
		_ = write.Invoke( window, [ token ] );
		Assert.Equal( 0, window.CursorRow );
		Assert.Equal( 2, window.CursorColumn );
		Assert.IsType<CursesRasterCell>( getScreen.Invoke( screen.VirtualScreen, [ 0, 1 ] ) );
		Assert.True( screen.VirtualScreen.GetCell( 0, 1 ).IsBlank );

		_ = write.Invoke( window, [ token ] );
		Assert.Equal( 1, window.CursorRow );
		Assert.Equal( 0, window.CursorColumn );
		Assert.IsType<CursesRasterCell>( getScreen.Invoke( screen.VirtualScreen, [ 0, 2 ] ) );
		Assert.True( screen.VirtualScreen.GetCell( 0, 2 ).IsBlank );
	}

	private static MethodInfo RequireMethod(
		Type type,
		string name,
		Type[] parameterTypes
	) {
		MethodInfo? method = type.GetMethod(
			name,
			BindingFlags.Instance | BindingFlags.Public,
			binder: null,
			types: parameterTypes,
			modifiers: null
		);
		Assert.True(
			method is not null,
			$"Required T1603 public method {type.FullName}.{name} is missing."
		);
		return method!;
	}
}
