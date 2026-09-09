using System.Text.Json;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Guards the decision that 1.0 promotes the exact 0.9 public contract.</summary>
public sealed class PublicStableApiBaselineTests {
	[Fact]
	public void OneDotZeroBaselineMatchesFrozenZeroDotNineContract() {
		ApiBaseline zeroDotNine = ReadBaseline(
			"Public-API-Fingerprint-0.9.json"
		);
		ApiBaseline oneDotZero = ReadBaseline(
			"Public-API-Fingerprint-1.0.json"
		);

		Assert.Equal( "0.9.0", zeroDotNine.Release );
		Assert.Equal( "1.0.0", oneDotZero.Release );
		Assert.Equal( zeroDotNine.Sha256, oneDotZero.Sha256 );
		Assert.Equal(
			zeroDotNine.ExportedTypeCount,
			oneDotZero.ExportedTypeCount
		);
		Assert.Equal(
			zeroDotNine.ContractLineCount,
			oneDotZero.ContractLineCount
		);
		Assert.Equal(
			zeroDotNine.ExportedTypes,
			oneDotZero.ExportedTypes
		);
	}

	private static ApiBaseline ReadBaseline( string fileName ) {
		ArgumentException.ThrowIfNullOrEmpty( fileName );
		string path = Path.Combine(
			AppContext.BaseDirectory,
			fileName
		);
		using JsonDocument document = JsonDocument.Parse(
			File.ReadAllText( path )
		);
		JsonElement root = document.RootElement;
		return new ApiBaseline(
			root.GetProperty( "release" ).GetString()!,
			root.GetProperty( "sha256" ).GetString()!,
			root.GetProperty( "exportedTypeCount" ).GetInt32(),
			root.GetProperty( "contractLineCount" ).GetInt32(),
			root.GetProperty( "exportedTypes" )
				.EnumerateArray()
				.Select( static current => current.GetString()! )
				.ToArray()
		);
	}

	private sealed record ApiBaseline(
		string Release,
		string Sha256,
		int ExportedTypeCount,
		int ContractLineCount,
		string[] ExportedTypes
	);
}
