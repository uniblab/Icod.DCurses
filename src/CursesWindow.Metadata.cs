namespace Icod.DCurses;

using Icod.DCurses.Internal;

/// <summary>Semantic-metadata operations for logical windows.</summary>
public sealed partial class CursesWindow {
	/// <summary>Gets semantic metadata associated with one window-local coordinate.</summary>
	/// <param name="row">The zero-based local row.</param>
	/// <param name="column">The zero-based local column.</param>
	/// <returns>
	/// The semantic metadata at the projected logical coordinate, or <see langword="null"/> when none
	/// is associated or the valid local coordinate is temporarily clipped outside the owning screen.
	/// </returns>
	public CursesCellMetadata? GetMetadata(
		int row,
		int column
	) {
		ValidateCoordinate(
			row,
			column
		);
		return TryMapToScreen(
			row,
			column,
			out int screenRow,
			out int screenColumn
		)
			? screen.VirtualScreen.GetMetadata(
				screenRow,
				screenColumn
			)
			: null
		;
	}

	/// <summary>Associates semantic metadata with one window-local logical text-element footprint.</summary>
	/// <param name="row">The zero-based local row.</param>
	/// <param name="column">The zero-based local column.</param>
	/// <param name="metadata">The metadata to associate, or <see langword="null"/> to remove metadata.</param>
	public void SetMetadata(
		int row,
		int column,
		CursesCellMetadata? metadata
	) {
		ValidateCoordinate(
			row,
			column
		);
		if ( TryMapToScreen(
			row,
			column,
			out int screenRow,
			out int screenColumn
		) ) {
			screen.VirtualScreen.SetMetadata(
				screenRow,
				screenColumn,
				metadata
			);
		}
	}

	/// <summary>Writes terminal-independent text with semantic metadata using <see cref="CurrentStyle"/>.</summary>
	/// <param name="text">The text to write.</param>
	/// <param name="metadata">The semantic metadata applied to written logical content.</param>
	public void WriteWithMetadata(
		string text,
		CursesCellMetadata metadata
	) {
		ArgumentNullException.ThrowIfNull( text );
		ArgumentNullException.ThrowIfNull( metadata );
		Write(
			text,
			CurrentStyle,
			metadata
		);
	}

	/// <summary>Writes terminal-independent text with an explicit style and semantic metadata.</summary>
	/// <param name="text">The text to write.</param>
	/// <param name="style">The style applied to the written text.</param>
	/// <param name="metadata">The semantic metadata applied to written logical content.</param>
	public void Write(
		string text,
		CursesStyle style,
		CursesCellMetadata metadata
	) {
		ArgumentNullException.ThrowIfNull( text );
		ArgumentNullException.ThrowIfNull( metadata );
		ValidateText( text );

		foreach ( string textElement in CursesUnicodeText.EnumerateTextElements( text ) ) {
			if ( !WriteTextElementWithMetadataCore(
				textElement,
				style,
				metadata
			) ) {
				break;
			}
		}
	}

	/// <summary>Writes one exact logical cell with semantic metadata at the current cursor position.</summary>
	/// <param name="cell">The logical cell to write.</param>
	/// <param name="metadata">The semantic metadata applied to the written cell footprint.</param>
	public void WriteCell(
		CursesCell cell,
		CursesCellMetadata metadata
	) {
		ArgumentNullException.ThrowIfNull( metadata );
		_ = WriteCellWithMetadataCore(
			cell,
			metadata
		);
	}

	private bool WriteTextElementWithMetadataCore(
		string textElement,
		CursesStyle style,
		CursesCellMetadata metadata
	) {
		ArgumentException.ThrowIfNullOrEmpty( textElement );
		ArgumentNullException.ThrowIfNull( metadata );

		if ( 1 == textElement.Length ) {
			switch ( textElement[ 0 ] ) {
				case '\r':
					cursorColumn = 0;
					return true;

				case '\n':
					cursorColumn = 0;
					return AdvanceRow();

				case '\t':
					return WriteTabWithMetadata(
						style,
						metadata
					);
			}
		}

		int width = screen.TextWidthProvider.GetWidth( textElement );
		if ( 0 > width || 2 < width ) {
			throw new InvalidOperationException(
				"The configured curses text-width provider returned a width outside the supported range."
			);
		}

		if ( 0 == width ) {
			return AppendZeroWidthTextWithMetadata(
				textElement,
				metadata
			);
		}

		return WriteDisplayCellWithMetadataCore(
			new CursesCell(
				textElement,
				style,
				width
			),
			width,
			metadata
		);
	}

	private bool WriteTabWithMetadata(
		CursesStyle style,
		CursesCellMetadata metadata
	) {
		ArgumentNullException.ThrowIfNull( metadata );

		int spaces = TabWidth - ( cursorColumn % TabWidth );
		for ( int index = 0; index < spaces; index++ ) {
			if ( !WriteCellWithMetadataCore(
				new CursesCell( " ", style ),
				metadata
			) ) {
				return false;
			}
		}
		return true;
	}

	private bool WriteCellWithMetadataCore(
		CursesCell cell,
		CursesCellMetadata metadata
	) {
		ArgumentNullException.ThrowIfNull( metadata );

		if ( cell.IsContinuation ) {
			RepairCellFootprint(
				cursorRow,
				cursorColumn
			);
			SetCellAndMetadataIfVisible(
				cursorRow,
				cursorColumn,
				cell,
				metadata
			);
			return AdvanceColumns( 1 );
		}

		return WriteDisplayCellWithMetadataCore(
			cell,
			cell.DisplayWidth,
			metadata
		);
	}

	private bool WriteDisplayCellWithMetadataCore(
		CursesCell cell,
		int width,
		CursesCellMetadata metadata
	) {
		ArgumentNullException.ThrowIfNull( metadata );
		if ( 1 > width || 2 < width ) {
			throw new ArgumentOutOfRangeException( nameof( width ) );
		}

		if ( 2 == width
			&& cursorColumn + 1 >= Columns ) {
			if ( CursesWrapMode.Clip == WrapMode ) {
				return false;
			}

			cursorColumn = 0;
			if ( !AdvanceRow() ) {
				cursorColumn = Columns - 1;
				return false;
			}
		}

		RepairCellFootprint(
			cursorRow,
			cursorColumn
		);
		if ( 2 == width ) {
			RepairCellFootprint(
				cursorRow,
				cursorColumn + 1
			);
		}

		SetCellAndMetadataIfVisible(
			cursorRow,
			cursorColumn,
			cell,
			metadata
		);
		if ( 2 == width ) {
			SetCellAndMetadataIfVisible(
				cursorRow,
				cursorColumn + 1,
				CursesCell.Continuation( cell.Style ),
				metadata
			);
		}

		return AdvanceColumns( width );
	}

	private bool AppendZeroWidthTextWithMetadata(
		string text,
		CursesCellMetadata metadata
	) {
		ArgumentNullException.ThrowIfNull( text );
		ArgumentNullException.ThrowIfNull( metadata );

		if ( 0 == cursorColumn ) {
			return true;
		}

		int targetColumn = cursorColumn - 1;
		CursesCell target = GetCellOrBackground(
			cursorRow,
			targetColumn
		);
		while ( target.IsContinuation && 0 < targetColumn ) {
			targetColumn--;
			target = GetCellOrBackground(
				cursorRow,
				targetColumn
			);
		}

		if ( target.IsBlank || target.IsContinuation ) {
			return true;
		}

		SetCellAndMetadataIfVisible(
			cursorRow,
			targetColumn,
			new CursesCell(
				target.Content + text,
				target.Style,
				target.DisplayWidth
			),
			metadata
		);
		return true;
	}

	private void SetCellAndMetadataIfVisible(
		int row,
		int column,
		CursesCell cell,
		CursesCellMetadata metadata
	) {
		ArgumentNullException.ThrowIfNull( metadata );

		if ( TryMapToScreen(
			row,
			column,
			out int screenRow,
			out int screenColumn
		) ) {
			screen.VirtualScreen[ screenRow, screenColumn ] = cell;
			screen.VirtualScreen.SetMetadata(
				screenRow,
				screenColumn,
				metadata
			);
		}
	}
}
