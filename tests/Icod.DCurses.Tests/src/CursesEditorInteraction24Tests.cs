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

namespace Icod.DCurses.Tests;

using Icod.DCurses.Editor.Sample;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

/// <summary>Exercises editor-specific composition over the existing interaction router.</summary>
public sealed class CursesEditorInteraction24Tests {
	[Fact]
	public void CapturedOutsideDragAndReleaseReturnToDocumentExactlyOnce() {
		CursesScreen screen = new( 80, 24 );
		using EditorSampleInteraction interaction = CreateInteraction( screen );

		CursesInteractionResult press = Route(
			interaction,
			Mouse( CursesMouseAction.Press, CursesMouseButton.Primary, 2, 3 )
		);
		CursesInteractionResult move = Route(
			interaction,
			Mouse( CursesMouseAction.Move, CursesMouseButton.Primary, 22, 20 )
		);
		CursesInteractionResult release = Route(
			interaction,
			Mouse( CursesMouseAction.Release, CursesMouseButton.Primary, 22, 20 )
		);
		CursesInteractionResult later = Route(
			interaction,
			Mouse( CursesMouseAction.Move, CursesMouseButton.Primary, 22, 20 )
		);

		Assert.True( interaction.IsDocumentTarget( press ) );
		Assert.True( interaction.IsDocumentTarget( move ) );
		Assert.True( interaction.IsDocumentTarget( release ) );
		Assert.False( move.PointerTarget!.IsInside );
		Assert.False( release.PointerTarget!.IsInside );
		Assert.Equal( CursesInteractionResultKind.Unrouted, later.Kind );
	}

	[Fact]
	public void ModalAndFocusTransitionsCancelCaptureWithoutBackgroundDelivery() {
		CursesScreen screen = new( 80, 24 );
		using EditorSampleInteraction interaction = CreateInteraction( screen );
		_ = Route(
			interaction,
			Mouse( CursesMouseAction.Press, CursesMouseButton.Primary, 2, 3 )
		);

		interaction.OpenPrompt();
		CursesInteractionResult modalRelease = Route(
			interaction,
			Mouse( CursesMouseAction.Release, CursesMouseButton.Primary, 2, 3 )
		);
		Assert.Equal( CursesInteractionResultKind.Unrouted, modalRelease.Kind );

		interaction.ClosePrompt();
		_ = Route(
			interaction,
			Mouse( CursesMouseAction.Press, CursesMouseButton.Primary, 2, 3 )
		);
		CursesCommandSequenceResult focus = interaction.Process(
			CursesInputEvent.FromFocus( new CursesFocusEvent( CursesFocusState.Unfocused ) )
		);
		Assert.Equal( CursesInteractionResultKind.Unrouted, focus.Fallback?.Kind );
		CursesInteractionResult focusRelease = Route(
			interaction,
			Mouse( CursesMouseAction.Release, CursesMouseButton.Primary, 22, 20 )
		);
		Assert.Equal( CursesInteractionResultKind.Unrouted, focusRelease.Kind );
	}

	[Fact]
	public void KeyReleaseCannotTriggerDocumentCommandTwice() {
		CursesScreen screen = new( 80, 24 );
		using EditorSampleInteraction interaction = CreateInteraction( screen );

		CursesCommandSequenceResult press = interaction.Process(
			CursesInputEvent.FromKey( CursesKey.Right )
		);
		CursesCommandSequenceResult release = interaction.Process(
			CursesInputEvent.FromKey(
				CursesKey.Right,
				keyPhase: CursesKeyEventPhase.Release
			)
		);

		Assert.Equal( EditorSampleInteraction.MoveRightCommandName, press.Fallback?.Command?.Name );
		Assert.Null( release.Fallback?.Command );
	}

	[Fact]
	public void BracketedPasteDataIsDeliveredAsTargetedText() {
		CursesScreen screen = new( 80, 24 );
		using EditorSampleInteraction interaction = CreateInteraction( screen );
		CursesInputEvent input = CursesInputEvent.FromPaste(
			new CursesPasteEvent( CursesPastePhase.Data, "a\n界\t" )
		);

		CursesCommandSequenceResult result = interaction.Process( input );

		Assert.Equal( CursesInteractionResultKind.Targeted, result.Fallback?.Kind );
		Assert.True( interaction.IsDocumentTarget( result.Fallback! ) );
		Assert.Same( input, result.Fallback?.Input );
		Assert.Equal( "a\n界\t", result.Fallback?.Input.Paste?.Text );
	}

	[Fact]
	public void EditorTabAndScrollbarPointerRemainSeparateFromFocusTraversal() {
		CursesScreen screen = new( 80, 24 );
		using EditorSampleInteraction interaction = CreateInteraction( screen );

		CursesCommandSequenceResult tab = interaction.Process(
			CursesInputEvent.FromKey( CursesKey.Tab )
		);
		CursesInteractionResult document = Route(
			interaction,
			Mouse( CursesMouseAction.Press, CursesMouseButton.Primary, 2, 78 )
		);
		_ = Route(
			interaction,
			Mouse( CursesMouseAction.Release, CursesMouseButton.Primary, 2, 78 )
		);
		CursesInteractionResult scrollbar = Route(
			interaction,
			Mouse( CursesMouseAction.Press, CursesMouseButton.Primary, 2, 79 )
		);

		Assert.Equal( EditorSampleInteraction.InsertTabCommandName, tab.Fallback?.Command?.Name );
		Assert.True( interaction.IsDocumentTarget( document ) );
		Assert.False( interaction.IsScrollbarTarget( document ) );
		Assert.True( interaction.IsScrollbarTarget( scrollbar ) );
		Assert.False( interaction.IsDocumentTarget( scrollbar ) );
	}

	[Fact]
	public async Task ClipboardReadFailureUsesApplicationOwnedFallback() {
		RecordingTerminalOutput output = new();
		TerminalSession terminal = await TerminalScreenTestSession.OpenAsync(
			TerminalProfiles.Dumb,
			output
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminal,
			new CursesSessionOptions {
				UseAlternateScreen = false,
				EnableKeypad = false,
				HideCursor = false
			}
		);
		EditorSampleState state = new( 12, 40 );
		EditorSampleClipboard clipboard = new();
		state.ToggleSelection();
		state.MoveHorizontal( 1 );

		Assert.True( await clipboard.CopyAsync( session, state ) );
		state.GoToRow( 1 );
		Assert.True( await clipboard.PasteAsync( session, state ) );

		Assert.StartsWith( "00000001", state.GetRecord( 1 ), StringComparison.Ordinal );
	}

	[Fact]
	public void PointerSelectionAndScrollbarRemainApplicationPolicy() {
		EditorSampleState state = new( 12, 40 );

		state.SetCaretFromPointer( 0, 2, extendSelection: false );
		int anchor = state.Offset;
		state.SetCaretFromPointer( 0, 8, extendSelection: true );

		Assert.Equal( anchor, state.SelectionAnchor );
		Assert.True( state.TryGetSelectedText( out string selected ) );
		Assert.NotEmpty( selected );

		state.EndPointerSelection();
		state.ScrollbarTo( 11 );

		Assert.Equal( EditorSampleState.DocumentRows - 1, state.Row );
		Assert.False( state.Selecting );
	}

	private static EditorSampleInteraction CreateInteraction( CursesScreen screen ) {
		EditorSampleInteraction interaction = new( screen );
		interaction.SetBounds(
			new CursesRectangle( 0, 0, 22, 80 ),
			new CursesRectangle( 23, 0, 1, 80 )
		);
		return interaction;
	}

	private static CursesInteractionResult Route(
		EditorSampleInteraction interaction,
		CursesInputEvent input
	) {
		return interaction.Process( input ).Fallback
			?? throw new InvalidOperationException( "The sample did not produce a routing result." );
	}

	private static CursesInputEvent Mouse(
		CursesMouseAction action,
		CursesMouseButton button,
		int row,
		int column
	) {
		return CursesInputEvent.FromMouse(
			new CursesMouseEvent( action, button, column, row )
		);
	}
}
