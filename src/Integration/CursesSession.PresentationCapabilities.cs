namespace Icod.DCurses;

/// <summary>Presentation-capability observations for the selected curses terminal.</summary>
public sealed partial class CursesSession {
	/// <summary>
	/// Gets an immutable curses-shaped view of the selected terminal's presentation capabilities.
	/// </summary>
	public CursesPresentationCapabilities PresentationCapabilities {
		get {
			return CursesPresentationCapabilities.Create( Terminal );
		}
	}
}
