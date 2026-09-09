using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies independent pad-viewport observation of semantic-only changes.</summary>
public sealed class CursesSemanticViewportIndependenceTests {
	[Fact]
	public void IndependentViewportsObserveSemanticChangeUntilEachPresents() {
		CursesPad pad = new( 6, 2 );
		pad.ContentWindow.Move( 0, 2 );
		pad.ContentWindow.Write( "X" );
		CursesScreen firstScreen = new( 6, 2 );
		CursesScreen secondScreen = new( 6, 2 );
		CursesPadViewport first = pad.CreateViewport(
			firstScreen.StandardWindow,
			0,
			0,
			2,
			6,
			0,
			0
		);
		CursesPadViewport second = pad.CreateViewport(
			secondScreen.StandardWindow,
			0,
			0,
			2,
			6,
			0,
			0
		);
		first.Present();
		second.Present();
		Assert.False( first.HasVisiblePadChanges );
		Assert.False( second.HasVisiblePadChanges );
		CursesCellMetadata metadata = new(
			new CursesHyperlink(
				"https://example.test/independent",
				"independent"
			)
		);

		pad.ContentWindow.SetMetadata(
			0,
			2,
			metadata
		);

		Assert.True( first.HasVisiblePadChanges );
		Assert.True( second.HasVisiblePadChanges );

		first.Present();

		Assert.False( first.HasVisiblePadChanges );
		Assert.True( second.HasVisiblePadChanges );
		Assert.Equal(
			metadata,
			firstScreen.StandardWindow.GetMetadata( 0, 2 )
		);
		Assert.Null( secondScreen.StandardWindow.GetMetadata( 0, 2 ) );

		second.Present();

		Assert.False( second.HasVisiblePadChanges );
		Assert.Equal(
			metadata,
			secondScreen.StandardWindow.GetMetadata( 0, 2 )
		);
	}
}
