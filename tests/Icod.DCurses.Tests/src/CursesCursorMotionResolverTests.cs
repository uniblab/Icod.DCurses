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

/// <summary>Verifies deterministic byte-cost selection for physical cursor movement.</summary>
public sealed class CursesCursorMotionResolverTests {
	[Fact]
	public async Task OneColumnLeftBeatsAbsoluteAddress() {
		CursorResult result = await ResolveAsync(
			new TerminalDescriptionBuilder( "left-one" )
				.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
				.SetString( StringCapability.CursorLeftOne, "\b" )
				.Build(),
			0,
			1,
			0,
			0
		);

		Assert.Equal( "\b", result.Output );
		Assert.Equal( 1, result.ByteCount );
	}

	[Fact]
	public async Task ParameterizedMotionBeatsRepeatedOneStepMotion() {
		CursorResult result = await ResolveAsync(
			new TerminalDescriptionBuilder( "right-parameterized" )
				.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
				.SetString( StringCapability.CursorRight, "R%p1%d" )
				.SetString( StringCapability.CursorRightOne, ">" )
				.Build(),
			0,
			0,
			0,
			5
		);

		Assert.Equal( "R5", result.Output );
		Assert.Equal( 2, result.ByteCount );
	}

	[Fact]
	public async Task RowAndColumnAddressingWorksFromUnknownState() {
		CursorResult result = await ResolveAsync(
			new TerminalDescriptionBuilder( "row-column" )
				.SetString( StringCapability.RowAddress, "V%p1%d" )
				.SetString( StringCapability.ColumnAddress, "H%p1%d" )
				.Build(),
			currentRow: null,
			currentColumn: null,
			targetRow: 2,
			targetColumn: 3
		);

		Assert.Equal( "V2H3", result.Output );
		Assert.Equal( 4, result.ByteCount );
	}

	[Fact]
	public async Task CarriageReturnCanWinOnSameRow() {
		CursorResult result = await ResolveAsync(
			new TerminalDescriptionBuilder( "carriage-return" )
				.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
				.SetString( StringCapability.CarriageReturn, "\r" )
				.Build(),
			4,
			17,
			4,
			0
		);

		Assert.Equal( "\r", result.Output );
		Assert.Equal( 1, result.ByteCount );
	}

	[Fact]
	public async Task HomeCanWinFromUnknownState() {
		CursorResult result = await ResolveAsync(
			new TerminalDescriptionBuilder( "home" )
				.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
				.SetString( StringCapability.CursorHome, "H" )
				.Build(),
			currentRow: null,
			currentColumn: null,
			targetRow: 0,
			targetColumn: 0
		);

		Assert.Equal( "H", result.Output );
		Assert.Equal( 1, result.ByteCount );
	}

	[Fact]
	public async Task AbsoluteAddressWinsEqualCostTie() {
		CursorResult result = await ResolveAsync(
			new TerminalDescriptionBuilder( "tie" )
				.SetString( StringCapability.CursorAddress, "AB" )
				.SetString( StringCapability.CarriageReturn, "CD" )
				.Build(),
			3,
			7,
			3,
			0
		);

		Assert.Equal( "AB", result.Output );
		Assert.Equal( 2, result.ByteCount );
	}

	[Fact]
	public async Task MissingSafeMotionThrowsControlledFailure() {
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "none" ).Build()
		);
		CursesCursorMotionResolver resolver = new( session.Screen );

		NotSupportedException exception = Assert.Throws<NotSupportedException>(
			() => resolver.Resolve(
				currentRow: null,
				currentColumn: null,
				targetRow: 1,
				targetColumn: 1
			)
		);

		Assert.Equal(
			"Terminal 'none' does not provide a safe cursor-motion plan for the requested position.",
			exception.Message
		);
	}

	[Theory]
	[InlineData( -1, 0 )]
	[InlineData( 0, -1 )]
	public async Task NegativeTargetCoordinatesAreRejected( int row, int column ) {
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "negative" )
				.SetString( StringCapability.CursorAddress, "A" )
				.Build()
		);
		CursesCursorMotionResolver resolver = new( session.Screen );

		Assert.Throws<ArgumentOutOfRangeException>(
			() => resolver.Resolve( null, null, row, column )
		);
	}

	[Theory]
	[InlineData( 0, null )]
	[InlineData( null, 0 )]
	public async Task PartialCurrentPositionIsRejected( int? row, int? column ) {
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "partial" )
				.SetString( StringCapability.CursorAddress, "A" )
				.Build()
		);
		CursesCursorMotionResolver resolver = new( session.Screen );

		Assert.Throws<InvalidOperationException>(
			() => resolver.Resolve( row, column, 0, 0 )
		);
	}

	private static async Task<CursorResult> ResolveAsync(
		TerminalDescription terminal,
		int? currentRow,
		int? currentColumn,
		int targetRow,
		int targetColumn
	) {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await TerminalScreenTestSession.OpenAsync(
			terminal,
			output
		);
		CursesCursorMotionResolver resolver = new( session.Screen );
		TerminalScreenOperationPlan plan = resolver.Resolve(
			currentRow,
			currentColumn,
			targetRow,
			targetColumn
		);

		await using TerminalScreenOutputTransaction transaction =
			session.CreateScreenOutputTransaction();
		transaction.Add( plan );
		await transaction.CommitAsync();

		Assert.Equal( TerminalScreenOperationKind.CursorMove, plan.Kind );
		Assert.Equal( 1, output.FlushCount );
		return new CursorResult( output.Text, plan.ByteCount );
	}

	private static ValueTask<TerminalSession> OpenSessionAsync( TerminalDescription terminal ) {
		return TerminalScreenTestSession.OpenAsync( terminal, new RecordingTerminalOutput() );
	}

	private sealed record CursorResult( string Output, int ByteCount );
}
