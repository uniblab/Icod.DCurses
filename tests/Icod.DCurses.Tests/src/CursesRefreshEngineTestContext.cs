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

namespace Icod.DCurses.Tests;

using System.Text;
using Icod.DCurses.Internal;
using LegacyTerminalOutput = Icod.DCurses.Terminal.ITerminalOutput;
using Icod.Terminal;
using Icod.TermInfo;

/// <summary>Owns a real Terminal session and its transaction-backed refresh engine for tests.</summary>
internal sealed class CursesRefreshEngineTestContext : IAsyncDisposable {
	private CursesRefreshEngineTestContext(
		TerminalSession session,
		CursesRefreshEngine engine
	) {
		this.Session = session;
		this.Engine = engine;
	}

	internal TerminalSession Session {
		get;
	}

	internal CursesRefreshEngine Engine {
		get;
	}

	internal static async ValueTask<CursesRefreshEngineTestContext> OpenAsync(
		TerminalDescription terminal,
		LegacyTerminalOutput output,
		bool useSynchronizedOutput = false
	) {
		ArgumentNullException.ThrowIfNull( terminal );
		ArgumentNullException.ThrowIfNull( output );
		TerminalSession session = await TerminalScreenTestSession.OpenAsync(
			terminal,
			new LegacyOutputAdapter( output )
		);
		return new CursesRefreshEngineTestContext(
			session,
			new CursesRefreshEngine(
				session,
				useSynchronizedOutput
			)
		);
	}

	internal static async ValueTask<CursesRefreshEngineTestContext> OpenAsync(
		TerminalDescription terminal,
		Icod.Terminal.ITerminalOutput output,
		bool useSynchronizedOutput = false
	) {
		ArgumentNullException.ThrowIfNull( terminal );
		ArgumentNullException.ThrowIfNull( output );
		TerminalSession session = await TerminalScreenTestSession.OpenAsync(
			terminal,
			output
		);
		return new CursesRefreshEngineTestContext(
			session,
			new CursesRefreshEngine(
				session,
				useSynchronizedOutput
			)
		);
	}

	public ValueTask DisposeAsync() {
		return this.Session.DisposeAsync();
	}

	private sealed class LegacyOutputAdapter : Icod.Terminal.ITerminalOutput {
		private readonly LegacyTerminalOutput output;

		internal LegacyOutputAdapter( LegacyTerminalOutput output ) {
			ArgumentNullException.ThrowIfNull( output );
			this.output = output;
		}

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return this.output.WriteTerminalStringAsync(
				Encoding.UTF8.GetString( buffer.Span ),
				cancellationToken: cancellationToken
			);
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			return this.output.FlushAsync( cancellationToken );
		}
	}
}
