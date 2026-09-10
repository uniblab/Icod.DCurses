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
/// Represents terminal-independent semantic metadata associated with retained logical content.
/// </summary>
/// <remarks>
/// Version 1.1 introduces hyperlinks as the first semantic metadata kind. The type is deliberately
/// distinct from <see cref="CursesStyle"/>, which remains presentation-only. The 1.1 constructor
/// requires a hyperlink because no other metadata kind exists yet, while the property is nullable so
/// future semantic kinds can be added without later weakening the published return-nullability contract.
/// </remarks>
public sealed record CursesCellMetadata {
	/// <summary>Initializes semantic metadata containing one hyperlink.</summary>
	/// <param name="hyperlink">The hyperlink semantic.</param>
	public CursesCellMetadata( CursesHyperlink hyperlink ) {
		ArgumentNullException.ThrowIfNull( hyperlink );
		Hyperlink = hyperlink;
	}

	/// <summary>Gets the optional hyperlink semantic.</summary>
	/// <remarks>Instances created by the 1.1 constructor always contain a hyperlink.</remarks>
	public CursesHyperlink? Hyperlink {
		get;
	}
}
