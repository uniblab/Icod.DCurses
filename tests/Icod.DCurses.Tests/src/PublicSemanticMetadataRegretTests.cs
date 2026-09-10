using System.Reflection;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes the pre-RC extensibility decision for the 1.1 semantic metadata container.</summary>
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
}
