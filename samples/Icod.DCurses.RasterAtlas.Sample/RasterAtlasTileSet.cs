/*
	Icod.DCurses.RasterAtlas.Sample
	Embedded 16x16 artwork for the persistent raster-atlas path.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

namespace Icod.DCurses.RasterAtlas.Sample;

using System.Buffers.Binary;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Icod.Terminal;

internal enum RasterAtlasTileKind {
	Water,
	Grass,
	Forest,
	Road,
	Player
}

/// <summary>Loads, validates and scales the sample's embedded 16x16 RGBA PNG artwork.</summary>
internal sealed class RasterAtlasTileSet {
	private const int SourceWidth = 16;
	private const int SourceHeight = 16;
	private const int SourceBytesPerPixel = 4;
	private const string ResourcePrefix = "Icod.DCurses.RasterAtlas.Sample.Assets.";
	private static readonly Lazy<RasterAtlasTileSet> defaultSet = new( LoadDefault );

	private readonly Dictionary<RasterAtlasTileKind, byte[]> sourcePixels;
	private readonly Dictionary<( RasterAtlasTileKind Kind, int Width, int Height ), byte[]> scaledPixels = [];
	private readonly object cacheGate = new();

	private RasterAtlasTileSet( Dictionary<RasterAtlasTileKind, byte[]> sourcePixels ) {
		this.sourcePixels = sourcePixels;
	}

	internal static RasterAtlasTileSet Default => defaultSet.Value;

	internal TerminalRasterImage CreateImage(
		RasterAtlasTileKind kind,
		int width,
		int height
	) {
		return TerminalRasterImage.CreateRgb24(
			width,
			height,
			GetScaledPixels( kind, width, height )
		);
	}

	internal void CopyTo(
		RasterAtlasTileKind kind,
		int width,
		int height,
		byte[] destination,
		int destinationWidth,
		int tileRow,
		int tileColumn
	) {
		ArgumentNullException.ThrowIfNull( destination );
		byte[] source = GetScaledPixels( kind, width, height );
		int sourceRowLength = checked( width * 3 );
		for ( int row = 0; row < height; row++ ) {
			int destinationOffset = checked(
				( ( tileRow * height + row ) * destinationWidth + tileColumn * width ) * 3
			);
			Buffer.BlockCopy(
				source,
				row * sourceRowLength,
				destination,
				destinationOffset,
				sourceRowLength
			);
		}
	}

	private static RasterAtlasTileSet LoadDefault() {
		Assembly assembly = typeof( RasterAtlasTileSet ).Assembly;
		return new RasterAtlasTileSet( new Dictionary<RasterAtlasTileKind, byte[]> {
			[ RasterAtlasTileKind.Water ] = LoadPng( assembly, "001.png" ),
			[ RasterAtlasTileKind.Grass ] = LoadPng( assembly, "004.png" ),
			[ RasterAtlasTileKind.Forest ] = LoadPng( assembly, "006.png" ),
			[ RasterAtlasTileKind.Road ] = LoadPng( assembly, "022.png" ),
			[ RasterAtlasTileKind.Player ] = LoadPng( assembly, "031.png" )
		} );
	}

	private byte[] GetScaledPixels(
		RasterAtlasTileKind kind,
		int width,
		int height
	) {
		if ( width < 1 ) {
			throw new ArgumentOutOfRangeException( nameof( width ) );
		}
		if ( height < 1 ) {
			throw new ArgumentOutOfRangeException( nameof( height ) );
		}
		if ( !this.sourcePixels.TryGetValue( kind, out byte[]? source ) ) {
			throw new ArgumentOutOfRangeException( nameof( kind ) );
		}

		lock ( this.cacheGate ) {
			if ( this.scaledPixels.TryGetValue( ( kind, width, height ), out byte[]? cached ) ) {
				return cached;
			}

			byte[] scaled = new byte[ checked( width * height * 3 ) ];
			for ( int y = 0; y < height; y++ ) {
				int sourceY = y * SourceHeight / height;
				for ( int x = 0; x < width; x++ ) {
					int sourceX = x * SourceWidth / width;
					int sourceOffset = ( sourceY * SourceWidth + sourceX ) * SourceBytesPerPixel;
					int destinationOffset = ( y * width + x ) * 3;
					scaled[ destinationOffset ] = source[ sourceOffset ];
					scaled[ destinationOffset + 1 ] = source[ sourceOffset + 1 ];
					scaled[ destinationOffset + 2 ] = source[ sourceOffset + 2 ];
				}
			}
			this.scaledPixels.Add( ( kind, width, height ), scaled );
			return scaled;
		}
	}

	private static byte[] LoadPng(
		Assembly assembly,
		string fileName
	) {
		using Stream stream = assembly.GetManifestResourceStream( ResourcePrefix + fileName )
			?? throw new InvalidOperationException( $"Embedded raster-atlas tile {fileName} was not found." );
		return DecodePng( stream, fileName );
	}

	private static byte[] DecodePng(
		Stream stream,
		string fileName
	) {
		Span<byte> signature = stackalloc byte[ 8 ];
		stream.ReadExactly( signature );
		ReadOnlySpan<byte> expectedSignature = [ 137, 80, 78, 71, 13, 10, 26, 10 ];
		if ( !signature.SequenceEqual( expectedSignature ) ) {
			throw new InvalidDataException( $"Embedded tile {fileName} is not a PNG file." );
		}

		using MemoryStream compressed = new();
		bool hasHeader = false;
		bool hasEnd = false;
		Span<byte> chunkPrefix = stackalloc byte[ 8 ];
		Span<byte> crc = stackalloc byte[ 4 ];
		while ( !hasEnd ) {
			stream.ReadExactly( chunkPrefix );
			int length = BinaryPrimitives.ReadInt32BigEndian( chunkPrefix );
			if ( length < 0 || length > 1024 * 1024 ) {
				throw new InvalidDataException( $"Embedded tile {fileName} contains an invalid PNG chunk." );
			}
			string type = Encoding.ASCII.GetString( chunkPrefix[ 4.. ] );
			byte[] data = new byte[ length ];
			stream.ReadExactly( data );
			stream.ReadExactly( crc );

			switch ( type ) {
				case "IHDR":
					ValidateHeader( data, fileName );
					hasHeader = true;
					break;
				case "IDAT":
					compressed.Write( data );
					break;
				case "IEND":
					hasEnd = true;
					break;
			}
		}
		if ( !hasHeader || 0 == compressed.Length ) {
			throw new InvalidDataException( $"Embedded tile {fileName} is missing required PNG data." );
		}

		compressed.Position = 0;
		using ZLibStream inflater = new( compressed, CompressionMode.Decompress );
		using MemoryStream raw = new();
		inflater.CopyTo( raw );
		int rowLength = SourceWidth * SourceBytesPerPixel;
		byte[] filtered = raw.ToArray();
		if ( filtered.Length != SourceHeight * ( rowLength + 1 ) ) {
			throw new InvalidDataException( $"Embedded tile {fileName} has an unexpected decoded size." );
		}

		byte[] pixels = new byte[ SourceWidth * SourceHeight * SourceBytesPerPixel ];
		for ( int row = 0; row < SourceHeight; row++ ) {
			int filteredOffset = row * ( rowLength + 1 );
			int outputOffset = row * rowLength;
			ApplyFilter(
				filtered[ filteredOffset ],
				filtered.AsSpan( filteredOffset + 1, rowLength ),
				pixels.AsSpan( outputOffset, rowLength ),
				0 == row ? ReadOnlySpan<byte>.Empty : pixels.AsSpan( outputOffset - rowLength, rowLength ),
				fileName
			);
		}
		for ( int index = 3; index < pixels.Length; index += SourceBytesPerPixel ) {
			if ( byte.MaxValue != pixels[ index ] ) {
				throw new InvalidDataException( $"Embedded tile {fileName} must remain fully opaque." );
			}
		}
		return pixels;
	}

	private static void ValidateHeader(
		ReadOnlySpan<byte> header,
		string fileName
	) {
		bool valid = 13 == header.Length
			&& SourceWidth == BinaryPrimitives.ReadInt32BigEndian( header )
			&& SourceHeight == BinaryPrimitives.ReadInt32BigEndian( header[ 4.. ] )
			&& 8 == header[ 8 ]
			&& 6 == header[ 9 ]
			&& 0 == header[ 10 ]
			&& 0 == header[ 11 ]
			&& 0 == header[ 12 ];
		if ( !valid ) {
			throw new InvalidDataException(
				$"Embedded tile {fileName} must be a non-interlaced 16x16 8-bit RGBA PNG."
			);
		}
	}

	private static void ApplyFilter(
		byte filter,
		ReadOnlySpan<byte> encoded,
		Span<byte> output,
		ReadOnlySpan<byte> previous,
		string fileName
	) {
		for ( int index = 0; index < encoded.Length; index++ ) {
			byte left = index < SourceBytesPerPixel ? (byte)0 : output[ index - SourceBytesPerPixel ];
			byte above = previous.IsEmpty ? (byte)0 : previous[ index ];
			byte upperLeft = previous.IsEmpty || index < SourceBytesPerPixel
				? (byte)0
				: previous[ index - SourceBytesPerPixel ];
			int predictor = filter switch {
				0 => 0,
				1 => left,
				2 => above,
				3 => ( left + above ) / 2,
				4 => Paeth( left, above, upperLeft ),
				_ => throw new InvalidDataException( $"Embedded tile {fileName} uses an unknown PNG filter." )
			};
			output[ index ] = unchecked( (byte)( encoded[ index ] + predictor ) );
		}
	}

	private static byte Paeth(
		byte left,
		byte above,
		byte upperLeft
	) {
		int estimate = left + above - upperLeft;
		int leftDistance = Math.Abs( estimate - left );
		int aboveDistance = Math.Abs( estimate - above );
		int upperLeftDistance = Math.Abs( estimate - upperLeft );
		return leftDistance <= aboveDistance && leftDistance <= upperLeftDistance
			? left
			: aboveDistance <= upperLeftDistance ? above : upperLeft;
	}
}
