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
using Icod.Terminal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes the additive Icod.DCurses 2.3 raster-atlas contract.</summary>
public sealed class CursesRasterAtlasPublicContractTests {
	[Fact]
	public void FrozenRasterAtlasSurfaceIsAvailableAtCompileTime() {
		TerminalRasterImage image = TerminalRasterImage.CreateRgb24(
			1,
			1,
			[ 0, 0, 0 ]
		);
		CursesRasterAtlasGeometry geometry = new( 2, 3, 4, 5 );
		CursesRasterAtlasTileUpdate update = new( 1, 2, image );
		CursesRasterAtlasPresentationResult result = default;
		CursesRasterAtlasPresentationStatus status = result.Status;

		Func<CursesRasterAtlas, int, int, CursesRasterCell> getCell =
			static ( atlas, row, column ) => atlas.GetCell( row, column );
		Func<
			CursesRasterAtlas,
			IReadOnlyList<CursesRasterAtlasTileUpdate>,
			CancellationToken,
			ValueTask<CursesRasterAtlasPresentationResult>
		> present = static ( atlas, updates, token ) => atlas.PresentAsync( updates, token );
		Func<
			CursesSession,
			int,
			int,
			TimeSpan,
			CancellationToken,
			ValueTask<CursesRasterAtlasGeometry>
		> query = static ( session, rows, columns, timeout, token ) =>
			session.QueryRasterAtlasGeometryAsync( rows, columns, timeout, token );
		Func<
			CursesSession,
			TerminalRasterImage,
			int,
			int,
			CancellationToken,
			ValueTask<TerminalControlResult<CursesRasterAtlas>>
		> create = static ( session, initialImage, rows, columns, token ) =>
			session.CreateRasterAtlasAsync( initialImage, rows, columns, token );
		Action<CursesWindow, int, int, CursesRasterAtlas, CursesRectangle> write =
			static ( window, row, column, atlas, rectangle ) =>
				window.WriteRasterAtlas( row, column, atlas, rectangle );

		Assert.Equal( 12, geometry.PixelWidth );
		Assert.Equal( 15, geometry.PixelHeight );
		Assert.Equal( 1, update.Row );
		Assert.Equal( 2, update.Column );
		Assert.Same( image, update.Image );
		Assert.Equal( CursesRasterAtlasPresentationStatus.NoChanges, status );
		Assert.NotNull( getCell );
		Assert.NotNull( present );
		Assert.NotNull( query );
		Assert.NotNull( create );
		Assert.NotNull( write );
	}

	[Fact]
	public void EnumValuesAndMaximumUpdateCountAreFrozen() {
		Assert.Equal( 0, (int)CursesRasterAtlasPresentationStatus.NoChanges );
		Assert.Equal( 1, (int)CursesRasterAtlasPresentationStatus.Presented );
		Assert.Equal( 2, (int)CursesRasterAtlasPresentationStatus.Unsupported );
		Assert.Equal( 3, (int)CursesRasterAtlasPresentationStatus.Unavailable );
		Assert.Equal( 4, (int)CursesRasterAtlasPresentationStatus.Failed );
		Assert.Equal( 4096, CursesRasterAtlas.MaximumUpdatesPerPresentation );
	}

	[Fact]
	public void PublicPropertiesAreReadOnlyAndTerminalIdentitiesRemainPrivate() {
		Type[] valueTypes = [
			typeof( CursesRasterAtlasGeometry ),
			typeof( CursesRasterAtlasTileUpdate ),
			typeof( CursesRasterAtlasPresentationResult )
		];
		foreach ( Type type in valueTypes ) {
			Assert.True( type.IsValueType );
			Assert.True( type.IsPublic );
			Assert.All(
				type.GetProperties( BindingFlags.Public | BindingFlags.Instance ),
				property => Assert.Null( property.SetMethod )
			);
		}

		Type atlasType = typeof( CursesRasterAtlas );
		Assert.True( atlasType.IsSealed );
		Assert.Contains( typeof( IAsyncDisposable ), atlasType.GetInterfaces() );
		Assert.Empty( atlasType.GetConstructors() );
		Assert.All(
			atlasType.GetProperties( BindingFlags.Public | BindingFlags.Instance ),
			property => Assert.Null( property.SetMethod )
		);

		Type[] forbiddenTypes = [
			typeof( TerminalRasterResource ),
			typeof( TerminalRasterPlaceholder ),
			typeof( TerminalRasterAnimation ),
			typeof( TerminalRasterAnimationFrame )
		];
		IEnumerable<Type> exposedTypes = atlasType
			.GetMembers( BindingFlags.Public | BindingFlags.Instance )
			.SelectMany( GetExposedTypes );
		Assert.DoesNotContain( exposedTypes, forbiddenTypes.Contains );
	}

	[Fact]
	public void AsyncMethodsFreezeCancellationDefaults() {
		AssertOptionalCancellation(
			typeof( CursesRasterAtlas ),
			nameof( CursesRasterAtlas.PresentAsync ),
			2
		);
		AssertOptionalCancellation(
			typeof( CursesSession ),
			nameof( CursesSession.QueryRasterAtlasGeometryAsync ),
			4
		);
		AssertOptionalCancellation(
			typeof( CursesSession ),
			nameof( CursesSession.CreateRasterAtlasAsync ),
			4
		);
	}

	private static IEnumerable<Type> GetExposedTypes( MemberInfo member ) {
		if ( member is PropertyInfo property ) {
			yield return property.PropertyType;
		}
		if ( member is MethodInfo method ) {
			yield return method.ReturnType;
			foreach ( ParameterInfo parameter in method.GetParameters() ) {
				yield return parameter.ParameterType;
			}
		}
	}

	private static void AssertOptionalCancellation(
		Type type,
		string name,
		int parameterCount
	) {
		MethodInfo method = Assert.Single(
			type.GetMethods( BindingFlags.Public | BindingFlags.Instance ),
			candidate => candidate.Name == name
				&& candidate.GetParameters().Length == parameterCount
		);
		ParameterInfo cancellation = method.GetParameters()[^1];
		Assert.Equal( typeof( CancellationToken ), cancellation.ParameterType );
		Assert.True( cancellation.HasDefaultValue );
		Assert.True( cancellation.IsOptional );
	}
}
