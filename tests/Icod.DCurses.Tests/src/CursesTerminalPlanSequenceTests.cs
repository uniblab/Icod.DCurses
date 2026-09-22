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

using Icod.DCurses.Internal;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies immutable ordered bundles of Terminal-owned operation plans.</summary>
public sealed class CursesTerminalPlanSequenceTests {
	[Fact]
	public async Task PlansPreserveOrderAggregateCostAndCopyTheInput() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await TerminalScreenTestSession.OpenAsync(
			new TerminalDescriptionBuilder( "plan-sequence" )
				.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
				.SetString( StringCapability.CursorRightOne, ">" )
				.Build(),
			output
		);
		TerminalScreenOperationPlan first = Assert.IsType<TerminalScreenOperationPlan>(
			session.Screen.PlanCursorMove(
				null,
				new TerminalScreenPosition( 1, 2 )
			)
		);
		TerminalScreenOperationPlan second = Assert.IsType<TerminalScreenOperationPlan>(
			session.Screen.PlanCursorMove(
				new TerminalScreenPosition( 1, 2 ),
				new TerminalScreenPosition( 1, 3 )
			)
		);
		List<TerminalScreenOperationPlan> input = [ first, second ];
		TerminalScreenOperationPlan[] expected = [ first, second ];

		CursesTerminalPlanSequence sequence = new(
			input,
			usesTemporaryScrollRegion: true
		);
		input[ 0 ] = second;

		Assert.Equal( expected, sequence.Plans );
		Assert.Equal(
			checked( first.ByteCount + second.ByteCount ),
			sequence.ByteCount
		);
		Assert.True( sequence.UsesTemporaryScrollRegion );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public void EmptySequenceIsRejected() {
		Assert.Throws<ArgumentException>(
			() => new CursesTerminalPlanSequence( [] )
		);
	}

	[Fact]
	public void DefaultPlanIsRejected() {
		Assert.Throws<ArgumentException>(
			() => new CursesTerminalPlanSequence( [ default ] )
		);
	}

	[Fact]
	public void NullSequenceIsRejected() {
		Assert.Throws<ArgumentNullException>(
			() => new CursesTerminalPlanSequence( null! )
		);
	}
}
