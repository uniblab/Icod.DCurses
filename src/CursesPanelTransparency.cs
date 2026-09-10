namespace Icod.DCurses;

/// <summary>Defines how blank cells owned by a panel participate in logical composition.</summary>
public enum CursesPanelTransparency {
	/// <summary>Every panel cell covers lower content, including blank cells.</summary>
	Opaque = 0,

	/// <summary>Blank panel cells contribute neither cell state nor semantic metadata.</summary>
	BlankCellsTransparent = 1
}
