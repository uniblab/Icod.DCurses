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

/// <summary>Describes the acknowledged extent of one raster-atlas presentation.</summary>
public readonly record struct CursesRasterAtlasPresentationResult {
	internal CursesRasterAtlasPresentationResult(
		CursesRasterAtlasPresentationStatus status,
		int requestedUpdateCount,
		int completedUpdateCount,
		bool frameSelected,
		string? message
	) {
		Status = status;
		RequestedUpdateCount = requestedUpdateCount;
		CompletedUpdateCount = completedUpdateCount;
		FrameSelected = frameSelected;
		Message = message;
	}

	/// <summary>Gets the semantic presentation status.</summary>
	public CursesRasterAtlasPresentationStatus Status { get; }

	/// <summary>Gets the number of validated changes requested by the caller.</summary>
	public int RequestedUpdateCount { get; }

	/// <summary>Gets the number of tile-region changes acknowledged before completion or failure.</summary>
	public int CompletedUpdateCount { get; }

	/// <summary>Gets whether the updated back frame was acknowledged as selected.</summary>
	public bool FrameSelected { get; }

	/// <summary>Gets an optional controlled diagnostic.</summary>
	public string? Message { get; }
}
