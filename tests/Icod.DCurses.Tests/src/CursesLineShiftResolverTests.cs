using System.Text;
using Icod.DCurses.Internal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies deterministic whole-row line-shift and scroll-region selection.</summary>
public sealed class CursesLineShiftResolverTests {
	[Fact]
	public void DeleteLineToScreenBottomUsesDirectDeleteLines() {
		CursesVirtualScreen physicalImage = CreateScreen(
			"AAAAAAAA",
			"BBBBBBBB",
			"CCCCCCCC",
			"DDDDDDDD"
		);
		CursesPhysicalScreenState physical = CreatePhysicalState( physicalImage );
		CursesVirtualScreen desired = CreateScreen(
			"AAAAAAAA",
			"CCCCCCCC",
			"DDDDDDDD",
			"        "
		);
		CursesLineShiftResolver resolver = new(
			new TerminalDescriptionBuilder( "direct-delete" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.DeleteLines, "D%p1%d" )
				.Build(),
			new CursesOutputCostModel( Encoding.UTF8 )
		);

		CursesLineShiftPlan? plan = resolver.Resolve(
			desired,
			physical,
			CursesStyle.Default,
			currentCursorRow: 1,
			currentCursorColumn: 0,
			requestedCursorRow: 1,
			requestedCursorColumn: 0
		);

		Assert.True( plan.HasValue );
		Assert.Equal( CursesLineShiftKind.Delete, plan.Value.Kind );
		Assert.Equal( CursesLineShiftOperation.DeleteLines, plan.Value.Operation );
		Assert.Equal( 1, plan.Value.TopRow );
		Assert.Equal( 3, plan.Value.BottomRow );
		Assert.Equal( 1, plan.Value.Count );
		Assert.Equal( "D1", plan.Value.OperationSequence );
		Assert.False( plan.Value.UsesTemporaryScrollRegion );
	}

	[Fact]
	public void InsertLineToScreenBottomUsesDirectInsertLines() {
		CursesVirtualScreen physicalImage = CreateScreen(
			"AAAAAAAA",
			"BBBBBBBB",
			"CCCCCCCC",
			"DDDDDDDD"
		);
		CursesPhysicalScreenState physical = CreatePhysicalState( physicalImage );
		CursesVirtualScreen desired = CreateScreen(
			"AAAAAAAA",
			"        ",
			"BBBBBBBB",
			"CCCCCCCC"
		);
		CursesLineShiftResolver resolver = new(
			new TerminalDescriptionBuilder( "direct-insert" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.InsertLines, "I%p1%d" )
				.Build(),
			new CursesOutputCostModel( Encoding.UTF8 )
		);

		CursesLineShiftPlan? plan = resolver.Resolve(
			desired,
			physical,
			CursesStyle.Default,
			currentCursorRow: 1,
			currentCursorColumn: 0,
			requestedCursorRow: 1,
			requestedCursorColumn: 0
		);

		Assert.True( plan.HasValue );
		Assert.Equal( CursesLineShiftKind.Insert, plan.Value.Kind );
		Assert.Equal( CursesLineShiftOperation.InsertLines, plan.Value.Operation );
		Assert.Equal( "I1", plan.Value.OperationSequence );
		Assert.False( plan.Value.UsesTemporaryScrollRegion );
	}

	[Fact]
	public void RepeatedOneLineCapabilityCanBeatParameterizedInsertLines() {
		CursesVirtualScreen physicalImage = CreateScreen(
			"AAAAAAAA",
			"BBBBBBBB",
			"CCCCCCCC",
			"DDDDDDDD",
			"EEEEEEEE"
		);
		CursesPhysicalScreenState physical = CreatePhysicalState( physicalImage );
		CursesVirtualScreen desired = CreateScreen(
			"AAAAAAAA",
			"        ",
			"        ",
			"BBBBBBBB",
			"CCCCCCCC"
		);
		CursesLineShiftResolver resolver = new(
			new TerminalDescriptionBuilder( "repeat-insert" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.InsertLines, "LONG%p1%d" )
				.SetString( StringCapability.InsertLine, "I" )
				.Build(),
			new CursesOutputCostModel( Encoding.UTF8 )
		);

		CursesLineShiftPlan? plan = resolver.Resolve(
			desired,
			physical,
			CursesStyle.Default,
			currentCursorRow: 1,
			currentCursorColumn: 0,
			requestedCursorRow: 1,
			requestedCursorColumn: 0
		);

		Assert.True( plan.HasValue );
		Assert.Equal( 2, plan.Value.Count );
		Assert.Equal( "II", plan.Value.OperationSequence );
	}

	[Fact]
	public void InteriorDeleteUsesTemporaryScrollRegion() {
		CursesVirtualScreen physicalImage = CreateScreen(
			"AAAAAAAA",
			"BBBBBBBB",
			"CCCCCCCC",
			"DDDDDDDD",
			"EEEEEEEE"
		);
		CursesPhysicalScreenState physical = CreatePhysicalState( physicalImage );
		CursesVirtualScreen desired = CreateScreen(
			"AAAAAAAA",
			"CCCCCCCC",
			"        ",
			"DDDDDDDD",
			"EEEEEEEE"
		);
		CursesLineShiftResolver resolver = new(
			new TerminalDescriptionBuilder( "bounded-delete" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.ChangeScrollRegion, "R" )
				.SetString( StringCapability.DeleteLine, "D" )
				.Build(),
			new CursesOutputCostModel( Encoding.UTF8 )
		);

		CursesLineShiftPlan? plan = resolver.Resolve(
			desired,
			physical,
			CursesStyle.Default,
			currentCursorRow: 0,
			currentCursorColumn: 0,
			requestedCursorRow: 0,
			requestedCursorColumn: 0
		);

		Assert.True( plan.HasValue );
		Assert.Equal( CursesLineShiftOperation.DeleteLines, plan.Value.Operation );
		Assert.True( plan.Value.UsesTemporaryScrollRegion );
		Assert.Equal( 1, plan.Value.TopRow );
		Assert.Equal( 2, plan.Value.BottomRow );
		Assert.Equal( "R", plan.Value.SetRegionSequence );
		Assert.Equal( "R", plan.Value.RestoreRegionSequence );
		Assert.Null( plan.Value.CursorAfterRow );
		Assert.Null( plan.Value.CursorAfterColumn );
	}

	[Fact]
	public void FullScreenScrollForwardCanBeatDeleteLines() {
		CursesVirtualScreen physicalImage = CreateScreen(
			"AAAAAAAA",
			"BBBBBBBB",
			"CCCCCCCC",
			"DDDDDDDD"
		);
		CursesPhysicalScreenState physical = CreatePhysicalState( physicalImage );
		CursesVirtualScreen desired = CreateScreen(
			"BBBBBBBB",
			"CCCCCCCC",
			"DDDDDDDD",
			"        "
		);
		CursesLineShiftResolver resolver = new(
			new TerminalDescriptionBuilder( "scroll-forward" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.DeleteLines, "DELETE%p1%d" )
				.SetString( StringCapability.ScrollForwardLines, "F%p1%d" )
				.Build(),
			new CursesOutputCostModel( Encoding.UTF8 )
		);

		CursesLineShiftPlan? plan = resolver.Resolve(
			desired,
			physical,
			CursesStyle.Default,
			currentCursorRow: 0,
			currentCursorColumn: 0,
			requestedCursorRow: 0,
			requestedCursorColumn: 0
		);

		Assert.True( plan.HasValue );
		Assert.Equal( CursesLineShiftOperation.ScrollForward, plan.Value.Operation );
		Assert.Equal( "F1", plan.Value.OperationSequence );
		Assert.False( plan.Value.UsesTemporaryScrollRegion );
		Assert.Equal( 3, plan.Value.OperationRow );
	}

	[Fact]
	public void WholeRowShiftPreservesCompleteWideCellFootprints() {
		CursesVirtualScreen physicalImage = new( 4, 2 );
		physicalImage[ 0, 0 ] = new CursesCell( "A" );
		physicalImage[ 0, 1 ] = new CursesCell( "B" );
		physicalImage[ 0, 2 ] = new CursesCell( "C" );
		physicalImage[ 0, 3 ] = new CursesCell( "D" );
		physicalImage[ 1, 0 ] = new CursesCell( "界" );
		physicalImage[ 1, 1 ] = CursesCell.Continuation();
		physicalImage[ 1, 2 ] = new CursesCell( "X" );
		physicalImage[ 1, 3 ] = new CursesCell( "Y" );
		CursesPhysicalScreenState physical = CreatePhysicalState( physicalImage );
		CursesVirtualScreen desired = new( 4, 2 );
		for ( int column = 0; column < 4; column++ ) {
			desired[ 0, column ] = physicalImage[ 1, column ];
			desired[ 1, column ] = CursesCell.Blank();
		}
		CursesLineShiftResolver resolver = new(
			new TerminalDescriptionBuilder( "wide-row" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.DeleteLine, "D" )
				.Build(),
			new CursesOutputCostModel( Encoding.UTF8 )
		);

		CursesLineShiftPlan? plan = resolver.Resolve(
			desired,
			physical,
			CursesStyle.Default,
			currentCursorRow: 0,
			currentCursorColumn: 0,
			requestedCursorRow: 0,
			requestedCursorColumn: 0
		);

		Assert.True( plan.HasValue );
		Assert.Equal( CursesLineShiftOperation.DeleteLines, plan.Value.Operation );
	}

	[Fact]
	public void StyledVacatedRowBlocksLineShift() {
		CursesVirtualScreen physicalImage = CreateScreen(
			"AAAAAAAA",
			"BBBBBBBB"
		);
		CursesPhysicalScreenState physical = CreatePhysicalState( physicalImage );
		CursesVirtualScreen desired = CreateScreen(
			"BBBBBBBB",
			"        "
		);
		CursesStyle bold = CursesStyle.Default.WithAttributes(
			CursesTextAttributes.Bold
		);
		for ( int column = 0; column < desired.Columns; column++ ) {
			desired[ 1, column ] = CursesCell.Blank( bold );
		}
		CursesLineShiftResolver resolver = new(
			new TerminalDescriptionBuilder( "styled-vacated" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.DeleteLine, "D" )
				.Build(),
			new CursesOutputCostModel( Encoding.UTF8 )
		);

		Assert.Null(
			resolver.Resolve(
				desired,
				physical,
				CursesStyle.Default,
				currentCursorRow: 0,
				currentCursorColumn: 0,
				requestedCursorRow: 0,
				requestedCursorColumn: 0
			)
		);
	}

	[Fact]
	public void UnknownPhysicalCellBlocksLineShift() {
		CursesVirtualScreen physicalImage = CreateScreen(
			"AAAAAAAA",
			"BBBBBBBB"
		);
		CursesPhysicalScreenState physical = new( 8, 2 );
		for ( int row = 0; row < 2; row++ ) {
			for ( int column = 0; column < 8; column++ ) {
				if ( 1 == row && 7 == column ) {
					continue;
				}
				physical.SetCell(
					row,
					column,
					physicalImage[ row, column ]
				);
			}
		}
		CursesVirtualScreen desired = CreateScreen(
			"BBBBBBBB",
			"        "
		);
		CursesLineShiftResolver resolver = new(
			new TerminalDescriptionBuilder( "unknown" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.DeleteLine, "D" )
				.Build(),
			new CursesOutputCostModel( Encoding.UTF8 )
		);

		Assert.Null(
			resolver.Resolve(
				desired,
				physical,
				CursesStyle.Default,
				currentCursorRow: 0,
				currentCursorColumn: 0,
				requestedCursorRow: 0,
				requestedCursorColumn: 0
			)
		);
	}

	[Fact]
	public void EqualCostRetainsOrdinaryRenderer() {
		CursesVirtualScreen physicalImage = CreateScreen(
			"A",
			"B"
		);
		CursesPhysicalScreenState physical = CreatePhysicalState( physicalImage );
		CursesVirtualScreen desired = CreateScreen(
			"B",
			" "
		);
		CursesLineShiftResolver resolver = new(
			new TerminalDescriptionBuilder( "tie" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.DeleteLine, "D" )
				.Build(),
			new CursesOutputCostModel( Encoding.UTF8 )
		);

		Assert.Null(
			resolver.Resolve(
				desired,
				physical,
				CursesStyle.Default,
				currentCursorRow: 0,
				currentCursorColumn: 0,
				requestedCursorRow: 0,
				requestedCursorColumn: 0
			)
		);
	}

	private static CursesVirtualScreen CreateScreen(
		params string[] rows
	) {
		ArgumentNullException.ThrowIfNull( rows );
		if ( 0 == rows.Length ) {
			throw new ArgumentException(
				"At least one row is required.",
				nameof( rows )
			);
		}
		int columns = rows[ 0 ].Length;
		if ( 0 == columns ) {
			throw new ArgumentException(
				"Rows must not be empty.",
				nameof( rows )
			);
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
				if ( ' ' == rows[ row ][ column ] ) {
					result[ row, column ] = CursesCell.Blank();
				} else {
					result[ row, column ] = new CursesCell(
						rows[ row ][ column ].ToString()
					);
				}
			}
		}
		return result;
	}

	private static CursesPhysicalScreenState CreatePhysicalState(
		CursesVirtualScreen source
	) {
		ArgumentNullException.ThrowIfNull( source );
		CursesPhysicalScreenState result = new(
			source.Columns,
			source.Rows
		);
		for ( int row = 0; row < source.Rows; row++ ) {
			for ( int column = 0; column < source.Columns; column++ ) {
				result.SetCell(
					row,
					column,
					source[ row, column ]
				);
			}
		}
		return result;
	}
}
