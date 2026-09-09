using System.Reflection;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Guards the zero-public-delta decision for the 0.8 hardening release.</summary>
public sealed class PublicHardeningApiContractTests {
	[Fact]
	public void SessionLifetimeHardeningTypesRemainNonpublic() {
		Type sessionType = typeof( CursesSession );
		Type? waitScope = sessionType.GetNestedType(
			"SessionWaitCancellationScope",
			BindingFlags.NonPublic
		);

		Assert.NotNull( waitScope );
		Assert.False( waitScope!.IsPublic );
		Assert.False( waitScope.IsNestedPublic );
	}

	[Fact]
	public void NoHardeningOrConcurrencyTypeIsExported() {
		string[] forbiddenFragments = [
			"Hardening",
			"Concurrency",
			"CancellationScope",
			"SessionLifetime"
		];
		Type[] exported = typeof( CursesSession ).Assembly.GetExportedTypes();

		foreach ( Type type in exported ) {
			Assert.DoesNotContain(
				forbiddenFragments,
				fragment => type.Name.Contains(
					fragment,
					StringComparison.Ordinal
				)
			);
		}
	}
}
