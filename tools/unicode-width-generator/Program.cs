namespace Icod.DCurses.UnicodeWidthGenerator;

using System.Globalization;
using System.Text;

internal static class Program {
	private const string ExpectedUnicodeVersion = "17.0.0";

	private static readonly UnicodeRange[] defaultWideRanges = [
		new( 0x3400, 0x4DBF ),
		new( 0x4E00, 0x9FFF ),
		new( 0xF900, 0xFAFF ),
		new( 0x20000, 0x2FFFD ),
		new( 0x30000, 0x3FFFD )
	];

	public static int Main(
		string[] args
	) {
		ArgumentNullException.ThrowIfNull( args );
		if ( 2 != args.Length ) {
			Console.Error.WriteLine(
				"Usage: dotnet run --project tools/unicode-width-generator/"
					+ "Icod.DCurses.UnicodeWidthGenerator.csproj -- <EastAsianWidth.txt> <output.cs>"
			);
			return 2;
		}

		try {
			string inputPath = Path.GetFullPath( args[ 0 ] );
			string outputPath = Path.GetFullPath( args[ 1 ] );
			string[] lines = File.ReadAllLines( inputPath );
			ValidateVersion( lines );

			List<UnicodeRange> ambiguous = [];
			List<UnicodeRange> wide = [ .. defaultWideRanges ];

			foreach ( string rawLine in lines ) {
				string line = rawLine.Split( '#', 2 )[ 0 ].Trim();
				if ( 0 == line.Length ) {
					continue;
				}

				string[] fields = line.Split( ';', 2 );
				if ( 2 != fields.Length ) {
					throw new InvalidDataException(
						$"Malformed EastAsianWidth line: {rawLine}"
					);
				}

				UnicodeRange range = ParseRange( fields[ 0 ].Trim() );
				string property = fields[ 1 ].Trim();
				ValidateDefaultWideOverlap(
					range,
					property
				);
				switch ( property ) {
					case "A":
						ambiguous.Add( range );
						break;

					case "W":
					case "F":
						wide.Add( range );
						break;
				}
			}

			UnicodeRange[] mergedAmbiguous = MergeRanges( ambiguous );
			UnicodeRange[] mergedWide = MergeRanges( wide );

			Directory.CreateDirectory(
				Path.GetDirectoryName( outputPath )
					?? throw new InvalidOperationException(
						"The output path has no parent directory."
					)
			);
			File.WriteAllText(
				outputPath,
				GenerateSource(
					mergedAmbiguous,
					mergedWide
				),
				new UTF8Encoding( encoderShouldEmitUTF8Identifier: false )
			);

			Console.WriteLine(
				$"Generated Unicode {ExpectedUnicodeVersion} East Asian width data: "
					+ $"{mergedAmbiguous.Length} ambiguous ranges, "
					+ $"{mergedWide.Length} wide/fullwidth ranges."
			);
			return 0;
		} catch ( Exception exception ) when (
			exception is IOException
				or UnauthorizedAccessException
				or InvalidDataException
				or InvalidOperationException
				or FormatException
				or ArgumentException
		) {
			Console.Error.WriteLine( exception.Message );
			return 1;
		}
	}

	private static void ValidateVersion( IReadOnlyList<string> lines ) {
		ArgumentNullException.ThrowIfNull( lines );
		string expectedHeader = $"# EastAsianWidth-{ExpectedUnicodeVersion}.txt";
		if ( !lines.Any(
			line => string.Equals(
				line.Trim(),
				expectedHeader,
				StringComparison.Ordinal
			)
		) ) {
			throw new InvalidDataException(
				$"Expected Unicode {ExpectedUnicodeVersion} EastAsianWidth data."
			);
		}
	}

	private static UnicodeRange ParseRange( string field ) {
		ArgumentException.ThrowIfNullOrWhiteSpace( field );
		string[] bounds = field.Split( "..", StringSplitOptions.None );
		if ( bounds.Length is < 1 or > 2 ) {
			throw new InvalidDataException( $"Invalid code-point range '{field}'." );
		}

		int first = int.Parse(
			bounds[ 0 ],
			NumberStyles.AllowHexSpecifier,
			CultureInfo.InvariantCulture
		);
		int last = 1 == bounds.Length
			? first
			: int.Parse(
				bounds[ 1 ],
				NumberStyles.AllowHexSpecifier,
				CultureInfo.InvariantCulture
			)
		;
		if ( first < 0 || last < first || 0x10FFFF < last ) {
			throw new InvalidDataException( $"Invalid code-point range '{field}'." );
		}

		return new UnicodeRange( first, last );
	}

	private static void ValidateDefaultWideOverlap(
		UnicodeRange range,
		string property
	) {
		ArgumentException.ThrowIfNullOrWhiteSpace( property );
		if ( property is "W" or "F" ) {
			return;
		}

		foreach ( UnicodeRange defaultWide in defaultWideRanges ) {
			if ( range.Last < defaultWide.First
				|| defaultWide.Last < range.First ) {
				continue;
			}

			throw new InvalidDataException(
				$"EastAsianWidth property '{property}' overlaps Unicode's "
					+ $"default-wide range U+{defaultWide.First:X}..U+{defaultWide.Last:X}."
			);
		}
	}

	private static UnicodeRange[] MergeRanges(
		IEnumerable<UnicodeRange> ranges
	) {
		ArgumentNullException.ThrowIfNull( ranges );
		UnicodeRange[] ordered = ranges
			.OrderBy( range => range.First )
			.ThenBy( range => range.Last )
			.ToArray();
		if ( 0 == ordered.Length ) {
			return [];
		}

		List<UnicodeRange> merged = [ ordered[ 0 ] ];
		for ( int index = 1; index < ordered.Length; index++ ) {
			UnicodeRange next = ordered[ index ];
			UnicodeRange previous = merged[ ^1 ];
			if ( next.First <= previous.Last + 1 ) {
				merged[ ^1 ] = new UnicodeRange(
					previous.First,
					Math.Max( previous.Last, next.Last )
				);
				continue;
			}

			merged.Add( next );
		}

		return merged.ToArray();
	}

	private static string GenerateSource(
		IReadOnlyList<UnicodeRange> ambiguous,
		IReadOnlyList<UnicodeRange> wide
	) {
		ArgumentNullException.ThrowIfNull( ambiguous );
		ArgumentNullException.ThrowIfNull( wide );

		StringBuilder source = new();
		source.AppendLine( "namespace Icod.DCurses.Internal.Generated;" );
		source.AppendLine();
		source.AppendLine( "// <auto-generated />" );
		source.AppendLine(
			$"// Generated from Unicode {ExpectedUnicodeVersion} EastAsianWidth.txt."
		);
		source.AppendLine();
		source.AppendLine(
			"internal readonly record struct UnicodeWidthRange( int First, int Last );"
		);
		source.AppendLine();
		source.AppendLine( "internal static class UnicodeEastAsianWidthData {" );
		source.AppendLine(
			$"\tinternal const string UnicodeVersion = \"{ExpectedUnicodeVersion}\";"
		);
		source.AppendLine();
		AppendArray( source, "AmbiguousRanges", ambiguous );
		source.AppendLine();
		AppendArray( source, "WideOrFullwidthRanges", wide );
		source.AppendLine( "}" );
		return source.ToString();
	}

	private static void AppendArray(
		StringBuilder source,
		string name,
		IReadOnlyList<UnicodeRange> ranges
	) {
		ArgumentNullException.ThrowIfNull( source );
		ArgumentException.ThrowIfNullOrWhiteSpace( name );
		ArgumentNullException.ThrowIfNull( ranges );

		source.AppendLine(
			$"\tinternal static readonly UnicodeWidthRange[] {name} = ["
		);
		foreach ( UnicodeRange range in ranges ) {
			source.AppendLine(
				$"\t\tnew( 0x{range.First:X}, 0x{range.Last:X} ),"
			);
		}
		source.AppendLine( "\t];" );
	}

	private readonly record struct UnicodeRange(
		int First,
		int Last
	);
}
