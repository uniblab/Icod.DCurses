namespace Icod.DCurses.Internal;

/// <summary>Collects bounded semantic observations for one enabled refresh attempt.</summary>
internal sealed class CursesRefreshDiagnosticsAccumulator {
	internal bool IsFullRepaint;
	internal bool PhysicalStateInvalidated;
	internal bool LogicalStatePublished;
	internal int LogicalCellsExamined;
	internal int LogicalCellsChanged;
	internal int DamagedRows;
	internal int DamagedRegions;
	internal int PreparedOutputItemCount;
	internal int ApplicationPayloadCount;
	internal int RasterPlaceholderCellCount;
	internal CursesRefreshOperationKinds OperationKinds;

	internal static int Increment( int value ) => value == int.MaxValue ? value : value + 1;

	internal CursesRefreshDiagnosticsSnapshot Snapshot( long sequence, CursesRefreshOutcome outcome ) =>
		new( sequence, outcome, IsFullRepaint, PhysicalStateInvalidated,
			LogicalStatePublished, LogicalCellsExamined, LogicalCellsChanged,
			DamagedRows, DamagedRegions, PreparedOutputItemCount,
			ApplicationPayloadCount, RasterPlaceholderCellCount, OperationKinds );
}
