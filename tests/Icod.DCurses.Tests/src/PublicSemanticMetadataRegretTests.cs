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

using System.Reflection;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes the pre-RC extensibility and source-compatibility decisions for 1.1 semantic metadata.</summary>
public sealed class PublicSemanticMetadataRegretTests {
	[Fact]
	public void HyperlinkPropertyIsNullableForFutureSemanticKinds() {
		PropertyInfo property = typeof( CursesCellMetadata ).GetProperty(
			nameof( CursesCellMetadata.Hyperlink )
		)!;
		NullabilityInfo nullability = new NullabilityInfoContext().Create( property );

		Assert.Equal( NullabilityState.Nullable, nullability.ReadState );
	}

	[Fact]
	public void CurrentConstructorStillRequiresARealHyperlink() {
		Assert.Throws<ArgumentNullException>(
			() => new CursesCellMetadata( null! )
		);
		CursesHyperlink hyperlink = new(
			"https://example.test/regret",
			"regret"
		);

		CursesCellMetadata metadata = new( hyperlink );

		Assert.Same( hyperlink, metadata.Hyperlink );
	}

	[Fact]
	public void SemanticConvenienceNameDoesNotAmbiguateStableDefaultStyleWrite() {
		CursesScreen screen = new(
			8,
			1
		);
		CursesWindow window = screen.StandardWindow;

		window.Write(
			"plain",
			default
		);
		window.Move(
			0,
			0
		);
		CursesCellMetadata metadata = new(
			new CursesHyperlink(
				"https://example.test/source-compatibility",
				"source-compatibility"
			)
		);
		window.WriteWithMetadata(
			"linked",
			metadata
		);

		Assert.Equal( metadata, window.GetMetadata( 0, 0 ) );
	}
}
