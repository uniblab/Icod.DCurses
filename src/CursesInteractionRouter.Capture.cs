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
	private CursesInteractionRegion? capturedPointerRegion;
	private CursesMouseButton capturedPointerButton;
	private CursesPointerCaptureLease? pointerCaptureLease;
	private long nextPointerCaptureGeneration;
	private bool pointerCaptureGenerationExhausted;

	/// <summary>Explicitly captures one concrete mouse button for an eligible owned region.</summary>
	/// <param name="region">The owned region which will receive matching move/release reports.</param>
	/// <param name="button">The concrete mouse button whose interaction is captured.</param>
	/// <returns>An idempotent lease which owns the capture lifetime.</returns>
	public CursesPointerCaptureLease CapturePointer(
		CursesInteractionRegion region,
		CursesMouseButton button
	) {
		ArgumentNullException.ThrowIfNull( region );
		if ( !Enum.IsDefined( button ) || CursesMouseButton.None == button ) {
			throw new ArgumentOutOfRangeException( nameof( button ) );
		}
		this.ThrowIfDisposed();
		if ( !ReferenceEquals(
			region.Owner,
			this
		) ) {
			throw new ArgumentException(
				"The interaction region belongs to another router.",
				nameof( region )
			);
		}
		if ( region.IsDisposed ) {
			throw new ObjectDisposedException( nameof( region ) );
		}

		this.RepairPointerCaptureIfNeeded();
		if ( this.capturedPointerRegion is not null ) {
			throw new InvalidOperationException(
				"The interaction router already owns a live pointer capture."
			);
		}
		if ( !this.IsRegionPointerEligible( region ) ) {
			throw new InvalidOperationException(
				"The interaction region is not currently eligible for pointer capture."
			);
		}
		if ( this.pointerCaptureGenerationExhausted ) {
			throw new InvalidOperationException(
				"The pointer-capture generation domain is exhausted."
			);
		}

		long generation = this.nextPointerCaptureGeneration;
		CursesPointerCaptureLease lease = new(
			this,
			generation
		);
		this.capturedPointerRegion = region;
		this.capturedPointerButton = button;
		this.pointerCaptureLease = lease;

		if ( long.MaxValue == generation ) {
			this.pointerCaptureGenerationExhausted = true;
		} else {
			this.nextPointerCaptureGeneration = generation + 1;
		}

		return lease;
	}

	internal void ReleasePointerCapture(
		CursesPointerCaptureLease lease
	) {
		ArgumentNullException.ThrowIfNull( lease );
		if ( lease.IsReleased ) {
			return;
		}

		if ( this.pointerCaptureLease is not null
			&& ReferenceEquals(
				this.pointerCaptureLease,
				lease
			)
			&& lease.Generation == this.pointerCaptureLease.Generation ) {
			this.ClearPointerCapture();
			return;
		}

		lease.MarkReleased();
	}

	internal void HandlePointerCaptureRegionChanged(
		CursesInteractionRegion region
	) {
		ArgumentNullException.ThrowIfNull( region );
		this.HandlePointerGestureRegionChanged( region );
		if ( ReferenceEquals(
			this.capturedPointerRegion,
			region
		) ) {
			this.RepairPointerCaptureIfNeeded();
		}
	}

	internal void ReleasePointerCaptureForRegion(
		CursesInteractionRegion region
	) {
		ArgumentNullException.ThrowIfNull( region );
		this.CancelPointerGestureStateForRegion( region );
		if ( ReferenceEquals(
			this.capturedPointerRegion,
			region
		) ) {
			this.ClearPointerCapture();
		}
	}

	internal void RepairPointerCaptureIfNeeded() {
		CursesInteractionRegion? region = this.capturedPointerRegion;
		if ( region is null || this.IsRegionPointerEligible( region ) ) {
			return;
		}

		this.ClearPointerCapture();
	}

	private bool TryGetCapturedPointerTarget(
		CursesMouseEvent mouse,
		out CursesPointerTarget? target
	) {
		ArgumentNullException.ThrowIfNull( mouse );
		this.RepairPointerCaptureIfNeeded();

		CursesInteractionRegion? region = this.capturedPointerRegion;
		if ( region is null
			|| mouse.Button != this.capturedPointerButton
			|| mouse.Action is not CursesMouseAction.Move
				and not CursesMouseAction.Release ) {
			target = null;
			return false;
		}

		target = this.CreatePointerTarget(
			region,
			mouse.Row,
			mouse.Column
		);
		return true;
	}

	private CursesPointerTarget CreatePointerTarget(
		CursesInteractionRegion region,
		int row,
		int column
	) {
		ArgumentNullException.ThrowIfNull( region );
		CursesRectangle bounds = region.Bounds;
		CursesPanel? panel = region.Panel;
		int originRow = panel is null
			? bounds.Row
			: checked( panel.Row + bounds.Row )
		;
		int originColumn = panel is null
			? bounds.Column
			: checked( panel.Column + bounds.Column )
		;
		int localRow = row - originRow;
		int localColumn = column - originColumn;
		bool isInside = row < this.Screen.Rows
			&& column < this.Screen.Columns
			&& TryGetLocalCoordinates(
				region,
				row,
				column,
				out _,
				out _
			);

		return new CursesPointerTarget(
			region,
			localRow,
			localColumn,
			isInside
		);
	}

	private bool IsRegionPointerEligible(
		CursesInteractionRegion region
	) {
		if ( region.IsDisposed
			|| !this.IsRegionWithinActiveScope( region )
			|| !region.IsEnabled ) {
			return false;
		}

		CursesRectangle bounds = region.Bounds;
		if ( bounds.IsEmpty ) {
			return false;
		}

		CursesPanel? panel = region.Panel;
		if ( panel is null ) {
			return bounds.Row < this.Screen.Rows
				&& bounds.Column < this.Screen.Columns;
		}
		if ( panel.IsDisposed || !panel.IsVisible ) {
			return false;
		}

		long top = (long)panel.Row + bounds.Row;
		long left = (long)panel.Column + bounds.Column;
		long bottom = Math.Min(
			(long)this.Screen.Rows,
			Math.Min(
				(long)panel.Row + panel.Rows,
				top + bounds.Rows
			)
		);
		long right = Math.Min(
			(long)this.Screen.Columns,
			Math.Min(
				(long)panel.Column + panel.Columns,
				left + bounds.Columns
			)
		);

		return top < bottom && left < right;
	}

	private void ClearPointerCapture() {
		CursesPointerCaptureLease? lease = this.pointerCaptureLease;
		this.CancelPointerGestureState( this.capturedPointerButton );
		this.capturedPointerRegion = null;
		this.capturedPointerButton = CursesMouseButton.None;
		this.pointerCaptureLease = null;
		lease?.MarkReleased();
	}
}
