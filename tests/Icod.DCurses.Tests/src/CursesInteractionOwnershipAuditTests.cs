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
using System.Text;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes the synchronous, callback-free, bounded ownership shape of 1.4 interaction routing.</summary>
public sealed class CursesInteractionOwnershipAuditTests {
	[Fact]
	public void RouterAndRegionPublicShapeRemainSynchronousAndCallbackFree() {
		Type[] types = [
			typeof( CursesInteractionRouter ),
			typeof( CursesInteractionRegion )
		];

		foreach ( Type type in types ) {
			Assert.Empty(
				type.GetEvents(
					BindingFlags.Public
					| BindingFlags.Instance
					| BindingFlags.DeclaredOnly
				)
			);

			MethodInfo[] methods = type.GetMethods(
				BindingFlags.Public
				| BindingFlags.Instance
				| BindingFlags.DeclaredOnly
			);
			foreach ( MethodInfo method in methods ) {
				foreach ( ParameterInfo parameter in method.GetParameters() ) {
					Assert.False(
						typeof( Delegate ).IsAssignableFrom( parameter.ParameterType ),
						$"{type.Name}.{method.Name} exposes delegate parameter {parameter.ParameterType}."
					);
				}

				Assert.False(
					IsAsyncReturnType( method.ReturnType ),
					$"{type.Name}.{method.Name} exposes asynchronous return type {method.ReturnType}."
				);
			}
		}
	}

	[Fact]
	public void RoutingSourcesContainNoHiddenTerminalIoOrBackgroundDispatch() {
		string root = FindRepositoryRoot();
		string[] files = [
			"src/CursesInteractionRouter.cs",
			"src/CursesInteractionRouter.Routing.cs",
			"src/CursesInteractionRegion.cs",
			"src/CursesInteractionRegion.Bindings.cs",
			"src/CursesInteractionHit.cs",
			"src/CursesInteractionResult.cs",
			"src/CursesKeyGesture.cs"
		];
		string[] forbidden = [
			"Icod.Terminal",
			"TerminalSession",
			"WriteAsync(",
			"ReadAsync(",
			"AcquirePointerShapeAsync(",
			"Task.Run("
		];

		foreach ( string file in files ) {
			string source = File.ReadAllText(
				Path.Combine(
					root,
					file.Replace(
						'/',
						Path.DirectorySeparatorChar
					)
				)
			);
			foreach ( string marker in forbidden ) {
				Assert.DoesNotContain(
					marker,
					source,
					StringComparison.Ordinal
				);
			}
		}
	}

	[Fact]
	public void FrozenBoundsRemainExact() {
		Assert.Equal(
			4096,
			CursesInteractionRouter.MaximumRegions
		);
		Assert.Equal(
			256,
			CursesInteractionRouter.MaximumRegionGestureBindings
		);
		Assert.Equal(
			1024,
			CursesInteractionRouter.MaximumGlobalGestureBindings
		);
		Assert.Equal(
			16384,
			CursesInteractionRouter.MaximumGestureBindings
		);
	}

	[Fact]
	public void PerRegionGestureBoundIsEnforcedWithoutReplacingExistingBindings() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			) {
				IsFocusable = true
			}
		);
		Assert.True( router.Focus( region ) );

		CursesCommand finalCommand = new( "region.final" );
		for ( int index = 0; index < CursesInteractionRouter.MaximumRegionGestureBindings; index++ ) {
			CursesCommand command = index == CursesInteractionRouter.MaximumRegionGestureBindings - 1
				? finalCommand
				: new CursesCommand( $"region.{index:D3}" )
			;
			region.BindGesture(
				CursesKeyGesture.ForCharacter( new Rune( 0x5000 + index ) ),
				command
			);
		}

		Assert.Throws<InvalidOperationException>(
			() => region.BindGesture(
				CursesKeyGesture.ForCharacter( new Rune( 0x6000 ) ),
				new CursesCommand( "region.overflow" )
			)
		);
		Assert.Equal(
			finalCommand,
			router.Route(
				CursesInputEvent.FromText(
					new Rune( 0x5000 + CursesInteractionRouter.MaximumRegionGestureBindings - 1 )
				)
			).Command
		);
	}

	[Fact]
	public void GlobalGestureBoundIsEnforcedWithoutReplacingExistingBindings() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		CursesCommand finalCommand = new( "global.final" );
		for ( int index = 0; index < CursesInteractionRouter.MaximumGlobalGestureBindings; index++ ) {
			CursesCommand command = index == CursesInteractionRouter.MaximumGlobalGestureBindings - 1
				? finalCommand
				: new CursesCommand( $"global.{index:D4}" )
			;
			router.BindGlobalGesture(
				CursesKeyGesture.ForCharacter( new Rune( 0x7000 + index ) ),
				command
			);
		}

		Assert.Throws<InvalidOperationException>(
			() => router.BindGlobalGesture(
				CursesKeyGesture.ForCharacter( new Rune( 0x8000 ) ),
				new CursesCommand( "global.overflow" )
			)
		);
		Assert.Equal(
			finalCommand,
			router.Route(
				CursesInputEvent.FromText(
					new Rune( 0x7000 + CursesInteractionRouter.MaximumGlobalGestureBindings - 1 )
				)
			).Command
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

	private static bool IsAsyncReturnType(
		Type returnType
	) {
		ArgumentNullException.ThrowIfNull( returnType );
		if ( typeof( Task ) == returnType
			|| typeof( ValueTask ) == returnType ) {
			return true;
		}
		if ( returnType.IsGenericType ) {
			Type genericDefinition = returnType.GetGenericTypeDefinition();
			return typeof( Task<> ) == genericDefinition
				|| typeof( ValueTask<> ) == genericDefinition
				|| typeof( IAsyncEnumerable<> ) == genericDefinition;
		}
		return false;
	}
}
