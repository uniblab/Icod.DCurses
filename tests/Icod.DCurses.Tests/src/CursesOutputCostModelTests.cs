using System.Text;
using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies deterministic byte-cost calculations used by refresh optimization.</summary>
public sealed class CursesOutputCostModelTests {
	[Fact]
	public void ApplicationTextUsesConfiguredEncoding() {
		CursesOutputCostModel utf8 = new( Encoding.UTF8 );
		CursesOutputCostModel utf16 = new( Encoding.Unicode );

		Assert.Equal(
			4,
			utf8.GetApplicationTextByteCount( "A界" )
		);
		Assert.Equal(
			4,
			utf16.GetApplicationTextByteCount( "A界" )
		);
	}

	[Fact]
	public void TerminalStringIgnoresPaddingDirectiveSourceBytes() {
		int byteCount = CursesOutputCostModel.GetTerminalStringByteCount(
			"\u001b[H$<25*>X",
			affectedLines: 8
		);

		Assert.Equal( 4, byteCount );
	}

	[Fact]
	public void TerminalStringCountsLatin1ProtocolCharactersAsOneByte() {
		int byteCount = CursesOutputCostModel.GetTerminalStringByteCount(
			"\u001b[38;5;255m"
		);

		Assert.Equal( 11, byteCount );
	}

	[Fact]
	public void TerminalStringRejectsNonpositiveAffectedLineCount() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesOutputCostModel.GetTerminalStringByteCount(
				"x",
				affectedLines: 0
			)
		);
	}
}
