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

using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes rich-text layout capacity and failure atomicity.</summary>
public sealed class CursesTextLayoutCapacityTests {
	[Fact]
	public void ProductionLimitsMatchThePublishedContract() {
		Assert.Equal( 16_777_216, CursesTextLayoutBuilder.MaximumSourceLength );
		Assert.Equal( 1_048_576, CursesTextLayoutBuilder.MaximumSpanCount );
		Assert.Equal( 4_194_304, CursesTextLayoutBuilder.ProductionCapacity.MaximumFragments );
		Assert.Equal( 16_777_216, CursesTextLayoutBuilder.ProductionCapacity.MaximumCells );
	}

	[Fact]
	public void SourceBeyondTheMaximumFailsBeforeScanning() {
		string text = new(
			'x',
			CursesTextLayoutBuilder.MaximumSourceLength + 1
		);

		ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesTextLayout.Create(
				text,
				new CursesTextLayoutOptions( 1 )
			)
		);

		Assert.Equal( "text", exception.ParamName );
	}

	[Fact]
	public void SpanCountBeyondTheMaximumFailsBeforeReadingTheList() {
		OversizedSpanList spans = new();

		ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesTextLayout.Create(
				string.Empty,
				new CursesTextLayoutOptions( 1 ),
				spans
			)
		);

		Assert.Equal( "spans", exception.ParamName );
		Assert.Equal( 0, spans.ReadCount );
	}

	[Fact]
	public void MaximumOptionExtentsAreAcceptedTogether() {
		CursesTextLayout layout = CursesTextLayout.Create(
			string.Empty,
			new CursesTextLayoutOptions( CursesTextLayoutOptions.MaximumExtent ) {
				MaximumRows = CursesTextLayoutOptions.MaximumExtent,
				StartingColumn = CursesTextLayoutOptions.MaximumExtent,
				TabInterval = CursesTextLayoutOptions.MaximumExtent
			}
		);

		CursesTextVisualLine line = Assert.Single( layout.Lines );
		Assert.Equal( CursesTextLayoutOptions.MaximumExtent, line.Column );
		Assert.Equal( 0, line.Columns );
	}

	[Fact]
	public void FragmentCapacityFailsBeforePublishingAResult() {
		InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
			static () => CursesTextLayoutBuilder.Build(
				"a",
				new CursesTextLayoutOptions( 1 ),
				null,
				new CursesTextLayoutBuilder.Capacity(
					0,
					1
				)
			)
		);

		Assert.Contains( "fragment capacity", exception.Message, StringComparison.Ordinal );
	}

	[Fact]
	public void CellCapacityFailsBeforePublishingAResult() {
		InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
			static () => CursesTextLayoutBuilder.Build(
				"a",
				new CursesTextLayoutOptions( 1 ),
				null,
				new CursesTextLayoutBuilder.Capacity(
					1,
					0
				)
			)
		);

		Assert.Contains( "cell capacity", exception.Message, StringComparison.Ordinal );
	}

	private sealed class OversizedSpanList : IReadOnlyList<CursesTextSpan> {
		public int Count => CursesTextLayoutBuilder.MaximumSpanCount + 1;
		public int ReadCount { get; private set; }

		public CursesTextSpan this[ int index ] {
			get {
				ReadCount++;
				throw new InvalidOperationException( "An oversized span list must not be read." );
			}
		}

		public IEnumerator<CursesTextSpan> GetEnumerator() {
			throw new InvalidOperationException( "An oversized span list must not be enumerated." );
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}
