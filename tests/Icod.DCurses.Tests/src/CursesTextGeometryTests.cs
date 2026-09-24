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

/// <summary>Specifies source/visual mapping, hit testing, and selection geometry.</summary>
public sealed class CursesTextGeometryTests {
	[Fact]
	public void GeometryValuesValidateAndNormalizeTheirInputs() {
		Assert.Equal(
			"line",
			Assert.Throws<ArgumentOutOfRangeException>(
				static () => new CursesTextVisualPosition( -1, 0 )
			).ParamName
		);
		Assert.Equal(
			"column",
			Assert.Throws<ArgumentOutOfRangeException>(
				static () => new CursesTextVisualPosition( 0, -1 )
			).ParamName
		);
		Assert.Equal(
			"affinity",
			Assert.Throws<ArgumentOutOfRangeException>(
				static () => new CursesTextVisualPosition(
					0,
					0,
					(CursesTextAffinity)int.MaxValue
				)
			).ParamName
		);
		Assert.Equal(
			"affinity",
			Assert.Throws<ArgumentOutOfRangeException>(
				static () => new CursesTextHitTestResult(
					new CursesTextPosition( 0 ),
					(CursesTextAffinity)int.MaxValue,
					false
				)
			).ParamName
		);

		CursesTextSelection selection = new(
			new CursesTextPosition( 5 ),
			new CursesTextPosition( 2 )
		);
		Assert.Equal( 5, selection.Anchor.Offset );
		Assert.Equal( 2, selection.Active.Offset );
		Assert.Equal( 2, selection.Start.Offset );
		Assert.Equal( 5, selection.End.Offset );
		Assert.False( selection.IsEmpty );
		Assert.True(
			new CursesTextSelection(
				new CursesTextPosition( 3 ),
				new CursesTextPosition( 3 )
			).IsEmpty
		);
	}

	[Fact]
	public void SoftWrapBoundaryHonorsAffinity() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"abcd",
			new CursesTextLayoutOptions( 2 ) {
				WrapMode = CursesTextWrapMode.TextElement
			}
		);

		Assert.Equal(
			new CursesTextVisualPosition(
				1,
				0,
				CursesTextAffinity.Leading
			),
			layout.GetVisualPosition( new CursesTextPosition( 2 ) )
		);
		Assert.Equal(
			new CursesTextVisualPosition(
				0,
				2,
				CursesTextAffinity.Trailing
			),
			layout.GetVisualPosition(
				new CursesTextPosition( 2 ),
				CursesTextAffinity.Trailing
			)
		);
	}

	[Fact]
	public void OrdinarySourceEdgesAndHitClampingMapExactly() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"ab",
			new CursesTextLayoutOptions( 5 ) { StartingColumn = 2 }
		);

		Assert.Equal(
			new CursesTextVisualPosition( 0, 2 ),
			layout.GetVisualPosition( new CursesTextPosition( 0 ) )
		);
		Assert.Equal(
			new CursesTextVisualPosition( 0, 3 ),
			layout.GetVisualPosition( new CursesTextPosition( 1 ) )
		);
		Assert.Equal(
			new CursesTextVisualPosition(
				0,
				3,
				CursesTextAffinity.Trailing
			),
			layout.GetVisualPosition(
				new CursesTextPosition( 1 ),
				CursesTextAffinity.Trailing
			)
		);
		Assert.Equal(
			new CursesTextVisualPosition( 0, 4 ),
			layout.GetVisualPosition( new CursesTextPosition( 2 ) )
		);

		Assert.Equal(
			new CursesTextHitTestResult(
				new CursesTextPosition( 0 ),
				CursesTextAffinity.Leading,
				false
			),
			layout.HitTest( 0, 1 )
		);
		Assert.Equal(
			new CursesTextHitTestResult(
				new CursesTextPosition( 0 ),
				CursesTextAffinity.Leading,
				true
			),
			layout.HitTest( 0, 2 )
		);
		Assert.Equal(
			new CursesTextHitTestResult(
				new CursesTextPosition( 1 ),
				CursesTextAffinity.Leading,
				true
			),
			layout.HitTest( 0, 3 )
		);
		Assert.Equal(
			new CursesTextHitTestResult(
				new CursesTextPosition( 2 ),
				CursesTextAffinity.Trailing,
				false
			),
			layout.HitTest( 0, 4 )
		);
	}

	[Fact]
	public void EllipsisHitMapsToFirstHiddenSourcePosition() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"abcde",
			new CursesTextLayoutOptions( 3 ) {
				Overflow = CursesTextOverflow.Ellipsis
			}
		);

		Assert.Equal(
			new CursesTextHitTestResult(
				new CursesTextPosition( 2 ),
				CursesTextAffinity.Leading,
				true
			),
			layout.HitTest( 0, 2 )
		);
		Assert.Equal(
			new CursesTextVisualPosition( 0, 2 ),
			layout.GetVisualPosition( new CursesTextPosition( 2 ) )
		);
	}

	[Fact]
	public void GeometryMethodsRejectInvalidLinesColumnsAndSourcePositions() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"ab",
			new CursesTextLayoutOptions( 2 )
		);

		Assert.Equal(
			"line",
			Assert.Throws<ArgumentOutOfRangeException>(
				() => layout.HitTest( -1, 0 )
			).ParamName
		);
		Assert.Equal(
			"line",
			Assert.Throws<ArgumentOutOfRangeException>(
				() => layout.HitTest( 1, 0 )
			).ParamName
		);
		Assert.Equal(
			"column",
			Assert.Throws<ArgumentOutOfRangeException>(
				() => layout.HitTest( 0, -1 )
			).ParamName
		);
		Assert.Equal(
			"position",
			Assert.Throws<ArgumentOutOfRangeException>(
				() => layout.GetVisualPosition( new CursesTextPosition( 3 ) )
			).ParamName
		);
		Assert.Equal(
			"affinity",
			Assert.Throws<ArgumentOutOfRangeException>(
				() => layout.GetVisualPosition(
					new CursesTextPosition( 0 ),
					(CursesTextAffinity)int.MaxValue
				)
			).ParamName
		);
	}

	[Fact]
	public void ReversedSelectionProducesOwnedRectanglesAcrossWrappedLines() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"abcdef",
			new CursesTextLayoutOptions( 2 ) {
				WrapMode = CursesTextWrapMode.TextElement
			}
		);
		CursesTextSelection selection = new(
			new CursesTextPosition( 5 ),
			new CursesTextPosition( 1 )
		);

		CursesRectangle[] actual = layout.GetSelectionRectangles(
			selection,
			0,
			3
		);
		Assert.Equal<CursesRectangle>(
			[
				new CursesRectangle( 0, 1, 1, 1 ),
				new CursesRectangle( 1, 0, 1, 2 ),
				new CursesRectangle( 2, 0, 1, 1 )
			],
			actual
		);
		Assert.NotSame(
			actual,
			layout.GetSelectionRectangles( selection, 0, 3 )
		);
	}

	[Fact]
	public void SelectionRectanglesClipToRequestedLinesAndVisibleSource() {
		CursesTextSelection selection = new(
			new CursesTextPosition( 1 ),
			new CursesTextPosition( 5 )
		);
		CursesTextLayout wrapped = CursesTextLayout.Create(
			"abcdef",
			new CursesTextLayoutOptions( 2 ) {
				WrapMode = CursesTextWrapMode.TextElement
			}
		);
		Assert.Equal<CursesRectangle>(
			[ new CursesRectangle( 1, 0, 1, 2 ) ],
			wrapped.GetSelectionRectangles( selection, 1, 1 )
		);

		CursesTextLayout clipped = CursesTextLayout.Create(
			"abcdef",
			new CursesTextLayoutOptions( 2 )
		);
		Assert.Equal<CursesRectangle>(
			[ new CursesRectangle( 0, 1, 1, 1 ) ],
			clipped.GetSelectionRectangles( selection, 0, 1 )
		);
		Assert.Empty(
			clipped.GetSelectionRectangles(
				new CursesTextSelection(
					new CursesTextPosition( 3 ),
					new CursesTextPosition( 5 )
				),
				0,
				1
			)
		);
	}

	[Fact]
	public void EmptySelectionProducesNoRectanglesAndRangesAreValidated() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"ab",
			new CursesTextLayoutOptions( 2 )
		);
		CursesTextSelection empty = new(
			new CursesTextPosition( 1 ),
			new CursesTextPosition( 1 )
		);

		Assert.Empty( layout.GetSelectionRectangles( empty, 0, 1 ) );
		Assert.Empty( layout.GetSelectionRectangles( empty, 1, 0 ) );
		Assert.Equal(
			"firstLine",
			Assert.Throws<ArgumentOutOfRangeException>(
				() => layout.GetSelectionRectangles( empty, -1, 0 )
			).ParamName
		);
		Assert.Equal(
			"lineCount",
			Assert.Throws<ArgumentOutOfRangeException>(
				() => layout.GetSelectionRectangles( empty, 0, -1 )
			).ParamName
		);
		Assert.Equal(
			"lineCount",
			Assert.Throws<ArgumentOutOfRangeException>(
				() => layout.GetSelectionRectangles( empty, 1, 1 )
			).ParamName
		);
		Assert.Equal(
			"selection",
			Assert.Throws<ArgumentOutOfRangeException>(
				() => layout.GetSelectionRectangles(
					new CursesTextSelection(
						new CursesTextPosition( 1 ),
						new CursesTextPosition( 3 )
					),
					0,
					1
				)
			).ParamName
		);
	}
}
