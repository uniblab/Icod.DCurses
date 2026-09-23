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

/// <summary>Specifies Unicode-aware caret navigation and hit testing.</summary>
public sealed class CursesTextGeometryUnicodeTests {
	[Fact]
	public void PreviousAndNextObserveAttachedZeroWidthBoundary() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"A\u200B",
			new CursesTextLayoutOptions( 2 )
		);

		Assert.Equal( 0, layout.GetPreviousPosition( new CursesTextPosition( 0 ) ).Offset );
		Assert.Equal( 0, layout.GetPreviousPosition( new CursesTextPosition( 1 ) ).Offset );
		Assert.Equal( 1, layout.GetPreviousPosition( new CursesTextPosition( 2 ) ).Offset );
		Assert.Equal( 1, layout.GetNextPosition( new CursesTextPosition( 0 ) ).Offset );
		Assert.Equal( 2, layout.GetNextPosition( new CursesTextPosition( 1 ) ).Offset );
		Assert.Equal( 2, layout.GetNextPosition( new CursesTextPosition( 2 ) ).Offset );
		Assert.Equal(
			new CursesTextVisualPosition( 0, 1 ),
			layout.GetVisualPosition( new CursesTextPosition( 1 ) )
		);
	}

	[Fact]
	public void LineEdgesExcludeHardBreakAndRejectCrLfInterior() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"a\r\nb",
			new CursesTextLayoutOptions( 2 )
		);

		Assert.Equal( 0, layout.GetLineStart( 0 ).Offset );
		Assert.Equal( 1, layout.GetLineEnd( 0 ).Offset );
		Assert.Equal( 3, layout.GetLineStart( 1 ).Offset );
		Assert.Equal( 4, layout.GetLineEnd( 1 ).Offset );
		Assert.Equal(
			new CursesTextVisualPosition( 0, 1 ),
			layout.GetVisualPosition( new CursesTextPosition( 1 ) )
		);
		Assert.Equal(
			new CursesTextVisualPosition( 1, 0 ),
			layout.GetVisualPosition( new CursesTextPosition( 3 ) )
		);
		Assert.Equal(
			"position",
			Assert.Throws<ArgumentOutOfRangeException>(
				() => layout.GetVisualPosition( new CursesTextPosition( 2 ) )
			).ParamName
		);
	}

	[Fact]
	public void WidthTwoContinuationMapsToTrailingSourceEdge() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"\u754C",
			new CursesTextLayoutOptions( 2 )
		);

		Assert.Equal(
			new CursesTextHitTestResult(
				new CursesTextPosition( 0 ),
				CursesTextAffinity.Leading,
				true
			),
			layout.HitTest( 0, 0 )
		);
		Assert.Equal(
			new CursesTextHitTestResult(
				new CursesTextPosition( 1 ),
				CursesTextAffinity.Trailing,
				true
			),
			layout.HitTest( 0, 1 )
		);
	}

	[Fact]
	public void LeadingZeroWidthElementMapsButIsNeverHit() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"\u0301A",
			new CursesTextLayoutOptions( 1 )
		);

		Assert.Equal(
			new CursesTextVisualPosition( 0, 0 ),
			layout.GetVisualPosition( new CursesTextPosition( 0 ) )
		);
		Assert.Equal(
			new CursesTextVisualPosition( 0, 0 ),
			layout.GetVisualPosition( new CursesTextPosition( 1 ) )
		);
		Assert.Equal(
			new CursesTextHitTestResult(
				new CursesTextPosition( 1 ),
				CursesTextAffinity.Leading,
				true
			),
			layout.HitTest( 0, 0 )
		);
	}

	[Fact]
	public void VerticalMovementClampsLineAndPreservesPreferredColumn() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"abcd\nx\nwxyz",
			new CursesTextLayoutOptions( 4 )
		);
		CursesTextVisualPosition initial = new( 0, 3 );

		CursesTextVisualPosition shortLine = layout.MoveVertically(
			initial,
			1,
			3
		);
		Assert.Equal(
			new CursesTextVisualPosition(
				1,
				1,
				CursesTextAffinity.Trailing
			),
			shortLine
		);
		Assert.Equal(
			new CursesTextVisualPosition( 2, 3 ),
			layout.MoveVertically( shortLine, 1, 3 )
		);
		Assert.Equal(
			new CursesTextVisualPosition( 0, 3 ),
			layout.MoveVertically( shortLine, int.MinValue, 3 )
		);
		Assert.Equal(
			new CursesTextVisualPosition( 2, 3 ),
			layout.MoveVertically( shortLine, int.MaxValue, 3 )
		);
	}

	[Fact]
	public void NavigationMethodsValidateTheirArguments() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"ab",
			new CursesTextLayoutOptions( 2 )
		);

		Assert.Equal(
			"position",
			Assert.Throws<ArgumentOutOfRangeException>(
				() => layout.GetNextPosition( new CursesTextPosition( 3 ) )
			).ParamName
		);
		Assert.Equal(
			"line",
			Assert.Throws<ArgumentOutOfRangeException>(
				() => layout.GetLineStart( 1 )
			).ParamName
		);
		Assert.Equal(
			"preferredColumn",
			Assert.Throws<ArgumentOutOfRangeException>(
				() => layout.MoveVertically(
					new CursesTextVisualPosition( 0, 0 ),
					0,
					-1
				)
			).ParamName
		);
		Assert.Equal(
			"position",
			Assert.Throws<ArgumentOutOfRangeException>(
				() => layout.MoveVertically(
					new CursesTextVisualPosition( 1, 0 ),
					0,
					0
				)
			).ParamName
		);
	}
}
