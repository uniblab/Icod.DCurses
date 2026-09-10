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

/// <summary>Freezes the public rendition, presentation, and semantic line-drawing surface introduced by 0.6.</summary>
public sealed class PublicPresentationApiContractTests {
	[Fact]
	public void CursesTextAttributeValuesRemainFrozen() {
		Assert.Equal( 0, (int)CursesTextAttributes.None );
		Assert.Equal( 1, (int)CursesTextAttributes.Bold );
		Assert.Equal( 2, (int)CursesTextAttributes.Dim );
		Assert.Equal( 4, (int)CursesTextAttributes.Underline );
		Assert.Equal( 8, (int)CursesTextAttributes.Reverse );
		Assert.Equal( 16, (int)CursesTextAttributes.Standout );
		Assert.Equal( 32, (int)CursesTextAttributes.Italic );
		Assert.Equal( 64, (int)CursesTextAttributes.Blink );
		Assert.Equal( 128, (int)CursesTextAttributes.Conceal );
		Assert.Equal( 256, (int)CursesTextAttributes.Strikeout );
	}

	[Fact]
	public void CursesLineGlyphValuesRemainFrozen() {
		Assert.Equal( 0, (int)CursesLineGlyph.Horizontal );
		Assert.Equal( 1, (int)CursesLineGlyph.Vertical );
		Assert.Equal( 2, (int)CursesLineGlyph.UpperLeftCorner );
		Assert.Equal( 3, (int)CursesLineGlyph.UpperRightCorner );
		Assert.Equal( 4, (int)CursesLineGlyph.LowerLeftCorner );
		Assert.Equal( 5, (int)CursesLineGlyph.LowerRightCorner );
		Assert.Equal( 6, (int)CursesLineGlyph.TeeUp );
		Assert.Equal( 7, (int)CursesLineGlyph.TeeDown );
		Assert.Equal( 8, (int)CursesLineGlyph.TeeLeft );
		Assert.Equal( 9, (int)CursesLineGlyph.TeeRight );
		Assert.Equal( 10, (int)CursesLineGlyph.Crossing );
	}

	[Fact]
	public void PresentationCapabilitiesPropertiesRemainFrozen() {
		Dictionary<string, Type> expected = new( StringComparer.Ordinal ) {
			[ nameof( CursesPresentationCapabilities.ColorRestrictedAttributes ) ] = typeof( CursesTextAttributes ),
			[ nameof( CursesPresentationCapabilities.IndexedColorCount ) ] = typeof( int ),
			[ nameof( CursesPresentationCapabilities.SupportedAttributes ) ] = typeof( CursesTextAttributes ),
			[ nameof( CursesPresentationCapabilities.SupportsAlternateCharacterSet ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsBackgroundColor ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsBlink ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsBold ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsColor ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsConceal ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsCursorHidden ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsCursorNormal ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsCursorVeryVisible ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsDefaultColorRestoration ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsDim ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsDirectRgb ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsForegroundColor ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsItalic ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsReverse ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsStandout ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsStrikeout ) ] = typeof( bool ),
			[ nameof( CursesPresentationCapabilities.SupportsUnderline ) ] = typeof( bool )
		};
		PropertyInfo[] properties = typeof( CursesPresentationCapabilities ).GetProperties(
			BindingFlags.Public
				| BindingFlags.Instance
				| BindingFlags.DeclaredOnly
		);

		Assert.Equal( expected.Count, properties.Length );
		foreach ( PropertyInfo property in properties ) {
			Assert.True(
				expected.TryGetValue(
					property.Name,
					out Type? expectedType
				),
				$"Unexpected public presentation-capability property '{property.Name}'."
			);
			Assert.Equal( expectedType, property.PropertyType );
			Assert.NotNull( property.GetMethod );
			Assert.Null( property.SetMethod );
		}
	}

	[Fact]
	public void SemanticLineCellSurfaceRemainsFrozen() {
		PropertyInfo? lineGlyph = typeof( CursesCell ).GetProperty(
			nameof( CursesCell.LineGlyph ),
			BindingFlags.Public
				| BindingFlags.Instance
		);
		PropertyInfo? isLineGlyph = typeof( CursesCell ).GetProperty(
			nameof( CursesCell.IsLineGlyph ),
			BindingFlags.Public
				| BindingFlags.Instance
		);
		MethodInfo? lineFactory = typeof( CursesCell ).GetMethod(
			nameof( CursesCell.Line ),
			BindingFlags.Public
				| BindingFlags.Static,
			binder: null,
			types: [ typeof( CursesLineGlyph ), typeof( CursesStyle ) ],
			modifiers: null
		);

		Assert.NotNull( lineGlyph );
		Assert.Equal( typeof( CursesLineGlyph? ), lineGlyph.PropertyType );
		Assert.NotNull( isLineGlyph );
		Assert.Equal( typeof( bool ), isLineGlyph.PropertyType );
		Assert.NotNull( lineFactory );
		Assert.Equal( typeof( CursesCell ), lineFactory.ReturnType );
		ParameterInfo[] parameters = lineFactory.GetParameters();
		Assert.Equal( 2, parameters.Length );
		Assert.True( parameters[ 1 ].HasDefaultValue );
	}

	[Fact]
	public void SessionPresentationCapabilitiesPropertyRemainsFrozen() {
		PropertyInfo? property = typeof( CursesSession ).GetProperty(
			nameof( CursesSession.PresentationCapabilities ),
			BindingFlags.Public
				| BindingFlags.Instance
		);

		Assert.NotNull( property );
		Assert.Equal( typeof( CursesPresentationCapabilities ), property.PropertyType );
		Assert.NotNull( property.GetMethod );
		Assert.Null( property.SetMethod );
	}

	[Fact]
	public void SemanticDrawingOverloadsRemainFrozen() {
		AssertWindowMethod(
			nameof( CursesWindow.DrawHorizontalLine ),
			[ typeof( int ), typeof( int ), typeof( int ) ]
		);
		AssertWindowMethod(
			nameof( CursesWindow.DrawVerticalLine ),
			[ typeof( int ), typeof( int ), typeof( int ) ]
		);
		AssertWindowMethod(
			nameof( CursesWindow.DrawBorder ),
			[]
		);
		AssertWindowMethod(
			nameof( CursesWindow.DrawBorder ),
			[ typeof( CursesStyle ) ]
		);
	}

	private static void AssertWindowMethod(
		string name,
		Type[] parameterTypes
	) {
		ArgumentException.ThrowIfNullOrWhiteSpace( name );
		ArgumentNullException.ThrowIfNull( parameterTypes );

		MethodInfo? method = typeof( CursesWindow ).GetMethod(
			name,
			BindingFlags.Public
				| BindingFlags.Instance,
			binder: null,
			types: parameterTypes,
			modifiers: null
		);

		Assert.NotNull( method );
		Assert.Equal( typeof( void ), method.ReturnType );
	}
}
