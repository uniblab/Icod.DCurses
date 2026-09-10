using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises the T1103 logical hyperlink and semantic-metadata contract.</summary>
public sealed class CursesSemanticMetadataTests {
	[Fact]
	public void HyperlinkCanonicalizesPercentEscapesAndEmptyIdentifier() {
		CursesHyperlink hyperlink = new(
			"https://example.test/a%2fb?q=%aa",
			string.Empty
		);

		Assert.Equal(
			"https://example.test/a%2Fb?q=%AA",
			hyperlink.Uri
		);
		Assert.Null( hyperlink.Identifier );
	}

	[Fact]
	public void HyperlinkRejectsRelativeAndNonAsciiTargets() {
		Assert.Throws<ArgumentException>(
			() => new CursesHyperlink( "relative/path" )
		);
		Assert.Throws<ArgumentException>(
			() => new CursesHyperlink( "https://example.test/界" )
		);
		Assert.Throws<ArgumentException>(
			() => new CursesHyperlink( "https://example.test/%zz" )
		);
	}

	[Fact]
	public void HyperlinkIdentifierUsesTerminalCompatibleGrammar() {
		CursesHyperlink hyperlink = new(
			"https://example.test/",
			"item-1_A.~"
		);
		Assert.Equal( "item-1_A.~", hyperlink.Identifier );

		Assert.Throws<ArgumentException>(
			() => new CursesHyperlink(
				"https://example.test/",
				"not allowed"
			)
		);
	}

	[Fact]
	public void MetadataAwareWriteIsVisibleThroughSharedSubwindows() {
		CursesScreen screen = new( 12, 4 );
		CursesWindow root = screen.CreateWindow(
			1,
			2,
			2,
			8
		);
		CursesWindow child = root.CreateSubwindow(
			0,
			1,
			1,
			6
		);
		CursesCellMetadata metadata = LinkMetadata();

		child.WriteWithMetadata(
			"abc",
			metadata
		);

		Assert.Equal( metadata, child.GetMetadata( 0, 0 ) );
		Assert.Equal( metadata, root.GetMetadata( 0, 1 ) );
		Assert.Equal( metadata, screen.VirtualScreen.GetMetadata( 1, 3 ) );
		Assert.Equal( metadata, screen.VirtualScreen.GetMetadata( 1, 5 ) );
		Assert.Null( screen.VirtualScreen.GetMetadata( 1, 6 ) );
	}

	[Fact]
	public void WideWriteAppliesOneMetadataValueAcrossFootprint() {
		CursesScreen screen = new( 6, 1 );
		CursesWindow window = screen.StandardWindow;
		CursesCellMetadata metadata = LinkMetadata();

		window.WriteWithMetadata(
			"界",
			metadata
		);

		Assert.Equal( "界", window.GetCell( 0, 0 ).Content );
		Assert.True( window.GetCell( 0, 1 ).IsContinuation );
		Assert.Equal( metadata, window.GetMetadata( 0, 0 ) );
		Assert.Equal( metadata, window.GetMetadata( 0, 1 ) );
	}

	[Fact]
	public void SettingMetadataOnContinuationUpdatesWholeWideFootprint() {
		CursesScreen screen = new( 6, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Write( "界" );
		CursesCellMetadata metadata = LinkMetadata();

		window.SetMetadata(
			0,
			1,
			metadata
		);

		Assert.Equal( metadata, window.GetMetadata( 0, 0 ) );
		Assert.Equal( metadata, window.GetMetadata( 0, 1 ) );
	}

	[Fact]
	public void OrdinaryRewriteClearsSemanticMetadataEvenWhenCellValueIsSame() {
		CursesScreen screen = new( 4, 1 );
		CursesWindow window = screen.StandardWindow;
		CursesCellMetadata metadata = LinkMetadata();
		window.WriteWithMetadata(
			"A",
			metadata
		);
		Assert.Equal( metadata, window.GetMetadata( 0, 0 ) );

		window.Move( 0, 0 );
		window.Write( "A" );

		Assert.Null( window.GetMetadata( 0, 0 ) );
	}

	[Fact]
	public void SemanticOnlyChangeMarksLogicalCoordinateDirty() {
		CursesVirtualScreen screen = new( 4, 1 );
		screen.MarkClean();
		Assert.False( screen.IsDirty( 0, 1 ) );

		screen.SetMetadata(
			0,
			1,
			LinkMetadata()
		);

		Assert.True( screen.IsDirty( 0, 1 ) );
		Assert.Equal( 1, screen.SemanticMetadataCount );
	}

	[Fact]
	public void FillThatOnlyRemovesMetadataStillInvalidatesSurface() {
		CursesVirtualScreen screen = new( 4, 1 );
		screen.SetMetadata(
			0,
			1,
			LinkMetadata()
		);
		screen.MarkClean();
		Assert.Equal( 1, screen.SemanticMetadataCount );

		screen.Fill( default );

		Assert.Equal( 0, screen.SemanticMetadataCount );
		Assert.Equal( screen.CellCount, screen.DirtyCellCount );
	}

	[Fact]
	public void RemovingMetadataReleasesLogicalSemanticValue() {
		CursesVirtualScreen screen = new( 4, 1 );
		screen.SetMetadata(
			0,
			1,
			LinkMetadata()
		);
		Assert.Equal( 1, screen.SemanticMetadataCount );

		screen.SetMetadata(
			0,
			1,
			null
		);

		Assert.Null( screen.GetMetadata( 0, 1 ) );
		Assert.Equal( 0, screen.SemanticMetadataCount );
	}

	private static CursesCellMetadata LinkMetadata() {
		return new CursesCellMetadata(
			new CursesHyperlink(
				"https://example.test/docs",
				"docs"
			)
		);
	}
}
