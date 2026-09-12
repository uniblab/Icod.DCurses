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
public sealed class CursesInteractionRouter : IDisposable {
	/// <summary>Gets the maximum number of live interaction regions owned by one router.</summary>
	public const int MaximumRegions = 4096;

	/// <summary>Gets the maximum number of gesture bindings owned by one region.</summary>
	public const int MaximumRegionGestureBindings = 256;

	/// <summary>Gets the maximum number of router-global gesture bindings.</summary>
	public const int MaximumGlobalGestureBindings = 1024;

	/// <summary>Gets the maximum total number of live gesture bindings owned by one router.</summary>
	public const int MaximumGestureBindings = 16384;

	private readonly List<CursesInteractionRegion> regions = [];
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
	}

	/// <summary>Gets the logical screen associated with this router.</summary>
	public CursesScreen Screen {
		get;
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

	/// <summary>Disposes all live regions and closes this router to further mutation.</summary>
	public void Dispose() {
		if ( this.disposed ) {
			return;
		}

		this.disposed = true;
		CursesInteractionRegion[] snapshot = this.regions.ToArray();
		foreach ( CursesInteractionRegion region in snapshot ) {
			region.Dispose();
		}
		this.regions.Clear();
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

		if ( !this.regions.Remove( region ) ) {
			throw new InvalidOperationException(
				"The interaction region is not registered with this router."
			);
		}
	}

	private void ThrowIfDisposed() {
		if ( this.disposed ) {
			throw new ObjectDisposedException( nameof( CursesInteractionRouter ) );
		}
	}
}
