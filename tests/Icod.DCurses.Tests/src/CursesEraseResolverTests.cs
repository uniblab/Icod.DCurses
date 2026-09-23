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

/// <summary>Verifies Terminal-planned cost and safety selection for physical erase operations.</summary>
public sealed class CursesEraseResolverTests {
	[Fact]
	public async Task LiteralBlanksRemainFallbackWhenShorterThanEraseLine() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "literal-win" )
				.SetString( StringCapability.ClearToEndOfLine, "<el>" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = new( 2, 1 );
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen( desired, "x" );

		CursesErasePlan? plan = CreateResolver( session ).Resolve(
			desired,
			physical,
			0,
			0,
			CursesStyle.Default,
			0,
			0
		);

		Assert.Null( plan );
		Assert.Equal( string.Empty, output.Text );
	}

	[Fact]
	public async Task EraseLineUsesOneOpaqueTerminalPlanWhenCheaper() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "el-win" )
				.SetString( StringCapability.ClearToEndOfLine, "E" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = new( 6, 1 );
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen( desired, "x" );

		CursesErasePlan plan = Assert.IsType<CursesErasePlan>(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				0,
				CursesStyle.Default,
				0,
				0
			)
		);

		Assert.Equal( CursesEraseKind.ClearToEndOfLine, plan.Kind );
		Assert.Equal( 1, plan.Sequence.ByteCount );
		TerminalScreenOperationPlan operation = Assert.Single( plan.Sequence.Plans );
		Assert.Equal( TerminalScreenOperationKind.Erase, operation.Kind );
		Assert.Equal( 1, operation.AffectedLines );
		Assert.Equal( CursesStyle.Default, plan.StyleAfter );
		Assert.Equal( 0, plan.CursorAfterRow );
		Assert.Equal( 0, plan.CursorAfterColumn );
		Assert.Equal( string.Empty, output.Text );

		await CommitAsync( session, plan.Sequence );

		Assert.Equal( "E", output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task EraseScreenUsesAffectedTailLineCount() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "ed-win" )
				.SetString( StringCapability.ClearToEndOfLine, "LLLL" )
				.SetString( StringCapability.ClearToEndOfScreen, "D" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = new( 6, 3 );
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen( desired, "x" );

		CursesErasePlan plan = Assert.IsType<CursesErasePlan>(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				1,
				2,
				CursesStyle.Default,
				1,
				2
			)
		);

		Assert.Equal( CursesEraseKind.ClearToEndOfScreen, plan.Kind );
		TerminalScreenOperationPlan operation = Assert.Single( plan.Sequence.Plans );
		Assert.Equal( TerminalScreenOperationKind.Erase, operation.Kind );
		Assert.Equal( 2, operation.AffectedLines );
		await CommitAsync( session, plan.Sequence );
		Assert.Equal( "D", output.Text );
	}

	[Fact]
	public async Task ClearScreenWinsAndLeavesCursorUnknown() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "clear-win" )
				.SetString( StringCapability.ClearToEndOfLine, "LLLL" )
				.SetString( StringCapability.ClearToEndOfScreen, "DD" )
				.SetString( StringCapability.ClearScreen, "C" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = new( 8, 4 );
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen( desired, "x" );

		CursesErasePlan plan = Assert.IsType<CursesErasePlan>(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				2,
				3,
				CursesStyle.Default,
				2,
				3
			)
		);

		Assert.Equal( CursesEraseKind.ClearScreen, plan.Kind );
		Assert.Null( plan.CursorAfterRow );
		Assert.Null( plan.CursorAfterColumn );
		TerminalScreenOperationPlan operation = Assert.Single( plan.Sequence.Plans );
		Assert.Equal( 4, operation.AffectedLines );
		await CommitAsync( session, plan.Sequence );
		Assert.Equal( "C", output.Text );
	}

	[Fact]
	public async Task NondefaultStyledBlankBlocksBroaderErase() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "style-block" )
				.SetString( StringCapability.ClearToEndOfLine, "E" )
				.SetString( StringCapability.ClearToEndOfScreen, "D" )
				.SetString( StringCapability.ClearScreen, "C" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = new( 6, 2 );
		desired[ 1, 0 ] = CursesCell.Blank(
			new CursesStyle(
				CursesColor.Default,
				CursesColor.Default,
				CursesTextAttributes.Bold
			)
		);
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen( desired, "x" );

		CursesErasePlan plan = Assert.IsType<CursesErasePlan>(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				0,
				CursesStyle.Default,
				0,
				0
			)
		);

		Assert.Equal( CursesEraseKind.ClearToEndOfLine, plan.Kind );
	}

	[Fact]
	public async Task ApplicationEncodingChangesLiteralBlankCost() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "encoding" )
			.SetString( StringCapability.ClearToEndOfLine, "EEE" )
			.Build();
		CursesVirtualScreen desired = new( 2, 1 );
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen( desired, "x" );
		RecordingTerminalOutput utf8Output = new();
		await using TerminalSession utf8Session = await OpenSessionAsync(
			terminal,
			utf8Output
		);
		RecordingTerminalOutput utf16Output = new();
		await using TerminalSession utf16Session = await OpenSessionAsync(
			terminal,
			utf16Output
		);

		Assert.Null(
			CreateResolver( utf8Session, Encoding.UTF8 ).Resolve(
				desired,
				physical,
				0,
				0,
				CursesStyle.Default,
				0,
				0
			)
		);
		CursesErasePlan? utf16Plan = CreateResolver(
			utf16Session,
			Encoding.Unicode
		).Resolve(
			desired,
			physical,
			0,
			0,
			CursesStyle.Default,
			0,
			0
		);
		Assert.Equal(
			CursesEraseKind.ClearToEndOfLine,
			Assert.IsType<CursesErasePlan>( utf16Plan ).Kind
		);
	}

	[Fact]
	public async Task MissingOrEmptyEraseOperationFallsBackWithoutOutput() {
		CursesVirtualScreen desired = new( 8, 1 );
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen( desired, "x" );

		foreach ( TerminalDescription terminal in new[] {
			new TerminalDescriptionBuilder( "missing-erase" ).Build(),
			new TerminalDescriptionBuilder( "empty-erase" )
				.SetString( StringCapability.ClearToEndOfLine, string.Empty )
				.Build()
		} ) {
			RecordingTerminalOutput output = new();
			await using TerminalSession session = await OpenSessionAsync( terminal, output );

			Assert.Null(
				CreateResolver( session ).Resolve(
					desired,
					physical,
					0,
					0,
					CursesStyle.Default,
					0,
					0
				)
			);
			Assert.Equal( string.Empty, output.Text );
		}
	}

	[Fact]
	public async Task RetainedStateInsideEraseRegionRejectsCandidate() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "inside-retained" )
				.SetString( StringCapability.ClearToEndOfLine, "E" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = new( 8, 1 );
		desired.SetMetadata(
			0,
			2,
			new CursesCellMetadata(
				new CursesHyperlink( "https://example.invalid/inside" )
			)
		);
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen( desired, "x" );

		Assert.Null(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				1,
				CursesStyle.Default,
				0,
				1
			)
		);

		desired.SetMetadata( 0, 2, null );
		physical.SetCell(
			0,
			2,
			new CursesCell( "x" ),
			metadata: null,
			rasterCell: CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell()
		);
		Assert.Null(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				1,
				CursesStyle.Default,
				0,
				1
			)
		);
	}

	[Fact]
	public async Task RetainedStateOutsideEraseRegionDoesNotRejectCandidate() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "outside-retained" )
				.SetString( StringCapability.ClearToEndOfLine, "E" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = new( 8, 2 );
		desired.SetMetadata(
			1,
			0,
			new CursesCellMetadata(
				new CursesHyperlink( "https://example.invalid/outside" )
			)
		);
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen( desired, "x" );
		physical.SetCell(
			0,
			2,
			new CursesCell( "x" ),
			metadata: null,
			rasterCell: CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell()
		);

		CursesErasePlan? plan = CreateResolver( session ).Resolve(
			desired,
			physical,
			1,
			1,
			CursesStyle.Default,
			1,
			1
		);

		Assert.Equal(
			CursesEraseKind.ClearToEndOfLine,
			Assert.IsType<CursesErasePlan>( plan ).Kind
		);
	}

	[Fact]
	public async Task UnknownPhysicalCellInsideEraseRegionRejectsCandidate() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "unknown-physical" )
				.SetString( StringCapability.ClearToEndOfLine, "E" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = new( 8, 1 );
		CursesPhysicalScreenState physical = new( desired.Columns, desired.Rows );
		for ( int column = 0; column < desired.Columns; column++ ) {
			if ( 2 != column ) {
				physical.SetCell( 0, column, new CursesCell( "x" ) );
			}
		}

		Assert.Null(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				1,
				CursesStyle.Default,
				0,
				1
			)
		);
	}

	[Fact]
	public async Task NondefaultRenditionIsResetBeforeErase() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "erase-rendition" )
				.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
				.SetString( StringCapability.EnterBoldMode, "<bold>" )
				.SetString( StringCapability.ClearToEndOfLine, "E" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = new( 20, 1 );
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen( desired, "x" );
		CursesStyle current = new(
			CursesColor.Default,
			CursesColor.Default,
			CursesTextAttributes.Bold
		);

		CursesErasePlan plan = Assert.IsType<CursesErasePlan>(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				0,
				current,
				0,
				0
			)
		);

		Assert.Equal( 2, plan.Sequence.Plans.Count );
		Assert.Equal( TerminalScreenOperationKind.Rendition, plan.Sequence.Plans[ 0 ].Kind );
		Assert.Equal( TerminalScreenOperationKind.Erase, plan.Sequence.Plans[ 1 ].Kind );
		await CommitAsync( session, plan.Sequence );
		Assert.Equal( "<sgr0>E", output.Text );
	}

	[Fact]
	public async Task CursorMovesBeforeEraseWhenOperationPositionIsNotCurrent() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "erase-cursor" )
				.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
				.SetString( StringCapability.ClearToEndOfLine, "E" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = new( 20, 1 );
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen( desired, "x" );

		CursesErasePlan plan = Assert.IsType<CursesErasePlan>(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				2,
				CursesStyle.Default,
				currentCursorRow: null,
				currentCursorColumn: null
			)
		);

		Assert.Equal( 2, plan.Sequence.Plans.Count );
		Assert.Equal( TerminalScreenOperationKind.CursorMove, plan.Sequence.Plans[ 0 ].Kind );
		Assert.Equal( TerminalScreenOperationKind.Erase, plan.Sequence.Plans[ 1 ].Kind );
		await CommitAsync( session, plan.Sequence );
		Assert.Equal( "<cup:0,2>E", output.Text );
	}

	private static CursesEraseResolver CreateResolver(
		TerminalSession session,
		Encoding? encoding = null
	) {
		ArgumentNullException.ThrowIfNull( session );
		return new CursesEraseResolver(
			session.Screen,
			new CursesOutputCostModel( encoding ?? Encoding.UTF8 )
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

	private static CursesPhysicalScreenState CreateFilledPhysicalScreen(
		CursesVirtualScreen desired,
		string content
	) {
		ArgumentNullException.ThrowIfNull( desired );
		ArgumentException.ThrowIfNullOrEmpty( content );
		CursesPhysicalScreenState physical = new(
			desired.Columns,
			desired.Rows
		);
		for ( int row = 0; row < desired.Rows; row++ ) {
			for ( int column = 0; column < desired.Columns; column++ ) {
				physical.SetCell(
					row,
					column,
					new CursesCell( content )
				);
			}
		}
		return physical;
	}
}
