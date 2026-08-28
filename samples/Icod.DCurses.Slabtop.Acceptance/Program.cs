using Icod.DCurses;

TimeSpan refreshInterval = TimeSpan.FromSeconds( 2 );

await using CursesSession session = await CursesSession.OpenAsync();
CursesWindow screen = session.StandardScreen;

CursesStyle titleStyle = new(
	CursesColor.Default,
	CursesColor.Default,
	CursesTextAttributes.Bold
);
CursesStyle headingStyle = new(
	CursesColor.Default,
	CursesColor.Default,
	CursesTextAttributes.Reverse
);

SlabSortKey sortKey = SlabSortKey.Name;
int generation = 0;
IReadOnlyList<SlabRow> snapshot = CreateSnapshot( generation );
string lifecycleStatus = "Ready.";
bool running = true;
bool dirty = true;

while ( running ) {
	if ( dirty ) {
		DrawSlabtop(
			screen,
			titleStyle,
			headingStyle,
			snapshot,
			sortKey,
			generation,
			lifecycleStatus
		);
		await session.RefreshAsync();
		dirty = false;
	}

	CursesEvent current = await session.ReadEventAsync( refreshInterval );

	switch ( current.Kind ) {
		case CursesEventKind.Timeout:
			generation++;
			snapshot = CreateSnapshot( generation );
			lifecycleStatus = "Timer sample.";
			dirty = true;
			break;

		case CursesEventKind.Lifecycle:
			if ( null == current.Lifecycle ) {
				break;
			}

			if ( current.RequiresRepaint ) {
				session.Invalidate();
				lifecycleStatus =
					$"{current.Lifecycle.Kind}: repaint current sample.";
				dirty = true;
			}

			if ( current.Lifecycle.Kind is CursesLifecycleEventKind.Interrupt
				or CursesLifecycleEventKind.Termination ) {
				running = false;
			}
			break;

		case CursesEventKind.Input:
			if ( null == current.Input ) {
				break;
			}

			if ( CursesInputEventKind.EndOfInput == current.Input.Kind ) {
				running = false;
				break;
			}

			if ( CursesKey.Space == current.Input.Key ) {
				generation++;
				snapshot = CreateSnapshot( generation );
				lifecycleStatus = "Immediate Space sample.";
				dirty = true;
				break;
			}

			if ( CursesInputEventKind.Text != current.Input.Kind
				|| !current.Input.Character.HasValue ) {
				break;
			}

			int character = current.Input.Character.Value.Value;
			if ( character is 'q' or 'Q' ) {
				running = false;
				break;
			}

			if ( TryGetSortKey(
				character,
				out SlabSortKey requestedSort
			) ) {
				sortKey = requestedSort;
				lifecycleStatus =
					$"Sort changed to {GetSortDescription( sortKey )}.";
				dirty = true;
			}
			break;
	}
}

return 0;

static void DrawSlabtop(
	CursesWindow screen,
	CursesStyle titleStyle,
	CursesStyle headingStyle,
	IReadOnlyList<SlabRow> snapshot,
	SlabSortKey sortKey,
	int generation,
	string lifecycleStatus
) {
	ArgumentNullException.ThrowIfNull( screen );
	ArgumentNullException.ThrowIfNull( snapshot );
	ArgumentNullException.ThrowIfNull( lifecycleStatus );

	screen.WrapMode = CursesWrapMode.Clip;
	screen.Clear();

	IReadOnlyList<SlabRow> rows = SortRows(
		snapshot,
		sortKey
	);

	long totalObjects = rows.Sum( row => row.TotalObjects );
	long activeObjects = rows.Sum( row => row.ActiveObjects );
	long totalSlabs = rows.Sum( row => row.TotalSlabs );
	long activeSlabs = rows.Sum( row => row.ActiveSlabs );
	long totalCacheKiB = rows.Sum( row => row.CacheSizeKiB );

	WriteLine(
		screen,
		0,
		"Icod.DCurses slabtop-shaped acceptance",
		titleStyle
	);
	WriteLine(
		screen,
		1,
		$"generation={generation}  sort={GetSortLetter( sortKey )}"
			+ $" ({GetSortDescription( sortKey )})"
	);
	WriteLine(
		screen,
		2,
		$"Active / Total Objects: {activeObjects} / {totalObjects}"
			+ $" ({Percent( activeObjects, totalObjects ):F1}%)"
	);
	WriteLine(
		screen,
		3,
		$"Active / Total Slabs:   {activeSlabs} / {totalSlabs}"
			+ $"   Cache Size: {totalCacheKiB} KiB"
	);
	WriteLine(
		screen,
		5,
		"  OBJS ACTIVE   USE OBJ SIZE SLABS OBJ/SLAB CACHE SIZE NAME",
		headingStyle
	);

	int firstRow = 6;
	int lastRow = Math.Max(
		firstRow,
		screen.Rows - 2
	);
	int displayed = Math.Min(
		rows.Count,
		Math.Max(
			0,
			lastRow - firstRow
		)
	);

	for ( int index = 0; index < displayed; index++ ) {
		SlabRow row = rows[ index ];
		WriteLine(
			screen,
			firstRow + index,
			FormatRow( row )
		);
	}

	WriteLine(
		screen,
		screen.Rows - 2,
		"a/b/c/l/v/n/o/p/s/u sort | Space sample | Q exit"
	);
	WriteLine(
		screen,
		screen.Rows - 1,
		lifecycleStatus
	);
}

static IReadOnlyList<SlabRow> SortRows(
	IReadOnlyList<SlabRow> rows,
	SlabSortKey sortKey
) {
	ArgumentNullException.ThrowIfNull( rows );

	IEnumerable<SlabRow> ordered = sortKey switch {
		SlabSortKey.ActiveObjects => rows.OrderByDescending( row => row.ActiveObjects ),
		SlabSortKey.ObjectsPerSlab => rows.OrderByDescending( row => row.ObjectsPerSlab ),
		SlabSortKey.CacheSize => rows.OrderByDescending( row => row.CacheSizeKiB ),
		SlabSortKey.TotalSlabs => rows.OrderByDescending( row => row.TotalSlabs ),
		SlabSortKey.ActiveSlabs => rows.OrderByDescending( row => row.ActiveSlabs ),
		SlabSortKey.Name => rows.OrderBy(
			row => row.Name,
			StringComparer.Ordinal
		),
		SlabSortKey.TotalObjects => rows.OrderByDescending( row => row.TotalObjects ),
		SlabSortKey.PagesPerSlab => rows.OrderByDescending( row => row.PagesPerSlab ),
		SlabSortKey.ObjectSize => rows.OrderByDescending( row => row.ObjectSizeBytes ),
		SlabSortKey.Utilization => rows.OrderByDescending( row => row.Utilization ),
		_ => throw new ArgumentOutOfRangeException( nameof( sortKey ) )
	};

	return ordered.ToArray();
}

static bool TryGetSortKey(
	int character,
	out SlabSortKey sortKey
) {
	switch ( character ) {
		case 'a':
		case 'A':
			sortKey = SlabSortKey.ActiveObjects;
			return true;

		case 'b':
		case 'B':
			sortKey = SlabSortKey.ObjectsPerSlab;
			return true;

		case 'c':
		case 'C':
			sortKey = SlabSortKey.CacheSize;
			return true;

		case 'l':
		case 'L':
			sortKey = SlabSortKey.TotalSlabs;
			return true;

		case 'v':
		case 'V':
			sortKey = SlabSortKey.ActiveSlabs;
			return true;

		case 'n':
		case 'N':
			sortKey = SlabSortKey.Name;
			return true;

		case 'o':
		case 'O':
			sortKey = SlabSortKey.TotalObjects;
			return true;

		case 'p':
		case 'P':
			sortKey = SlabSortKey.PagesPerSlab;
			return true;

		case 's':
		case 'S':
			sortKey = SlabSortKey.ObjectSize;
			return true;

		case 'u':
		case 'U':
			sortKey = SlabSortKey.Utilization;
			return true;

		default:
			sortKey = default;
			return false;
	}
}

static char GetSortLetter(
	SlabSortKey sortKey
) {
	return sortKey switch {
		SlabSortKey.ActiveObjects => 'a',
		SlabSortKey.ObjectsPerSlab => 'b',
		SlabSortKey.CacheSize => 'c',
		SlabSortKey.TotalSlabs => 'l',
		SlabSortKey.ActiveSlabs => 'v',
		SlabSortKey.Name => 'n',
		SlabSortKey.TotalObjects => 'o',
		SlabSortKey.PagesPerSlab => 'p',
		SlabSortKey.ObjectSize => 's',
		SlabSortKey.Utilization => 'u',
		_ => throw new ArgumentOutOfRangeException( nameof( sortKey ) )
	};
}

static string GetSortDescription(
	SlabSortKey sortKey
) {
	return sortKey switch {
		SlabSortKey.ActiveObjects => "active objects",
		SlabSortKey.ObjectsPerSlab => "objects per slab",
		SlabSortKey.CacheSize => "cache size",
		SlabSortKey.TotalSlabs => "total slabs",
		SlabSortKey.ActiveSlabs => "active slabs",
		SlabSortKey.Name => "cache name",
		SlabSortKey.TotalObjects => "total objects",
		SlabSortKey.PagesPerSlab => "pages per slab",
		SlabSortKey.ObjectSize => "object size",
		SlabSortKey.Utilization => "utilization",
		_ => throw new ArgumentOutOfRangeException( nameof( sortKey ) )
	};
}

static string FormatRow(
	SlabRow row
) {
	ArgumentNullException.ThrowIfNull( row );

	return string.Format(
		System.Globalization.CultureInfo.InvariantCulture,
		"{0,6} {1,6} {2,5:F1}% {3,8} {4,5} {5,8} {6,10} {7}",
		row.TotalObjects,
		row.ActiveObjects,
		row.Utilization,
		row.ObjectSizeBytes,
		row.TotalSlabs,
		row.ObjectsPerSlab,
		$"{row.CacheSizeKiB}K",
		row.Name
	);
}

static double Percent(
	long numerator,
	long denominator
) {
	if ( 0 >= denominator ) {
		return 0.0;
	}

	return 100.0 * numerator / denominator;
}

static IReadOnlyList<SlabRow> CreateSnapshot(
	int generation
) {
	int pulse = generation % 7;
	return [
		new SlabRow(
			"dentry",
			11800 + ( pulse * 31 ),
			12500,
			192,
			98,
			94,
			128,
			6,
			75264
		),
		new SlabRow(
			"inode_cache",
			7600 + ( pulse * 17 ),
			8192,
			640,
			128,
			120,
			64,
			10,
			81920
		),
		new SlabRow(
			"kmalloc-256",
			4400 + ( pulse * 43 ),
			5120,
			256,
			80,
			72,
			64,
			4,
			20480
		),
		new SlabRow(
			"buffer_head",
			3050 + ( pulse * 13 ),
			3600,
			104,
			90,
			82,
			40,
			2,
			7200
		),
		new SlabRow(
			"task_struct",
			1280 + ( pulse * 5 ),
			1344,
			7040,
			168,
			161,
			8,
			14,
			94080
		),
		new SlabRow(
			"radix_tree_node",
			980 + ( pulse * 11 ),
			1152,
			576,
			72,
			65,
			16,
			3,
			13824
		)
	];
}

static void WriteLine(
	CursesWindow screen,
	int row,
	string text,
	CursesStyle style = default
) {
	ArgumentNullException.ThrowIfNull( screen );
	ArgumentNullException.ThrowIfNull( text );

	if ( row < 0 || row >= screen.Rows ) {
		return;
	}

	screen.Move(
		row,
		0
	);
	screen.Write(
		text,
		style
	);
}

internal enum SlabSortKey {
	ActiveObjects,
	ObjectsPerSlab,
	CacheSize,
	TotalSlabs,
	ActiveSlabs,
	Name,
	TotalObjects,
	PagesPerSlab,
	ObjectSize,
	Utilization
}

internal sealed class SlabRow {
	internal SlabRow(
		string name,
		long activeObjects,
		long totalObjects,
		long objectSizeBytes,
		long totalSlabs,
		long activeSlabs,
		long objectsPerSlab,
		long pagesPerSlab,
		long cacheSizeKiB
	) {
		ArgumentException.ThrowIfNullOrWhiteSpace( name );
		this.Name = name;
		this.ActiveObjects = activeObjects;
		this.TotalObjects = totalObjects;
		this.ObjectSizeBytes = objectSizeBytes;
		this.TotalSlabs = totalSlabs;
		this.ActiveSlabs = activeSlabs;
		this.ObjectsPerSlab = objectsPerSlab;
		this.PagesPerSlab = pagesPerSlab;
		this.CacheSizeKiB = cacheSizeKiB;
	}

	internal string Name {
		get;
	}

	internal long ActiveObjects {
		get;
	}

	internal long TotalObjects {
		get;
	}

	internal long ObjectSizeBytes {
		get;
	}

	internal long TotalSlabs {
		get;
	}

	internal long ActiveSlabs {
		get;
	}

	internal long ObjectsPerSlab {
		get;
	}

	internal long PagesPerSlab {
		get;
	}

	internal long CacheSizeKiB {
		get;
	}

	internal double Utilization {
		get {
			if ( 0 >= this.TotalObjects ) {
				return 0.0;
			}

			return 100.0 * this.ActiveObjects / this.TotalObjects;
		}
	}
}
