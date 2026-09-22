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
using Icod.DCurses.Internal;
using Icod.Terminal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes the T1603 sparse retained-raster representation cost and allocation shape.</summary>
public sealed class CursesRasterRepresentationBaselineTests {
	[Fact]
	public void UnconditionalRasterReferenceWouldRepeatTheRejectedFourMiBLargePadTax() {
		Assert.Equal( 8, IntPtr.Size );
		const long Rows = 2_048;
		const long Columns = 256;
		long cellCount = Rows * Columns;
		long permanentReferencePayload = cellCount * IntPtr.Size;

		Assert.Equal( 524_288L, cellCount );
		Assert.Equal( 4L * 1024L * 1024L, permanentReferencePayload );
	}

	[Fact]
	public void TenSparseRasterRowsRetainTheEstablishedThirtySixKiBReferenceShape() {
		Assert.Equal( 8, IntPtr.Size );
		const int Rows = 2_048;
		const int Columns = 256;
		const int RasterRows = 10;
		CursesSparseCellPlane<object> plane = new( Columns, Rows );
		object marker = new();

		for ( int index = 0; index < RasterRows; index++ ) {
			plane.Set(
				index * 197,
				index,
				marker
			);
		}

		Assert.Equal( RasterRows, plane.AllocatedRowCount );
		long materializedSlots = Rows + ( (long)RasterRows * Columns );
		Assert.Equal( 4_608L, materializedSlots );
		Assert.Equal( 36L * 1024L, materializedSlots * IntPtr.Size );
	}

	[Fact]
	public void VirtualScreenRasterStorageIsLazyAndReleasedWhenEmpty() {
		CursesVirtualScreen screen = new( 256, 2_048 );
		PropertyInfo storageAllocated = RequireInternalProperty(
			typeof( CursesVirtualScreen ),
			"RasterStorageAllocated"
		);
		PropertyInfo allocatedRows = RequireInternalProperty(
			typeof( CursesVirtualScreen ),
			"RasterAllocatedRowCount"
		);
		MethodInfo setRasterCell = RequirePublicMethod(
			typeof( CursesVirtualScreen ),
			"SetRasterCell"
		);
		CursesRasterCell token = CreateLogicalRasterCell();

		Assert.False( Assert.IsType<bool>( storageAllocated.GetValue( screen ) ) );
		Assert.Equal( 0, Assert.IsType<int>( allocatedRows.GetValue( screen ) ) );

		_ = setRasterCell.Invoke(
			screen,
			[ 197, 7, token ]
		);

		Assert.True( Assert.IsType<bool>( storageAllocated.GetValue( screen ) ) );
		Assert.Equal( 1, Assert.IsType<int>( allocatedRows.GetValue( screen ) ) );

		_ = setRasterCell.Invoke(
			screen,
			[ 197, 7, null ]
		);

		Assert.False( Assert.IsType<bool>( storageAllocated.GetValue( screen ) ) );
		Assert.Equal( 0, Assert.IsType<int>( allocatedRows.GetValue( screen ) ) );
	}

	internal static CursesRasterCell CreateLogicalRasterCell() {
		return CreateLogicalRasterCell(
			TerminalRasterOwnershipStatus.Current,
			TerminalRasterOwnershipLossReason.None
		);
	}

	internal static CursesRasterCell CreateLogicalRasterCell(
		TerminalSession terminalSession
	) {
		ArgumentNullException.ThrowIfNull( terminalSession );
		return CreateLogicalRasterCell(
			TerminalRasterOwnershipStatus.Current,
			TerminalRasterOwnershipLossReason.None,
			terminalSession,
			createTerminalCell: true
		);
	}

	internal static CursesRasterCell CreateLogicalRasterCell(
		TerminalRasterOwnershipStatus status,
		TerminalRasterOwnershipLossReason reason
	) {
		return CreateLogicalRasterCell(
			status,
			reason,
			terminalSession: null,
			createTerminalCell: false
		);
	}

	private static CursesRasterCell CreateLogicalRasterCell(
		TerminalRasterOwnershipStatus status,
		TerminalRasterOwnershipLossReason reason,
		TerminalSession? terminalSession,
		bool createTerminalCell
	) {
		if ( TerminalRasterOwnershipStatus.Disposed == status ) {
			if ( TerminalRasterOwnershipLossReason.ExplicitDisposal != reason ) {
				throw new ArgumentException(
					"Disposed synthetic raster ownership requires ExplicitDisposal.",
					nameof( reason )
				);
			}
			return CreateDisposedLogicalRasterCell();
		}

		Assembly terminalAssembly = typeof( TerminalRasterPlaceholder ).Assembly;
		Type resourceStateType = terminalAssembly.GetType(
			"Icod.Terminal.TerminalPersistentRasterResourceState",
			throwOnError: true
		) ?? throw new InvalidOperationException( "Terminal persistent raster resource state type is unavailable." );
		Type placeholderStateType = terminalAssembly.GetType(
			"Icod.Terminal.TerminalPersistentRasterPlaceholderState",
			throwOnError: true
		) ?? throw new InvalidOperationException( "Terminal persistent raster placeholder state type is unavailable." );

		object resourceState = RequireNonPublicConstructor(
			resourceStateType,
			[ typeof( uint ), typeof( long ) ]
		).Invoke( [ 1u, 0L ] );
		object placeholderState = RequireNonPublicConstructor(
			placeholderStateType,
			[
				resourceStateType,
				typeof( uint ),
				typeof( long ),
				typeof( int ),
				typeof( int )
			]
		).Invoke( [ resourceState, 1u, 0L, 1, 1 ] );

		switch ( status ) {
			case TerminalRasterOwnershipStatus.Current:
				if ( TerminalRasterOwnershipLossReason.None != reason ) {
					throw new ArgumentException(
						"Current synthetic raster ownership requires no loss reason.",
						nameof( reason )
					);
				}
				break;

			case TerminalRasterOwnershipStatus.Stale:
				Assert.True(
					InvokeLifecycleTransition(
						placeholderStateType,
						placeholderState,
						"TryMarkStale",
						reason
					)
				);
				break;

			case TerminalRasterOwnershipStatus.Released:
				Assert.True(
					InvokeLifecycleTransition(
						placeholderStateType,
						placeholderState,
						"TryMarkReleased",
						reason
					)
				);
				break;

			default:
				throw new ArgumentOutOfRangeException(
					nameof( status ),
					status,
					"Unsupported synthetic raster ownership status."
				);
		}

		TerminalSession ownerSession = terminalSession
			?? (TerminalSession)RuntimeHelpers.GetUninitializedObject(
				typeof( TerminalSession )
			);
		TerminalRasterPlaceholder terminalPlaceholder =
			(TerminalRasterPlaceholder)RequireNonPublicConstructor(
				typeof( TerminalRasterPlaceholder ),
				[ typeof( TerminalSession ), placeholderStateType ]
			).Invoke( [ ownerSession, placeholderState ] );
		Assert.Equal( status, terminalPlaceholder.OwnershipState.Status );
		Assert.Equal( reason, terminalPlaceholder.OwnershipState.LossReason );

		CursesSession owner = (CursesSession)RuntimeHelpers.GetUninitializedObject(
			typeof( CursesSession )
		);
		CursesRasterPlaceholder placeholder = new(
			owner,
			terminalPlaceholder
		);
		return new CursesRasterCell(
			placeholder,
			createTerminalCell
				? terminalPlaceholder.GetCell( 0, 0 )
				: default
		);
	}

	internal static CursesRasterCell CreateDisposedLogicalRasterCell() {
		CursesRasterPlaceholder placeholder =
			(CursesRasterPlaceholder)RuntimeHelpers.GetUninitializedObject(
				typeof( CursesRasterPlaceholder )
			);
		return new CursesRasterCell(
			placeholder,
			default
		);
	}

	private static bool InvokeLifecycleTransition(
		Type stateType,
		object state,
		string methodName,
		TerminalRasterOwnershipLossReason reason
	) {
		MethodInfo? method = stateType.GetMethod(
			methodName,
			BindingFlags.Instance | BindingFlags.NonPublic,
			binder: null,
			types: [ typeof( TerminalRasterOwnershipLossReason ) ],
			modifiers: null
		);
		Assert.NotNull( method );
		return Assert.IsType<bool>( method!.Invoke( state, [ reason ] ) );
	}

	private static ConstructorInfo RequireNonPublicConstructor(
		Type type,
		Type[] parameterTypes
	) {
		ConstructorInfo? constructor = type.GetConstructor(
			BindingFlags.Instance | BindingFlags.NonPublic,
			binder: null,
			types: parameterTypes,
			modifiers: null
		);
		Assert.True(
			constructor is not null,
			$"Required internal constructor on {type.FullName} is missing."
		);
		return constructor!;
	}

	private static PropertyInfo RequireInternalProperty(
		Type type,
		string name
	) {
		PropertyInfo? property = type.GetProperty(
			name,
			BindingFlags.Instance | BindingFlags.NonPublic
		);
		Assert.True(
			property is not null,
			$"Required T1603 internal property {type.FullName}.{name} is missing."
		);
		return property!;
	}

	private static MethodInfo RequirePublicMethod(
		Type type,
		string name
	) {
		MethodInfo? method = type.GetMethods( BindingFlags.Instance | BindingFlags.Public )
			.SingleOrDefault( current => string.Equals( current.Name, name, StringComparison.Ordinal ) );
		Assert.True(
			method is not null,
			$"Required T1603 public method {type.FullName}.{name} is missing."
		);
		return method!;
	}
}
