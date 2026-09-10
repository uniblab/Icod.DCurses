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
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies exact and conservative physical insert/delete-character selection.</summary>
public sealed class CursesCharacterShiftResolverTests {
	[Fact]
	public void ParameterizedInsertWinsForExactDefaultBlankShift() {
		CursesCharacterShiftResolver resolver = CreateResolver(
			new TerminalDescriptionBuilder( "insert" )
				.SetString( StringCapability.InsertCharacters, "I%p1%d" )
				.Build()
		);
		CursesVirtualScreen desired = CreateLogicalRow( "A  BCDEF" );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCDEFGH" );

		CursesCharacterShiftPlan? plan = resolver.Resolve(
			desired,
			physical,
			0,
			CursesStyle.Default
		);

		Assert.True( plan.HasValue );
		Assert.Equal( CursesCharacterShiftKind.Insert, plan.Value.Kind );
		Assert.Equal( 1, plan.Value.Column );
		Assert.Equal( 2, plan.Value.Count );
		Assert.Equal( "I2", plan.Value.Sequence );
		Assert.Equal( 2, plan.Value.ByteCount );
	}

	[Fact]
	public void ParameterizedDeleteWinsForExactDefaultBlankShift() {
		CursesCharacterShiftResolver resolver = CreateResolver(
			new TerminalDescriptionBuilder( "delete" )
				.SetString( StringCapability.DeleteCharacters, "D%p1%d" )
				.Build()
		);
		CursesVirtualScreen desired = CreateLogicalRow( "ADEFGH  " );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCDEFGH" );

		CursesCharacterShiftPlan? plan = resolver.Resolve(
			desired,
			physical,
			0,
			CursesStyle.Default
		);

		Assert.True( plan.HasValue );
		Assert.Equal( CursesCharacterShiftKind.Delete, plan.Value.Kind );
		Assert.Equal( 1, plan.Value.Column );
		Assert.Equal( 2, plan.Value.Count );
		Assert.Equal( "D2", plan.Value.Sequence );
		Assert.Equal( 2, plan.Value.ByteCount );
	}

	[Fact]
	public void RepeatedOneCharacterInsertCanBeatParameterizedInsert() {
		CursesCharacterShiftResolver resolver = CreateResolver(
			new TerminalDescriptionBuilder( "insert-one" )
				.SetString( StringCapability.InsertCharacters, "<insert:%p1%d>" )
				.SetString( StringCapability.InsertCharacter, "i" )
				.Build()
		);
		CursesVirtualScreen desired = CreateLogicalRow( "A  BCDEF" );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCDEFGH" );

		CursesCharacterShiftPlan? plan = resolver.Resolve(
			desired,
			physical,
			0,
			CursesStyle.Default
		);

		Assert.True( plan.HasValue );
		Assert.Equal( "ii", plan.Value.Sequence );
		Assert.Equal( 2, plan.Value.ByteCount );
	}

	[Fact]
	public void EqualCostDoesNotReplaceRewriteFallback() {
		CursesCharacterShiftResolver resolver = CreateResolver(
			new TerminalDescriptionBuilder( "tie" )
				.SetString( StringCapability.InsertCharacter, "XYZ" )
				.Build()
		);
		CursesVirtualScreen desired = CreateLogicalRow( " ABC" );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCD" );

		CursesCharacterShiftPlan? plan = resolver.Resolve(
			desired,
			physical,
			0,
			CursesStyle.Default
		);

		Assert.False( plan.HasValue );
	}

	[Fact]
	public void ApplicationEncodingParticipatesInStrictCostGate() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "encoding" )
			.SetString( StringCapability.InsertCharacter, "XYZ" )
			.Build();
		CursesVirtualScreen desired = CreateLogicalRow( " ABC" );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCD" );
		CursesCharacterShiftResolver utf8 = CreateResolver(
			terminal,
			Encoding.UTF8
		);
		CursesCharacterShiftResolver utf16 = CreateResolver(
			terminal,
			Encoding.Unicode
		);

		Assert.False(
			utf8.Resolve(
				desired,
				physical,
				0,
				CursesStyle.Default
			).HasValue
		);
		Assert.True(
			utf16.Resolve(
				desired,
				physical,
				0,
				CursesStyle.Default
			).HasValue
		);
	}

	[Fact]
	public void UnknownPhysicalCellBlocksShiftOptimization() {
		CursesCharacterShiftResolver resolver = CreateResolver(
			new TerminalDescriptionBuilder( "unknown" )
				.SetString( StringCapability.InsertCharacters, "I%p1%d" )
				.Build()
		);
		CursesVirtualScreen desired = CreateLogicalRow( "A  BCDEF" );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCDEFGH" );
		physical.Invalidate();

		Assert.False(
			resolver.Resolve(
				desired,
				physical,
				0,
				CursesStyle.Default
			).HasValue
		);
	}

	[Fact]
	public void NondefaultRenditionBlocksInsertedBlankOptimization() {
		CursesCharacterShiftResolver resolver = CreateResolver(
			new TerminalDescriptionBuilder( "style" )
				.SetString( StringCapability.InsertCharacters, "I%p1%d" )
				.Build()
		);
		CursesVirtualScreen desired = CreateLogicalRow( "A  BCDEF" );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCDEFGH" );
		CursesStyle bold = CursesStyle.Default.WithAttributes(
			CursesTextAttributes.Bold
		);

		Assert.False(
			resolver.Resolve(
				desired,
				physical,
				0,
				bold
			).HasValue
		);
	}

	[Fact]
	public void NondefaultInsertedBlankBlocksInsertOptimization() {
		CursesCharacterShiftResolver resolver = CreateResolver(
			new TerminalDescriptionBuilder( "insert-style" )
				.SetString( StringCapability.InsertCharacter, "i" )
				.Build()
		);
		CursesVirtualScreen desired = CreateLogicalRow( "A BCDE" );
		CursesPhysicalScreenState physical = CreatePhysicalRow( "ABCDEF" );
		desired[ 0, 1 ] = CursesCell.Blank(
			CursesStyle.Default.WithAttributes(
				CursesTextAttributes.Bold
			)
		);

		Assert.False(
			resolver.Resolve(
				desired,
				physical,
				0,
				CursesStyle.Default
			).HasValue
		);
	}

	[Fact]
	public void WideCellInShiftRangeBlocksOptimization() {
		CursesCharacterShiftResolver resolver = CreateResolver(
			new TerminalDescriptionBuilder( "wide" )
				.SetString( StringCapability.InsertCharacter, "i" )
				.Build()
		);
		CursesVirtualScreen desired = new( 6, 1 );
		CursesPhysicalScreenState physical = new( 6, 1 );
		CursesCell wide = new(
			"界",
			CursesStyle.Default,
			displayWidth: 2
		);
		CursesCell continuation = CursesCell.Continuation();
		CursesCell[] physicalCells = [
			new CursesCell( "A" ),
			wide,
			continuation,
			new CursesCell( "B" ),
			new CursesCell( "C" ),
			new CursesCell( "D" )
		];
		CursesCell[] desiredCells = [
			CursesCell.Blank(),
			new CursesCell( "A" ),
			wide,
			continuation,
			new CursesCell( "B" ),
			new CursesCell( "C" )
		];
		for ( int column = 0; column < 6; column++ ) {
			physical.SetCell( 0, column, physicalCells[ column ] );
			desired[ 0, column ] = desiredCells[ column ];
		}

		Assert.False(
			resolver.Resolve(
				desired,
				physical,
				0,
				CursesStyle.Default
			).HasValue
		);
	}

	private static CursesCharacterShiftResolver CreateResolver(
		TerminalDescription terminal,
		Encoding? encoding = null
	) {
		ArgumentNullException.ThrowIfNull( terminal );
		return new CursesCharacterShiftResolver(
			terminal,
			new CursesOutputCostModel( encoding ?? Encoding.UTF8 )
		);
	}

	private static CursesVirtualScreen CreateLogicalRow(
		string value
	) {
		ArgumentNullException.ThrowIfNull( value );
		CursesVirtualScreen result = new( value.Length, 1 );
		for ( int column = 0; column < value.Length; column++ ) {
			result[ 0, column ] = CreateCell( value[ column ] );
		}
		return result;
	}

	private static CursesPhysicalScreenState CreatePhysicalRow(
		string value
	) {
		ArgumentNullException.ThrowIfNull( value );
		CursesPhysicalScreenState result = new( value.Length, 1 );
		for ( int column = 0; column < value.Length; column++ ) {
			result.SetCell(
				0,
				column,
				CreateCell( value[ column ] )
			);
		}
		return result;
	}

	private static CursesCell CreateCell(
		char value
	) {
		return ' ' == value
			? CursesCell.Blank()
			: new CursesCell( value.ToString() )
		;
	}
}
