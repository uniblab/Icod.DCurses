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
	private readonly Dictionary<CursesKeyGesture, CursesCommand> globalGestureBindings = [];

	/// <summary>Binds one semantic gesture to a router-global application command.</summary>
	/// <param name="gesture">The bindable semantic gesture.</param>
	/// <param name="command">The application command identity.</param>
	public void BindGlobalGesture(
		CursesKeyGesture gesture,
		CursesCommand command
	) {
		ValidateBindableGesture(
			gesture,
			nameof( gesture )
		);
		ArgumentNullException.ThrowIfNull( command );
		this.ThrowIfDisposed();

		if ( this.globalGestureBindings.ContainsKey( gesture ) ) {
			throw new InvalidOperationException(
				"The interaction router already has a global binding for this gesture."
			);
		}
		if ( MaximumGlobalGestureBindings <= this.globalGestureBindings.Count ) {
			throw new InvalidOperationException(
				$"An interaction router cannot own more than {MaximumGlobalGestureBindings} global gesture bindings."
			);
		}

		this.EnsureGestureBindingCapacity();
		this.globalGestureBindings.Add(
			gesture,
			command
		);
	}

	/// <summary>Removes one router-global gesture binding when present.</summary>
	/// <param name="gesture">The bindable semantic gesture.</param>
	/// <returns><see langword="true"/> when a binding was removed; otherwise <see langword="false"/>.</returns>
	public bool UnbindGlobalGesture(
		CursesKeyGesture gesture
	) {
		ValidateBindableGesture(
			gesture,
			nameof( gesture )
		);
		this.ThrowIfDisposed();
		return this.globalGestureBindings.Remove( gesture );
	}

	/// <summary>Routes one normalized input event without invoking application callbacks.</summary>
	/// <param name="input">The normalized DCurses input event.</param>
	/// <returns>An immutable structured routing result.</returns>
	public CursesInteractionResult Route(
		CursesInputEvent input
	) {
		ArgumentNullException.ThrowIfNull( input );
		this.ThrowIfDisposed();

		return input.Kind switch {
			CursesInputEventKind.Text => this.RouteKeyboardInput( input ),
			CursesInputEventKind.Key => this.RouteKeyboardInput( input ),
			CursesInputEventKind.Paste => this.RoutePasteInput( input ),
			CursesInputEventKind.Mouse => this.RouteMouseInput( input ),
			CursesInputEventKind.Focus => CursesInteractionResult.Unrouted( input ),
			CursesInputEventKind.EndOfInput => CursesInteractionResult.Unrouted( input ),
			_ => throw new InvalidOperationException(
				$"Unsupported interaction input kind: {input.Kind}."
			)
		};
	}

	internal void EnsureGestureBindingCapacity() {
		int count = this.globalGestureBindings.Count;
		foreach ( CursesInteractionRegion region in this.regions ) {
			count += region.GestureBindingCount;
			if ( MaximumGestureBindings <= count ) {
				throw new InvalidOperationException(
					$"An interaction router cannot own more than {MaximumGestureBindings} total gesture bindings."
				);
			}
		}

		if ( MaximumGestureBindings <= count ) {
			throw new InvalidOperationException(
				$"An interaction router cannot own more than {MaximumGestureBindings} total gesture bindings."
			);
		}
	}

	private CursesInteractionResult RouteKeyboardInput(
		CursesInputEvent input
	) {
		ArgumentNullException.ThrowIfNull( input );
		this.RepairFocusIfNeeded();

		if ( this.focusedRegion is not null
			&& this.focusedRegion.TryGetCommand(
				input,
				out CursesCommand? localCommand
			) ) {
			return CursesInteractionResult.CommandMatch(
				input,
				this.focusedRegion,
				localCommand!
			);
		}

		if ( this.TryGetGlobalCommand(
			input,
			out CursesCommand? globalCommand
		) ) {
			return CursesInteractionResult.CommandMatch(
				input,
				region: null,
				globalCommand!
			);
		}

		return this.focusedRegion is null
			? CursesInteractionResult.Unrouted( input )
			: CursesInteractionResult.Targeted(
				input,
				this.focusedRegion
			)
		;
	}

	private CursesInteractionResult RoutePasteInput(
		CursesInputEvent input
	) {
		ArgumentNullException.ThrowIfNull( input );
		this.RepairFocusIfNeeded();
		return this.focusedRegion is null
			? CursesInteractionResult.Unrouted( input )
			: CursesInteractionResult.Targeted(
				input,
				this.focusedRegion
			)
		;
	}

	private CursesInteractionResult RouteMouseInput(
		CursesInputEvent input
	) {
		ArgumentNullException.ThrowIfNull( input );
		CursesMouseEvent mouse = input.Mouse
			?? throw new InvalidOperationException(
				"A mouse input event must carry a mouse payload."
			);
		this.RepairPointerGestureStateIfNeeded();

		if ( this.TryGetCapturedPointerTarget(
			mouse,
			out CursesPointerTarget? capturedTarget
		) ) {
			CursesPointerGesture capturedGesture = this.ClassifyPointerGesture(
				mouse,
				capturedTarget
			);
			CursesInteractionResult capturedResult = CursesInteractionResult.Targeted(
				input,
				capturedTarget!.Region,
				hit: null,
				pointerTarget: capturedTarget,
				pointerGesture: capturedGesture
			);
			if ( CursesMouseAction.Release == mouse.Action ) {
				this.ClearPointerCapture();
			}
			return capturedResult;
		}

		CursesInteractionHit? hit = this.HitTest(
			mouse.Row,
			mouse.Column
		);
		CursesPointerTarget? target = hit is null
			? null
			: this.CreatePointerTarget(
				hit.Region,
				mouse.Row,
				mouse.Column
			)
		;
		CursesPointerGesture gesture = this.ClassifyPointerGesture(
			mouse,
			target
		);

		return hit is null
			? CursesInteractionResult.Unrouted(
				input,
				gesture
			)
			: CursesInteractionResult.Targeted(
				input,
				hit.Region,
				hit,
				pointerTarget: null,
				pointerGesture: gesture
			)
		;
	}

	private bool TryGetGlobalCommand(
		CursesInputEvent input,
		out CursesCommand? command
	) {
		ArgumentNullException.ThrowIfNull( input );
		foreach ( KeyValuePair<CursesKeyGesture, CursesCommand> binding in this.globalGestureBindings ) {
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
