/*
	Icod.DCurses
	Managed, cross-platform curses-style terminal UI library for .NET.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.DCurses;

public sealed partial class CursesInteractionRouter {
	private const int PointerPressStateCount = 7;
	private readonly PointerPressState[] pointerPressStates = new PointerPressState[PointerPressStateCount];

	private CursesPointerGesture ClassifyPointerGesture(
		CursesMouseEvent mouse,
		CursesPointerTarget? target
	) {
		ArgumentNullException.ThrowIfNull( mouse );
		this.RepairPointerGestureStateIfNeeded();

		return mouse.Action switch {
			CursesMouseAction.Press => this.ClassifyPointerPress( mouse, target ),
			CursesMouseAction.Release => this.ClassifyPointerRelease( mouse, target ),
			CursesMouseAction.Move => this.ClassifyPointerMove( mouse, target ),
			CursesMouseAction.WheelUp => CreatePointerGesture(
				CursesPointerGestureKind.WheelUp,
				mouse,
				target
			),
			CursesMouseAction.WheelDown => CreatePointerGesture(
				CursesPointerGestureKind.WheelDown,
				mouse,
				target
			),
			CursesMouseAction.WheelLeft => CreatePointerGesture(
				CursesPointerGestureKind.WheelLeft,
				mouse,
				target
			),
			CursesMouseAction.WheelRight => CreatePointerGesture(
				CursesPointerGestureKind.WheelRight,
				mouse,
				target
			),
			_ => throw new InvalidOperationException(
				$"Unsupported mouse action: {mouse.Action}."
			)
		};
	}

	private CursesPointerGesture ClassifyPointerPress(
		CursesMouseEvent mouse,
		CursesPointerTarget? target
	) {
		int index = GetPointerPressStateIndex( mouse.Button );
		if ( 0 <= index ) {
			this.pointerPressStates[index] = target is null
				? default
				: new PointerPressState(
					target.Region,
					mouse.Row,
					mouse.Column
				)
			;
		}

		return CreatePointerGesture(
			CursesPointerGestureKind.Press,
			mouse,
			target
		);
	}

	private CursesPointerGesture ClassifyPointerRelease(
		CursesMouseEvent mouse,
		CursesPointerTarget? target
	) {
		CursesPointerGestureKind kind = CursesPointerGestureKind.Release;
		int index = GetPointerPressStateIndex( mouse.Button );
		if ( 0 <= index ) {
			PointerPressState state = this.pointerPressStates[index];
			if ( state.IsActive
				&& target is not null
				&& ReferenceEquals(
					state.Region,
					target.Region
				) ) {
				kind = state.IsDragging
					? CursesPointerGestureKind.DragEnd
					: CursesPointerGestureKind.Click
				;
			}
			this.pointerPressStates[index] = default;
		}

		return CreatePointerGesture(
			kind,
			mouse,
			target
		);
	}

	private CursesPointerGesture ClassifyPointerMove(
		CursesMouseEvent mouse,
		CursesPointerTarget? target
	) {
		int index = GetPointerPressStateIndex( mouse.Button );
		if ( 0 > index ) {
			return CreatePointerGesture(
				CursesPointerGestureKind.Move,
				mouse,
				target
			);
		}

		PointerPressState state = this.pointerPressStates[index];
		if ( !state.IsActive ) {
			return CreatePointerGesture(
				CursesPointerGestureKind.Move,
				mouse,
				target
			);
		}
		if ( target is null
			|| !ReferenceEquals(
				state.Region,
				target.Region
			) ) {
			this.pointerPressStates[index] = default;
			return CreatePointerGesture(
				CursesPointerGestureKind.Move,
				mouse,
				target
			);
		}

		bool cellChanged = state.Row != mouse.Row
			|| state.Column != mouse.Column;
		if ( !state.IsDragging && !cellChanged ) {
			return CreatePointerGesture(
				CursesPointerGestureKind.Move,
				mouse,
				target
			);
		}

		CursesPointerGestureKind kind;
		if ( !state.IsDragging ) {
			state.IsDragging = true;
			kind = CursesPointerGestureKind.DragStart;
		} else {
			kind = CursesPointerGestureKind.DragMove;
		}
		state.Row = mouse.Row;
		state.Column = mouse.Column;
		this.pointerPressStates[index] = state;

		return CreatePointerGesture(
			kind,
			mouse,
			target
		);
	}

	private void RepairPointerGestureStateIfNeeded() {
		for ( int index = 0; index < this.pointerPressStates.Length; index++ ) {
			PointerPressState state = this.pointerPressStates[index];
			if ( state.IsActive
				&& ( state.Region is null
					|| !this.IsRegionPointerEligible( state.Region ) ) ) {
				this.pointerPressStates[index] = default;
			}
		}
	}

	private void HandlePointerGestureRegionChanged(
		CursesInteractionRegion region
	) {
		ArgumentNullException.ThrowIfNull( region );
		if ( this.IsRegionPointerEligible( region ) ) {
			return;
		}

		this.CancelPointerGestureStateForRegion( region );
	}

	private void CancelPointerGestureState(
		CursesMouseButton button
	) {
		int index = GetPointerPressStateIndex( button );
		if ( 0 <= index ) {
			this.pointerPressStates[index] = default;
		}
	}

	private void CancelPointerGestureStateForRegion(
		CursesInteractionRegion region
	) {
		ArgumentNullException.ThrowIfNull( region );
		for ( int index = 0; index < this.pointerPressStates.Length; index++ ) {
			if ( ReferenceEquals(
				this.pointerPressStates[index].Region,
				region
			) ) {
				this.pointerPressStates[index] = default;
			}
		}
	}

	private static CursesPointerGesture CreatePointerGesture(
		CursesPointerGestureKind kind,
		CursesMouseEvent mouse,
		CursesPointerTarget? target
	) {
		ArgumentNullException.ThrowIfNull( mouse );
		return new CursesPointerGesture(
			kind,
			mouse.Button,
			mouse.Modifiers,
			target
		);
	}

	private static int GetPointerPressStateIndex(
		CursesMouseButton button
	) {
		return button switch {
			CursesMouseButton.Primary => 0,
			CursesMouseButton.Middle => 1,
			CursesMouseButton.Secondary => 2,
			CursesMouseButton.Button4 => 3,
			CursesMouseButton.Button5 => 4,
			CursesMouseButton.Button6 => 5,
			CursesMouseButton.Button7 => 6,
			_ => -1
		};
	}

	private struct PointerPressState {
		internal PointerPressState(
			CursesInteractionRegion region,
			int row,
			int column
		) {
			ArgumentNullException.ThrowIfNull( region );
			this.Region = region;
			this.Row = row;
			this.Column = column;
			this.IsDragging = false;
		}

		internal CursesInteractionRegion? Region;
		internal int Row;
		internal int Column;
		internal bool IsDragging;
		internal readonly bool IsActive => this.Region is not null;
	}
}
