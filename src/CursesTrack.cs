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

/// <summary>Distinguishes a fixed-size track from one assigned proportional remaining space.</summary>
public enum CursesTrackKind {
	Fixed,
	Weighted
}

/// <summary>Specifies how surplus space outside tracks is placed.</summary>
public enum CursesTrackDistribution {
	Start,
	Center,
	End,
	SpaceBetween,
	SpaceAround,
	SpaceEvenly
}

/// <summary>Describes a fixed or weighted track with optional size limits.</summary>
public readonly record struct CursesTrack {
	private CursesTrack( CursesTrackKind kind, int value, int minimum, int? maximum ) {
		Kind = kind;
		Value = value;
		Minimum = minimum;
		Maximum = maximum;
	}

	/// <summary>Creates a track with a preferred fixed size.</summary>
	public static CursesTrack Fixed( int size, int minimum = 0, int? maximum = null ) {
		if ( size < 0 ) {
			throw new ArgumentOutOfRangeException( nameof( size ) );
		}
		ValidateLimits( minimum, maximum );
		return new CursesTrack( CursesTrackKind.Fixed, size, minimum, maximum );
	}

	/// <summary>Creates a track that receives a positive proportional share.</summary>
	public static CursesTrack Weighted( int weight = 1, int minimum = 0, int? maximum = null ) {
		if ( weight <= 0 ) {
			throw new ArgumentOutOfRangeException( nameof( weight ) );
		}
		ValidateLimits( minimum, maximum );
		return new CursesTrack( CursesTrackKind.Weighted, weight, minimum, maximum );
	}

	/// <summary>Gets the track kind.</summary>
	public CursesTrackKind Kind { get; }

	/// <summary>Gets the preferred fixed size or positive weight.</summary>
	public int Value { get; }

	/// <summary>Gets the minimum size.</summary>
	public int Minimum { get; }

	/// <summary>Gets the optional maximum size.</summary>
	public int? Maximum { get; }

	private static void ValidateLimits( int minimum, int? maximum ) {
		if ( minimum < 0 ) {
			throw new ArgumentOutOfRangeException( nameof( minimum ) );
		}
		if ( maximum.HasValue && maximum.Value < minimum ) {
			throw new ArgumentOutOfRangeException( nameof( maximum ) );
		}
	}
}
