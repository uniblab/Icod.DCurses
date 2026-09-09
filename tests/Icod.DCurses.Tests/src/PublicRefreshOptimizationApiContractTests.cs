using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes the deliberate public API delta for the 0.7 refresh-optimization release.</summary>
public sealed class PublicRefreshOptimizationApiContractTests {
	[Fact]
	public void SynchronizedOutputOptionIsThePublicRefreshOptimizationDelta() {
		System.Reflection.PropertyInfo? property = typeof( CursesSessionOptions )
			.GetProperty( nameof( CursesSessionOptions.UseSynchronizedOutput ) );

		Assert.NotNull( property );
		Assert.Equal( typeof( bool ), property.PropertyType );
		Assert.True( property.CanRead );
		Assert.True( property.CanWrite );
		Assert.False( new CursesSessionOptions().UseSynchronizedOutput );
	}

	[Fact]
	public void OptimizationImplementationTypesRemainInternal() {
		Type[] implementationTypes = [
			typeof( CursesOutputCostModel ),
			typeof( CursesCursorMotionResolver ),
			typeof( CursesEraseResolver ),
			typeof( CursesCharacterShiftResolver ),
			typeof( CursesLineShiftResolver ),
			typeof( CursesRefreshEngine )
		];

		Assert.All(
			implementationTypes,
			type => {
				Assert.False( type.IsPublic );
				Assert.False( type.IsNestedPublic );
			}
		);
	}
}
