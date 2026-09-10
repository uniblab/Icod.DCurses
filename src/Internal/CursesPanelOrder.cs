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

namespace Icod.DCurses.Internal;

/// <summary>
/// Maintains deterministic bottom-to-top ordering for panel identities.
/// </summary>
/// <typeparam name="T">The reference-identity panel type.</typeparam>
internal sealed class CursesPanelOrder<T>
	where T : class {
	private readonly List<T> panels = [];

	/// <summary>Gets the number of ordered panel identities.</summary>
	internal int Count => panels.Count;

	/// <summary>Adds one panel identity at the top of the order.</summary>
	/// <param name="panel">The panel identity to add.</param>
	internal void Add( T panel ) {
		ArgumentNullException.ThrowIfNull( panel );

		if ( 0 <= IndexOfReference( panel ) ) {
			throw new InvalidOperationException(
				"The panel is already a member of this order."
			);
		}

		panels.Add( panel );
	}

	/// <summary>Removes one panel identity when it is present.</summary>
	/// <param name="panel">The panel identity to remove.</param>
	/// <returns><see langword="true"/> when the panel was removed; otherwise <see langword="false"/>.</returns>
	internal bool Remove( T panel ) {
		ArgumentNullException.ThrowIfNull( panel );

		int index = IndexOfReference( panel );
		if ( 0 > index ) {
			return false;
		}

		panels.RemoveAt( index );
		return true;
	}

	/// <summary>Moves one panel identity to the top while preserving all other relative order.</summary>
	/// <param name="panel">The panel identity to move.</param>
	internal void MoveToTop( T panel ) {
		ArgumentNullException.ThrowIfNull( panel );

		int index = GetRequiredIndex( panel );
		if ( panels.Count - 1 == index ) {
			return;
		}

		panels.RemoveAt( index );
		panels.Add( panel );
	}

	/// <summary>Moves one panel identity to the bottom while preserving all other relative order.</summary>
	/// <param name="panel">The panel identity to move.</param>
	internal void MoveToBottom( T panel ) {
		ArgumentNullException.ThrowIfNull( panel );

		int index = GetRequiredIndex( panel );
		if ( 0 == index ) {
			return;
		}

		panels.RemoveAt( index );
		panels.Insert(
			0,
			panel
		);
	}

	/// <summary>Moves one panel identity immediately above another panel identity.</summary>
	/// <param name="panel">The panel identity to move.</param>
	/// <param name="sibling">The sibling which should immediately precede <paramref name="panel"/>.</param>
	internal void MoveAbove(
		T panel,
		T sibling
	) {
		ArgumentNullException.ThrowIfNull( panel );
		ArgumentNullException.ThrowIfNull( sibling );
		ValidateDistinctReferences(
			panel,
			sibling
		);
		MoveRelative(
			panel,
			sibling,
			above: true
		);
	}

	/// <summary>Moves one panel identity immediately below another panel identity.</summary>
	/// <param name="panel">The panel identity to move.</param>
	/// <param name="sibling">The sibling which should immediately follow <paramref name="panel"/>.</param>
	internal void MoveBelow(
		T panel,
		T sibling
	) {
		ArgumentNullException.ThrowIfNull( panel );
		ArgumentNullException.ThrowIfNull( sibling );
		ValidateDistinctReferences(
			panel,
			sibling
		);
		MoveRelative(
			panel,
			sibling,
			above: false
		);
	}

	/// <summary>Creates a stable bottom-to-top snapshot of the current order.</summary>
	/// <returns>A new array containing the current ordered identities.</returns>
	internal T[] SnapshotBottomToTop() {
		return panels.ToArray();
	}

	private void MoveRelative(
		T panel,
		T sibling,
		bool above
	) {
		ArgumentNullException.ThrowIfNull( panel );
		ArgumentNullException.ThrowIfNull( sibling );

		int panelIndex = GetRequiredIndex( panel );
		int siblingIndex = GetRequiredIndex( sibling );

		panels.RemoveAt( panelIndex );
		if ( panelIndex < siblingIndex ) {
			siblingIndex--;
		}

		int destinationIndex = above
			? siblingIndex + 1
			: siblingIndex
		;
		panels.Insert(
			destinationIndex,
			panel
		);
	}

	private int GetRequiredIndex( T panel ) {
		ArgumentNullException.ThrowIfNull( panel );

		int index = IndexOfReference( panel );
		if ( 0 > index ) {
			throw new InvalidOperationException(
				"The panel is not a member of this order."
			);
		}

		return index;
	}

	private int IndexOfReference( T panel ) {
		ArgumentNullException.ThrowIfNull( panel );

		for ( int index = 0; index < panels.Count; index++ ) {
			if ( ReferenceEquals(
				panels[ index ],
				panel
			) ) {
				return index;
			}
		}

		return -1;
	}

	private static void ValidateDistinctReferences(
		T panel,
		T sibling
	) {
		ArgumentNullException.ThrowIfNull( panel );
		ArgumentNullException.ThrowIfNull( sibling );

		if ( ReferenceEquals(
			panel,
			sibling
		) ) {
			throw new ArgumentException(
				"A panel cannot be ordered relative to itself.",
				nameof( sibling )
			);
		}
	}
}
