/*
	Icod.DCurses.Tests
	Automated test suite for Icod.DCurses.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.DCurses.Tests;

using System.Reflection;
using Icod.Terminal;
using Xunit;

/// <summary>Verifies the narrow DCurses cursor-style contract and mapping.</summary>
public sealed class CursesCursorStyleContractTests {
	[Fact]
	public void StyleMappingAndLeaseSignatureAreStable() {
		Assert.Equal(
			[
				"BlinkingBlock",
				"SteadyBlock",
				"BlinkingUnderline",
				"SteadyUnderline",
				"BlinkingBar",
				"SteadyBar"
			],
			Enum.GetNames<CursesCursorStyle>()
		);
		Assert.Equal(
			TerminalCursorStyle.BlinkingBlock,
			CursesSession.ToTerminalCursorStyle( CursesCursorStyle.BlinkingBlock )
		);
		Assert.Equal(
			TerminalCursorStyle.SteadyBlock,
			CursesSession.ToTerminalCursorStyle( CursesCursorStyle.SteadyBlock )
		);
		Assert.Equal(
			TerminalCursorStyle.BlinkingUnderline,
			CursesSession.ToTerminalCursorStyle( CursesCursorStyle.BlinkingUnderline )
		);
		Assert.Equal(
			TerminalCursorStyle.SteadyUnderline,
			CursesSession.ToTerminalCursorStyle( CursesCursorStyle.SteadyUnderline )
		);
		Assert.Equal(
			TerminalCursorStyle.BlinkingBar,
			CursesSession.ToTerminalCursorStyle( CursesCursorStyle.BlinkingBar )
		);
		Assert.Equal(
			TerminalCursorStyle.SteadyBar,
			CursesSession.ToTerminalCursorStyle( CursesCursorStyle.SteadyBar )
		);

		Assert.True( typeof( CursesCursorStyleLease ).IsSealed );
		Assert.Contains( typeof( IAsyncDisposable ), typeof( CursesCursorStyleLease ).GetInterfaces() );
		Assert.Equal(
			typeof( CursesCursorStyle ),
			typeof( CursesCursorStyleLease ).GetProperty( "Style" )!.PropertyType
		);
		MethodInfo method = typeof( CursesSession ).GetMethod(
			"AcquireCursorStyleAsync",
			[
				typeof( CursesCursorStyle ),
				typeof( TimeSpan ),
				typeof( CancellationToken )
			]
		)!;
		Assert.Equal( typeof( ValueTask<CursesCursorStyleLease> ), method.ReturnType );
		Assert.True( method.GetParameters()[ 2 ].HasDefaultValue );
	}

	[Fact]
	public void InvalidStyleFailsBeforeOutputMapping() {
		Assert.Throws<ArgumentOutOfRangeException>( () =>
			CursesSession.ToTerminalCursorStyle( (CursesCursorStyle)99 )
		);
	}
}
