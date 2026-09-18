/*
	Icod.DCurses.Tests
	Automated test suite for Icod.DCurses.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Reflection;
using Icod.DCurses.Internal;
using Icod.Terminal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes the T2003 source and constructor boundary before the vertical cutover.</summary>
public sealed class T2003VerticalCutoverContractTests {
	private static readonly string[] MigratedProductionPaths = [
		"src/CursesPresentationCapabilities.cs",
		"src/Internal/CursesPresentationResolver.cs",
		"src/Internal/CursesLinePresentationResolver.cs",
		"src/Internal/CursesCursorMotionResolver.cs",
		"src/Internal/CursesRefreshEngine.cs",
		"src/Integration/CursesSession.PresentationCapabilities.cs",
		"src/Integration/CursesSession.Presentation.Terminal.cs",
		"src/Integration/CursesSession.Refresh.Terminal.cs"
	];

	private static readonly string[] ForbiddenTokens = [
		"Icod.TermInfo",
		"TerminalDescription",
		"StringCapability",
		"TermInfoParameter",
		"TermInfoOutput",
		"TerminalCapabilityWriter",
		"WriteTerminalStringAsync",
		"TerminalSessionCursesOutput"
	];

	[Fact]
	public void MigratedProductionPathsContainNoLegacyOutputOrTermInfoTokens() {
		string root = FindRepositoryRoot();

		foreach ( string path in MigratedProductionPaths ) {
			string source = File.ReadAllText(
				Path.Combine(
					root,
					path.Replace(
						'/',
						Path.DirectorySeparatorChar
					)
				)
			);

			foreach ( string token in ForbiddenTokens ) {
				Assert.DoesNotContain(
					token,
					source,
					StringComparison.Ordinal
				);
			}
		}
	}

	[Fact]
	public void RefreshIntegrationDelegatesSynchronizedOutputToTerminalTransaction() {
		string source = ReadRepositoryFile(
			"src/Integration/CursesSession.Refresh.Terminal.cs"
		);

		Assert.Contains(
			"UseSynchronizedOutput = this.Options.UseSynchronizedOutput",
			source,
			StringComparison.Ordinal
		);
		Assert.DoesNotContain(
			"AcquireSynchronizedOutputAsync",
			source,
			StringComparison.Ordinal
		);
	}

	[Fact]
	public void RefreshEngineUsesTerminalSessionConstructorWithoutLegacyDescription() {
		ConstructorInfo? terminalConstructor = typeof( CursesRefreshEngine ).GetConstructor(
			BindingFlags.Instance | BindingFlags.NonPublic,
			binder: null,
			[ typeof( TerminalSession ), typeof( bool ) ],
			modifiers: null
		);
		Assert.NotNull( terminalConstructor );
		Assert.DoesNotContain(
			typeof( CursesRefreshEngine ).GetConstructors( BindingFlags.Instance | BindingFlags.NonPublic ),
			constructor => constructor.GetParameters().Any(
				parameter => "Icod.TermInfo.TerminalDescription" == parameter.ParameterType.FullName
			)
		);
	}

	private static string ReadRepositoryFile( string relativePath ) {
		return File.ReadAllText(
			Path.Combine(
				FindRepositoryRoot(),
				relativePath.Replace(
					'/',
					Path.DirectorySeparatorChar
				)
			)
		);
	}

	private static string FindRepositoryRoot() {
		DirectoryInfo? current = new( AppContext.BaseDirectory );
		while ( current is not null ) {
			if ( File.Exists(
				Path.Combine(
					current.FullName,
					"Icod.DCurses.sln"
				)
			) ) {
				return current.FullName;
			}
			current = current.Parent;
		}

		throw new InvalidOperationException( "Repository root not found." );
	}
}
