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
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Qualifies editor-shaped rich-text layout inspection and allocation costs.</summary>
[Collection( AllocationMeasurementCollection.Name )]
public sealed class CursesTextLayoutPerformanceTests {
	private const int AllocationSamples = 8;
	private const long EditorSliceAllocationCeiling = 8L * 1024L * 1024L;
	private const int VisibleColumns = 80;
	private const int VisibleRows = 40;
	private const int WarmupIterations = 8;

	[Fact]
	public void EditorSliceInspectsOnlySuppliedVisibleTextWithLinearWidthCalls() {
		EditorDocument document = new( 10_000 );
		string halfSlice = ReadSlice(
			document,
			verticalOrigin: 5_000,
			rows: VisibleRows / 2
		);
		Assert.Equal( VisibleRows / 2, document.ReadCount );
		document.ResetReadCount();
		string fullSlice = ReadSlice(
			document,
			verticalOrigin: 5_000,
			rows: VisibleRows
		);
		Assert.Equal( VisibleRows, document.ReadCount );

		CountingWidthProvider provider = new();
		CursesTextLayout half = CreateLayout(
			halfSlice,
			VisibleRows / 2,
			provider
		);
		int halfCalls = provider.CallCount;
		provider.Reset();
		CursesTextLayout full = CreateLayout(
			fullSlice,
			VisibleRows,
			provider
		);

		Assert.Equal( VisibleRows / 2, half.Lines.Count );
		Assert.Equal( VisibleRows, full.Lines.Count );
		Assert.Equal( ( VisibleColumns - 1 ) * ( VisibleRows / 2 ), halfCalls );
		Assert.Equal( halfCalls * 2, provider.CallCount );
		Assert.Equal( ( VisibleColumns - 1 ) * VisibleRows, full.CellCount );

		Action operation = () => {
			provider.Reset();
			CursesTextLayout layout = CreateLayout(
				fullSlice,
				VisibleRows,
				provider
			);
			if ( VisibleRows != layout.Lines.Count
				|| ( VisibleColumns - 1 ) * VisibleRows != layout.CellCount
				|| ( VisibleColumns - 1 ) * VisibleRows != provider.CallCount ) {
				throw new InvalidOperationException(
					"The representative editor slice changed during allocation measurement."
				);
			}
			GC.KeepAlive( layout );
		};

		long allocated = MeasureMinimumAllocatedBytes( operation );
		Assert.InRange(
			allocated,
			0,
			EditorSliceAllocationCeiling
		);
	}

	private static CursesTextLayout CreateLayout(
		string text,
		int rows,
		ICursesTextWidthProvider provider
	) {
		return CursesTextLayout.Create(
			text,
			new CursesTextLayoutOptions( VisibleColumns ) {
				MaximumRows = rows,
				WidthProvider = provider
			}
		);
	}

	private static string ReadSlice(
		IReadOnlyList<string> document,
		int verticalOrigin,
		int rows
	) {
		StringBuilder builder = new( rows * VisibleColumns );
		for ( int row = 0; row < rows; row++ ) {
			if ( 0 != row ) {
				_ = builder.Append( '\n' );
			}
			_ = builder.Append( document[ verticalOrigin + row ] );
		}
		return builder.ToString();
	}

	private static long MeasureMinimumAllocatedBytes( Action operation ) {
		for ( int index = 0; index < WarmupIterations; index++ ) {
			operation();
		}

		long minimum = long.MaxValue;
		for ( int sample = 0; sample < AllocationSamples; sample++ ) {
			int thread = Environment.CurrentManagedThreadId;
			long before = GC.GetAllocatedBytesForCurrentThread();
			operation();
			long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
			Assert.Equal( thread, Environment.CurrentManagedThreadId );
			minimum = Math.Min( minimum, allocated );
		}
		return minimum;
	}

	private sealed class CountingWidthProvider : ICursesTextWidthProvider {
		public int CallCount { get; private set; }

		public int GetWidth( string textElement ) {
			CallCount++;
			return UnicodeCursesTextWidthProvider.Instance.GetWidth( textElement );
		}

		public void Reset() {
			CallCount = 0;
		}
	}

	private sealed class EditorDocument : IReadOnlyList<string> {
		private readonly string[] lines;

		public EditorDocument( int count ) {
			lines = new string[ count ];
			for ( int index = 0; index < lines.Length; index++ ) {
				lines[ index ] = $"line {index:D5} ".PadRight(
					VisibleColumns - 1,
					(char)( 'a' + ( index % 26 ) )
				);
			}
		}

		public int Count => lines.Length;
		public int ReadCount { get; private set; }

		public string this[ int index ] {
			get {
				ReadCount++;
				return lines[ index ];
			}
		}

		public IEnumerator<string> GetEnumerator() {
			return ( (IEnumerable<string>)lines ).GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public void ResetReadCount() {
			ReadCount = 0;
		}
	}
}
