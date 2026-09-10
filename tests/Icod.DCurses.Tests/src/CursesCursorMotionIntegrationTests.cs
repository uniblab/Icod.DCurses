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

using System.Text;
using Icod.DCurses.Internal;
using Icod.DCurses.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies cost-aware cursor motion at the physical refresh-engine output seam.</summary>
public sealed class CursesCursorMotionIntegrationTests {
	[Fact]
	public async Task KnownCursorUsesCheaperRelativeMotion() {
		RecordingOutput output = new();
		TerminalDescription terminal = new TerminalDescriptionBuilder( "relative-integration" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.CursorRightOne, ">" )
			.Build();
		CursesRefreshEngine engine = new( terminal, output );

		await engine.SetCursorPositionAsync( 2, 3 );
		output.Clear();
		await engine.SetCursorPositionAsync( 2, 4 );

		Assert.Equal( ">", output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task AbsoluteAddressRemainsFallback() {
		RecordingOutput output = new();
		TerminalDescription terminal = new TerminalDescriptionBuilder( "absolute-fallback" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.Build();
		CursesRefreshEngine engine = new( terminal, output );

		await engine.SetCursorPositionAsync( 1, 1 );
		output.Clear();
		await engine.SetCursorPositionAsync( 3, 5 );

		Assert.Equal( "<cup:3,5>", output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task EqualCostTieRetainsAbsoluteAddress() {
		RecordingOutput output = new();
		TerminalDescription terminal = new TerminalDescriptionBuilder( "absolute-tie" )
			.SetString( StringCapability.CursorAddress, "AB" )
			.SetString( StringCapability.CarriageReturn, "CD" )
			.Build();
		CursesRefreshEngine engine = new( terminal, output );

		await engine.SetCursorPositionAsync( 4, 7 );
		output.Clear();
		await engine.SetCursorPositionAsync( 4, 0 );

		Assert.Equal( "AB", output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	private sealed class RecordingOutput : ITerminalOutput {
		private readonly StringBuilder text = new();

		internal int FlushCount {
			get;
			private set;
		}

		internal string Text => text.ToString();

		internal void Clear() {
			text.Clear();
			FlushCount = 0;
		}

		public ValueTask WriteTextAsync(
			string value,
			CancellationToken cancellationToken = default
		) {
			ArgumentNullException.ThrowIfNull( value );
			cancellationToken.ThrowIfCancellationRequested();
			text.Append( value );
			return ValueTask.CompletedTask;
		}

		public ValueTask WriteTerminalStringAsync(
			string value,
			int affectedLines = 1,
			CancellationToken cancellationToken = default
		) {
			ArgumentNullException.ThrowIfNull( value );
			if ( 0 >= affectedLines ) {
				throw new ArgumentOutOfRangeException( nameof( affectedLines ) );
			}
			cancellationToken.ThrowIfCancellationRequested();
			text.Append( value );
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			FlushCount++;
			return ValueTask.CompletedTask;
		}
	}
}
