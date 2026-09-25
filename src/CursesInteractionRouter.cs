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

/// <summary>
/// Owns a bounded set of application interaction regions associated with one logical screen.
/// </summary>
public sealed partial class CursesInteractionRouter : IDisposable {
	/// <summary>Gets the maximum number of live interaction regions owned by one router.</summary>
	public const int MaximumRegions = 4096;

	/// <summary>Gets the maximum number of gesture bindings owned by one region.</summary>
	public const int MaximumRegionGestureBindings = 256;

	/// <summary>Gets the maximum number of router-global gesture bindings.</summary>
	public const int MaximumGlobalGestureBindings = 1024;

	/// <summary>Gets the maximum total number of live gesture bindings owned by one router.</summary>
	public const int MaximumGestureBindings = 16384;

	/// <summary>Gets the maximum number of gestures in one command sequence.</summary>
	public const int MaximumCommandSequenceLength = 8;

	/// <summary>Gets the maximum number of command sequence bindings owned by one region.</summary>
	public const int MaximumRegionCommandSequenceBindings = 128;

	/// <summary>Gets the maximum number of command sequence bindings owned by one scope.</summary>
	public const int MaximumScopeCommandSequenceBindings = 128;

	/// <summary>Gets the maximum number of router-global command sequence bindings.</summary>
	public const int MaximumGlobalCommandSequenceBindings = 512;

	/// <summary>Gets the maximum total number of live command sequence bindings owned by one router.</summary>
	public const int MaximumCommandSequenceBindings = 4096;

	private readonly List<CursesInteractionRegion> regions = [];
	private CursesInteractionRegion? focusedRegion;
	private long nextRegistrationOrdinal;
	private bool registrationOrdinalExhausted;
	private bool disposed;

	/// <summary>Initializes an interaction router for one logical curses screen.</summary>
	/// <param name="screen">The logical screen whose coordinate space is routed.</param>
	public CursesInteractionRouter(
		CursesScreen screen
	) {
		ArgumentNullException.ThrowIfNull( screen );
		this.Screen = screen;
		this.Screen.Resized += this.HandleScreenResized;
	}

	/// <summary>Gets the logical screen associated with this router.</summary>
	public CursesScreen Screen {
		get;
	}

	/// <summary>Gets the current logical-focus region after deterministic lazy repair.</summary>
	public CursesInteractionRegion? FocusedRegion {
		get {
			this.ThrowIfDisposed();
			this.RepairFocusIfNeeded();
			return this.focusedRegion;
		}
	}

	/// <summary>Registers one interaction region.</summary>
	/// <param name="options">The region geometry and initial interaction state.</param>
	/// <returns>The registered region.</returns>
	public CursesInteractionRegion RegisterRegion(
		CursesInteractionRegionOptions options
	) {
		ArgumentNullException.ThrowIfNull( options );
		this.ThrowIfDisposed();

		CursesPanel? panel = options.Panel;
		if ( panel is not null ) {
			if ( panel.IsDisposed ) {
				throw new ObjectDisposedException( nameof( options.Panel ) );
			}
			if ( !ReferenceEquals(
				panel.Owner,
				this.Screen
			) ) {
				throw new ArgumentException(
					"The interaction region panel belongs to another screen.",
					nameof( options )
				);
			}
		}
		this.ValidateRegionScope( options.Scope );

		if ( MaximumRegions <= this.regions.Count ) {
			throw new InvalidOperationException(
				$"An interaction router cannot own more than {MaximumRegions} live regions."
			);
		}
		if ( this.registrationOrdinalExhausted ) {
			throw new InvalidOperationException(
				"The interaction router registration ordinal domain is exhausted."
			);
		}

		long registrationOrdinal = this.nextRegistrationOrdinal;
		CursesInteractionRegion region = new(
			this,
			options,
			registrationOrdinal
		);
		this.regions.Add( region );

		if ( long.MaxValue == registrationOrdinal ) {
			this.registrationOrdinalExhausted = true;
		} else {
			this.nextRegistrationOrdinal = registrationOrdinal + 1;
		}

		return region;
	}

	/// <summary>Explicitly gives logical focus to one eligible owned region.</summary>
	/// <param name="region">The owned region to focus.</param>
	/// <returns><see langword="true"/> when the region is eligible and was focused; otherwise <see langword="false"/>.</returns>
	public bool Focus(
		CursesInteractionRegion region
	) {
		ArgumentNullException.ThrowIfNull( region );
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

		this.RepairFocusIfNeeded();
		if ( !this.IsRegionEligible( region ) ) {
			return false;
		}

		if ( ReferenceEquals(
			this.focusedRegion,
			region
		) ) {
			return true;
		}
		this.focusedRegion = region;
		this.ClearPendingCommandSequence();
		return true;
	}

	/// <summary>Clears application logical focus without changing terminal focus state.</summary>
	public void ClearFocus() {
		this.ThrowIfDisposed();
		if ( this.focusedRegion is null ) {
			return;
		}
		this.focusedRegion = null;
		this.ClearPendingCommandSequence();
	}

	/// <summary>Moves logical focus using sequential or deterministic spatial navigation.</summary>
	/// <param name="direction">The focus-navigation direction.</param>
	/// <returns>The newly focused region, or <see langword="null"/> when no candidate exists.</returns>
	public CursesInteractionRegion? MoveFocus(
		CursesFocusDirection direction
	) {
		if ( !Enum.IsDefined( direction ) ) {
			throw new ArgumentOutOfRangeException( nameof( direction ) );
		}
		this.ThrowIfDisposed();
		this.RepairFocusIfNeeded();
		CursesInteractionRegion? original = this.focusedRegion;

		if ( CursesFocusDirection.Up == direction
			|| CursesFocusDirection.Down == direction
			|| CursesFocusDirection.Left == direction
			|| CursesFocusDirection.Right == direction ) {
			CursesInteractionRegion? spatial = this.MoveSpatialFocus( direction );
			if ( !ReferenceEquals(
				original,
				this.focusedRegion
			) ) {
				this.ClearPendingCommandSequence();
			}
			return spatial;
		}

		CursesInteractionRegion? next;
		if ( this.focusedRegion is null ) {
			next = CursesFocusDirection.Forward == direction
				? this.FindFirstEligibleRegion()
				: this.FindLastEligibleRegion()
			;
		} else if ( CursesFocusDirection.Forward == direction ) {
			next = this.FindNextEligibleRegion(
				this.focusedRegion.TraversalOrder,
				this.focusedRegion.RegistrationOrdinal
			);
		} else {
			next = this.FindPreviousEligibleRegion(
				this.focusedRegion.TraversalOrder,
				this.focusedRegion.RegistrationOrdinal
			);
		}

		this.focusedRegion = next;
		if ( !ReferenceEquals(
			original,
			next
		) ) {
			this.ClearPendingCommandSequence();
		}
		return next;
	}

	/// <summary>Resolves the highest-precedence enabled interaction region at one screen coordinate.</summary>
	/// <param name="row">The non-negative zero-based screen row.</param>
	/// <param name="column">The non-negative zero-based screen column.</param>
	/// <returns>The resolved hit, or <see langword="null"/> when no region is eligible at the coordinate.</returns>
	public CursesInteractionHit? HitTest(
		int row,
		int column
	) {
		if ( 0 > row ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( 0 > column ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		this.ThrowIfDisposed();

		if ( row >= this.Screen.Rows
			|| column >= this.Screen.Columns ) {
			return null;
		}

		CursesInteractionRegion? selected = null;
		int selectedLocalRow = 0;
		int selectedLocalColumn = 0;

		foreach ( CursesInteractionRegion region in this.regions ) {
			if ( !this.IsRegionWithinActiveScope( region )
				|| !region.IsEnabled ) {
				continue;
			}
			if ( !TryGetLocalCoordinates(
				region,
				row,
				column,
				out int localRow,
				out int localColumn
			) ) {
				continue;
			}

			if ( selected is null
				|| this.IsPreferredHitCandidate(
					region,
					selected
				) ) {
				selected = region;
				selectedLocalRow = localRow;
				selectedLocalColumn = localColumn;
			}
		}

		return selected is null
			? null
			: new CursesInteractionHit(
				selected,
				selectedLocalRow,
				selectedLocalColumn
			)
		;
	}

	/// <summary>Disposes all live regions and scopes and closes this router to further mutation.</summary>
	public void Dispose() {
		if ( this.disposed ) {
			return;
		}

		this.ClearPendingCommandSequence();
		this.Screen.Resized -= this.HandleScreenResized;
		this.disposed = true;
		this.focusedRegion = null;
		CursesInteractionRegion[] snapshot = this.regions.ToArray();
		foreach ( CursesInteractionRegion region in snapshot ) {
			region.Dispose();
		}
		this.regions.Clear();
		this.DisposeScopeState();
	}

	internal void HandleRegionEligibilityChanged(
		CursesInteractionRegion region
	) {
		ArgumentNullException.ThrowIfNull( region );
		if ( !ReferenceEquals(
			region.Owner,
			this
		) ) {
			throw new ArgumentException(
				"The interaction region belongs to another router.",
				nameof( region )
			);
		}
		this.ClearPendingCommandSequence();

		if ( ReferenceEquals(
			this.focusedRegion,
			region
		) && !this.IsRegionEligible( region ) ) {
			this.focusedRegion = this.FindNextEligibleRegion(
				region.TraversalOrder,
				region.RegistrationOrdinal
			);
		}
	}

	internal void RemoveRegion(
		CursesInteractionRegion region
	) {
		ArgumentNullException.ThrowIfNull( region );
		if ( !ReferenceEquals(
			region.Owner,
			this
		) ) {
			throw new ArgumentException(
				"The interaction region belongs to another router.",
				nameof( region )
			);
		}

		bool wasFocused = ReferenceEquals(
			this.focusedRegion,
			region
		);
		int traversalOrder = region.TraversalOrder;
		long registrationOrdinal = region.RegistrationOrdinal;
		if ( !this.regions.Remove( region ) ) {
			throw new InvalidOperationException(
				"The interaction region is not registered with this router."
			);
		}
		this.ClearPendingCommandSequence();

		if ( wasFocused ) {
			this.focusedRegion = this.FindNextEligibleRegion(
				traversalOrder,
				registrationOrdinal
			);
		}
	}

	private CursesInteractionRegion? FindFirstEligibleRegion() {
		CursesInteractionRegion? selected = null;
		foreach ( CursesInteractionRegion region in this.regions ) {
			if ( !this.IsRegionEligible( region ) ) {
				continue;
			}
			if ( selected is null
				|| IsTraversalBefore(
					region,
					selected
				) ) {
				selected = region;
			}
		}
		return selected;
	}

	private CursesInteractionRegion? FindLastEligibleRegion() {
		CursesInteractionRegion? selected = null;
		foreach ( CursesInteractionRegion region in this.regions ) {
			if ( !this.IsRegionEligible( region ) ) {
				continue;
			}
			if ( selected is null
				|| IsTraversalAfter(
					region,
					selected
				) ) {
				selected = region;
			}
		}
		return selected;
	}

	private CursesInteractionRegion? FindNextEligibleRegion(
		int traversalOrder,
		long registrationOrdinal
	) {
		CursesInteractionRegion? selected = null;
		foreach ( CursesInteractionRegion region in this.regions ) {
			if ( !this.IsRegionEligible( region )
				|| !IsTraversalAfter(
					region,
					traversalOrder,
					registrationOrdinal
				) ) {
				continue;
			}
			if ( selected is null
				|| IsTraversalBefore(
					region,
					selected
				) ) {
				selected = region;
			}
		}

		return selected ?? this.FindFirstEligibleRegion();
	}

	private CursesInteractionRegion? FindPreviousEligibleRegion(
		int traversalOrder,
		long registrationOrdinal
	) {
		CursesInteractionRegion? selected = null;
		foreach ( CursesInteractionRegion region in this.regions ) {
			if ( !this.IsRegionEligible( region )
				|| !IsTraversalBefore(
					region,
					traversalOrder,
					registrationOrdinal
				) ) {
				continue;
			}
			if ( selected is null
				|| IsTraversalAfter(
					region,
					selected
				) ) {
				selected = region;
			}
		}

		return selected ?? this.FindLastEligibleRegion();
	}

	private bool IsPreferredHitCandidate(
		CursesInteractionRegion candidate,
		CursesInteractionRegion selected
	) {
		CursesPanel? candidatePanel = candidate.Panel;
		CursesPanel? selectedPanel = selected.Panel;

		if ( candidatePanel is not null && selectedPanel is null ) {
			return true;
		}
		if ( candidatePanel is null && selectedPanel is not null ) {
			return false;
		}

		if ( candidatePanel is not null
			&& selectedPanel is not null
			&& !ReferenceEquals(
				candidatePanel,
				selectedPanel
			) ) {
			int candidatePanelIndex = this.Screen.GetPanelOrderIndex( candidatePanel );
			int selectedPanelIndex = this.Screen.GetPanelOrderIndex( selectedPanel );
			if ( candidatePanelIndex != selectedPanelIndex ) {
				return candidatePanelIndex > selectedPanelIndex;
			}
		}

		if ( candidate.HitTestPriority != selected.HitTestPriority ) {
			return candidate.HitTestPriority > selected.HitTestPriority;
		}

		return candidate.RegistrationOrdinal > selected.RegistrationOrdinal;
	}

	private bool IsRegionEligible(
		CursesInteractionRegion region
	) {
		if ( region.IsDisposed
			|| !this.IsRegionWithinActiveScope( region )
			|| !region.IsEnabled
			|| !region.IsFocusable ) {
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

	private void RepairFocusIfNeeded() {
		if ( this.focusedRegion is null
			|| this.IsRegionEligible( this.focusedRegion ) ) {
			return;
		}

		int traversalOrder = this.focusedRegion.TraversalOrder;
		long registrationOrdinal = this.focusedRegion.RegistrationOrdinal;
		CursesInteractionRegion original = this.focusedRegion;
		this.focusedRegion = this.FindNextEligibleRegion(
			traversalOrder,
			registrationOrdinal
		);
		if ( !ReferenceEquals(
			original,
			this.focusedRegion
		) ) {
			this.ClearPendingCommandSequence();
		}
	}

	private void HandleScreenResized(
		object? sender,
		CursesScreenResizedEventArgs eventArgs
	) {
		if ( this.disposed ) {
			return;
		}

		this.ClearPendingCommandSequence();
		this.RepairPointerCaptureIfNeeded();
		this.RepairPointerGestureStateIfNeeded();
	}

	private static bool IsTraversalAfter(
		CursesInteractionRegion candidate,
		CursesInteractionRegion reference
	) {
		return IsTraversalAfter(
			candidate,
			reference.TraversalOrder,
			reference.RegistrationOrdinal
		);
	}

	private static bool IsTraversalAfter(
		CursesInteractionRegion candidate,
		int traversalOrder,
		long registrationOrdinal
	) {
		return candidate.TraversalOrder > traversalOrder
			|| ( candidate.TraversalOrder == traversalOrder
				&& candidate.RegistrationOrdinal > registrationOrdinal );
	}

	private static bool IsTraversalBefore(
		CursesInteractionRegion candidate,
		CursesInteractionRegion reference
	) {
		return IsTraversalBefore(
			candidate,
			reference.TraversalOrder,
			reference.RegistrationOrdinal
		);
	}

	private static bool IsTraversalBefore(
		CursesInteractionRegion candidate,
		int traversalOrder,
		long registrationOrdinal
	) {
		return candidate.TraversalOrder < traversalOrder
			|| ( candidate.TraversalOrder == traversalOrder
				&& candidate.RegistrationOrdinal < registrationOrdinal );
	}

	private static bool TryGetLocalCoordinates(
		CursesInteractionRegion region,
		int row,
		int column,
		out int localRow,
		out int localColumn
	) {
		CursesRectangle bounds = region.Bounds;
		CursesPanel? panel = region.Panel;

		if ( panel is null ) {
			if ( !bounds.Contains(
				row,
				column
			) ) {
				localRow = 0;
				localColumn = 0;
				return false;
			}

			localRow = row - bounds.Row;
			localColumn = column - bounds.Column;
			return true;
		}

		if ( panel.IsDisposed || !panel.IsVisible ) {
			localRow = 0;
			localColumn = 0;
			return false;
		}
		if ( !panel.Bounds.Contains(
			row,
			column
		) ) {
			localRow = 0;
			localColumn = 0;
			return false;
		}

		int panelLocalRow = row - panel.Row;
		int panelLocalColumn = column - panel.Column;
		if ( !bounds.Contains(
			panelLocalRow,
			panelLocalColumn
		) ) {
			localRow = 0;
			localColumn = 0;
			return false;
		}

		localRow = panelLocalRow - bounds.Row;
		localColumn = panelLocalColumn - bounds.Column;
		return true;
	}

	private void ThrowIfDisposed() {
		if ( this.disposed ) {
			throw new ObjectDisposedException( nameof( CursesInteractionRouter ) );
		}
	}
}
