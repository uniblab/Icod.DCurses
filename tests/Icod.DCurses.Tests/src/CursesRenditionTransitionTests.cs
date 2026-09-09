using System.Text;
using Icod.DCurses.Internal;
using Icod.DCurses.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies differential physical rendition transitions.</summary>
public sealed class CursesRenditionTransitionTests {
	[Fact]
	public async Task AdditiveAttributeTransitionAvoidsResetAndColorRestore() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 2, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell(
			"A",
			Style( CursesTextAttributes.Bold )
		);
		screen.VirtualScreen[ 0, 1 ] = new CursesCell(
			"B",
			Style(
				CursesTextAttributes.Bold
					| CursesTextAttributes.Underline
			)
		);

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( 1, Count( output.Text, "<sgr0>" ) );
		Assert.Equal( 1, Count( output.Text, "<op>" ) );
		Assert.Equal( 1, Count( output.Text, "<bold>" ) );
		Assert.Equal( 1, Count( output.Text, "<underline>" ) );
		Assert.Contains( "A<underline>B", output.Text );
	}

	[Fact]
	public async Task NondefaultColorChangeAvoidsResetAndOriginalPairRestore() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 2, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell(
			"A",
			new CursesStyle(
				CursesColor.Indexed( 1 ),
				CursesColor.Default
			)
		);
		screen.VirtualScreen[ 0, 1 ] = new CursesCell(
			"B",
			new CursesStyle(
				CursesColor.Indexed( 2 ),
				CursesColor.Default
			)
		);

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( 1, Count( output.Text, "<sgr0>" ) );
		Assert.Equal( 1, Count( output.Text, "<op>" ) );
		Assert.Equal( 1, Count( output.Text, "<fg:1>" ) );
		Assert.Equal( 1, Count( output.Text, "<fg:2>" ) );
	}

	[Fact]
	public async Task AttributeRemovalRetainsResetFirstSafety() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 2, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell(
			"A",
			Style(
				CursesTextAttributes.Bold
					| CursesTextAttributes.Underline
			)
		);
		screen.VirtualScreen[ 0, 1 ] = new CursesCell(
			"B",
			Style( CursesTextAttributes.Underline )
		);

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( 2, Count( output.Text, "<sgr0>" ) );
		Assert.Equal( 2, Count( output.Text, "<op>" ) );
		Assert.Equal( 1, Count( output.Text, "<bold>" ) );
		Assert.Equal( 2, Count( output.Text, "<underline>" ) );
	}

	[Fact]
	public async Task ReturningOneColorChannelToDefaultRestoresAndReappliesOtherChannel() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 2, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell(
			"A",
			new CursesStyle(
				CursesColor.Indexed( 1 ),
				CursesColor.Indexed( 2 )
			)
		);
		screen.VirtualScreen[ 0, 1 ] = new CursesCell(
			"B",
			new CursesStyle(
				CursesColor.Default,
				CursesColor.Indexed( 2 )
			)
		);

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( 1, Count( output.Text, "<sgr0>" ) );
		Assert.Equal( 2, Count( output.Text, "<op>" ) );
		Assert.Equal( 1, Count( output.Text, "<fg:1>" ) );
		Assert.Equal( 2, Count( output.Text, "<bg:2>" ) );
	}

	[Fact]
	public async Task LogicalStylesResolvingToSamePhysicalStyleDoNotRepeatReset() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 2, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell(
			"A",
			Style( CursesTextAttributes.Italic )
		);
		screen.VirtualScreen[ 0, 1 ] = new CursesCell(
			"B",
			CursesStyle.Default
		);

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( 1, Count( output.Text, "<sgr0>" ) );
		Assert.Equal( 1, Count( output.Text, "<op>" ) );
		Assert.DoesNotContain( "<italic>", output.Text );
	}

	private static CursesStyle Style(
		CursesTextAttributes attributes
	) {
		return new CursesStyle(
			CursesColor.Default,
			CursesColor.Default,
			attributes
		);
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "rendition-transition" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetNumber( NumericCapability.Colors, 8 )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.SetString( StringCapability.EnterUnderlineMode, "<underline>" )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.Build();
	}

	private static int Count(
		string source,
		string value
	) {
		ArgumentNullException.ThrowIfNull( source );
		ArgumentNullException.ThrowIfNull( value );

		int count = 0;
		int offset = 0;
		while ( true ) {
			int match = source.IndexOf(
				value,
				offset,
				StringComparison.Ordinal
			);
			if ( 0 > match ) {
				return count;
			}
			count++;
			offset = match + value.Length;
		}
	}

	private sealed class RecordingOutput : ITerminalOutput {
		private readonly StringBuilder text = new();

		internal string Text => text.ToString();

		public ValueTask WriteTextAsync(
			string value,
			CancellationToken cancellationToken = default
		) {
			ArgumentNullException.ThrowIfNull( value );
			cancellationToken.ThrowIfCancellationRequested();
			text.Append( value );
			return ValueTask.CompletedTask;
		}

		public ValueTask WriteTerminalStringAsync(
			string value,
			int affectedLines = 1,
			CancellationToken cancellationToken = default
		) {
			ArgumentNullException.ThrowIfNull( value );
			if ( 0 >= affectedLines ) {
				throw new ArgumentOutOfRangeException( nameof( affectedLines ) );
			}
			cancellationToken.ThrowIfCancellationRequested();
			text.Append( value );
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}
	}
}
