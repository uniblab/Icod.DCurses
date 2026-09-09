namespace Icod.DCurses;

using Icod.DCurses.Internal;

/// <summary>
/// Stores the application-requested logical image of a terminal screen.
/// </summary>
/// <remarks>
/// This type does not write to a terminal. Physical-screen knowledge is tracked separately by the
/// refresh layer so application intent cannot be confused with what is believed to be on the terminal.
/// Coordinates are zero-based.
/// </remarks>
public sealed class CursesVirtualScreen {
	private readonly CursesCell[] cells;
	private readonly bool[] dirtyCells;
	private CursesSparseCellPlane<CursesCellMetadata>? semanticMetadata;
	private ulong[]? cellChangeRevisions;
	private int dirtyCellCount;
	private ulong changeRevision;

	/// <summary>Initializes a blank logical screen.</summary>
	/// <param name="columns">The positive number of columns.</param>
	/// <param name="rows">The positive number of rows.</param>
	public CursesVirtualScreen(
		int columns,
		int rows ) {
		int cellCount = ValidateDimensions(
			columns,
			rows
		);

		Columns = columns;
		Rows = rows;
		cells = new CursesCell[ cellCount ];
		dirtyCells = new bool[ cellCount ];
		Array.Fill(
			dirtyCells,
			true
		);
		dirtyCellCount = cellCount;
	}

	/// <summary>Gets the number of columns.</summary>
	public int Columns {
		get;
	}

	/// <summary>Gets the number of rows.</summary>
	public int Rows {
		get;
	}

	/// <summary>Gets the number of cells in the logical screen.</summary>
	public int CellCount => cells.Length;

	/// <summary>Gets or sets one logical cell using zero-based row and column coordinates.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	/// <value>The logical cell at the requested coordinate.</value>
	public CursesCell this[
		int row,
		int column ] {
		get => GetCell(
			row,
			column
		);
		set => SetCell(
			row,
			column,
			value
		);
	}

	/// <summary>Gets one logical cell.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	/// <returns>The logical cell at the requested coordinate.</returns>
	public CursesCell GetCell(
		int row,
		int column ) {
		return cells[ GetOffset( row, column ) ];
	}

	/// <summary>Gets semantic metadata associated with one logical coordinate.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	/// <returns>The semantic metadata, or <see langword="null"/> when none is associated.</returns>
	public CursesCellMetadata? GetMetadata(
		int row,
		int column ) {
		_ = GetOffset(
			row,
			column
		);
		return semanticMetadata?.Get(
			row,
			column
		);
	}

	/// <summary>Associates semantic metadata with one logical text element footprint.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	/// <param name="metadata">The metadata to associate, or <see langword="null"/> to remove metadata.</param>
	/// <remarks>
	/// When the coordinate belongs to a valid two-column text element, both the leader and continuation
	/// coordinate receive the same semantic metadata.
	/// </remarks>
	public void SetMetadata(
		int row,
		int column,
		CursesCellMetadata? metadata ) {
		int offset = GetOffset(
			row,
			column
		);
		CursesCell cell = cells[ offset ];

		if ( cell.IsContinuation
			&& 0 < column ) {
			CursesCell leader = cells[ offset - 1 ];
			if ( !leader.IsContinuation
				&& 2 == leader.DisplayWidth ) {
				SetMetadataRaw(
					row,
					column - 1,
					offset - 1,
					metadata
				);
				SetMetadataRaw(
					row,
					column,
					offset,
					metadata
				);
				return;
			}
		}

		if ( 2 == cell.DisplayWidth
			&& column + 1 < Columns
			&& cells[ offset + 1 ].IsContinuation ) {
			SetMetadataRaw(
				row,
				column,
				offset,
				metadata
			);
			SetMetadataRaw(
				row,
				column + 1,
				offset + 1,
				metadata
			);
			return;
		}

		SetMetadataRaw(
			row,
			column,
			offset,
			metadata
		);
	}

	/// <summary>Sets one logical cell.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	/// <param name="cell">The replacement logical cell.</param>
	/// <remarks>
	/// Replacing a logical cell removes semantic metadata associated with the existing text-element
	/// footprint. Use <see cref="SetMetadata(int,int,CursesCellMetadata?)"/> after replacement when the
	/// new content should carry metadata.
	/// </remarks>
	public void SetCell(
		int row,
		int column,
		CursesCell cell ) {
		int offset = GetOffset(
			row,
			column
		);

		ClearExistingMetadataFootprint(
			row,
			column,
			offset
		);
		if ( cells[ offset ] == cell ) {
			return;
		}

		if ( RepairWideFootprintsOnReplacement
			&& !cell.IsContinuation ) {
			RepairExistingWideFootprint(
				row,
				column,
				offset
			);
		}

		SetCellRaw(
			offset,
			cell
		);
	}

	/// <summary>Clears the logical screen to default blank cells.</summary>
	public void Clear() {
		Fill( default );
	}

	/// <summary>Clears the logical screen to blank cells carrying the supplied style.</summary>
	/// <param name="style">The style assigned to each blank cell.</param>
	public void Clear( CursesStyle style ) {
		Fill( CursesCell.Blank( style ) );
	}

	/// <summary>Fills every logical coordinate with the same cell value.</summary>
	/// <param name="cell">The cell value copied to every coordinate.</param>
	public void Fill( CursesCell cell ) {
		bool removedSemanticMetadata = semanticMetadata is not null;
		semanticMetadata = null;
		for ( int offset = 0; offset < cells.Length; offset++ ) {
			SetCellRaw(
				offset,
				cell
			);
		}
		if ( removedSemanticMetadata ) {
			Invalidate();
		}
	}

	/// <summary>Gets the number of logical cells currently marked dirty.</summary>
	internal int DirtyCellCount => dirtyCellCount;

	/// <summary>Gets the latest logical content/damage revision for this surface.</summary>
	internal ulong ChangeRevision => changeRevision;

	/// <summary>Gets whether per-cell logical content/damage revision tracking is enabled.</summary>
	internal bool ChangeTrackingEnabled => null != cellChangeRevisions;

	/// <summary>Gets the number of coordinates carrying semantic metadata.</summary>
	internal int SemanticMetadataCount => semanticMetadata?.Count ?? 0;

	/// <summary>
	/// Gets or sets whether ordinary replacement writes repair an existing wide-cell footprint.
	/// </summary>
	/// <remarks>
	/// Standalone virtual screens retain exact cell-storage semantics. Screens owned by
	/// <see cref="CursesScreen"/> enable this so window operations cannot strand the other half of an
	/// existing two-column glyph when replacing a leader or continuation.
	/// </remarks>
	internal bool RepairWideFootprintsOnReplacement {
		get;
		set;
	}

	/// <summary>Enables per-cell logical content/damage revision tracking for specialized consumers.</summary>
	internal void EnableChangeTracking() {
		cellChangeRevisions ??= new ulong[ cells.Length ];
	}

	/// <summary>Gets whether one logical coordinate is marked dirty.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	/// <returns><see langword="true"/> when the cell requires refresh processing.</returns>
	internal bool IsDirty(
		int row,
		int column ) {
		return dirtyCells[ GetOffset( row, column ) ];
	}

	/// <summary>Gets the latest logical content/damage revision for one cell.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	/// <returns>The cell-local change revision.</returns>
	internal ulong GetCellChangeRevision(
		int row,
		int column ) {
		ulong[] revisions = cellChangeRevisions
			?? throw new InvalidOperationException(
				"Logical change revision tracking is not enabled for this virtual screen."
			);
		return revisions[ GetOffset( row, column ) ];
	}

	/// <summary>Marks one logical coordinate dirty without changing its value.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	internal void TouchCell(
		int row,
		int column ) {
		int offset = GetOffset(
			row,
			column
		);
		RecordChange( offset );
		MarkDirty( offset );
	}

	/// <summary>Gets a read-only view of the logical cell storage.</summary>
	internal ReadOnlySpan<CursesCell> Cells => cells;

	/// <summary>Marks all logical cells clean after a successful physical refresh.</summary>
	internal void MarkClean() {
		Array.Clear( dirtyCells );
		dirtyCellCount = 0;
	}

	/// <summary>Marks every logical cell dirty.</summary>
	internal void Invalidate() {
		if ( null != cellChangeRevisions ) {
			for ( int offset = 0; offset < cells.Length; offset++ ) {
				RecordChange( offset );
			}
		}
		Array.Fill(
			dirtyCells,
			true
		);
		dirtyCellCount = dirtyCells.Length;
	}

	private void RepairExistingWideFootprint(
		int row,
		int column,
		int offset ) {
		CursesCell existing = cells[ offset ];
		if ( existing.IsContinuation ) {
			if ( 0 < column ) {
				CursesCell leader = cells[ offset - 1 ];
				if ( !leader.IsContinuation
					&& 2 == leader.DisplayWidth ) {
					SetCellRaw(
						offset - 1,
						CursesCell.Blank( leader.Style )
					);
				}
			}
			return;
		}

		if ( 2 == existing.DisplayWidth
			&& column + 1 < Columns ) {
			CursesCell continuation = cells[ offset + 1 ];
			if ( continuation.IsContinuation ) {
				SetCellRaw(
					offset + 1,
					CursesCell.Blank( continuation.Style )
				);
			}
		}
	}

	private void ClearExistingMetadataFootprint(
		int row,
		int column,
		int offset ) {
		if ( semanticMetadata is null ) {
			return;
		}

		CursesCell existing = cells[ offset ];
		if ( existing.IsContinuation
			&& 0 < column ) {
			CursesCell leader = cells[ offset - 1 ];
			if ( !leader.IsContinuation
				&& 2 == leader.DisplayWidth ) {
				SetMetadataRaw(
					row,
					column - 1,
					offset - 1,
					null
				);
			}
		}

		SetMetadataRaw(
			row,
			column,
			offset,
			null
		);
		if ( 2 == existing.DisplayWidth
			&& column + 1 < Columns ) {
			SetMetadataRaw(
				row,
				column + 1,
				offset + 1,
				null
			);
		}
	}

	private void SetMetadataRaw(
		int row,
		int column,
		int offset,
		CursesCellMetadata? metadata ) {
		CursesCellMetadata? current = semanticMetadata?.Get(
			row,
			column
		);
		if ( Equals(
			current,
			metadata
		) ) {
			return;
		}

		if ( metadata is null ) {
			if ( semanticMetadata is null ) {
				return;
			}
			semanticMetadata.Set(
				row,
				column,
				null
			);
			if ( semanticMetadata.IsEmpty ) {
				semanticMetadata = null;
			}
		} else {
			semanticMetadata ??= new CursesSparseCellPlane<CursesCellMetadata>(
				Columns,
				Rows
			);
			semanticMetadata.Set(
				row,
				column,
				metadata
			);
		}

		RecordChange( offset );
		MarkDirty( offset );
	}

	private void SetCellRaw(
		int offset,
		CursesCell cell ) {
		if ( cells[ offset ] == cell ) {
			return;
		}

		cells[ offset ] = cell;
		RecordChange( offset );
		MarkDirty( offset );
	}

	private void RecordChange( int offset ) {
		ulong[]? revisions = cellChangeRevisions;
		if ( null == revisions ) {
			return;
		}

		unchecked {
			changeRevision++;
			revisions[ offset ] = changeRevision;
		}
	}

	private void MarkDirty( int offset ) {
		if ( dirtyCells[ offset ] ) {
			return;
		}

		dirtyCells[ offset ] = true;
		dirtyCellCount++;
	}

	private int GetOffset(
		int row,
		int column ) {
		if ( row < 0 || row >= Rows ) {
			throw new ArgumentOutOfRangeException(
				nameof( row ),
				row,
				"The row must be inside the virtual screen."
			);
		}

		if ( column < 0 || column >= Columns ) {
			throw new ArgumentOutOfRangeException(
				nameof( column ),
				column,
				"The column must be inside the virtual screen."
			);
		}

		return ( row * Columns ) + column;
	}

	private static int ValidateDimensions(
		int columns,
		int rows ) {
		if ( columns <= 0 ) {
			throw new ArgumentOutOfRangeException(
				nameof( columns ),
				columns,
				"The virtual-screen column count must be positive."
			);
		}

		if ( rows <= 0 ) {
			throw new ArgumentOutOfRangeException(
				nameof( rows ),
				rows,
				"The virtual-screen row count must be positive."
			);
		}

		long cellCount = (long)columns * rows;
		if ( int.MaxValue < cellCount ) {
			throw new ArgumentOutOfRangeException(
				nameof( rows ),
				rows,
				"The requested virtual screen contains too many cells."
			);
		}

		return (int)cellCount;
	}
}
