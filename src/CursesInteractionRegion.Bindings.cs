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

public sealed partial class CursesInteractionRegion {
	private readonly Dictionary<CursesKeyGesture, CursesCommand> gestureBindings = [];

	/// <summary>Binds one semantic gesture to an application command in this region.</summary>
	/// <param name="gesture">The bindable semantic gesture.</param>
	/// <param name="command">The application command identity.</param>
	public void BindGesture(
		CursesKeyGesture gesture,
		CursesCommand command
	) {
		ValidateBindableGesture(
			gesture,
			nameof( gesture )
		);
		ArgumentNullException.ThrowIfNull( command );
		this.ThrowIfDisposed();

		if ( this.gestureBindings.ContainsKey( gesture ) ) {
			throw new InvalidOperationException(
				"The interaction region already has a binding for this gesture."
			);
		}
		if ( CursesInteractionRouter.MaximumRegionGestureBindings <= this.gestureBindings.Count ) {
			throw new InvalidOperationException(
				$"An interaction region cannot own more than {CursesInteractionRouter.MaximumRegionGestureBindings} gesture bindings."
			);
		}

		this.owner.EnsureGestureBindingCapacity();
		this.gestureBindings.Add(
			gesture,
			command
		);
	}

	/// <summary>Removes one gesture binding from this region when present.</summary>
	/// <param name="gesture">The bindable semantic gesture.</param>
	/// <returns><see langword="true"/> when a binding was removed; otherwise <see langword="false"/>.</returns>
	public bool UnbindGesture(
		CursesKeyGesture gesture
	) {
		ValidateBindableGesture(
			gesture,
			nameof( gesture )
		);
		this.ThrowIfDisposed();
		return this.gestureBindings.Remove( gesture );
	}

	internal int GestureBindingCount => this.gestureBindings.Count;

	internal IEnumerable<KeyValuePair<CursesKeyGesture, CursesCommand>> GestureBindings => this.gestureBindings;

	internal bool TryGetCommand(
		CursesInputEvent input,
		out CursesCommand? command
	) {
		ArgumentNullException.ThrowIfNull( input );
		foreach ( KeyValuePair<CursesKeyGesture, CursesCommand> binding in this.gestureBindings ) {
			if ( binding.Key.Matches( input ) ) {
				command = binding.Value;
				return true;
			}
		}

		command = null;
		return false;
	}

	private static void ValidateBindableGesture(
		CursesKeyGesture gesture,
		string parameterName
	) {
		ArgumentException.ThrowIfNullOrEmpty( parameterName );
		if ( !gesture.IsBindable ) {
			throw new ArgumentException(
				"The gesture must be created by a CursesKeyGesture factory.",
				parameterName
			);
		}
	}
}
