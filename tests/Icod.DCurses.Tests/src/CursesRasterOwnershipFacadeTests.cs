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
using System.Runtime.CompilerServices;
using Icod.Terminal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes T1602 ownership projection and facade validation behavior.</summary>
public sealed class CursesRasterOwnershipFacadeTests {
	[Theory]
	[InlineData( TerminalRasterOwnershipStatus.Current, TerminalRasterOwnershipLossReason.None, "Current", "None" )]
	[InlineData( TerminalRasterOwnershipStatus.Stale, TerminalRasterOwnershipLossReason.SessionStateLost, "Stale", "SessionStateLost" )]
	[InlineData( TerminalRasterOwnershipStatus.Stale, TerminalRasterOwnershipLossReason.ResourceMissing, "Stale", "ResourceMissing" )]
	[InlineData( TerminalRasterOwnershipStatus.Released, TerminalRasterOwnershipLossReason.ParentPlacementLost, "Released", "ParentPlacementLost" )]
	[InlineData( TerminalRasterOwnershipStatus.Released, TerminalRasterOwnershipLossReason.AncestorReleased, "Released", "AncestorReleased" )]
	[InlineData( TerminalRasterOwnershipStatus.Released, TerminalRasterOwnershipLossReason.ResourceReleased, "Released", "ResourceReleased" )]
	[InlineData( TerminalRasterOwnershipStatus.Disposed, TerminalRasterOwnershipLossReason.ExplicitDisposal, "Disposed", "ExplicitDisposal" )]
	public void OwnershipProjectionMapsTerminalVocabularyBySemanticName(
		TerminalRasterOwnershipStatus status,
		TerminalRasterOwnershipLossReason reason,
		string expectedStatus,
		string expectedReason
	) {
		Assembly assembly = typeof( CursesSession ).Assembly;
		Type mapperType = RequireType(
			assembly,
			"Icod.DCurses.Internal.CursesRasterOwnershipMapper"
		);
		MethodInfo? method = mapperType.GetMethod(
			"FromTerminal",
			BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
			binder: null,
			types: [ typeof( TerminalRasterOwnershipState ) ],
			modifiers: null
		);
		Assert.NotNull( method );

		object? projected = method!.Invoke(
			null,
			[ new TerminalRasterOwnershipState( status, reason ) ]
		);
		Assert.NotNull( projected );
		Type stateType = projected!.GetType();
		Assert.Equal(
			expectedStatus,
			stateType.GetProperty( "Status" )?.GetValue( projected )?.ToString()
		);
		Assert.Equal(
			expectedReason,
			stateType.GetProperty( "LossReason" )?.GetValue( projected )?.ToString()
		);
	}

	[Theory]
	[InlineData( 0, 1 )]
	[InlineData( 1, 0 )]
	[InlineData( -1, 1 )]
	[InlineData( 1, -1 )]
	[InlineData( 257, 1 )]
	[InlineData( 1, 257 )]
	public void PlaceholderDimensionsAreRejectedBeforeTerminalStateIsAccessed(
		int columns,
		int rows
	) {
		Assembly assembly = typeof( CursesSession ).Assembly;
		Type resourceType = RequireType(
			assembly,
			"Icod.DCurses.CursesRasterResource"
		);
		object resource = RuntimeHelpers.GetUninitializedObject( resourceType );
		MethodInfo? method = resourceType.GetMethod(
			"CreatePlaceholderAsync",
			BindingFlags.Instance | BindingFlags.Public,
			binder: null,
			types: [ typeof( int ), typeof( int ), typeof( CancellationToken ) ],
			modifiers: null
		);
		Assert.NotNull( method );

		TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
			() => method!.Invoke(
				resource,
				[ columns, rows, CancellationToken.None ]
			)
		);
		Assert.IsType<ArgumentOutOfRangeException>( exception.InnerException );
	}

	[Fact]
	public async Task EmptyOwnershipFacadesBehaveAsDisposedAndDisposalIsIdempotent() {
		Assembly assembly = typeof( CursesSession ).Assembly;
		foreach ( string fullName in new[] {
			"Icod.DCurses.CursesRasterResource",
			"Icod.DCurses.CursesRasterPlaceholder"
		} ) {
			Type facadeType = RequireType(
				assembly,
				fullName
			);
			object facade = RuntimeHelpers.GetUninitializedObject( facadeType );
			object? state = facadeType.GetProperty( "OwnershipState" )?.GetValue( facade );
			Assert.NotNull( state );
			Assert.Equal(
				"Disposed",
				state!.GetType().GetProperty( "Status" )?.GetValue( state )?.ToString()
			);
			Assert.Equal(
				"ExplicitDisposal",
				state.GetType().GetProperty( "LossReason" )?.GetValue( state )?.ToString()
			);

			MethodInfo? dispose = facadeType.GetMethod( "DisposeAsync" );
			Assert.NotNull( dispose );
			ValueTask first = Assert.IsType<ValueTask>( dispose!.Invoke( facade, null ) );
			await first.ConfigureAwait( false );
			ValueTask second = Assert.IsType<ValueTask>( dispose.Invoke( facade, null ) );
			await second.ConfigureAwait( false );
		}
	}

	private static Type RequireType(
		Assembly assembly,
		string fullName
	) {
		ArgumentNullException.ThrowIfNull( assembly );
		ArgumentException.ThrowIfNullOrWhiteSpace( fullName );
		Type? type = assembly.GetType(
			fullName,
			throwOnError: false,
			ignoreCase: false
		);
		Assert.True(
			type is not null,
			$"Required T1602 type {fullName} is missing."
		);
		return type!;
	}
}
