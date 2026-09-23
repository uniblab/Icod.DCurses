using Xunit;

namespace Icod.DCurses.Tests;

public sealed class T2101CorePresentationRedWitnessTests {
	[Fact]
	public void RichTextSpanCarriesValidatedSourcePresentation() {
		CursesTextPosition start = new( 0 );
		CursesTextSpan span = new(
			start,
			1,
			CursesStyle.Default
		);

		Assert.Equal( 0, span.Start.Offset );
		Assert.Equal( 1, span.Length );
		Assert.Equal( CursesStyle.Default, span.Style );
		Assert.Null( span.Metadata );
	}
}
