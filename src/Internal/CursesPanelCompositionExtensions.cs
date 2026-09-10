namespace Icod.DCurses;

using Icod.DCurses.Internal;

/// <summary>Provides the internal logical-frame composition operation used while the 1.2 contract is under development.</summary>
internal static class CursesPanelCompositionExtensions {
	/// <summary>Creates a composed logical frame without mutating the base screen or any panel surface.</summary>
	/// <param name="screen">The screen whose base frame and panels should be composed.</param>
	/// <returns>A new logical frame containing the base screen plus visible panels in bottom-to-top order.</returns>
	internal static CursesVirtualScreen ComposePanels( this CursesScreen screen ) {
		ArgumentNullException.ThrowIfNull( screen );

		return CursesPanelCompositor.Compose(
			screen.VirtualScreen,
			screen.SnapshotPanelsBottomToTop()
		);
	}
}
