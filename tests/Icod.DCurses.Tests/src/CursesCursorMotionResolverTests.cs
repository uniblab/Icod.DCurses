using Icod.DCurses.Internal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies deterministic byte-cost selection for physical cursor movement.</summary>
public sealed class CursesCursorMotionResolverTests {
	[Fact]
	public void OneColumnLeftBeatsAbsoluteAddress() {
		CursesCursorMotionResolver resolver = new(
			new TerminalDescriptionBuilder( "left-one" )
				.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
				.SetString( StringCapability.CursorLeftOne, "\b" )
				.Build()
		);

		CursesCursorMotion motion = resolver.Resolve(
			0,
			1,
			0,
			0
		);

		Assert.Equal( "\b", motion.Sequence );
		Assert.Equal( 1, motion.ByteCount );
	}

	[Fact]
	public void ParameterizedMotionBeatsRepeatedOneStepMotion() {
		CursesCursorMotionResolver resolver = new(
			new TerminalDescriptionBuilder( "right-parameterized" )
				.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
				.SetString( StringCapability.CursorRight, "R%p1%d" )
				.SetString( StringCapability.CursorRightOne, ">" )
				.Build()
		);

		CursesCursorMotion motion = resolver.Resolve(
			0,
			0,
			0,
			5
		);

		Assert.Equal( "R5", motion.Sequence );
		Assert.Equal( 2, motion.ByteCount );
	}

	[Fact]
	public void RowAndColumnAddressingWorksFromUnknownState() {
		CursesCursorMotionResolver resolver = new(
			new TerminalDescriptionBuilder( "row-column" )
				.SetString( StringCapability.RowAddress, "V%p1%d" )
				.SetString( StringCapability.ColumnAddress, "H%p1%d" )
				.Build()
		);

		CursesCursorMotion motion = resolver.Resolve(
			currentRow: null,
			currentColumn: null,
			targetRow: 2,
			targetColumn: 3
		);

		Assert.Equal( "V2H3", motion.Sequence );
		Assert.Equal( 4, motion.ByteCount );
	}

	[Fact]
	public void CarriageReturnCanWinOnSameRow() {
		CursesCursorMotionResolver resolver = new(
			new TerminalDescriptionBuilder( "carriage-return" )
				.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
				.SetString( StringCapability.CarriageReturn, "\r" )
				.Build()
		);

		CursesCursorMotion motion = resolver.Resolve(
			4,
			17,
			4,
			0
		);

		Assert.Equal( "\r", motion.Sequence );
		Assert.Equal( 1, motion.ByteCount );
	}

	[Fact]
	public void HomeCanWinFromUnknownState() {
		CursesCursorMotionResolver resolver = new(
			new TerminalDescriptionBuilder( "home" )
				.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
				.SetString( StringCapability.CursorHome, "H" )
				.Build()
		);

		CursesCursorMotion motion = resolver.Resolve(
			currentRow: null,
			currentColumn: null,
			targetRow: 0,
			targetColumn: 0
		);

		Assert.Equal( "H", motion.Sequence );
		Assert.Equal( 1, motion.ByteCount );
	}

	[Fact]
	public void AbsoluteAddressWinsEqualCostTie() {
		CursesCursorMotionResolver resolver = new(
			new TerminalDescriptionBuilder( "tie" )
				.SetString( StringCapability.CursorAddress, "AB" )
				.SetString( StringCapability.CarriageReturn, "CD" )
				.Build()
		);

		CursesCursorMotion motion = resolver.Resolve(
			3,
			7,
			3,
			0
		);

		Assert.Equal( "AB", motion.Sequence );
		Assert.Equal( 2, motion.ByteCount );
	}

	[Fact]
	public void MissingSafeMotionThrowsControlledFailure() {
		CursesCursorMotionResolver resolver = new(
			new TerminalDescriptionBuilder( "none" ).Build()
		);

		Assert.Throws<NotSupportedException>(
			() => resolver.Resolve(
				currentRow: null,
				currentColumn: null,
				targetRow: 1,
				targetColumn: 1
			)
		);
	}
}
