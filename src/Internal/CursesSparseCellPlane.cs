namespace Icod.DCurses.Internal;

/// <summary>
/// Stores sparse per-cell reference data without imposing a reference field on every logical cell.
/// </summary>
/// <typeparam name="T">The reference type stored for populated coordinates.</typeparam>
internal sealed class CursesSparseCellPlane<T>
	where T : class {
	private readonly int columns;
	private readonly int rows;
	private T?[]?[]? rowValues;
	private int valueCount;
	private int allocatedRowCount;

	/// <summary>Initializes an empty sparse plane.</summary>
	/// <param name="columns">The positive column count.</param>
	/// <param name="rows">The positive row count.</param>
	internal CursesSparseCellPlane(
		int columns,
		int rows
	) {
		if ( 0 >= columns ) {
			throw new ArgumentOutOfRangeException(
				nameof( columns ),
				columns,
				"The sparse-plane column count must be positive."
			);
		}
		if ( 0 >= rows ) {
			throw new ArgumentOutOfRangeException(
				nameof( rows ),
				rows,
				"The sparse-plane row count must be positive."
			);
		}
		if ( int.MaxValue < (long)columns * rows ) {
			throw new ArgumentOutOfRangeException(
				nameof( rows ),
				rows,
				"The requested sparse plane contains too many coordinates."
			);
		}

		this.columns = columns;
		this.rows = rows;
	}

	/// <summary>Gets the number of populated coordinates.</summary>
	internal int Count => this.valueCount;

	/// <summary>Gets the number of rows with allocated reference storage.</summary>
	internal int AllocatedRowCount => this.allocatedRowCount;

	/// <summary>Gets whether the plane currently contains any values.</summary>
	internal bool IsEmpty => 0 == this.valueCount;

	/// <summary>Gets one coordinate value, or <see langword="null"/> when absent.</summary>
	internal T? Get(
		int row,
		int column
	) {
		ValidateCoordinate(
			row,
			column
		);

		return this.rowValues?[ row ]?[ column ];
	}

	/// <summary>Sets or removes one coordinate value.</summary>
	internal void Set(
		int row,
		int column,
		T? value
	) {
		ValidateCoordinate(
			row,
			column
		);

		if ( value is null ) {
			Remove(
				row,
				column
			);
			return;
		}

		this.rowValues ??= new T?[]?[ this.rows ];
		T?[]? rowStorage = this.rowValues[ row ];
		if ( rowStorage is null ) {
			rowStorage = new T?[ this.columns ];
			this.rowValues[ row ] = rowStorage;
			this.allocatedRowCount++;
		}

		if ( rowStorage[ column ] is null ) {
			this.valueCount++;
		}
		rowStorage[ column ] = value;
	}

	/// <summary>Returns a detached snapshot of one row.</summary>
	internal T?[] SnapshotRow( int row ) {
		if ( 0 > row || row >= this.rows ) {
			throw new ArgumentOutOfRangeException(
				nameof( row ),
				row,
				"The row must be inside the sparse plane."
			);
		}

		T?[] result = new T?[ this.columns ];
		T?[]? source = this.rowValues?[ row ];
		if ( source is not null ) {
			Array.Copy(
				source,
				result,
				this.columns
			);
		}
		return result;
	}

	/// <summary>Replaces one complete row from a detached snapshot.</summary>
	internal void ReplaceRow(
		int row,
		IReadOnlyList<T?> values
	) {
		ArgumentNullException.ThrowIfNull( values );
		if ( 0 > row || row >= this.rows ) {
			throw new ArgumentOutOfRangeException(
				nameof( row ),
				row,
				"The row must be inside the sparse plane."
			);
		}
		if ( values.Count != this.columns ) {
			throw new ArgumentException(
				"The replacement row width must match the sparse plane.",
				nameof( values )
			);
		}

		ClearRow( row );
		for ( int column = 0; column < this.columns; column++ ) {
			T? value = values[ column ];
			if ( value is not null ) {
				Set(
					row,
					column,
					value
				);
			}
		}
	}

	/// <summary>Removes every value while releasing all allocated row storage.</summary>
	internal void Clear() {
		this.rowValues = null;
		this.valueCount = 0;
		this.allocatedRowCount = 0;
	}

	private void Remove(
		int row,
		int column
	) {
		T?[]? rowStorage = this.rowValues?[ row ];
		if ( rowStorage is null || rowStorage[ column ] is null ) {
			return;
		}

		rowStorage[ column ] = null;
		this.valueCount--;
		if ( RowContainsValue( rowStorage ) ) {
			return;
		}

		this.rowValues![ row ] = null;
		this.allocatedRowCount--;
		if ( 0 == this.valueCount ) {
			this.rowValues = null;
			this.allocatedRowCount = 0;
		}
	}

	private void ClearRow( int row ) {
		T?[]? rowStorage = this.rowValues?[ row ];
		if ( rowStorage is null ) {
			return;
		}

		for ( int column = 0; column < rowStorage.Length; column++ ) {
			if ( rowStorage[ column ] is not null ) {
				this.valueCount--;
			}
		}

		this.rowValues![ row ] = null;
		this.allocatedRowCount--;
		if ( 0 == this.valueCount ) {
			this.rowValues = null;
			this.allocatedRowCount = 0;
		}
	}

	private static bool RowContainsValue( T?[] row ) {
		ArgumentNullException.ThrowIfNull( row );
		for ( int column = 0; column < row.Length; column++ ) {
			if ( row[ column ] is not null ) {
				return true;
			}
		}
		return false;
	}

	private void ValidateCoordinate(
		int row,
		int column
	) {
		if ( 0 > row || row >= this.rows ) {
			throw new ArgumentOutOfRangeException(
				nameof( row ),
				row,
				"The row must be inside the sparse plane."
			);
		}
		if ( 0 > column || column >= this.columns ) {
			throw new ArgumentOutOfRangeException(
				nameof( column ),
				column,
				"The column must be inside the sparse plane."
			);
		}
	}
}
