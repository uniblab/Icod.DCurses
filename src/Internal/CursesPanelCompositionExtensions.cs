namespace Icod.DCurses;

using System.Runtime.CompilerServices;
using Icod.DCurses.Internal;

/// <summary>Provides the internal logical-frame composition operation used while the 1.2 contract is under development.</summary>
internal static class CursesPanelCompositionExtensions {
	private static readonly ConditionalWeakTable<CursesScreen, CursesPanelCompositionState> compositionStates = new();

	/// <summary>Creates or incrementally updates the retained composed logical frame for one screen.</summary>
	/// <param name="screen">The screen whose base frame and panels should be composed.</param>
	/// <returns>The retained logical frame containing the base screen plus visible panels in bottom-to-top order.</returns>
	internal static CursesVirtualScreen ComposePanels( this CursesScreen screen ) {
		ArgumentNullException.ThrowIfNull( screen );

		CursesPanelCompositionState state = compositionStates.GetValue(
			screen,
			static _ => new CursesPanelCompositionState()
		);
		return state.Compose( screen );
	}
}
