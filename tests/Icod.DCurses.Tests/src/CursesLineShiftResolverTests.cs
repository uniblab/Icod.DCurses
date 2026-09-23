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

using System.Text;
using Icod.DCurses.Internal;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies deterministic Terminal-planned whole-row shifts.</summary>
public sealed class CursesLineShiftResolverTests {
	[Fact]
	public async Task DeleteLineToScreenBottomUsesDirectDeleteLines() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "direct-delete" )
				.SetString( StringCapability.CursorAddress, "<C:%p1%d,%p2%d>" )
				.SetString( StringCapability.DeleteLines, "D%p1%d" )
				.Build(),
			output
		);
		CursesVirtualScreen physicalImage = CreateScreen(
			"AAAAAAAA", "BBBBBBBB", "CCCCCCCC", "DDDDDDDD"
		);
		CursesVirtualScreen desired = CreateScreen(
			"AAAAAAAA", "CCCCCCCC", "DDDDDDDD", "        "
		);

		CursesLineShiftPlan plan = Assert.IsType<CursesLineShiftPlan>(
			CreateResolver( session ).Resolve(
				desired,
				CreatePhysicalState( physicalImage ),
				CursesStyle.Default,
				currentCursorRow: 1,
				currentCursorColumn: 0,
				requestedCursorRow: 1,
				requestedCursorColumn: 0
			)
		);

		Assert.Equal( CursesLineShiftKind.Delete, plan.Kind );
		Assert.Equal( CursesLineShiftOperation.DeleteLines, plan.Operation );
		Assert.Equal( 1, plan.TopRow );
		Assert.Equal( 3, plan.BottomRow );
		Assert.Equal( 1, plan.Count );
		Assert.False( plan.Sequence.UsesTemporaryScrollRegion );
		Assert.Equal( CursesStyle.Default, plan.StyleAfter );
		Assert.Equal( 1, plan.CursorAfterRow );
		Assert.Equal( 0, plan.CursorAfterColumn );
		Assert.Equal( string.Empty, output.Text );
		Assert.Contains(
			plan.Sequence.Plans,
			value => TerminalScreenOperationKind.LineShift == value.Kind
				&& 3 == value.AffectedLines
		);

		await CommitAsync( session, plan.Sequence );

		Assert.Equal( "D1", output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task InsertLineToScreenBottomUsesDirectInsertLines() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "direct-insert" )
				.SetString( StringCapability.CursorAddress, "<C:%p1%d,%p2%d>" )
				.SetString( StringCapability.InsertLines, "I%p1%d" )
				.Build(),
			output
		);
		CursesVirtualScreen physicalImage = CreateScreen(
			"AAAAAAAA", "BBBBBBBB", "CCCCCCCC", "DDDDDDDD"
		);
		CursesVirtualScreen desired = CreateScreen(
			"AAAAAAAA", "        ", "BBBBBBBB", "CCCCCCCC"
		);

		CursesLineShiftPlan plan = Assert.IsType<CursesLineShiftPlan>(
			CreateResolver( session ).Resolve(
				desired,
				CreatePhysicalState( physicalImage ),
				CursesStyle.Default,
				1,
				0,
				1,
				0
			)
		);

		Assert.Equal( CursesLineShiftKind.Insert, plan.Kind );
		Assert.Equal( CursesLineShiftOperation.InsertLines, plan.Operation );
		await CommitAsync( session, plan.Sequence );
		Assert.Equal( "I1", output.Text );
	}

	[Fact]
	public async Task RepeatedSingleLineCanBeatParameterizedInsertLines() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "repeat-insert" )
				.SetString( StringCapability.CursorAddress, "<C:%p1%d,%p2%d>" )
				.SetString( StringCapability.InsertLines, "LONG%p1%d" )
				.SetString( StringCapability.InsertLine, "I" )
				.Build(),
			output
		);
		CursesVirtualScreen physicalImage = CreateScreen(
			"AAAAAAAA", "BBBBBBBB", "CCCCCCCC", "DDDDDDDD", "EEEEEEEE"
		);
		CursesVirtualScreen desired = CreateScreen(
			"AAAAAAAA", "        ", "        ", "BBBBBBBB", "CCCCCCCC"
		);

		CursesLineShiftPlan plan = Assert.IsType<CursesLineShiftPlan>(
			CreateResolver( session ).Resolve(
				desired,
				CreatePhysicalState( physicalImage ),
				CursesStyle.Default,
				1,
				0,
				1,
				0
			)
		);

		Assert.Equal( 2, plan.Count );
		await CommitAsync( session, plan.Sequence );
		Assert.Equal( "II", output.Text );
	}

	[Fact]
	public async Task InteriorDeleteUsesOrderedTemporaryScrollRegion() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "bounded-delete" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.ChangeScrollRegion, "R" )
				.SetString( StringCapability.DeleteLine, "D" )
				.Build(),
			output
		);
		CursesVirtualScreen physicalImage = CreateScreen(
			"AAAAAAAA", "BBBBBBBB", "CCCCCCCC", "DDDDDDDD", "EEEEEEEE"
		);
		CursesVirtualScreen desired = CreateScreen(
			"AAAAAAAA", "CCCCCCCC", "        ", "DDDDDDDD", "EEEEEEEE"
		);

		CursesLineShiftPlan plan = Assert.IsType<CursesLineShiftPlan>(
			CreateResolver( session ).Resolve(
				desired,
				CreatePhysicalState( physicalImage ),
				CursesStyle.Default,
				0,
				0,
				0,
				0
			)
		);

		Assert.Equal( CursesLineShiftOperation.DeleteLines, plan.Operation );
		Assert.Equal( 1, plan.TopRow );
		Assert.Equal( 2, plan.BottomRow );
		Assert.True( plan.Sequence.UsesTemporaryScrollRegion );
		Assert.Equal(
			new[] {
				TerminalScreenOperationKind.ScrollRegion,
				TerminalScreenOperationKind.CursorMove,
				TerminalScreenOperationKind.LineShift,
				TerminalScreenOperationKind.ScrollRegion,
				TerminalScreenOperationKind.CursorMove
			},
			plan.Sequence.Plans.Select( value => value.Kind )
		);
		Assert.Equal( 2, plan.Sequence.Plans[ 0 ].AffectedLines );
		Assert.Equal( 2, plan.Sequence.Plans[ 2 ].AffectedLines );
		Assert.Equal( 5, plan.Sequence.Plans[ 3 ].AffectedLines );
		Assert.Equal(
			plan.Sequence.Plans.Sum( value => value.ByteCount ),
			plan.Sequence.ByteCount
		);
		Assert.Equal( string.Empty, output.Text );

		await CommitAsync( session, plan.Sequence );

		Assert.Equal( "RCDRC", output.Text );
	}

	[Fact]
	public async Task FullScreenScrollForwardCanBeatDeleteLines() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "scroll-forward" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.DeleteLines, "DELETE%p1%d" )
				.SetString( StringCapability.ScrollForwardLines, "F%p1%d" )
				.Build(),
			output
		);
		CursesVirtualScreen physicalImage = CreateScreen(
			"AAAAAAAA", "BBBBBBBB", "CCCCCCCC", "DDDDDDDD"
		);
		CursesVirtualScreen desired = CreateScreen(
			"BBBBBBBB", "CCCCCCCC", "DDDDDDDD", "        "
		);

		CursesLineShiftPlan plan = Assert.IsType<CursesLineShiftPlan>(
			CreateResolver( session ).Resolve(
				desired,
				CreatePhysicalState( physicalImage ),
				CursesStyle.Default,
				0,
				0,
				0,
				0
			)
		);

		Assert.Equal( CursesLineShiftOperation.ScrollForward, plan.Operation );
		Assert.False( plan.Sequence.UsesTemporaryScrollRegion );
		await CommitAsync( session, plan.Sequence );
		Assert.Equal( "CF1C", output.Text );
	}

	[Fact]
	public async Task FullScreenScrollReverseCanBeatInsertLines() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "scroll-reverse" )
				.SetString( StringCapability.CursorAddress, "<C:%p1%d,%p2%d>" )
				.SetString( StringCapability.InsertLines, "INSERT%p1%d" )
				.SetString( StringCapability.ScrollReverseLines, "V%p1%d" )
				.Build(),
			output
		);
		CursesVirtualScreen physicalImage = CreateScreen(
			"AAAAAAAA", "BBBBBBBB", "CCCCCCCC", "DDDDDDDD"
		);
		CursesVirtualScreen desired = CreateScreen(
			"        ", "AAAAAAAA", "BBBBBBBB", "CCCCCCCC"
		);

		CursesLineShiftPlan plan = Assert.IsType<CursesLineShiftPlan>(
			CreateResolver( session ).Resolve(
				desired,
				CreatePhysicalState( physicalImage ),
				CursesStyle.Default,
				0,
				0,
				0,
				0
			)
		);

		Assert.Equal( CursesLineShiftOperation.ScrollReverse, plan.Operation );
		await CommitAsync( session, plan.Sequence );
		Assert.Equal( "V1", output.Text );
	}

	[Fact]
	public async Task WholeRowShiftPreservesCompleteWideCellFootprints() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "wide-row" )
				.SetString( StringCapability.CursorAddress, "<C:%p1%d,%p2%d>" )
				.SetString( StringCapability.DeleteLine, "D" )
				.Build(),
			output
		);
		CursesVirtualScreen physicalImage = new( 4, 2 );
		physicalImage[ 0, 0 ] = new CursesCell( "A" );
		physicalImage[ 0, 1 ] = new CursesCell( "B" );
		physicalImage[ 0, 2 ] = new CursesCell( "C" );
		physicalImage[ 0, 3 ] = new CursesCell( "D" );
		physicalImage[ 1, 0 ] = new CursesCell( "界", CursesStyle.Default, 2 );
		physicalImage[ 1, 1 ] = CursesCell.Continuation();
		physicalImage[ 1, 2 ] = new CursesCell( "X" );
		physicalImage[ 1, 3 ] = new CursesCell( "Y" );
		CursesVirtualScreen desired = new( 4, 2 );
		for ( int column = 0; column < 4; column++ ) {
			desired[ 0, column ] = physicalImage[ 1, column ];
			desired[ 1, column ] = CursesCell.Blank();
		}

		Assert.NotNull(
			CreateResolver( session ).Resolve(
				desired,
				CreatePhysicalState( physicalImage ),
				CursesStyle.Default,
				0,
				0,
				0,
				0
			)
		);
	}

	[Fact]
	public async Task StyledVacatedRowBlocksLineShift() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "styled-vacated" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.DeleteLine, "D" )
				.Build(),
			output
		);
		CursesVirtualScreen physicalImage = CreateScreen( "AAAAAAAA", "BBBBBBBB" );
		CursesVirtualScreen desired = CreateScreen( "BBBBBBBB", "        " );
		CursesStyle bold = CursesStyle.Default.WithAttributes( CursesTextAttributes.Bold );
		for ( int column = 0; column < desired.Columns; column++ ) {
			desired[ 1, column ] = CursesCell.Blank( bold );
		}

		Assert.Null(
			CreateResolver( session ).Resolve(
				desired,
				CreatePhysicalState( physicalImage ),
				CursesStyle.Default,
				0,
				0,
				0,
				0
			)
		);
	}

	[Fact]
	public async Task UnknownPhysicalCellBlocksLineShift() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "unknown" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.DeleteLine, "D" )
				.Build(),
			output
		);
		CursesVirtualScreen physicalImage = CreateScreen( "AAAAAAAA", "BBBBBBBB" );
		CursesPhysicalScreenState physical = CreatePhysicalState( physicalImage );
		physical.Invalidate();

		Assert.Null(
			CreateResolver( session ).Resolve(
				CreateScreen( "BBBBBBBB", "        " ),
				physical,
				CursesStyle.Default,
				0,
				0,
				0,
				0
			)
		);
	}

	[Fact]
	public async Task EqualCostRetainsOrdinaryRenderer() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "tie" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.DeleteLine, "D" )
				.Build(),
			output
		);

		Assert.Null(
			CreateResolver( session ).Resolve(
				CreateScreen( "B", " " ),
				CreatePhysicalState( CreateScreen( "A", "B" ) ),
				CursesStyle.Default,
				0,
				0,
				0,
				0
			)
		);
		Assert.Equal( string.Empty, output.Text );
	}

	[Theory]
	[InlineData( "desired-metadata" )]
	[InlineData( "physical-metadata" )]
	[InlineData( "desired-raster" )]
	[InlineData( "physical-raster" )]
	public async Task RetainedStateInsideRegionBlocksLineShift( string retainedKind ) {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			CreateInteriorTerminal( "retained-inside" ),
			output
		);
		CursesVirtualScreen physicalImage = CreateInteriorPhysicalImage();
		CursesPhysicalScreenState physical = CreatePhysicalState( physicalImage );
		CursesVirtualScreen desired = CreateInteriorDesired();
		SetRetainedState( retainedKind, desired, physical, row: 2, column: 3 );

		Assert.Null(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				CursesStyle.Default,
				0,
				0,
				0,
				0
			)
		);
	}

	[Fact]
	public async Task RetainedStateOutsideRegionDoesNotBlockLineShift() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			CreateInteriorTerminal( "retained-outside" ),
			output
		);
		CursesVirtualScreen physicalImage = CreateInteriorPhysicalImage();
		CursesPhysicalScreenState physical = CreatePhysicalState( physicalImage );
		CursesVirtualScreen desired = CreateInteriorDesired();
		desired.SetMetadata(
			4,
			2,
			new CursesCellMetadata( new CursesHyperlink( "https://example.test/outside" ) )
		);
		Assert.True( physical.TryGetCell( 4, 3, out CursesCell outsideCell ) );
		physical.SetCell(
			4,
			3,
			outsideCell,
			metadata: null,
			rasterCell: CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell()
		);

		Assert.NotNull(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				CursesStyle.Default,
				0,
				0,
				0,
				0
			)
		);
	}

	[Fact]
	public async Task MissingTemporaryRegionPlanReturnsNullWithoutOutput() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "missing-region" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.DeleteLine, "D" )
				.Build(),
			output
		);

		Assert.Null(
			CreateResolver( session ).Resolve(
				CreateInteriorDesired(),
				CreatePhysicalState( CreateInteriorPhysicalImage() ),
				CursesStyle.Default,
				0,
				0,
				0,
				0
			)
		);
		Assert.Equal( string.Empty, output.Text );
	}

	[Fact]
	public async Task ZeroByteLineShiftPlanReturnsNullWithoutOutput() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "empty-line" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.DeleteLine, string.Empty )
				.Build(),
			output
		);

		Assert.Null(
			CreateResolver( session ).Resolve(
				CreateScreen( "BBBBBBBB", "        " ),
				CreatePhysicalState( CreateScreen( "AAAAAAAA", "BBBBBBBB" ) ),
				CursesStyle.Default,
				0,
				0,
				0,
				0
			)
		);
		Assert.Equal( string.Empty, output.Text );
	}

	private static CursesLineShiftResolver CreateResolver( TerminalSession session ) {
		ArgumentNullException.ThrowIfNull( session );
		return new CursesLineShiftResolver(
			session.Screen,
			new CursesOutputCostModel( Encoding.UTF8 )
		);
	}

	private static ValueTask<TerminalSession> OpenSessionAsync(
		TerminalDescription terminal,
		RecordingTerminalOutput output
	) {
		return TerminalScreenTestSession.OpenAsync( terminal, output );
	}

	private static async Task CommitAsync(
		TerminalSession session,
		CursesTerminalPlanSequence sequence
	) {
		TerminalScreenOutputTransaction transaction =
			session.CreateScreenOutputTransaction();
		foreach ( TerminalScreenOperationPlan plan in sequence.Plans ) {
			transaction.Add( plan );
		}
		await transaction.CommitAsync();
	}

	private static TerminalDescription CreateInteriorTerminal( string name ) {
		return new TerminalDescriptionBuilder( name )
			.SetString( StringCapability.CursorAddress, "C" )
			.SetString( StringCapability.ChangeScrollRegion, "R" )
			.SetString( StringCapability.DeleteLine, "D" )
			.Build();
	}

	private static CursesVirtualScreen CreateInteriorPhysicalImage() {
		return CreateScreen(
			"AAAAAAAA", "BBBBBBBB", "CCCCCCCC", "DDDDDDDD", "EEEEEEEE"
		);
	}

	private static CursesVirtualScreen CreateInteriorDesired() {
		return CreateScreen(
			"AAAAAAAA", "CCCCCCCC", "        ", "DDDDDDDD", "EEEEEEEE"
		);
	}

	private static void SetRetainedState(
		string retainedKind,
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physical,
		int row,
		int column
	) {
		CursesCellMetadata metadata = new(
			new CursesHyperlink( "https://example.test/inside" )
		);
		CursesRasterCell raster =
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		switch ( retainedKind ) {
			case "desired-metadata":
				desired.SetMetadata( row, column, metadata );
				break;
			case "physical-metadata":
				Assert.True( physical.TryGetCell( row, column, out CursesCell metadataCell ) );
				physical.SetCell( row, column, metadataCell, metadata );
				break;
			case "desired-raster":
				desired.SetRasterCell( row, column, raster );
				break;
			case "physical-raster":
				Assert.True( physical.TryGetCell( row, column, out CursesCell rasterCell ) );
				physical.SetCell(
					row,
					column,
					rasterCell,
					metadata: null,
					rasterCell: raster
				);
				break;
			default:
				throw new ArgumentOutOfRangeException( nameof( retainedKind ) );
		}
	}

	private static CursesVirtualScreen CreateScreen( params string[] rows ) {
		ArgumentNullException.ThrowIfNull( rows );
		if ( 0 == rows.Length ) {
			throw new ArgumentException( "At least one row is required.", nameof( rows ) );
		}
		int columns = rows[ 0 ].Length;
		if ( 0 == columns ) {
			throw new ArgumentException( "Rows must not be empty.", nameof( rows ) );
		}

		CursesVirtualScreen result = new( columns, rows.Length );
		for ( int row = 0; row < rows.Length; row++ ) {
			if ( rows[ row ].Length != columns ) {
				throw new ArgumentException(
					"All rows must have the same width.",
					nameof( rows )
				);
			}
			for ( int column = 0; column < columns; column++ ) {
				result[ row, column ] = ' ' == rows[ row ][ column ]
					? CursesCell.Blank()
					: new CursesCell( rows[ row ][ column ].ToString() );
			}
		}
		return result;
	}

	private static CursesPhysicalScreenState CreatePhysicalState(
		CursesVirtualScreen source
	) {
		ArgumentNullException.ThrowIfNull( source );
		CursesPhysicalScreenState result = new( source.Columns, source.Rows );
		for ( int row = 0; row < source.Rows; row++ ) {
			for ( int column = 0; column < source.Columns; column++ ) {
				result.SetCell( row, column, source[ row, column ] );
			}
		}
		return result;
	}
}
