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

/// <summary>Verifies exact and conservative Terminal-planned character shifts.</summary>
public sealed class CursesCharacterShiftResolverTests {
	[Fact]
	public async Task ParameterizedInsertWinsForExactDefaultBlankShift() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "insert" )
				.SetString( StringCapability.InsertCharacters, "I%p1%d" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = CreateLogicalRow( "A  BCDEF" );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCDEFGH" );

		CursesCharacterShiftPlan plan = Assert.IsType<CursesCharacterShiftPlan>(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				CursesStyle.Default,
				0,
				1
			)
		);

		Assert.Equal( CursesCharacterShiftKind.Insert, plan.Kind );
		Assert.Equal( 0, plan.Row );
		Assert.Equal( 1, plan.Column );
		Assert.Equal( 2, plan.Count );
		Assert.Equal( 2, plan.Sequence.ByteCount );
		TerminalScreenOperationPlan operation = Assert.Single( plan.Sequence.Plans );
		Assert.Equal( TerminalScreenOperationKind.CharacterShift, operation.Kind );
		Assert.Equal( CursesStyle.Default, plan.StyleAfter );
		Assert.Equal( 0, plan.CursorAfterRow );
		Assert.Equal( 1, plan.CursorAfterColumn );
		Assert.Equal( string.Empty, output.Text );

		await CommitAsync( session, plan.Sequence );

		Assert.Equal( "I2", output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task ParameterizedDeleteWinsForExactDefaultBlankShift() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "delete" )
				.SetString( StringCapability.DeleteCharacters, "D%p1%d" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = CreateLogicalRow( "ADEFGH  " );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCDEFGH" );

		CursesCharacterShiftPlan plan = Assert.IsType<CursesCharacterShiftPlan>(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				CursesStyle.Default,
				0,
				1
			)
		);

		Assert.Equal( CursesCharacterShiftKind.Delete, plan.Kind );
		Assert.Equal( 1, plan.Column );
		Assert.Equal( 2, plan.Count );
		Assert.Equal( 2, plan.Sequence.ByteCount );
		await CommitAsync( session, plan.Sequence );
		Assert.Equal( "D2", output.Text );
	}

	[Fact]
	public async Task RepeatedSingleInsertCanBeatParameterizedInsert() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "insert-one" )
				.SetString( StringCapability.InsertCharacters, "<insert:%p1%d>" )
				.SetString( StringCapability.InsertCharacter, "i" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = CreateLogicalRow( "A  BCDEF" );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCDEFGH" );

		CursesCharacterShiftPlan plan = Assert.IsType<CursesCharacterShiftPlan>(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				CursesStyle.Default,
				0,
				1
			)
		);

		Assert.Equal( 2, plan.Sequence.ByteCount );
		await CommitAsync( session, plan.Sequence );
		Assert.Equal( "ii", output.Text );
	}

	[Fact]
	public async Task EqualCompleteCostRetainsRewriteFallback() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "tie" )
				.SetString( StringCapability.InsertCharacter, "XYZ" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = CreateLogicalRow( " ABC" );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCD" );

		Assert.Null(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				CursesStyle.Default,
				0,
				0
			)
		);
		Assert.Equal( string.Empty, output.Text );
	}

	[Fact]
	public async Task CursorCostParticipatesInStrictGate() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "cursor-cost" )
				.SetString( StringCapability.CursorRightOne, "r" )
				.SetString( StringCapability.InsertCharacters, "I%p1%d" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = CreateLogicalRow( "A BC" );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCD" );

		Assert.Null(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				CursesStyle.Default,
				0,
				0
			)
		);
		Assert.Equal( string.Empty, output.Text );
	}

	[Fact]
	public async Task ApplicationEncodingParticipatesInStrictCostGate() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "encoding" )
			.SetString( StringCapability.InsertCharacter, "XYZ" )
			.Build();
		CursesVirtualScreen desired = CreateLogicalRow( " ABC" );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCD" );
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
				CursesStyle.Default,
				0,
				0
			)
		);
		Assert.NotNull(
			CreateResolver( utf16Session, Encoding.Unicode ).Resolve(
				desired,
				physical,
				0,
				CursesStyle.Default,
				0,
				0
			)
		);
		Assert.Equal( string.Empty, utf8Output.Text );
		Assert.Equal( string.Empty, utf16Output.Text );
	}

	[Fact]
	public async Task UnknownPhysicalCellBlocksShiftOptimization() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "unknown" )
				.SetString( StringCapability.InsertCharacters, "I%p1%d" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = CreateLogicalRow( "A  BCDEF" );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCDEFGH" );
		physical.Invalidate();

		Assert.Null(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				CursesStyle.Default,
				0,
				1
			)
		);
	}

	[Fact]
	public async Task NondefaultInsertedBlankBlocksInsertOptimization() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "insert-style" )
				.SetString( StringCapability.InsertCharacter, "i" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = CreateLogicalRow( "A BCDE" );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCDEF" );
		desired[ 0, 1 ] = CursesCell.Blank(
			CursesStyle.Default.WithAttributes( CursesTextAttributes.Bold )
		);

		Assert.Null(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				CursesStyle.Default,
				0,
				1
			)
		);
	}

	[Theory]
	[InlineData( "continuation" )]
	[InlineData( "width-two" )]
	[InlineData( "line-glyph" )]
	public async Task UnsafeCellInCompleteRowTailBlocksOptimization( string cellKind ) {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "unsafe-cell" )
				.SetString( StringCapability.InsertCharacters, "I%p1%d" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = CreateLogicalRow( "A  BCDEF" );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCDEFGH" );
		CursesCell unsafeCell = cellKind switch {
			"continuation" => CursesCell.Continuation(),
			"width-two" => new CursesCell( "界", CursesStyle.Default, displayWidth: 2 ),
			"line-glyph" => CursesCell.Line( CursesLineGlyph.Horizontal ),
			_ => throw new ArgumentOutOfRangeException( nameof( cellKind ) )
		};
		physical.SetCell( 0, 1, unsafeCell );
		desired[ 0, 3 ] = unsafeCell;

		Assert.Null(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				CursesStyle.Default,
				0,
				1
			)
		);
	}

	[Theory]
	[InlineData( "desired-metadata" )]
	[InlineData( "physical-metadata" )]
	[InlineData( "desired-raster" )]
	[InlineData( "physical-raster" )]
	public async Task RetainedStateInsideRowTailBlocksOptimization( string retainedKind ) {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "retained-inside" )
				.SetString( StringCapability.InsertCharacters, "I%p1%d" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = CreateLogicalRow( "A  BCDEF" );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCDEFGH" );
		CursesCellMetadata metadata = new(
			new CursesHyperlink( "https://example.test/shift" )
		);
		CursesRasterCell raster =
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		const int column = 4;
		switch ( retainedKind ) {
			case "desired-metadata":
				desired.SetMetadata( 0, column, metadata );
				break;
			case "physical-metadata":
				Assert.True( physical.TryGetCell( 0, column, out CursesCell metadataCell ) );
				physical.SetCell( 0, column, metadataCell, metadata );
				break;
			case "desired-raster":
				desired.SetRasterCell( 0, column, raster );
				break;
			case "physical-raster":
				Assert.True( physical.TryGetCell( 0, column, out CursesCell rasterCell ) );
				physical.SetCell(
					0,
					column,
					rasterCell,
					metadata: null,
					rasterCell: raster
				);
				break;
			default:
				throw new ArgumentOutOfRangeException( nameof( retainedKind ) );
		}

		Assert.Null(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				CursesStyle.Default,
				0,
				1
			)
		);
	}

	[Fact]
	public async Task RetainedStateInAnotherRowDoesNotBlockOptimization() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "retained-outside" )
				.SetString( StringCapability.InsertCharacters, "I%p1%d" )
				.Build(),
			output
		);
		CursesVirtualScreen desired = new( 8, 2 );
		CursesPhysicalScreenState physical = new( 8, 2 );
		SetLogicalRow( desired, 0, "A  BCDEF" );
		SetLogicalRow( desired, 1, "12345678" );
		SetPhysicalRow( physical, 0, "ABCDEFGH" );
		SetPhysicalRow( physical, 1, "12345678" );
		desired.SetMetadata(
			1,
			2,
			new CursesCellMetadata( new CursesHyperlink( "https://example.test/outside" ) )
		);
		Assert.True( physical.TryGetCell( 1, 3, out CursesCell outsideCell ) );
		physical.SetCell(
			1,
			3,
			outsideCell,
			metadata: null,
			rasterCell: CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell()
		);

		Assert.NotNull(
			CreateResolver( session ).Resolve(
				desired,
				physical,
				0,
				CursesStyle.Default,
				0,
				1
			)
		);
	}

	[Fact]
	public async Task MissingCursorPlanReturnsNullWithoutOutput() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "missing-cursor" )
				.SetString( StringCapability.InsertCharacters, "I%p1%d" )
				.Build(),
			output
		);

		Assert.Null(
			CreateResolver( session ).Resolve(
				CreateLogicalRow( "A  BCDEF" ),
				CreatePhysicalRow( "ABCDEFGH" ),
				0,
				CursesStyle.Default,
				currentCursorRow: null,
				currentCursorColumn: null
			)
		);
		Assert.Equal( string.Empty, output.Text );
	}

	[Fact]
	public async Task MissingDefaultRenditionPlanReturnsNullWithoutOutput() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "missing-baseline" )
				.SetString( StringCapability.EnterBoldMode, "<bold>" )
				.SetString( StringCapability.InsertCharacters, "I%p1%d" )
				.Build(),
			output
		);

		Assert.Null(
			CreateResolver( session ).Resolve(
				CreateLogicalRow( "A  BCDEF" ),
				CreatePhysicalRow( "ABCDEFGH" ),
				0,
				currentStyle: null,
				currentCursorRow: 0,
				currentCursorColumn: 1
			)
		);
		Assert.Equal( string.Empty, output.Text );
	}

	[Fact]
	public async Task MissingCharacterShiftPlanReturnsNullWithoutOutput() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "missing-shift" ).Build(),
			output
		);

		Assert.Null(
			CreateResolver( session ).Resolve(
				CreateLogicalRow( "A  BCDEF" ),
				CreatePhysicalRow( "ABCDEFGH" ),
				0,
				CursesStyle.Default,
				0,
				1
			)
		);
		Assert.Equal( string.Empty, output.Text );
	}

	[Fact]
	public async Task NondefaultRenditionIsResetBeforeShift() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "reset-before-shift" )
				.SetString( StringCapability.EnterBoldMode, "<bold>" )
				.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
				.SetString( StringCapability.InsertCharacters, "I%p1%d" )
				.Build(),
			output
		);
		CursesStyle bold = CursesStyle.Default.WithAttributes(
			CursesTextAttributes.Bold
		);

		CursesCharacterShiftPlan plan = Assert.IsType<CursesCharacterShiftPlan>(
			CreateResolver( session ).Resolve(
				CreateLogicalRow( "A  BCDEFGHIJKLMN" ),
				CreatePhysicalRow( "ABCDEFGHIJKLMNOP" ),
				0,
				bold,
				0,
				1
			)
		);

		Assert.Equal( 2, plan.Sequence.Plans.Count );
		Assert.Equal( TerminalScreenOperationKind.Rendition, plan.Sequence.Plans[ 0 ].Kind );
		Assert.Equal( TerminalScreenOperationKind.CharacterShift, plan.Sequence.Plans[ 1 ].Kind );
		await CommitAsync( session, plan.Sequence );
		Assert.Equal( "<sgr0>I2", output.Text );
	}

	[Fact]
	public async Task ZeroByteCharacterShiftPlanIsRejectedWithoutOutput() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			new TerminalDescriptionBuilder( "empty-shift" )
				.SetString( StringCapability.InsertCharacters, string.Empty )
				.Build(),
			output
		);

		Assert.Null(
			CreateResolver( session ).Resolve(
				CreateLogicalRow( "A  BCDEF" ),
				CreatePhysicalRow( "ABCDEFGH" ),
				0,
				CursesStyle.Default,
				0,
				1
			)
		);
		Assert.Equal( string.Empty, output.Text );
	}

	private static CursesCharacterShiftResolver CreateResolver(
		TerminalSession session,
		Encoding? encoding = null
	) {
		ArgumentNullException.ThrowIfNull( session );
		return new CursesCharacterShiftResolver(
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

	private static CursesVirtualScreen CreateLogicalRow( string value ) {
		ArgumentNullException.ThrowIfNull( value );
		CursesVirtualScreen result = new( value.Length, 1 );
		SetLogicalRow( result, 0, value );
		return result;
	}

	private static CursesPhysicalScreenState CreatePhysicalRow( string value ) {
		ArgumentNullException.ThrowIfNull( value );
		CursesPhysicalScreenState result = new( value.Length, 1 );
		SetPhysicalRow( result, 0, value );
		return result;
	}

	private static void SetLogicalRow(
		CursesVirtualScreen screen,
		int row,
		string value
	) {
		ArgumentNullException.ThrowIfNull( screen );
		ArgumentNullException.ThrowIfNull( value );
		Assert.Equal( screen.Columns, value.Length );
		for ( int column = 0; column < value.Length; column++ ) {
			screen[ row, column ] = CreateCell( value[ column ] );
		}
	}

	private static void SetPhysicalRow(
		CursesPhysicalScreenState screen,
		int row,
		string value
	) {
		ArgumentNullException.ThrowIfNull( screen );
		ArgumentNullException.ThrowIfNull( value );
		Assert.Equal( screen.Columns, value.Length );
		for ( int column = 0; column < value.Length; column++ ) {
			screen.SetCell( row, column, CreateCell( value[ column ] ) );
		}
	}

	private static CursesCell CreateCell( char value ) {
		return ' ' == value
			? CursesCell.Blank()
			: new CursesCell( value.ToString() );
	}
}
