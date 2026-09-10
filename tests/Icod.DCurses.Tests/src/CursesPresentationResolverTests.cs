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
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies deterministic logical-to-physical presentation degradation.</summary>
public sealed class CursesPresentationResolverTests {
	[Fact]
	public void InRangeIndexedColorIsPreserved() {
		CursesPresentationResolver resolver = new( CreateEightColorTerminal() );
		CursesStyle requested = new(
			CursesColor.Indexed( 7 ),
			CursesColor.Default
		);

		CursesStyle resolved = resolver.Resolve( requested );

		Assert.Equal( requested, resolved );
	}

	[Fact]
	public void OutOfRangeIndexedColorDegradesToTerminalDefault() {
		CursesPresentationResolver resolver = new( CreateEightColorTerminal() );
		CursesStyle requested = new(
			CursesColor.Indexed( 8 ),
			CursesColor.Default
		);

		CursesStyle resolved = resolver.Resolve( requested );

		Assert.True( resolved.Foreground.IsDefault );
		Assert.True( resolved.Background.IsDefault );
	}

	[Fact]
	public void IndexedColorWithoutSafeDefaultRestorationDegradesToDefault() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "no-op" )
			.SetNumber( NumericCapability.Colors, 8 )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.Build();
		CursesPresentationResolver resolver = new( terminal );

		CursesStyle resolved = resolver.Resolve(
			new CursesStyle(
				CursesColor.Indexed( 2 ),
				CursesColor.Default
			)
		);

		Assert.True( resolved.Foreground.IsDefault );
	}

	[Fact]
	public void RgbOnIndexedTerminalDegradesToDefault() {
		CursesPresentationResolver resolver = new( CreateEightColorTerminal() );

		CursesStyle resolved = resolver.Resolve(
			new CursesStyle(
				CursesColor.Rgb( 12, 34, 56 ),
				CursesColor.Default
			)
		);

		Assert.True( resolved.Foreground.IsDefault );
	}

	[Fact]
	public void DirectRgbIsPreservedWhenPackedValueIsSafe() {
		CursesPresentationResolver resolver = new( TerminalProfiles.XtermDirect256 );
		CursesColor requested = CursesColor.Rgb( 255, 0, 0 );

		CursesStyle resolved = resolver.Resolve(
			new CursesStyle(
				requested,
				CursesColor.Default
			)
		);

		Assert.Equal( requested, resolved.Foreground );
	}

	[Fact]
	public void DirectRgbCollisionWithRetainedIndexedPrefixDegradesToDefault() {
		CursesPresentationResolver resolver = new( TerminalProfiles.XtermDirect256 );

		CursesStyle resolved = resolver.Resolve(
			new CursesStyle(
				CursesColor.Rgb( 0, 0, 1 ),
				CursesColor.Default
			)
		);

		Assert.True( resolved.Foreground.IsDefault );
	}

	[Fact]
	public void UnsupportedAttributesAreOmittedAndStandoutFallsBackToReverse() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "reverse-only" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.EnterReverseMode, "<reverse>" )
			.Build();
		CursesPresentationResolver resolver = new( terminal );

		CursesStyle resolved = resolver.Resolve(
			new CursesStyle(
				CursesColor.Default,
				CursesColor.Default,
				CursesTextAttributes.Standout
					| CursesTextAttributes.Italic
			)
		);

		Assert.Equal( CursesTextAttributes.Reverse, resolved.Attributes );
	}

	[Fact]
	public void NoColorVideoRestrictionsApplyOnlyWhenPhysicalColorIsActive() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "ncv" )
			.SetNumber( NumericCapability.Colors, 8 )
			.SetNumber( NumericCapability.NoColorVideo, 32 )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.Build();
		CursesPresentationResolver resolver = new( terminal );

		CursesStyle colored = resolver.Resolve(
			new CursesStyle(
				CursesColor.Indexed( 1 ),
				CursesColor.Default,
				CursesTextAttributes.Bold
			)
		);
		CursesStyle uncolored = resolver.Resolve(
			new CursesStyle(
				CursesColor.Default,
				CursesColor.Default,
				CursesTextAttributes.Bold
			)
		);

		Assert.Equal( CursesTextAttributes.None, colored.Attributes );
		Assert.Equal( CursesTextAttributes.Bold, uncolored.Attributes );
	}

	private static TerminalDescription CreateEightColorTerminal() {
		return new TerminalDescriptionBuilder( "eight-color" )
			.SetNumber( NumericCapability.Colors, 8 )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.Build();
	}
}
