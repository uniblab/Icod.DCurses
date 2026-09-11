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
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises representative 1.3 layouts and freezes steady-state allocation ceilings.</summary>
public sealed class CursesLayoutApplicationAcceptanceTests {
	[Fact]
	public void RepresentativeApplicationLayoutChainsAndAppliesDeterministically() {
		CursesScreen screen = new( 80, 24 );
		ComputeApplicationLayout(
			screen.Bounds,
			out CursesRectangle headerBounds,
			out CursesRectangle statusBounds,
			out CursesRectangle sidebarBounds,
			out CursesRectangle bodyBounds,
			out CursesRectangle dialogBounds
		);
		CursesWindow header = screen.CreateWindow( 0, 0, 1, 1 );
		CursesWindow status = screen.CreateWindow( 0, 0, 1, 1 );
		CursesWindow sidebar = screen.CreateWindow( 0, 0, 1, 1 );
		CursesWindow body = screen.CreateWindow( 0, 0, 1, 1 );
		using CursesPanel dialog = screen.CreatePanel( 0, 0, 1, 1 );

		header.SetBounds( headerBounds );
		status.SetBounds( statusBounds );
		sidebar.SetBounds( sidebarBounds );
		body.SetBounds( bodyBounds );
		dialog.SetBounds( dialogBounds );

		Assert.Equal( headerBounds, header.Bounds );
		Assert.Equal( statusBounds, status.Bounds );
		Assert.Equal( sidebarBounds, sidebar.Bounds );
		Assert.Equal( bodyBounds, body.Bounds );
		Assert.Equal( dialogBounds, dialog.Bounds );
		Assert.True( screen.Bounds.Contains( headerBounds ) );
		Assert.True( screen.Bounds.Contains( statusBounds ) );
		Assert.True( screen.Bounds.Contains( sidebarBounds ) );
		Assert.True( screen.Bounds.Contains( bodyBounds ) );
		Assert.True( bodyBounds.Contains( dialogBounds ) );

		header.WrapMode = CursesWrapMode.Clip;
		status.WrapMode = CursesWrapMode.Clip;
		sidebar.WrapMode = CursesWrapMode.Clip;
		body.WrapMode = CursesWrapMode.Clip;
		dialog.ContentWindow.WrapMode = CursesWrapMode.Clip;
		header.Write( "HEADER" );
		status.Write( "STATUS" );
		sidebar.Write( "SIDE" );
		body.Write( "BODY" );
		dialog.ContentWindow.Write( "DIALOG" );
		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.Equal( "HEADER", ReadText( composed, headerBounds.Row, headerBounds.Column, 6 ) );
		Assert.Equal( "STATUS", ReadText( composed, statusBounds.Row, statusBounds.Column, 6 ) );
		Assert.Equal( "SIDE", ReadText( composed, sidebarBounds.Row, sidebarBounds.Column, 4 ) );
		Assert.Equal( "DIALOG", ReadText( composed, dialogBounds.Row, dialogBounds.Column, 6 ) );
		CursesCellFootprint.Validate( composed );
	}

	[Fact]
	public void RepeatedPureGeometryCalculationIsAllocationFree() {
		CursesRectangle bounds = new( 0, 0, 48, 160 );
		for ( int index = 0; index < 64; index++ ) {
			ComputeApplicationLayout(
				bounds,
				out _,
				out _,
				out _,
				out _,
				out _
			);
		}

		long before = GC.GetAllocatedBytesForCurrentThread();
		for ( int index = 0; index < 100000; index++ ) {
			ComputeApplicationLayout(
				bounds,
				out _,
				out _,
				out _,
				out _,
				out _
			);
		}
		long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.Equal( 0, allocated );
	}

	[Fact]
	public void SteadyStateLayoutApplyAndCompositionStayWithinAllocationCeiling() {
		CursesScreen screen = new( 120, 40 );
		ComputeApplicationLayout(
			screen.Bounds,
			out CursesRectangle headerBounds,
			out CursesRectangle statusBounds,
			out CursesRectangle sidebarBounds,
			out CursesRectangle bodyBounds,
			out CursesRectangle dialogBounds
		);
		CursesWindow header = screen.CreateWindow(
			headerBounds.Row,
			headerBounds.Column,
			headerBounds.Rows,
			headerBounds.Columns
		);
		CursesWindow status = screen.CreateWindow(
			statusBounds.Row,
			statusBounds.Column,
			statusBounds.Rows,
			statusBounds.Columns
		);
		CursesWindow sidebar = screen.CreateWindow(
			sidebarBounds.Row,
			sidebarBounds.Column,
			sidebarBounds.Rows,
			sidebarBounds.Columns
		);
		CursesWindow body = screen.CreateWindow(
			bodyBounds.Row,
			bodyBounds.Column,
			bodyBounds.Rows,
			bodyBounds.Columns
		);
		using CursesPanel dialog = screen.CreatePanel(
			dialogBounds.Row,
			dialogBounds.Column,
			dialogBounds.Rows,
			dialogBounds.Columns
		);
		dialog.ContentWindow.Write( "steady" );
		CursesVirtualScreen retained = screen.ComposePanels();
		retained.MarkClean();

		for ( int index = 0; index < 32; index++ ) {
			ApplyLayout(
				screen,
				header,
				status,
				sidebar,
				body,
				dialog
			);
			_ = screen.ComposePanels();
		}

		long before = GC.GetAllocatedBytesForCurrentThread();
		for ( int index = 0; index < 1024; index++ ) {
			ApplyLayout(
				screen,
				header,
				status,
				sidebar,
				body,
				dialog
			);
			CursesVirtualScreen current = screen.ComposePanels();
			if ( !ReferenceEquals(
				retained,
				current
			) ) {
				throw new InvalidOperationException(
					"Steady-state layout application replaced the retained composed frame."
				);
			}
		}
		long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.InRange( allocated, 0, 262144 );
		Assert.Equal( 0, retained.DirtyCellCount );
	}

	[Fact]
	public void LayoutUtilityRetainsNoInstanceOrMutableStaticState() {
		Type type = typeof( CursesLayout );
		FieldInfo[] fields = type.GetFields(
			BindingFlags.Public
				| BindingFlags.NonPublic
				| BindingFlags.Instance
				| BindingFlags.Static
		);

		Assert.True( type.IsAbstract );
		Assert.True( type.IsSealed );
		Assert.Empty( fields );
	}

	private static void ApplyLayout(
		CursesScreen screen,
		CursesWindow header,
		CursesWindow status,
		CursesWindow sidebar,
		CursesWindow body,
		CursesPanel dialog
	) {
		ArgumentNullException.ThrowIfNull( screen );
		ArgumentNullException.ThrowIfNull( header );
		ArgumentNullException.ThrowIfNull( status );
		ArgumentNullException.ThrowIfNull( sidebar );
		ArgumentNullException.ThrowIfNull( body );
		ArgumentNullException.ThrowIfNull( dialog );

		ComputeApplicationLayout(
			screen.Bounds,
			out CursesRectangle headerBounds,
			out CursesRectangle statusBounds,
			out CursesRectangle sidebarBounds,
			out CursesRectangle bodyBounds,
			out CursesRectangle dialogBounds
		);
		header.SetBounds( headerBounds );
		status.SetBounds( statusBounds );
		sidebar.SetBounds( sidebarBounds );
		body.SetBounds( bodyBounds );
		dialog.SetBounds( dialogBounds );
	}

	private static void ComputeApplicationLayout(
		CursesRectangle bounds,
		out CursesRectangle header,
		out CursesRectangle status,
		out CursesRectangle sidebar,
		out CursesRectangle body,
		out CursesRectangle dialog
	) {
		CursesLayout.Dock(
			bounds,
			CursesDockEdge.Top,
			2,
			out header,
			out CursesRectangle belowHeader
		);
		CursesLayout.Dock(
			belowHeader,
			CursesDockEdge.Bottom,
			2,
			out status,
			out CursesRectangle center
		);
		CursesLayout.Dock(
			center,
			CursesDockEdge.Left,
			18,
			out sidebar,
			out body
		);
		CursesLayout.SplitRowsProportional(
			body,
			1,
			1,
			out _,
			out CursesRectangle lowerBody
		);
		CursesLayout.SplitColumnsProportional(
			lowerBody,
			1,
			1,
			out _,
			out CursesRectangle lowerRight
		);
		CursesLayout.Dock(
			lowerRight,
			CursesDockEdge.Top,
			Math.Min(
				6,
				lowerRight.Rows
			),
			out dialog,
			out _
		);
	}

	private static string ReadText(
		CursesVirtualScreen screen,
		int row,
		int column,
		int length
	) {
		ArgumentNullException.ThrowIfNull( screen );
		if ( 0 > row ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( 0 > column ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		if ( 0 > length ) {
			throw new ArgumentOutOfRangeException( nameof( length ) );
		}

		return string.Concat(
			Enumerable.Range(
				column,
				length
			).Select(
				current => screen.GetCell( row, current ).Content
			)
		);
	}
}
