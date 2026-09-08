using System.Reflection;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes the public pad and viewport surface introduced by the 0.5 release line.</summary>
public sealed class PublicPadApiContractTests {
	[Fact]
	public void CursesPadPublicSurfaceIsFrozen() {
		Type type = typeof( CursesPad );
		Assert.True( type.IsSealed );

		ConstructorInfo constructor = Assert.Single(
			type.GetConstructors(
				BindingFlags.Public
					| BindingFlags.Instance
			)
		);
		AssertParameterTypes(
			constructor.GetParameters(),
			[
				typeof( int ),
				typeof( int ),
				typeof( ICursesTextWidthProvider )
			]
		);
		ParameterInfo widthProvider = constructor.GetParameters()[ 2 ];
		Assert.True( widthProvider.HasDefaultValue );
		Assert.Null( widthProvider.DefaultValue );

		AssertDeclaredProperties(
			type,
			[
				( nameof( CursesPad.Columns ), typeof( int ) ),
				( nameof( CursesPad.ContentWindow ), typeof( CursesWindow ) ),
				( nameof( CursesPad.Rows ), typeof( int ) ),
				( nameof( CursesPad.TextWidthProvider ), typeof( ICursesTextWidthProvider ) )
			]
		);

		MethodInfo[] methods = GetDeclaredPublicMethods( type );
		Assert.Equal( 2, methods.Length );
		AssertMethod(
			methods,
			nameof( CursesPad.CreateViewport ),
			typeof( CursesPadViewport ),
			[
				typeof( CursesWindow ),
				typeof( int ),
				typeof( int ),
				typeof( int ),
				typeof( int ),
				typeof( int ),
				typeof( int )
			]
		);
		AssertMethod(
			methods,
			nameof( CursesPad.PresentTo ),
			typeof( void ),
			[
				typeof( CursesWindow ),
				typeof( int ),
				typeof( int ),
				typeof( int ),
				typeof( int ),
				typeof( int ),
				typeof( int )
			]
		);
	}

	[Fact]
	public void CursesPadViewportPublicSurfaceIsFrozen() {
		Type type = typeof( CursesPadViewport );
		Assert.True( type.IsSealed );
		Assert.Empty(
			type.GetConstructors(
				BindingFlags.Public
					| BindingFlags.Instance
			)
		);

		AssertDeclaredProperties(
			type,
			[
				( nameof( CursesPadViewport.Columns ), typeof( int ) ),
				( nameof( CursesPadViewport.DestinationColumn ), typeof( int ) ),
				( nameof( CursesPadViewport.DestinationRow ), typeof( int ) ),
				( nameof( CursesPadViewport.HasVisiblePadChanges ), typeof( bool ) ),
				( nameof( CursesPadViewport.PadColumn ), typeof( int ) ),
				( nameof( CursesPadViewport.PadRow ), typeof( int ) ),
				( nameof( CursesPadViewport.Rows ), typeof( int ) )
			]
		);

		MethodInfo[] methods = GetDeclaredPublicMethods( type );
		Assert.Equal( 3, methods.Length );
		AssertMethod(
			methods,
			nameof( CursesPadViewport.PanBy ),
			typeof( void ),
			[ typeof( int ), typeof( int ) ]
		);
		AssertMethod(
			methods,
			nameof( CursesPadViewport.Present ),
			typeof( void ),
			[]
		);
		AssertMethod(
			methods,
			nameof( CursesPadViewport.SetSource ),
			typeof( void ),
			[ typeof( int ), typeof( int ) ]
		);
	}

	[Fact]
	public void RejectedPadSurfacesRemainAbsent() {
		Assembly assembly = typeof( CursesPad ).Assembly;
		Assert.Null( assembly.GetType( "Icod.DCurses.CursesSubpad" ) );

		string[] rejectedPadMethods = [
			"MarkClean",
			"Acknowledge",
			"AcknowledgeChanges"
		];
		foreach ( string methodName in rejectedPadMethods ) {
			Assert.Null(
				typeof( CursesPad ).GetMethod(
					methodName,
					BindingFlags.Public
						| BindingFlags.Instance
				)
			);
		}
	}

	private static MethodInfo[] GetDeclaredPublicMethods( Type type ) {
		ArgumentNullException.ThrowIfNull( type );
		return type.GetMethods(
			BindingFlags.Public
				| BindingFlags.Instance
				| BindingFlags.DeclaredOnly
		)
			.Where( static method => !method.IsSpecialName )
			.OrderBy( static method => method.Name, StringComparer.Ordinal )
			.ToArray();
	}

	private static void AssertDeclaredProperties(
		Type type,
		IReadOnlyList<( string Name, Type Type )> expected
	) {
		ArgumentNullException.ThrowIfNull( type );
		ArgumentNullException.ThrowIfNull( expected );

		PropertyInfo[] properties = type.GetProperties(
			BindingFlags.Public
				| BindingFlags.Instance
				| BindingFlags.DeclaredOnly
		)
			.OrderBy( static property => property.Name, StringComparer.Ordinal )
			.ToArray();
		Assert.Equal( expected.Count, properties.Length );
		for ( int index = 0; index < expected.Count; index++ ) {
			Assert.Equal( expected[ index ].Name, properties[ index ].Name );
			Assert.Equal( expected[ index ].Type, properties[ index ].PropertyType );
			Assert.NotNull( properties[ index ].GetMethod );
			Assert.Null( properties[ index ].SetMethod );
		}
	}

	private static void AssertMethod(
		IReadOnlyList<MethodInfo> methods,
		string name,
		Type returnType,
		Type[] parameterTypes
	) {
		ArgumentNullException.ThrowIfNull( methods );
		ArgumentException.ThrowIfNullOrWhiteSpace( name );
		ArgumentNullException.ThrowIfNull( returnType );
		ArgumentNullException.ThrowIfNull( parameterTypes );

		MethodInfo method = Assert.Single(
			methods.Where( candidate => string.Equals(
				candidate.Name,
				name,
				StringComparison.Ordinal
			) )
		);
		Assert.Equal( returnType, method.ReturnType );
		AssertParameterTypes(
			method.GetParameters(),
			parameterTypes
		);
	}

	private static void AssertParameterTypes(
		IReadOnlyList<ParameterInfo> parameters,
		IReadOnlyList<Type> expectedTypes
	) {
		ArgumentNullException.ThrowIfNull( parameters );
		ArgumentNullException.ThrowIfNull( expectedTypes );
		Assert.Equal( expectedTypes.Count, parameters.Count );
		for ( int index = 0; index < expectedTypes.Count; index++ ) {
			Assert.Equal(
				expectedTypes[ index ],
				parameters[ index ].ParameterType
			);
		}
	}
}
