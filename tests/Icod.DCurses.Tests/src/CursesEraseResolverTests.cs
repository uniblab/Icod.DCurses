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
using Icod.DCurses;
using Icod.DCurses.Internal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies deterministic cost and safety selection for physical erase operations.</summary>
public sealed class CursesEraseResolverTests {
	[Fact]
	public void LiteralBlanksRemainFallbackWhenShorterThanEraseLine() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "literal-win" )
			.SetString( StringCapability.ClearToEndOfLine, "<el>" )
			.Build();
		CursesEraseResolver resolver = CreateResolver( terminal );
		CursesVirtualScreen desired = new( 2, 1 );
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen(
			desired,
			"x"
		);

		CursesErasePlan? plan = resolver.Resolve(
			desired,
			physical,
			0,
			0
		);

		Assert.Null( plan );
	}

	[Fact]
	public void EraseLineWinsWhenCheaperThanLiteralBlanks() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "el-win" )
			.SetString( StringCapability.ClearToEndOfLine, "E" )
			.Build();
		CursesEraseResolver resolver = CreateResolver( terminal );
		CursesVirtualScreen desired = new( 6, 1 );
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen(
			desired,
			"x"
		);

		CursesErasePlan? plan = resolver.Resolve(
			desired,
			physical,
			0,
			0
		);

		Assert.True( plan.HasValue );
		Assert.Equal( CursesEraseKind.ClearToEndOfLine, plan.Value.Kind );
		Assert.Equal( "E", plan.Value.Sequence );
		Assert.Equal( 1, plan.Value.AffectedLines );
	}

	[Fact]
	public void EraseScreenWinsAcrossDefaultBlankTail() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "ed-win" )
			.SetString( StringCapability.ClearToEndOfLine, "LLLL" )
			.SetString( StringCapability.ClearToEndOfScreen, "D" )
			.Build();
		CursesEraseResolver resolver = CreateResolver( terminal );
		CursesVirtualScreen desired = new( 6, 3 );
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen(
			desired,
			"x"
		);

		CursesErasePlan? plan = resolver.Resolve(
			desired,
			physical,
			1,
			2
		);

		Assert.True( plan.HasValue );
		Assert.Equal( CursesEraseKind.ClearToEndOfScreen, plan.Value.Kind );
		Assert.Equal( "D", plan.Value.Sequence );
		Assert.Equal( 2, plan.Value.AffectedLines );
	}

	[Fact]
	public void ClearScreenCanWinWhenWholeLogicalScreenIsDefaultBlank() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "clear-win" )
			.SetString( StringCapability.ClearToEndOfLine, "LLLL" )
			.SetString( StringCapability.ClearToEndOfScreen, "DD" )
			.SetString( StringCapability.ClearScreen, "C" )
			.Build();
		CursesEraseResolver resolver = CreateResolver( terminal );
		CursesVirtualScreen desired = new( 8, 4 );
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen(
			desired,
			"x"
		);

		CursesErasePlan? plan = resolver.Resolve(
			desired,
			physical,
			2,
			3
		);

		Assert.True( plan.HasValue );
		Assert.Equal( CursesEraseKind.ClearScreen, plan.Value.Kind );
		Assert.Equal( "C", plan.Value.Sequence );
		Assert.Equal( 4, plan.Value.AffectedLines );
	}

	[Fact]
	public void NondefaultStyledBlankBlocksBroaderErase() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "style-block" )
			.SetString( StringCapability.ClearToEndOfLine, "E" )
			.SetString( StringCapability.ClearToEndOfScreen, "D" )
			.SetString( StringCapability.ClearScreen, "C" )
			.Build();
		CursesEraseResolver resolver = CreateResolver( terminal );
		CursesVirtualScreen desired = new( 6, 2 );
		desired[ 1, 0 ] = CursesCell.Blank(
			new CursesStyle(
				CursesColor.Default,
				CursesColor.Default,
				CursesTextAttributes.Bold
			)
		);
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen(
			desired,
			"x"
		);

		CursesErasePlan? plan = resolver.Resolve(
			desired,
			physical,
			0,
			0
		);

		Assert.True( plan.HasValue );
		Assert.Equal( CursesEraseKind.ClearToEndOfLine, plan.Value.Kind );
	}

	[Fact]
	public void ApplicationEncodingChangesLiteralBlankCost() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "encoding" )
			.SetString( StringCapability.ClearToEndOfLine, "EEE" )
			.Build();
		CursesVirtualScreen desired = new( 2, 1 );
		CursesPhysicalScreenState physical = CreateFilledPhysicalScreen(
			desired,
			"x"
		);
		CursesEraseResolver utf8 = new(
			terminal,
			new CursesOutputCostModel( Encoding.UTF8 )
		);
		CursesEraseResolver utf16 = new(
			terminal,
			new CursesOutputCostModel( Encoding.Unicode )
		);

		Assert.Null( utf8.Resolve( desired, physical, 0, 0 ) );
		CursesErasePlan? utf16Plan = utf16.Resolve(
			desired,
			physical,
			0,
			0
		);
		Assert.True( utf16Plan.HasValue );
		Assert.Equal( CursesEraseKind.ClearToEndOfLine, utf16Plan.Value.Kind );
	}

	private static CursesEraseResolver CreateResolver(
		TerminalDescription terminal
	) {
		ArgumentNullException.ThrowIfNull( terminal );
		return new CursesEraseResolver(
			terminal,
			new CursesOutputCostModel( Encoding.UTF8 )
		);
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
