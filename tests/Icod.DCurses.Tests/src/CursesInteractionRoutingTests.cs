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
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class CursesInteractionRoutingTests {
	[Fact]
	public void CommandPreservesExactOrdinalName() {
		CursesCommand command = new( "  Open.File  " );
		CursesCommand equal = new( "  Open.File  " );
		CursesCommand differentCase = new( "  open.file  " );

		Assert.Equal( "  Open.File  ", command.Name );
		Assert.Equal( "  Open.File  ", command.ToString() );
		Assert.Equal( command, equal );
		Assert.NotEqual( command, differentCase );
	}

	[Fact]
	public void CommandRejectsNullWhitespaceAndOversizedNames() {
		Assert.Throws<ArgumentNullException>(
			() => {
				_ = new CursesCommand( null! );
			}
		);
		Assert.Throws<ArgumentException>(
			() => {
				_ = new CursesCommand( " \t\r\n" );
			}
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => {
				_ = new CursesCommand(
					new string( 'x', CursesCommand.MaximumNameLength + 1 )
				);
			}
		);

		CursesCommand maximum = new(
			new string( 'x', CursesCommand.MaximumNameLength )
		);
		Assert.Equal( CursesCommand.MaximumNameLength, maximum.Name.Length );
	}

	[Fact]
	public void RegionBindingRejectsInvalidArgumentsAndDuplicatesBeforeMutation() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( router );
		CursesKeyGesture gesture = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand first = new( "First" );
		CursesCommand second = new( "Second" );

		Assert.Throws<ArgumentException>(
			() => {
				region.BindGesture( default, first );
			}
		);
		Assert.Throws<ArgumentNullException>(
			() => {
				region.BindGesture( gesture, null! );
			}
		);

		region.BindGesture( gesture, first );
		Assert.Throws<InvalidOperationException>(
			() => {
				region.BindGesture( gesture, second );
			}
		);

		Assert.True( router.Focus( region ) );
		CursesInteractionResult result = router.Route(
			CursesInputEvent.FromKey( CursesKey.Enter )
		);
		Assert.Equal( first, result.Command );

		Assert.True( region.UnbindGesture( gesture ) );
		Assert.False( region.UnbindGesture( gesture ) );
		region.BindGesture( gesture, second );
		Assert.Equal(
			second,
			router.Route(
				CursesInputEvent.FromKey( CursesKey.Enter )
			).Command
		);
	}

	[Fact]
	public void GlobalBindingRejectsInvalidArgumentsAndDuplicatesBeforeMutation() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		CursesKeyGesture gesture = CursesKeyGesture.ForKey( CursesKey.Escape );
		CursesCommand first = new( "First" );
		CursesCommand second = new( "Second" );

		Assert.Throws<ArgumentException>(
			() => {
				router.BindGlobalGesture( default, first );
			}
		);
		Assert.Throws<ArgumentNullException>(
			() => {
				router.BindGlobalGesture( gesture, null! );
			}
		);

		router.BindGlobalGesture( gesture, first );
		Assert.Throws<InvalidOperationException>(
			() => {
				router.BindGlobalGesture( gesture, second );
			}
		);

		Assert.Equal(
			first,
			router.Route(
				CursesInputEvent.FromKey( CursesKey.Escape )
			).Command
		);
		Assert.True( router.UnbindGlobalGesture( gesture ) );
		Assert.False( router.UnbindGlobalGesture( gesture ) );
		router.BindGlobalGesture( gesture, second );
		Assert.Equal(
			second,
			router.Route(
				CursesInputEvent.FromKey( CursesKey.Escape )
			).Command
		);
	}

	[Fact]
	public void DisposedRegionRejectsBindingOperations() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		CursesInteractionRegion region = RegisterFocusable( router );
		CursesKeyGesture gesture = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand command = new( "Enter" );
		region.Dispose();

		Assert.Throws<ObjectDisposedException>(
			() => {
				region.BindGesture( gesture, command );
			}
		);
		Assert.Throws<ObjectDisposedException>(
			() => {
				_ = region.UnbindGesture( gesture );
			}
		);
	}

	[Fact]
	public void PerRegionBindingLimitIsEnforcedAndUnbindReleasesCapacity() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( router );
		CursesCommand command = new( "Command" );

		for ( int index = 0; index < CursesInteractionRouter.MaximumRegionGestureBindings; index++ ) {
			region.BindGesture(
				CharacterGesture( index ),
				command
			);
		}

		Assert.Throws<InvalidOperationException>(
			() => {
				region.BindGesture(
					CharacterGesture( CursesInteractionRouter.MaximumRegionGestureBindings ),
					command
				);
			}
		);

		Assert.True( region.UnbindGesture( CharacterGesture( 0 ) ) );
		region.BindGesture(
			CharacterGesture( CursesInteractionRouter.MaximumRegionGestureBindings ),
			command
		);
	}

	[Fact]
	public void GlobalBindingLimitIsEnforcedAndUnbindReleasesCapacity() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		CursesCommand command = new( "Command" );

		for ( int index = 0; index < CursesInteractionRouter.MaximumGlobalGestureBindings; index++ ) {
			router.BindGlobalGesture(
				CharacterGesture( index ),
				command
			);
		}

		Assert.Throws<InvalidOperationException>(
			() => {
				router.BindGlobalGesture(
					CharacterGesture( CursesInteractionRouter.MaximumGlobalGestureBindings ),
					command
				);
			}
		);

		Assert.True( router.UnbindGlobalGesture( CharacterGesture( 0 ) ) );
		router.BindGlobalGesture(
			CharacterGesture( CursesInteractionRouter.MaximumGlobalGestureBindings ),
			command
		);
	}

	[Fact]
	public void TotalBindingLimitIsEnforcedAndRegionDisposalReleasesCapacity() {
		CursesScreen screen = new( 100, 100 );
		using CursesInteractionRouter router = new( screen );
		CursesCommand command = new( "Command" );
		List<CursesInteractionRegion> regions = [];

		try {
			int regionCount = CursesInteractionRouter.MaximumGestureBindings
				/ CursesInteractionRouter.MaximumRegionGestureBindings;
			for ( int regionIndex = 0; regionIndex < regionCount; regionIndex++ ) {
				CursesInteractionRegion region = RegisterFocusable(
					router,
					row: regionIndex % 100,
					column: regionIndex / 100
				);
				regions.Add( region );
				for ( int gestureIndex = 0; gestureIndex < CursesInteractionRouter.MaximumRegionGestureBindings; gestureIndex++ ) {
					region.BindGesture(
						CharacterGesture( gestureIndex ),
						command
					);
				}
			}

			using CursesInteractionRegion overflow = RegisterFocusable(
				router,
				row: 99,
				column: 99
			);
			Assert.Throws<InvalidOperationException>(
				() => {
					overflow.BindGesture(
						CharacterGesture( 300 ),
						command
					);
				}
			);

			regions[ 0 ].Dispose();
			overflow.BindGesture(
				CharacterGesture( 300 ),
				command
			);
		} finally {
			foreach ( CursesInteractionRegion region in regions ) {
				region.Dispose();
			}
		}
	}

	[Fact]
	public void FocusedLocalCommandPrecedesMatchingGlobalCommand() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( router );
		CursesKeyGesture gesture = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand local = new( "Local" );
		CursesCommand global = new( "Global" );
		region.BindGesture( gesture, local );
		router.BindGlobalGesture( gesture, global );
		Assert.True( router.Focus( region ) );

		CursesInputEvent input = CursesInputEvent.FromKey( CursesKey.Enter );
		CursesInteractionResult result = router.Route( input );

		Assert.Equal( CursesInteractionResultKind.Command, result.Kind );
		Assert.Same( input, result.Input );
		Assert.Same( region, result.Region );
		Assert.Equal( local, result.Command );
		Assert.Null( result.Hit );
	}

	[Fact]
	public void GlobalCommandHasNoRegionAndDoesNotCaptureFocus() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( router );
		CursesKeyGesture gesture = CursesKeyGesture.ForKey( CursesKey.Escape );
		CursesCommand global = new( "Global" );
		router.BindGlobalGesture( gesture, global );
		Assert.True( router.Focus( region ) );

		CursesInteractionResult result = router.Route(
			CursesInputEvent.FromKey( CursesKey.Escape )
		);

		Assert.Equal( CursesInteractionResultKind.Command, result.Kind );
		Assert.Null( result.Region );
		Assert.Equal( global, result.Command );
		Assert.Null( result.Hit );
		Assert.Same( region, router.FocusedRegion );
	}

	[Fact]
	public void TextGestureRoutesThroughLocalThenGlobalBindings() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( router );
		CursesKeyGesture gesture = CursesKeyGesture.ForCharacter( new Rune( 'x' ) );
		CursesCommand local = new( "LocalX" );
		CursesCommand global = new( "GlobalX" );
		region.BindGesture( gesture, local );
		router.BindGlobalGesture( gesture, global );
		Assert.True( router.Focus( region ) );

		CursesInputEvent input = CursesInputEvent.FromText( new Rune( 'x' ) );
		Assert.Equal( local, router.Route( input ).Command );

		Assert.True( region.UnbindGesture( gesture ) );
		CursesInteractionResult globalResult = router.Route( input );
		Assert.Equal( CursesInteractionResultKind.Command, globalResult.Kind );
		Assert.Equal( global, globalResult.Command );
		Assert.Null( globalResult.Region );
	}

	[Fact]
	public void UnmatchedKeyboardInputTargetsEligibleFocusOrIsUnrouted() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( router );
		CursesInputEvent input = CursesInputEvent.FromKey( CursesKey.Tab );

		Assert.True( router.Focus( region ) );
		CursesInteractionResult targeted = router.Route( input );
		Assert.Equal( CursesInteractionResultKind.Targeted, targeted.Kind );
		Assert.Same( region, targeted.Region );
		Assert.Null( targeted.Command );
		Assert.Null( targeted.Hit );

		router.ClearFocus();
		CursesInteractionResult unrouted = router.Route( input );
		Assert.Equal( CursesInteractionResultKind.Unrouted, unrouted.Kind );
		Assert.Null( unrouted.Region );
		Assert.Null( unrouted.Command );
		Assert.Null( unrouted.Hit );
		Assert.Same( input, unrouted.Input );
	}

	[Fact]
	public void PasteRepairsFocusThenTargetsCurrentEligibleRegion() {
		CursesScreen screen = new( 20, 10 );
		using CursesPanel panel = screen.CreatePanel( 1, 1, 3, 3 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion fallback = RegisterFocusable(
			router,
			traversalOrder: 0
		);
		using CursesInteractionRegion panelRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 3, 3 )
			) {
				Panel = panel,
				IsFocusable = true,
				TraversalOrder = 1
			}
		);
		Assert.True( router.Focus( panelRegion ) );
		panel.Hide();
		CursesInputEvent input = CursesInputEvent.FromPaste(
			new CursesPasteEvent( CursesPastePhase.Data, "hello" )
		);

		CursesInteractionResult result = router.Route( input );

		Assert.Equal( CursesInteractionResultKind.Targeted, result.Kind );
		Assert.Same( fallback, result.Region );
		Assert.Same( fallback, router.FocusedRegion );
	}

	[Fact]
	public void MouseRoutesOnlyThroughHitTestingAndDoesNotChangeFocus() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion focused = RegisterFocusable(
			router,
			row: 0,
			column: 0
		);
		using CursesInteractionRegion hitRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 4, 5, 3, 4 )
			)
		);
		Assert.True( router.Focus( focused ) );
		CursesInputEvent input = CursesInputEvent.FromMouse(
			new CursesMouseEvent(
				CursesMouseAction.Press,
				CursesMouseButton.Primary,
				column: 7,
				row: 5
			)
		);

		CursesInteractionResult result = router.Route( input );

		Assert.Equal( CursesInteractionResultKind.Targeted, result.Kind );
		Assert.Same( input, result.Input );
		Assert.Same( hitRegion, result.Region );
		Assert.Null( result.Command );
		Assert.NotNull( result.Hit );
		Assert.Same( hitRegion, result.Hit.Region );
		Assert.Equal( 1, result.Hit.LocalRow );
		Assert.Equal( 2, result.Hit.LocalColumn );
		Assert.Same( focused, router.FocusedRegion );
	}

	[Fact]
	public void MouseWithoutHitIsUnroutedAndDoesNotRepairFocus() {
		CursesScreen screen = new( 20, 10 );
		using CursesPanel panel = screen.CreatePanel( 1, 1, 2, 2 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion focused = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 2, 2 )
			) {
				Panel = panel,
				IsFocusable = true
			}
		);
		Assert.True( router.Focus( focused ) );
		panel.Hide();
		CursesInputEvent input = CursesInputEvent.FromMouse(
			new CursesMouseEvent(
				CursesMouseAction.Move,
				CursesMouseButton.None,
				column: 19,
				row: 9
			)
		);

		CursesInteractionResult result = router.Route( input );
		Assert.Equal( CursesInteractionResultKind.Unrouted, result.Kind );

		panel.Show();
		Assert.Same( focused, router.FocusedRegion );
	}

	[Fact]
	public void FocusAndEndOfInputAreUnroutedAndDoNotChangeLogicalFocus() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( router );
		Assert.True( router.Focus( region ) );
		CursesInputEvent focus = CursesInputEvent.FromFocus(
			new CursesFocusEvent( CursesFocusState.Unfocused )
		);
		CursesInputEvent end = CursesInputEvent.EndOfInput();

		Assert.Equal(
			CursesInteractionResultKind.Unrouted,
			router.Route( focus ).Kind
		);
		Assert.Same( region, router.FocusedRegion );
		Assert.Equal(
			CursesInteractionResultKind.Unrouted,
			router.Route( end ).Kind
		);
		Assert.Same( region, router.FocusedRegion );
	}

	[Fact]
	public void RouteResultRemainsSnapshotAfterRegionDisposal() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		CursesInteractionRegion region = RegisterFocusable( router );
		Assert.True( router.Focus( region ) );
		CursesInputEvent input = CursesInputEvent.FromKey( CursesKey.Tab );
		CursesInteractionResult result = router.Route( input );

		region.Dispose();

		Assert.Equal( CursesInteractionResultKind.Targeted, result.Kind );
		Assert.Same( input, result.Input );
		Assert.Same( region, result.Region );
		Assert.Null( result.Command );
		Assert.Null( result.Hit );
	}

	[Fact]
	public void RouteAndGlobalBindingOperationsRejectDisposedRouter() {
		CursesScreen screen = new( 20, 10 );
		CursesInteractionRouter router = new( screen );
		CursesKeyGesture gesture = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand command = new( "Command" );
		router.Dispose();

		Assert.Throws<ObjectDisposedException>(
			() => {
				router.BindGlobalGesture( gesture, command );
			}
		);
		Assert.Throws<ObjectDisposedException>(
			() => {
				_ = router.UnbindGlobalGesture( gesture );
			}
		);
		Assert.Throws<ObjectDisposedException>(
			() => {
				_ = router.Route( CursesInputEvent.FromKey( CursesKey.Enter ) );
			}
		);
	}

	[Fact]
	public void RouteRejectsNullInputBeforeMutation() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( router );
		Assert.True( router.Focus( region ) );

		Assert.Throws<ArgumentNullException>(
			() => {
				_ = router.Route( null! );
			}
		);
		Assert.Same( region, router.FocusedRegion );
	}

	private static CursesKeyGesture CharacterGesture(
		int index
	) {
		return CursesKeyGesture.ForCharacter(
			new Rune( 0x1000 + index )
		);
	}

	private static CursesInteractionRegion RegisterFocusable(
		CursesInteractionRouter router,
		int row = 0,
		int column = 0,
		int traversalOrder = 0
	) {
		ArgumentNullException.ThrowIfNull( router );
		return router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( row, column, 1, 1 )
			) {
				IsFocusable = true,
				TraversalOrder = traversalOrder
			}
		);
	}
}
