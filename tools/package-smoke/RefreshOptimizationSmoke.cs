namespace Icod.DCurses.PackageSmoke;

using System.Runtime.CompilerServices;
using Icod.DCurses;

/// <summary>Exercises the deliberate 0.7 public refresh-optimization delta from the packed package.</summary>
internal static class RefreshOptimizationSmoke {
	[ModuleInitializer]
	internal static void Validate() {
		CursesSessionOptions defaults = new();
		if ( defaults.UseSynchronizedOutput ) {
			throw new InvalidOperationException(
				"DCurses package-only synchronized-output default changed unexpectedly."
			);
		}

		CursesSessionOptions enabled = new() {
			UseSynchronizedOutput = true
		};
		if ( !enabled.UseSynchronizedOutput ) {
			throw new InvalidOperationException(
				"DCurses package-only synchronized-output option is unavailable."
			);
		}
	}
}
