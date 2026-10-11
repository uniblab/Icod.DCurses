/*
	Icod.DCurses.Editor.Sample
	Interactive editor-foundation acceptance sample for Icod.DCurses.
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

namespace Icod.DCurses.Editor.Sample;

using System.Text;
using Icod.DCurses;

/// <summary>Demonstrates explicit host clipboard I/O with an application-owned fallback.</summary>
internal sealed class EditorSampleClipboard {
	private static readonly UTF8Encoding StrictUtf8 = new(
		encoderShouldEmitUTF8Identifier: false,
		throwOnInvalidBytes: true
	);
	private string fallback = string.Empty;

	internal async ValueTask<bool> CopyAsync(
		CursesSession session,
		EditorSampleState state,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( session );
		ArgumentNullException.ThrowIfNull( state );
		if ( !state.TryGetSelectedText( out string value ) ) {
			return false;
		}

		fallback = value;
		try {
			await session.WriteClipboardAsync(
				CursesClipboardSelection.Clipboard,
				value,
				cancellationToken
			).ConfigureAwait( false );
		} catch ( Exception exception ) when ( IsHostClipboardUnavailable( exception ) ) {
			// The application-owned fallback remains authoritative for this sample.
		}
		return true;
	}

	internal async ValueTask<bool> PasteAsync(
		CursesSession session,
		EditorSampleState state,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( session );
		ArgumentNullException.ThrowIfNull( state );
		string value = fallback;
		try {
			byte[] payload = await session.ReadClipboardAsync(
				CursesClipboardSelection.Clipboard,
				TimeSpan.FromMilliseconds( 250 ),
				cancellationToken
			).ConfigureAwait( false );
			value = StrictUtf8.GetString( payload );
		} catch ( Exception exception ) when ( IsHostClipboardUnavailable( exception ) ) {
			// Timeout is inconclusive; use the private fallback without claiming lack of support.
		}
		return value.Length > 0 && state.Insert( value );
	}

	private static bool IsHostClipboardUnavailable( Exception exception ) {
		return exception is TimeoutException
			or NotSupportedException
			or InvalidOperationException
			or FormatException
			or DecoderFallbackException
			or IOException;
	}
}
