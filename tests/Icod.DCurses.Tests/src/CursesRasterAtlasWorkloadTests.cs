/*
	Icod.DCurses.Tests
	Automated test suite for Icod.DCurses.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

using Icod.Terminal;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed partial class CursesRasterAtlasTransactionIntegrationTests {
	[Theory]
	[InlineData( 1, false )]
	[InlineData( 4, false )]
	[InlineData( 16, false )]
	[InlineData( 64, false )]
	[InlineData( 121, false )]
	[InlineData( 256, false )]
	[InlineData( 1, true )]
	[InlineData( 4, true )]
	[InlineData( 16, true )]
	[InlineData( 64, true )]
	[InlineData( 121, true )]
	[InlineData( 256, true )]
	public async Task PackageShapedWorkloadsCoalesceOnlyFullCoverage(
		int changedTiles,
		bool rgba32
	) {
		AtlasTransport transport = new();
		await using CursesSession session = await OpenSessionAsync( transport );
		CursesRasterAtlas atlas = await CreateAtlasAsync( session, transport, Rgb24( 16, 16, 0 ) );
		await using ( atlas ) {
			CursesRasterAtlasTileUpdate[] updates = Enumerable.Range( 0, changedTiles ).Select(
				index => new CursesRasterAtlasTileUpdate(
					index / 16,
					index % 16,
					rgba32 ? Rgba32( 1, 1, (byte)index ) : Rgb24( 1, 1, (byte)index )
				)
			).ToArray();
			int start = transport.WriteCount;
			Task<CursesRasterAtlasPresentationResult> presentation = atlas.PresentAsync( updates ).AsTask();
			int regionOperationCount = 256 == changedTiles ? 1 : changedTiles;
			int operationCount = regionOperationCount + 2;
			for ( int offset = 0; offset < operationCount; offset++ ) {
				await transport.WaitForWriteCountAsync( start + offset + 1 );
				transport.PublishOk();
			}

			CursesRasterAtlasPresentationResult result = await presentation;
			Assert.Equal( CursesRasterAtlasPresentationStatus.Presented, result.Status );
			Assert.Equal( changedTiles, result.CompletedUpdateCount );
			Assert.Equal( operationCount, transport.WriteCount - start );
			Assert.Contains( "a=c", transport.GetAsciiWrite( start ), StringComparison.Ordinal );
			for ( int offset = 1; offset <= regionOperationCount; offset++ ) {
				Assert.Contains(
					rgba32 ? "a=f,f=32" : "a=f,f=24",
					transport.GetAsciiWrite( start + offset ),
					StringComparison.Ordinal
				);
			}
			if ( 256 == changedTiles ) {
				Assert.Contains(
					"s=16,v=16",
					transport.GetAsciiWrite( start + 1 ),
					StringComparison.Ordinal
				);
			}
			Assert.Contains(
				"a=a",
				transport.GetAsciiWrite( start + operationCount - 1 ),
				StringComparison.Ordinal
			);
		}
	}
}
