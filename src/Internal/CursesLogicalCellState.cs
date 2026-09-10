namespace Icod.DCurses.Internal;

/// <summary>
/// Captures one retained logical cell together with its optional semantic metadata for temporary
/// editing, composition, and structural-copy operations.
/// </summary>
/// <remarks>
/// This is intentionally an internal transient value. The stable public <see cref="CursesCell"/>
/// representation remains independent of surface-owned semantic metadata.
/// </remarks>
internal readonly record struct CursesLogicalCellState(
	CursesCell Cell,
	CursesCellMetadata? Metadata
);
