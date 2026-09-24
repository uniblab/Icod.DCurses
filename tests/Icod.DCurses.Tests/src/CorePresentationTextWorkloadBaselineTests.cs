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

using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Characterizes 2.0 editor and roguelike composition costs before 2.1 API work.</summary>
public sealed class CorePresentationTextWorkloadBaselineTests {
	private const int AllocationSamples = 8;
	private const long EditorVisibleSliceAllocationCeiling = 93L * 1024L * 1024L;
	private const long FullWorldFrameAllocationCeiling = 12L * 1024L * 1024L;
	private const int MeasurementIterations = 256;
	private const long MeasurementNoiseAllowance = 64L * 1024L;
	private const long PadViewportAllocationCeiling = 54L * 1024L * 1024L;
	private const long SparseWorldUpdateAllocationCeiling = 0;
	private const long TextHelperBatchAllocationCeiling = 76L * 1024L * 1024L;
	private const int WarmupIterations = 32;

	[Fact]
	public void EditorVisibleSliceInspectsOnlyVisibleLinesAndRecordsScalarCost() {
		string[] document = CreateEditorDocument();
		CursesScreen screen = new( 80, 40 );
		screen.StandardWindow.WrapMode = CursesWrapMode.Clip;
		screen.VirtualScreen.MarkClean();

		int inspectedLines = RenderEditorFrame(
			document,
			screen.StandardWindow,
			verticalOrigin: 5_000,
			horizontalOrigin: 0
		);

		Assert.Equal( 40, inspectedLines );
		Assert.Equal( 969, screen.VirtualScreen.DirtyCellCount );
		screen.VirtualScreen.MarkClean();
		Assert.Equal(
			40,
			RenderEditorFrame(
				document,
				screen.StandardWindow,
				verticalOrigin: 5_000,
				horizontalOrigin: 0
			)
		);
		Assert.Equal( 969, screen.VirtualScreen.DirtyCellCount );
		Assert.Equal(
			"A",
			CursesText.SliceByColumns(
				"\u754CA",
				1,
				80
			)
		);

		long allocated = MeasureMinimumAllocatedBytes(
			() => {
				screen.VirtualScreen.MarkClean();
				if ( 40 != RenderEditorFrame(
					document,
					screen.StandardWindow,
					verticalOrigin: 5_000,
					horizontalOrigin: 0
				) ) {
					throw new InvalidOperationException(
						"The editor workload did not inspect exactly the visible lines."
					);
				}
			}
		);

		Assert.True(
			allocated <= EditorVisibleSliceAllocationCeiling,
			$"Editor visible-slice minimum allocation was {allocated} bytes; "
				+ $"ceiling {EditorVisibleSliceAllocationCeiling}."
		);
	}

	[Fact]
	public void AlgorithmicRoguelikeKeepsSparseUpdateBoundedAndRecordsScalarCost() {
		CursesScreen screen = new( 80, 24 );
		screen.StandardWindow.WrapMode = CursesWrapMode.Clip;
		screen.VirtualScreen.MarkClean();

		int fullFrameWrites = RenderWorldFrame(
			screen.StandardWindow,
			worldRow: 1_024,
			worldColumn: 1_024
		);
		Assert.Equal( 80 * 24, fullFrameWrites );
		Assert.Equal( 80 * 24, screen.VirtualScreen.DirtyCellCount );
		screen.VirtualScreen.MarkClean();

		int sparseWrites = WriteWorldNeighborhood(
			screen.StandardWindow,
			new CursesCell( "*" )
		);
		Assert.Equal( 9, sparseWrites );
		Assert.InRange( screen.VirtualScreen.DirtyCellCount, 1, 9 );

		long fullFrameAllocated = MeasureMinimumAllocatedBytes(
			() => {
				screen.VirtualScreen.MarkClean();
				if ( 80 * 24 != RenderWorldFrame(
					screen.StandardWindow,
					worldRow: 1_024,
					worldColumn: 1_024
				) ) {
					throw new InvalidOperationException(
						"The roguelike workload did not issue one scalar write per visible cell."
					);
				}
			}
		);
		bool useAlternateCell = false;
		long sparseAllocated = MeasureMinimumAllocatedBytes(
			() => {
				useAlternateCell = !useAlternateCell;
				screen.VirtualScreen.MarkClean();
				CursesCell cell = new( useAlternateCell ? "*" : "+" );
				if ( 9 != WriteWorldNeighborhood(
					screen.StandardWindow,
					cell
				) ) {
					throw new InvalidOperationException(
						"The roguelike workload did not issue nine scalar writes."
					);
				}
			}
		);

		Assert.True(
			fullFrameAllocated <= FullWorldFrameAllocationCeiling
				&& sparseAllocated <= SparseWorldUpdateAllocationCeiling,
			$"Roguelike allocation: full={fullFrameAllocated} bytes "
				+ $"(ceiling {FullWorldFrameAllocationCeiling}), sparse={sparseAllocated} bytes "
				+ $"(ceiling {SparseWorldUpdateAllocationCeiling})."
		);
	}

	[Fact]
	public void PadViewportRecordsFullContentAlternativeCost() {
		CursesPad pad = new( 2_048, 256 );
		for ( int row = 0; row < pad.Rows; row += 17 ) {
			pad.ContentWindow.FillRectangle(
				row,
				0,
				1,
				pad.Columns,
				new CursesCell( ( (char)( 'A' + ( row % 26 ) ) ).ToString() )
			);
		}
		CursesScreen screen = new( 80, 24 );
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow,
			0,
			0,
			24,
			80,
			0,
			0
		);
		viewport.Present();

		Assert.Equal( 2_048, pad.Columns );
		Assert.Equal( 256, pad.Rows );
		Assert.Equal( 80, viewport.Columns );
		Assert.Equal( 24, viewport.Rows );

		int sourceRow = 0;
		long allocated = MeasureMinimumAllocatedBytes(
			() => {
				sourceRow = 1 - sourceRow;
				viewport.SetSource(
					sourceRow,
					0
				);
				screen.VirtualScreen.MarkClean();
				viewport.Present();
				if ( 80 * 24 != screen.VirtualScreen.DirtyCellCount ) {
					throw new InvalidOperationException(
						"A moved pad viewport did not damage its complete destination rectangle."
					);
				}
			}
		);

		Assert.True(
			allocated <= PadViewportAllocationCeiling,
			$"Pad viewport minimum allocation was {allocated} bytes; "
				+ $"ceiling {PadViewportAllocationCeiling}."
		);
	}

	[Fact]
	public void TextAndLayoutHelpersHaveDeterministicResultsAndRecordedCosts() {
		const string text = "ASCII e\u0301 \u754C \u2764\uFE0F \u03A9";
		Assert.Equal( 15, CursesText.MeasureColumns( text ) );
		Assert.Equal(
			"ASCII e\u0301 ",
			CursesText.TruncateToColumns(
				text,
				8
			)
		);
		Assert.Equal(
			"e\u0301 \u754C",
			CursesText.SliceByColumns(
				text,
				6,
				4
			)
		);

		long textAllocated = MeasureMinimumAllocatedBytes(
			() => RunTextHelperBatch( text ),
			measurementIterations: 1
		);
		int geometryChecksum = 0;
		long geometryAllocated = MeasureMinimumAllocatedBytes(
			() => geometryChecksum = RunGeometryBatch(),
			measurementIterations: 1
		);

		Assert.Equal( 20_800_000, geometryChecksum );
		Assert.True(
			textAllocated <= TextHelperBatchAllocationCeiling,
			$"Text-helper batch minimum allocation was {textAllocated} bytes; "
				+ $"ceiling {TextHelperBatchAllocationCeiling}. "
				+ $"Geometry batch allocation was {geometryAllocated} bytes."
		);
		Assert.InRange(
			geometryAllocated,
			0,
			MeasurementNoiseAllowance
		);
	}

	private static string[] CreateEditorDocument() {
		string[] corpus = [
			"plain ASCII document line",
			"columns    expanded tab stop",
			"combining e\u0301 document text",
			"wide \u754C document text",
			"emoji \u2764\uFE0F document text",
			"ambiguous \u03A9 document text"
		];
		string[] document = new string[ 10_000 ];
		for ( int index = 0; index < document.Length; index++ ) {
			document[ index ] = corpus[ index % corpus.Length ];
		}
		return document;
	}

	private static int RenderEditorFrame(
		IReadOnlyList<string> document,
		CursesWindow window,
		int verticalOrigin,
		int horizontalOrigin
	) {
		ArgumentNullException.ThrowIfNull( document );
		ArgumentNullException.ThrowIfNull( window );

		int inspectedLines = 0;
		for ( int row = 0; row < window.Rows; row++ ) {
			string visible = CursesText.SliceByColumns(
				document[ verticalOrigin + row ],
				horizontalOrigin,
				window.Columns
			);
			window.Move( row, 0 );
			window.Write( visible );
			inspectedLines++;
		}
		return inspectedLines;
	}

	private static int RenderWorldFrame(
		CursesWindow window,
		int worldRow,
		int worldColumn
	) {
		ArgumentNullException.ThrowIfNull( window );

		int writes = 0;
		for ( int row = 0; row < window.Rows; row++ ) {
			for ( int column = 0; column < window.Columns; column++ ) {
				int value = ( ( worldRow + row ) * 31 )
					+ ( ( worldColumn + column ) * 17 );
				char glyph = 0 == value % 23
					? '#'
					: '.'
				;
				window.Move( row, column );
				window.WriteCell( new CursesCell( glyph.ToString() ) );
				writes++;
			}
		}
		return writes;
	}

	private static int WriteWorldNeighborhood(
		CursesWindow window,
		CursesCell cell
	) {
		ArgumentNullException.ThrowIfNull( window );

		int writes = 0;
		for ( int row = 11; row <= 13; row++ ) {
			for ( int column = 39; column <= 41; column++ ) {
				window.Move( row, column );
				window.WriteCell( cell );
				writes++;
			}
		}
		return writes;
	}

	private static void RunTextHelperBatch( string text ) {
		ArgumentNullException.ThrowIfNull( text );

		for ( int index = 0; index < 10_000; index++ ) {
			if ( 15 != CursesText.MeasureColumns( text )
				|| !string.Equals(
					"ASCII e\u0301 ",
					CursesText.TruncateToColumns( text, 8 ),
					StringComparison.Ordinal
				)
				|| !string.Equals(
					"e\u0301 \u754C",
					CursesText.SliceByColumns( text, 6, 4 ),
					StringComparison.Ordinal
				) ) {
				throw new InvalidOperationException(
					"The text-helper workload changed its deterministic result."
				);
			}
		}
	}

	private static int RunGeometryBatch() {
		CursesRectangle bounds = new( 0, 0, 48, 160 );
		int checksum = 0;
		for ( int index = 0; index < 100_000; index++ ) {
			CursesLayout.Dock(
				bounds,
				CursesDockEdge.Top,
				2,
				out CursesRectangle header,
				out CursesRectangle remaining
			);
			CursesLayout.SplitColumnsProportional(
				remaining,
				3,
				1,
				out CursesRectangle body,
				out CursesRectangle sidebar
			);
			checksum += header.Rows
				+ remaining.Rows
				+ body.Columns
				+ sidebar.Columns;
		}
		return checksum;
	}

	private static long MeasureMinimumAllocatedBytes(
		Action operation,
		int measurementIterations = MeasurementIterations
	) {
		ArgumentNullException.ThrowIfNull( operation );
		if ( 0 >= measurementIterations ) {
			throw new ArgumentOutOfRangeException( nameof( measurementIterations ) );
		}
		for ( int index = 0; index < WarmupIterations; index++ ) {
			operation();
		}

		long minimum = long.MaxValue;
		for ( int sample = 0; sample < AllocationSamples; sample++ ) {
			int thread = Environment.CurrentManagedThreadId;
			long before = GC.GetAllocatedBytesForCurrentThread();
			for ( int index = 0; index < measurementIterations; index++ ) {
				operation();
			}
			long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
			Assert.Equal( thread, Environment.CurrentManagedThreadId );
			minimum = Math.Min( minimum, allocated );
		}
		return minimum;
	}
}
