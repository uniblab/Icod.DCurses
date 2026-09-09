using System.Text;
using Icod.DCurses.Internal;
using Icod.DCurses.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Records deterministic refresh byte baselines and accepted 0.7 optimization deltas.</summary>
public sealed class CursesRefreshCostBaselineTests {
	[Fact]
	public async Task CleanRefreshAfterBaselineEmitsNoBytes() {
		MeasuringOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new(
			4,
			1
		);
		await EstablishBaselineAsync(
			engine,
			screen,
			output
		);

		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Equal( 0, output.ByteCount );
		Assert.Equal( 0, output.WriteCount );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task OneAsciiCellBaselineEmitsTenBytes() {
		MeasuringOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new(
			4,
			1
		);
		await EstablishBaselineAsync(
			engine,
			screen,
			output
		);
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "X" );

		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Equal( 10, output.ByteCount );
		Assert.Equal( 2, output.WriteCount );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task OneWideCellBaselineCountsEncodedTextBytes() {
		MeasuringOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new(
			4,
			1
		);
		await EstablishBaselineAsync(
			engine,
			screen,
			output
		);
		screen.StandardWindow.Move(
			0,
			0
		);
		screen.StandardWindow.Write( "界" );

		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Equal( 12, output.ByteCount );
		Assert.Equal( 2, output.WriteCount );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task BoldCellT707TransitionImprovesNineteenByteT701Baseline() {
		MeasuringOutput output = new();
		CursesRefreshEngine engine = new(
			CreateRenditionTerminal(),
			output
		);
		CursesScreen screen = new(
			4,
			1
		);
		await EstablishBaselineAsync(
			engine,
			screen,
			output
		);
		screen.VirtualScreen[ 0, 0 ] = new CursesCell(
			"X",
			new CursesStyle(
				CursesColor.Default,
				CursesColor.Default,
				CursesTextAttributes.Bold
			)
		);

		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Equal( 13, output.ByteCount );
		Assert.Equal( 3, output.WriteCount );
		Assert.Equal( 1, output.FlushCount );
	}

	private static async ValueTask EstablishBaselineAsync(
		CursesRefreshEngine engine,
		CursesScreen screen,
		MeasuringOutput output
	) {
		ArgumentNullException.ThrowIfNull( engine );
		ArgumentNullException.ThrowIfNull( screen );
		ArgumentNullException.ThrowIfNull( output );

		await engine.RefreshAsync(
			screen,
			0,
			0
		);
		output.Reset();
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "cost-baseline" )
			.SetString(
				StringCapability.CursorAddress,
				"<cup:%p1%d,%p2%d>"
			)
			.Build();
	}

	private static TerminalDescription CreateRenditionTerminal() {
		return new TerminalDescriptionBuilder( "cost-rendition-baseline" )
			.SetString(
				StringCapability.CursorAddress,
				"<cup:%p1%d,%p2%d>"
			)
			.SetString(
				StringCapability.ExitAttributeMode,
				"<sgr0>"
			)
			.SetString(
				StringCapability.EnterBoldMode,
				"<b>"
			)
			.Build();
	}

	private sealed class MeasuringOutput : ITerminalOutput {
		private readonly CursesOutputCostModel costModel = new( Encoding.UTF8 );

		internal int ByteCount {
			get;
			private set;
		}

		internal int WriteCount {
			get;
			private set;
		}

		internal int FlushCount {
			get;
			private set;
		}

		internal void Reset() {
			this.ByteCount = 0;
			this.WriteCount = 0;
			this.FlushCount = 0;
		}

		public ValueTask WriteTextAsync(
			string value,
			CancellationToken cancellationToken = default
		) {
			ArgumentNullException.ThrowIfNull( value );
			cancellationToken.ThrowIfCancellationRequested();
			this.ByteCount = checked(
				this.ByteCount + this.costModel.GetApplicationTextByteCount( value )
			);
			this.WriteCount = checked( this.WriteCount + 1 );
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
			this.ByteCount = checked(
				this.ByteCount
					+ CursesOutputCostModel.GetTerminalStringByteCount(
						value,
						affectedLines
					)
			);
			this.WriteCount = checked( this.WriteCount + 1 );
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			this.FlushCount = checked( this.FlushCount + 1 );
			return ValueTask.CompletedTask;
		}
	}
}
