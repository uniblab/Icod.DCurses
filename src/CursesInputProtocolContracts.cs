/*
	Icod.DCurses
	Managed, cross-platform curses-style terminal UI library for .NET.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.DCurses;

using Icod.Terminal;

/// <summary>
/// Identifies the requested intensity of terminal mouse tracking.
/// </summary>
public enum CursesMouseTrackingMode {
	/// <summary>Report mouse button press, release, and wheel activity.</summary>
	ButtonEvents,

	/// <summary>Report button activity plus motion while a button is held.</summary>
	ButtonMotion,

	/// <summary>Report button activity plus all mouse motion.</summary>
	AnyMotion
}

/// <summary>
/// Identifies the requested semantic keyboard-reporting intensity.
/// </summary>
public enum CursesKeyboardReportingMode {
	/// <summary>
	/// Disambiguate keyboard escape/control sequences while preserving ordinary direct text delivery.
	/// </summary>
	Disambiguated = 0,

	/// <summary>
	/// Add press/repeat/release information where the negotiated protocol reports it.
	/// </summary>
	EventTypes = 1,

	/// <summary>
	/// Request uniform key-event reporting, including text-producing and modifier keys.
	/// </summary>
	AllKeys = 2
}

/// <summary>
/// Describes reversible rich-input protocol reporting requested by a curses consumer.
/// </summary>
public sealed class CursesInputProtocolOptions {
	/// <summary>Gets or initializes whether bracketed-paste reporting is required.</summary>
	public bool BracketedPaste {
		get;
		init;
	}

	/// <summary>Gets or initializes whether terminal focus reporting is required.</summary>
	public bool FocusReporting {
		get;
		init;
	}

	/// <summary>Gets or initializes the requested mouse tracking intensity, when any.</summary>
	public CursesMouseTrackingMode? MouseTrackingMode {
		get;
		init;
	}

	/// <summary>Gets or initializes the requested keyboard reporting intensity, when any.</summary>
	public CursesKeyboardReportingMode? KeyboardReportingMode {
		get;
		init;
	}

	internal void Validate() {
		if ( this.MouseTrackingMode.HasValue
			&& !Enum.IsDefined( this.MouseTrackingMode.Value ) ) {
			throw new ArgumentOutOfRangeException(
				nameof( this.MouseTrackingMode ),
				this.MouseTrackingMode.Value,
				"The curses mouse tracking mode is not recognized."
			);
		}
		if ( this.KeyboardReportingMode.HasValue
			&& !Enum.IsDefined( this.KeyboardReportingMode.Value ) ) {
			throw new ArgumentOutOfRangeException(
				nameof( this.KeyboardReportingMode ),
				this.KeyboardReportingMode.Value,
				"The curses keyboard reporting mode is not recognized."
			);
		}
		if ( !this.BracketedPaste
			&& !this.FocusReporting
			&& !this.MouseTrackingMode.HasValue
			&& !this.KeyboardReportingMode.HasValue ) {
			throw new ArgumentException(
				"At least one curses input protocol must be requested."
			);
		}
	}

	internal TerminalInputProtocolOptions ToTerminalOptions() {
		this.Validate();
		return new TerminalInputProtocolOptions {
			BracketedPaste = this.BracketedPaste,
			FocusReporting = this.FocusReporting,
			MouseTrackingMode = this.MouseTrackingMode switch {
				CursesMouseTrackingMode.ButtonEvents => TerminalMouseTrackingMode.ButtonEvents,
				CursesMouseTrackingMode.ButtonMotion => TerminalMouseTrackingMode.ButtonMotion,
				CursesMouseTrackingMode.AnyMotion => TerminalMouseTrackingMode.AnyMotion,
				null => null,
				_ => throw new ArgumentOutOfRangeException( nameof( this.MouseTrackingMode ) )
			},
			KeyboardReportingMode = this.KeyboardReportingMode switch {
				CursesKeyboardReportingMode.Disambiguated => TerminalKeyboardReportingMode.Disambiguated,
				CursesKeyboardReportingMode.EventTypes => TerminalKeyboardReportingMode.EventTypes,
				CursesKeyboardReportingMode.AllKeys => TerminalKeyboardReportingMode.AllKeys,
				null => null,
				_ => throw new ArgumentOutOfRangeException( nameof( this.KeyboardReportingMode ) )
			}
		};
	}
}

/// <summary>
/// Owns one reversible rich-input protocol request made through a curses session.
/// </summary>
public sealed class CursesInputProtocolLease : IAsyncDisposable {
	private readonly TerminalInputProtocolLease terminalLease;

	internal CursesInputProtocolLease(
		TerminalInputProtocolLease terminalLease,
		CursesInputProtocolOptions options
	) {
		ArgumentNullException.ThrowIfNull( terminalLease );
		ArgumentNullException.ThrowIfNull( options );

		this.terminalLease = terminalLease;
		this.BracketedPaste = options.BracketedPaste;
		this.FocusReporting = options.FocusReporting;
		this.MouseTrackingMode = options.MouseTrackingMode;
		this.KeyboardReportingMode = options.KeyboardReportingMode;
	}

	/// <summary>Gets whether this lease requests bracketed-paste reporting.</summary>
	public bool BracketedPaste {
		get;
	}

	/// <summary>Gets whether this lease requests focus reporting.</summary>
	public bool FocusReporting {
		get;
	}

	/// <summary>Gets the mouse tracking request owned by this lease, when any.</summary>
	public CursesMouseTrackingMode? MouseTrackingMode {
		get;
	}

	/// <summary>Gets the keyboard reporting request owned by this lease, when any.</summary>
	public CursesKeyboardReportingMode? KeyboardReportingMode {
		get;
	}

	/// <summary>Releases this input-protocol request.</summary>
	public ValueTask DisposeAsync() {
		return this.terminalLease.DisposeAsync();
	}
}
