namespace Icod.DCurses.PackageVerifier;

using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

internal static class Program {
	private const string PackageId = "Icod.DCurses";
	private const string RepositoryUrl = "https://github.com/uniblab/Icod.DCurses";
	private const string TargetFramework = "net10.0";
	private const string TerminalDependencyVersion = "0.3.0";
	private const string TermInfoDependencyVersion = "1.4.1";

	public static int Main(
		string[] args
	) {
		ArgumentNullException.ThrowIfNull( args );

		if ( 1 < args.Length ) {
			Console.Error.WriteLine(
				"Usage: dotnet run --project tools/package-verifier/"
					+ "Icod.DCurses.PackageVerifier.csproj -- [artifact-directory]"
			);
			return 2;
		}

		try {
			string root = FindRepositoryRoot();
			string artifactDirectory = 0 == args.Length
				? Path.Combine(
					root,
					"artifacts"
				)
				: Path.GetFullPath(
					args[ 0 ],
					root
				)
			;

			(
				string PackageVersion,
				string AssemblyVersion
			) projectMetadata = ReadProjectMetadata( root );

			string packagePath = Path.Combine(
				artifactDirectory,
				$"{PackageId}.{projectMetadata.PackageVersion}.nupkg"
			);
			string symbolsPath = Path.Combine(
				artifactDirectory,
				$"{PackageId}.{projectMetadata.PackageVersion}.snupkg"
			);

			Require(
				File.Exists( packagePath ),
				$"Package not found: {packagePath}"
			);
			Require(
				File.Exists( symbolsPath ),
				$"Symbol package not found: {symbolsPath}"
			);

			VerifyPrimaryPackage(
				packagePath,
				projectMetadata.PackageVersion,
				projectMetadata.AssemblyVersion
			);
			VerifySymbolPackage( symbolsPath );

			Console.WriteLine(
				"Verified package structure, metadata, dependency closure, "
					+ "assembly identity, XML documentation, and portable symbols "
					+ $"for {projectMetadata.PackageVersion}."
			);
			return 0;
		} catch ( Exception exception ) when (
			exception is IOException
				or UnauthorizedAccessException
				or InvalidDataException
				or InvalidOperationException
				or BadImageFormatException
				or XmlException
		) {
			Console.Error.WriteLine( exception.Message );
			return 1;
		}
	}

	private static string FindRepositoryRoot() {
		string[] starts = [
			Directory.GetCurrentDirectory(),
			AppContext.BaseDirectory
		];

		foreach ( string start in starts ) {
			DirectoryInfo? current = new( start );
			while ( null != current ) {
				if (
					File.Exists(
						Path.Combine(
							current.FullName,
							"Icod.DCurses.csproj"
						)
					)
					&& Directory.Exists(
						Path.Combine(
							current.FullName,
							"src"
						)
					)
				) {
					return current.FullName;
				}

				current = current.Parent;
			}
		}

		throw new InvalidOperationException(
			"Unable to locate the Icod.DCurses repository root."
		);
	}

	private static (
		string PackageVersion,
		string AssemblyVersion
	) ReadProjectMetadata(
		string root
	) {
		ArgumentException.ThrowIfNullOrWhiteSpace( root );

		XDocument project = XDocument.Load(
			Path.Combine(
				root,
				"Icod.DCurses.csproj"
			),
			LoadOptions.None
		);

		string? version = ReadProjectProperty(
			project,
			"Version"
		);
		string? packageVersion = ReadProjectProperty(
			project,
			"PackageVersion"
		);
		string? assemblyVersion = ReadProjectProperty(
			project,
			"AssemblyVersion"
		);

		Require(
			!string.IsNullOrWhiteSpace( version )
				&& !string.IsNullOrWhiteSpace( packageVersion )
				&& string.Equals(
					version,
					packageVersion,
					StringComparison.Ordinal
				),
			"Version and PackageVersion must both be present and identical."
		);
		Require(
			!string.IsNullOrWhiteSpace( assemblyVersion )
				&& Version.TryParse(
					assemblyVersion,
					out _
				),
			"AssemblyVersion must be present and valid."
		);

		return (
			packageVersion!,
			assemblyVersion!
		);
	}

	private static string? ReadProjectProperty(
		XDocument project,
		string name
	) {
		ArgumentNullException.ThrowIfNull( project );
		ArgumentException.ThrowIfNullOrWhiteSpace( name );

		return project
			.Descendants()
			.FirstOrDefault(
				element => name == element.Name.LocalName
			)
			?.Value
			.Trim();
	}

	private static void VerifyPrimaryPackage(
		string packagePath,
		string expectedVersion,
		string expectedAssemblyVersion
	) {
		ArgumentException.ThrowIfNullOrWhiteSpace( packagePath );
		ArgumentException.ThrowIfNullOrWhiteSpace( expectedVersion );
		ArgumentException.ThrowIfNullOrWhiteSpace( expectedAssemblyVersion );

		using ZipArchive package = ZipFile.OpenRead( packagePath );
		HashSet<string> names = package.Entries
			.Select( entry => entry.FullName )
			.ToHashSet( StringComparer.Ordinal );

		string assemblyPath = $"lib/{TargetFramework}/{PackageId}.dll";
		string documentationPath = $"lib/{TargetFramework}/{PackageId}.xml";
		string[] required = [
			"README.md",
			"icon.png",
			"icod_tui_toolchain.jpg",
			assemblyPath,
			documentationPath
		];

		string[] missing = required
			.Where( name => !names.Contains( name ) )
			.OrderBy(
				name => name,
				StringComparer.Ordinal
			)
			.ToArray();
		Require(
			0 == missing.Length,
			"Primary package is missing required entries: "
				+ string.Join(
					", ",
					missing
				)
		);

		foreach ( string requiredName in required ) {
			Require(
				0 < package.GetEntry( requiredName )!.Length,
				$"{requiredName} is empty in the primary package."
			);
		}

		Require(
			!names.Any(
				name => name.EndsWith(
					".pdb",
					StringComparison.OrdinalIgnoreCase
				)
			),
			"Primary package unexpectedly contains portable PDB payloads."
		);
		Require(
			!names.Any(
				name => name.StartsWith(
					"runtimes/",
					StringComparison.Ordinal
				)
			),
			"Primary package unexpectedly contains a runtimes/ payload."
		);
		Require(
			!names.Any( HasNativeLibraryExtension ),
			"Primary package unexpectedly contains a native library payload."
		);
		Require(
			!names.Any( IsRepositoryOnlyEntry ),
			"Primary package unexpectedly contains repository-only tests, "
				+ "samples, tools, docs, or workflows."
		);

		string[] dlls = names
			.Where(
				name => name.EndsWith(
					".dll",
					StringComparison.OrdinalIgnoreCase
				)
			)
			.OrderBy(
				name => name,
				StringComparer.Ordinal
			)
			.ToArray();
		Require(
			1 == dlls.Length
				&& assemblyPath == dlls[ 0 ],
			"Primary package contains unexpected DLL payloads: "
				+ string.Join(
					", ",
					dlls
				)
		);

		VerifyAssemblyIdentity(
			package,
			assemblyPath,
			expectedAssemblyVersion
		);
		VerifyDocumentation(
			package,
			documentationPath
		);
		VerifyNuspec(
			package,
			expectedVersion
		);
	}

	private static void VerifyAssemblyIdentity(
		ZipArchive package,
		string assemblyPath,
		string expectedAssemblyVersion
	) {
		ArgumentNullException.ThrowIfNull( package );
		ArgumentException.ThrowIfNullOrWhiteSpace( assemblyPath );
		ArgumentException.ThrowIfNullOrWhiteSpace( expectedAssemblyVersion );

		ZipArchiveEntry? entry = package.GetEntry( assemblyPath );
		Require(
			null != entry,
			$"Primary package is missing {assemblyPath}."
		);

		string temporaryPath = Path.Combine(
			Path.GetTempPath(),
			$"{PackageId}-{Guid.NewGuid():N}.dll"
		);

		try {
			using (
				Stream input = entry!.Open()
			) {
				using FileStream output = File.Create( temporaryPath );
				input.CopyTo( output );
			}

			AssemblyName assemblyName = AssemblyName.GetAssemblyName( temporaryPath );
			Require(
				PackageId == assemblyName.Name,
				$"{assemblyPath} identifies unexpected assembly '{assemblyName.Name}'."
			);
			Require(
				expectedAssemblyVersion == assemblyName.Version?.ToString(),
				$"{assemblyPath} has unexpected assembly version "
					+ $"'{assemblyName.Version}'."
			);

			byte[]? publicKeyToken = assemblyName.GetPublicKeyToken();
			Require(
				null == publicKeyToken
					|| 0 == publicKeyToken.Length,
				$"{assemblyPath} unexpectedly has a strong-name public key token."
			);
		} finally {
			if ( File.Exists( temporaryPath ) ) {
				File.Delete( temporaryPath );
			}
		}
	}

	private static void VerifyDocumentation(
		ZipArchive package,
		string documentationPath
	) {
		ArgumentNullException.ThrowIfNull( package );
		ArgumentException.ThrowIfNullOrWhiteSpace( documentationPath );

		ZipArchiveEntry? entry = package.GetEntry( documentationPath );
		Require(
			null != entry,
			$"Primary package is missing {documentationPath}."
		);

		using Stream stream = entry!.Open();
		XDocument documentation = XDocument.Load(
			stream,
			LoadOptions.None
		);
		string? assemblyName = documentation
			.Descendants()
			.FirstOrDefault(
				element => "assembly" == element.Name.LocalName
			)
			?.Elements()
			.FirstOrDefault(
				element => "name" == element.Name.LocalName
			)
			?.Value;

		Require(
			PackageId == assemblyName,
			$"{documentationPath} identifies unexpected assembly '{assemblyName}'."
		);

		int memberCount = documentation
			.Descendants()
			.Count(
				element => "member" == element.Name.LocalName
			);
		Require(
			0 < memberCount,
			$"{documentationPath} contains no documented members."
		);
	}

	private static void VerifyNuspec(
		ZipArchive package,
		string expectedVersion
	) {
		ArgumentNullException.ThrowIfNull( package );
		ArgumentException.ThrowIfNullOrWhiteSpace( expectedVersion );

		ZipArchiveEntry[] nuspecs = package.Entries
			.Where(
				entry => entry.FullName.EndsWith(
					".nuspec",
					StringComparison.OrdinalIgnoreCase
				)
			)
			.ToArray();
		Require(
			1 == nuspecs.Length,
			$"Expected one nuspec, found {nuspecs.Length}."
		);

		using Stream stream = nuspecs[ 0 ].Open();
		XDocument nuspec = XDocument.Load(
			stream,
			LoadOptions.None
		);
		XElement? metadata = nuspec
			.Descendants()
			.FirstOrDefault(
				element => "metadata" == element.Name.LocalName
			);
		Require(
			null != metadata,
			"Package nuspec has no metadata element."
		);

		Require(
			PackageId == GetMetadataText(
				metadata!,
				"id"
			),
			"Unexpected package id."
		);
		Require(
			expectedVersion == GetMetadataText(
				metadata!,
				"version"
			),
			"Unexpected package version."
		);
		Require(
			PackageId == GetMetadataText(
				metadata!,
				"title"
			),
			"Unexpected package title."
		);
		Require(
			"Timothy J. Bruce" == GetMetadataText(
				metadata!,
				"authors"
			),
			"Unexpected package authors."
		);
		Require(
			RepositoryUrl == GetMetadataText(
				metadata!,
				"projectUrl"
			),
			"Unexpected package project URL."
		);
		Require(
			"README.md" == GetMetadataText(
				metadata!,
				"readme"
			),
			"Package metadata does not identify README.md."
		);
		Require(
			"icon.png" == GetMetadataText(
				metadata!,
				"icon"
			),
			"Package metadata does not identify icon.png."
		);
		Require(
			string.Equals(
				GetMetadataText(
					metadata!,
					"requireLicenseAcceptance"
				),
				"true",
				StringComparison.OrdinalIgnoreCase
			),
			"Package must require license acceptance."
		);
		Require(
			!string.IsNullOrWhiteSpace(
				GetMetadataText(
					metadata!,
					"description"
				)
			),
			"Package description is missing."
		);
		Require(
			!string.IsNullOrWhiteSpace(
				GetMetadataText(
					metadata!,
					"tags"
				)
			),
			"Package tags are missing."
		);

		XElement? license = metadata!
			.Elements()
			.FirstOrDefault(
				element => "license" == element.Name.LocalName
			);
		Require(
			null != license,
			"Package metadata has no license element."
		);
		Require(
			"expression" == license!.Attribute( "type" )?.Value,
			"Package license is not an expression."
		);
		Require(
			"LGPL-3.0-or-later" == license!.Value,
			"Unexpected package license expression."
		);

		XElement? repository = metadata
			.Descendants()
			.FirstOrDefault(
				element => "repository" == element.Name.LocalName
			);
		Require(
			null != repository,
			"Package metadata has no repository element."
		);
		Require(
			"git" == repository!.Attribute( "type" )?.Value,
			"Repository metadata is not git."
		);
		Require(
			RepositoryUrl == repository!.Attribute( "url" )?.Value,
			"Unexpected repository URL in package metadata."
		);

		string? commit = repository!.Attribute( "commit" )?.Value;
		if ( !string.IsNullOrWhiteSpace( commit ) ) {
			Require(
				Regex.IsMatch(
					commit,
					"^[0-9a-fA-F]{40}$",
					RegexOptions.CultureInvariant
				),
				$"Repository metadata has invalid commit id '{commit}'."
			);
		}

		VerifyDependencies( metadata! );
	}

	private static void VerifyDependencies(
		XElement metadata
	) {
		ArgumentNullException.ThrowIfNull( metadata );

		XElement? dependencies = metadata
			.Elements()
			.FirstOrDefault(
				element => "dependencies" == element.Name.LocalName
			);
		Require(
			null != dependencies,
			"Package metadata has no dependencies element."
		);
		Require(
			!dependencies!.Elements().Any(
				element => "dependency" == element.Name.LocalName
			),
			"Package dependencies must remain grouped by target framework."
		);

		XElement[] groups = dependencies!.Elements()
			.Where(
				element => "group" == element.Name.LocalName
			)
			.ToArray();
		Require(
			1 == groups.Length,
			$"Expected one dependency group, found {groups.Length}."
		);

		string framework = groups[ 0 ].Attribute( "targetFramework" )?.Value
			?? string.Empty;
		Require(
			framework.Contains(
				"10.0",
				StringComparison.OrdinalIgnoreCase
			),
			$"Unexpected dependency target framework '{framework}'."
		);

		XElement[] packageDependencies = groups[ 0 ]
			.Elements()
			.Where(
				element => "dependency" == element.Name.LocalName
			)
			.ToArray();
		Require(
			2 == packageDependencies.Length,
			"DCurses net10.0 package group must contain exactly two runtime dependencies."
		);

		VerifyDependency(
			packageDependencies,
			"Icod.Terminal",
			TerminalDependencyVersion
		);
		VerifyDependency(
			packageDependencies,
			"Icod.TermInfo",
			TermInfoDependencyVersion
		);
	}

	private static void VerifyDependency(
		IEnumerable<XElement> dependencies,
		string packageId,
		string expectedVersion
	) {
		ArgumentNullException.ThrowIfNull( dependencies );
		ArgumentException.ThrowIfNullOrWhiteSpace( packageId );
		ArgumentException.ThrowIfNullOrWhiteSpace( expectedVersion );

		XElement[] matches = dependencies
			.Where(
				dependency => string.Equals(
					dependency.Attribute( "id" )?.Value,
					packageId,
					StringComparison.Ordinal
				)
			)
			.ToArray();
		Require(
			1 == matches.Length,
			$"Package must reference {packageId} exactly once."
		);
		Require(
			expectedVersion == matches[ 0 ].Attribute( "version" )?.Value,
			$"Package references unexpected {packageId} version."
		);
	}

	private static void VerifySymbolPackage(
		string symbolsPath
	) {
		ArgumentException.ThrowIfNullOrWhiteSpace( symbolsPath );

		using ZipArchive package = ZipFile.OpenRead( symbolsPath );
		string pdbPath = $"lib/{TargetFramework}/{PackageId}.pdb";
		ZipArchiveEntry? pdb = package.GetEntry( pdbPath );
		Require(
			null != pdb,
			$"Symbol package is missing {pdbPath}."
		);
		Require(
			0 < pdb!.Length,
			$"{pdbPath} is empty."
		);

		ZipArchiveEntry[] pdbs = package.Entries
			.Where(
				entry => entry.FullName.EndsWith(
					".pdb",
					StringComparison.OrdinalIgnoreCase
				)
			)
			.ToArray();
		Require(
			1 == pdbs.Length,
			$"Expected one PDB in symbol package, found {pdbs.Length}."
		);

		using Stream stream = pdb!.Open();
		Span<byte> signature = stackalloc byte[ 4 ];
		int read = stream.Read( signature );
		Require(
			4 == read
				&& (byte)'B' == signature[ 0 ]
				&& (byte)'S' == signature[ 1 ]
				&& (byte)'J' == signature[ 2 ]
				&& (byte)'B' == signature[ 3 ],
			$"{pdbPath} is not a portable PDB."
		);
	}

	private static string GetMetadataText(
		XElement metadata,
		string name
	) {
		ArgumentNullException.ThrowIfNull( metadata );
		ArgumentException.ThrowIfNullOrWhiteSpace( name );

		return metadata
			.Elements()
			.FirstOrDefault(
				element => name == element.Name.LocalName
			)
			?.Value
			?? string.Empty;
	}

	private static bool HasNativeLibraryExtension(
		string name
	) {
		ArgumentException.ThrowIfNullOrWhiteSpace( name );

		return name.EndsWith(
			".so",
			StringComparison.OrdinalIgnoreCase
		)
			|| name.EndsWith(
				".dylib",
				StringComparison.OrdinalIgnoreCase
			);
	}

	private static bool IsRepositoryOnlyEntry(
		string name
	) {
		ArgumentException.ThrowIfNullOrWhiteSpace( name );

		string[] prefixes = [
			".github/",
			"docs/",
			"samples/",
			"tests/",
			"tools/"
		];

		return prefixes.Any(
			prefix => name.StartsWith(
				prefix,
				StringComparison.OrdinalIgnoreCase
			)
		)
			|| name.EndsWith(
				"-Development-Roadmap.md",
				StringComparison.OrdinalIgnoreCase
			);
	}

	private static void Require(
		bool condition,
		string message
	) {
		ArgumentException.ThrowIfNullOrWhiteSpace( message );

		if ( !condition ) {
			throw new InvalidDataException( message );
		}
	}
}
