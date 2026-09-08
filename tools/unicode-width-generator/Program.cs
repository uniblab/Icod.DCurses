namespace Icod.DCurses.UnicodeWidthGenerator;

using System.Globalization;
using System.Text;

internal static class Program {
	private const string ExpectedUnicodeVersion = "17.0.0";
	private const string ExpectedEmojiVersion = "17.0";

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

		try {
			if ( 2 == args.Length ) {
				GenerateEastAsianWidth(
					args[ 0 ],
					args[ 1 ]
				);
				return 0;
			}
			if ( 3 == args.Length
				&& string.Equals(
					args[ 0 ],
					"--emoji",
					StringComparison.Ordinal
				) ) {
				GenerateEmoji(
					args[ 1 ],
					args[ 2 ]
				);
				return 0;
			}

			WriteUsage();
			return 2;
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

	private static void GenerateEastAsianWidth(
		string inputArgument,
		string outputArgument ) {
		ArgumentException.ThrowIfNullOrWhiteSpace( inputArgument );
		ArgumentException.ThrowIfNullOrWhiteSpace( outputArgument );
		string inputPath = Path.GetFullPath( inputArgument );
		string outputPath = Path.GetFullPath( outputArgument );
		string[] lines = File.ReadAllLines( inputPath );
		ValidateEastAsianWidthVersion( lines );

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
		WriteGeneratedSource(
			outputPath,
			GenerateEastAsianWidthSource(
				mergedAmbiguous,
				mergedWide
			)
		);

		Console.WriteLine(
			$"Generated Unicode {ExpectedUnicodeVersion} East Asian width data: "
				+ $"{mergedAmbiguous.Length} ambiguous ranges, "
				+ $"{mergedWide.Length} wide/fullwidth ranges."
		);
	}

	private static void GenerateEmoji(
		string inputArgument,
		string outputArgument ) {
		ArgumentException.ThrowIfNullOrWhiteSpace( inputArgument );
		ArgumentException.ThrowIfNullOrWhiteSpace( outputArgument );
		string inputPath = Path.GetFullPath( inputArgument );
		string outputPath = Path.GetFullPath( outputArgument );
		string[] lines = File.ReadAllLines( inputPath );
		ValidateEmojiVersion( lines );

		List<UnicodeRange> emoji = [];
		foreach ( string rawLine in lines ) {
			string line = rawLine.Split( '#', 2 )[ 0 ].Trim();
			if ( 0 == line.Length ) {
				continue;
			}

			string[] fields = line.Split( ';', 2 );
			if ( 2 != fields.Length ) {
				throw new InvalidDataException(
					$"Malformed emoji-data line: {rawLine}"
				);
			}

			if ( !string.Equals(
				fields[ 1 ].Trim(),
				"Emoji",
				StringComparison.Ordinal
			) ) {
				continue;
			}

			emoji.Add( ParseRange( fields[ 0 ].Trim() ) );
		}

		UnicodeRange[] mergedEmoji = MergeRanges( emoji );
		if ( 0 == mergedEmoji.Length ) {
			throw new InvalidDataException(
				"The Unicode emoji data did not contain any Emoji property ranges."
			);
		}

		WriteGeneratedSource(
			outputPath,
			GenerateEmojiSource( mergedEmoji )
		);
		Console.WriteLine(
			$"Generated Unicode {ExpectedUnicodeVersion} Emoji property data: "
				+ $"{mergedEmoji.Length} ranges."
		);
	}

	private static void WriteUsage() {
		Console.Error.WriteLine(
			"Usage: dotnet run --project tools/unicode-width-generator/"
				+ "Icod.DCurses.UnicodeWidthGenerator.csproj -- "
				+ "<EastAsianWidth.txt> <output.cs>"
		);
		Console.Error.WriteLine(
			"   or: dotnet run --project tools/unicode-width-generator/"
				+ "Icod.DCurses.UnicodeWidthGenerator.csproj -- "
				+ "--emoji <emoji-data.txt> <output.cs>"
		);
	}

	private static void ValidateEastAsianWidthVersion(
		IReadOnlyList<string> lines ) {
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

	private static void ValidateEmojiVersion(
		IReadOnlyList<string> lines ) {
		ArgumentNullException.ThrowIfNull( lines );
		string expectedVersion = $"# Version: {ExpectedEmojiVersion}";
		if ( !lines.Any(
			line => string.Equals(
				line.Trim(),
				expectedVersion,
				StringComparison.Ordinal
			)
		) ) {
			throw new InvalidDataException(
				$"Expected Unicode {ExpectedUnicodeVersion} emoji data."
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
		string property ) {
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
		IEnumerable<UnicodeRange> ranges ) {
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

	private static void WriteGeneratedSource(
		string outputPath,
		string source ) {
		ArgumentException.ThrowIfNullOrWhiteSpace( outputPath );
		ArgumentNullException.ThrowIfNull( source );
		Directory.CreateDirectory(
			Path.GetDirectoryName( outputPath )
				?? throw new InvalidOperationException(
					"The output path has no parent directory."
				)
		);
		File.WriteAllText(
			outputPath,
			source,
			new UTF8Encoding( encoderShouldEmitUTF8Identifier: false )
		);
	}

	private static string GenerateEastAsianWidthSource(
		IReadOnlyList<UnicodeRange> ambiguous,
		IReadOnlyList<UnicodeRange> wide ) {
		ArgumentNullException.ThrowIfNull( ambiguous );
		ArgumentNullException.ThrowIfNull( wide );

		StringBuilder source = CreateSourceHeader(
			$"Generated from Unicode {ExpectedUnicodeVersion} EastAsianWidth.txt."
		);
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

	private static string GenerateEmojiSource(
		IReadOnlyList<UnicodeRange> emoji ) {
		ArgumentNullException.ThrowIfNull( emoji );

		StringBuilder source = CreateSourceHeader(
			$"Generated from Unicode {ExpectedUnicodeVersion} emoji-data.txt (Emoji property)."
		);
		source.AppendLine( "internal static class UnicodeEmojiData {" );
		source.AppendLine(
			$"\tinternal const string UnicodeVersion = \"{ExpectedUnicodeVersion}\";"
		);
		source.AppendLine();
		AppendArray( source, "EmojiRanges", emoji );
		source.AppendLine( "}" );
		return source.ToString();
	}

	private static StringBuilder CreateSourceHeader( string generatedFrom ) {
		ArgumentException.ThrowIfNullOrWhiteSpace( generatedFrom );
		StringBuilder source = new();
		source.AppendLine( "namespace Icod.DCurses.Internal.Generated;" );
		source.AppendLine();
		source.AppendLine( "// <auto-generated />" );
		source.AppendLine( $"// {generatedFrom}" );
		source.AppendLine();
		return source;
	}

	private static void AppendArray(
		StringBuilder source,
		string name,
		IReadOnlyList<UnicodeRange> ranges ) {
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
