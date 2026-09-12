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

/// <summary>Represents one immutable snapshot of an interaction-routing outcome.</summary>
public sealed class CursesInteractionResult {
	private CursesInteractionResult(
		CursesInteractionResultKind kind,
		CursesInputEvent input,
		CursesInteractionRegion? region,
		CursesCommand? command,
		CursesInteractionHit? hit
	) {
		ArgumentNullException.ThrowIfNull( input );
		this.Kind = kind;
		this.Input = input;
		this.Region = region;
		this.Command = command;
		this.Hit = hit;
	}

	/// <summary>Gets the semantic routing outcome.</summary>
	public CursesInteractionResultKind Kind {
		get;
	}

	/// <summary>Gets the exact normalized input object which was routed.</summary>
	public CursesInputEvent Input {
		get;
	}

	/// <summary>Gets the targeted region for targeted or local-command outcomes.</summary>
	public CursesInteractionRegion? Region {
		get;
	}

	/// <summary>Gets the matched command identity for command outcomes.</summary>
	public CursesCommand? Command {
		get;
	}

	/// <summary>Gets the mouse-hit snapshot for mouse-targeted outcomes.</summary>
	public CursesInteractionHit? Hit {
		get;
	}

	internal static CursesInteractionResult Unrouted(
		CursesInputEvent input
	) {
		ArgumentNullException.ThrowIfNull( input );
		return new CursesInteractionResult(
			CursesInteractionResultKind.Unrouted,
			input,
			region: null,
			command: null,
			hit: null
		);
	}

	internal static CursesInteractionResult Targeted(
		CursesInputEvent input,
		CursesInteractionRegion region,
		CursesInteractionHit? hit = null
	) {
		ArgumentNullException.ThrowIfNull( input );
		ArgumentNullException.ThrowIfNull( region );
		return new CursesInteractionResult(
			CursesInteractionResultKind.Targeted,
			input,
			region,
			command: null,
			hit
		);
	}

	internal static CursesInteractionResult CommandMatch(
		CursesInputEvent input,
		CursesInteractionRegion? region,
		CursesCommand command
	) {
		ArgumentNullException.ThrowIfNull( input );
		ArgumentNullException.ThrowIfNull( command );
		return new CursesInteractionResult(
			CursesInteractionResultKind.Command,
			input,
			region,
			command,
			hit: null
		);
	}
}
