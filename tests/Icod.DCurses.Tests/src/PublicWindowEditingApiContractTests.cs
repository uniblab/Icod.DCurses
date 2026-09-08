using System.Reflection;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes the public window-editing surface introduced by the 0.4 release line.</summary>
public sealed class PublicWindowEditingApiContractTests {
	[Fact]
	public void ApprovedWindowEditingMethodsHaveFrozenSignatures() {
		AssertMethod(
			nameof( CursesWindow.Reposition ),
			typeof( void ),
			[ typeof( int ), typeof( int ) ]
		);
		AssertMethod(
			nameof( CursesWindow.GetCell ),
			typeof( CursesCell ),
			[ typeof( int ), typeof( int ) ]
		);
		AssertMethod(
			nameof( CursesWindow.FillRectangle ),
			typeof( void ),
			[ typeof( int ), typeof( int ), typeof( int ), typeof( int ), typeof( CursesCell ) ]
		);
		AssertMethod(
			nameof( CursesWindow.ClearToBeginningOfLine ),
			typeof( void ),
			[]
		);
		AssertEditMethod( nameof( CursesWindow.InsertCells ) );
		AssertEditMethod( nameof( CursesWindow.DeleteCells ) );
		AssertEditMethod( nameof( CursesWindow.InsertLines ) );
		AssertEditMethod( nameof( CursesWindow.DeleteLines ) );
		AssertMethod(
			nameof( CursesWindow.CopyRectangleTo ),
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
		AssertMethod(
			nameof( CursesWindow.OverlayRectangleTo ),
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
		AssertMethod(
			nameof( CursesWindow.DrawHorizontalLine ),
			typeof( void ),
			[ typeof( int ), typeof( int ), typeof( int ), typeof( CursesCell ) ]
		);
		AssertMethod(
			nameof( CursesWindow.DrawVerticalLine ),
			typeof( void ),
			[ typeof( int ), typeof( int ), typeof( int ), typeof( CursesCell ) ]
		);
		AssertMethod(
			nameof( CursesWindow.DrawBorder ),
			typeof( void ),
			[
				typeof( CursesCell ),
				typeof( CursesCell ),
				typeof( CursesCell ),
				typeof( CursesCell ),
				typeof( CursesCell ),
				typeof( CursesCell )
			]
		);
		AssertMethod(
			nameof( CursesWindow.TouchRegion ),
			typeof( void ),
			[ typeof( int ), typeof( int ), typeof( int ), typeof( int ) ]
		);
		AssertMethod(
			nameof( CursesWindow.IsRegionTouched ),
			typeof( bool ),
			[ typeof( int ), typeof( int ), typeof( int ), typeof( int ) ]
		);
	}

	[Fact]
	public void UnsafeUntouchSurfaceIsNotPublic() {
		MethodInfo? method = typeof( CursesWindow ).GetMethod(
			"UntouchRegion",
			BindingFlags.Public
				| BindingFlags.Instance
		);

		Assert.Null( method );
	}

	private static void AssertEditMethod( string name ) {
		MethodInfo method = AssertMethod(
			name,
			typeof( void ),
			[ typeof( int ) ]
		);
		ParameterInfo count = Assert.Single( method.GetParameters() );
		Assert.True( count.HasDefaultValue );
		Assert.Equal( 1, count.DefaultValue );
	}

	private static MethodInfo AssertMethod(
		string name,
		Type returnType,
		Type[] parameterTypes
	) {
		ArgumentException.ThrowIfNullOrWhiteSpace( name );
		ArgumentNullException.ThrowIfNull( returnType );
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
		Assert.Equal( returnType, method.ReturnType );
		return method;
	}
}
