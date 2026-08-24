namespace Icod.DCurses.Internal;

using System.Buffers;
using System.Text;
using Icod.DCurses.Terminal;
using Icod.TermInfo;

/// <summary>
/// Decodes the byte stream supplied by a terminal backend into terminal-independent input events.
/// </summary>
internal sealed class CursesInputDecoder {
	private const byte EscapeByte = 0x1B;
	private const int ReadBufferSize = 256;

	private readonly ITerminalInput input;
	private readonly TimeSpan escapeSequenceTimeout;
	private readonly List<byte> bufferedBytes = [];
	private readonly byte[] readBuffer = new byte[ ReadBufferSize ];
	private readonly List<KeySequence> keySequences = [];

	private Task<int>? pendingRead;
	private bool endOfInput;

	internal CursesInputDecoder(
		ITerminalInput input,
		TerminalDescription terminal,
		TimeSpan escapeSequenceTimeout) {
		ArgumentNullException.ThrowIfNull( input );
		ArgumentNullException.ThrowIfNull( terminal );

		if ( TimeSpan.Zero > escapeSequenceTimeout ) {
			throw new ArgumentOutOfRangeException( nameof( escapeSequenceTimeout ) );
		}

		this.input = input;
		this.escapeSequenceTimeout = escapeSequenceTimeout;

		AddCapability(
			terminal,
			StringCapability.KeyBackspace,
			CursesInputEvent.FromKey( CursesKey.Backspace )
		);
		AddCapability(
			terminal,
			StringCapability.KeyBackTab,
			CursesInputEvent.FromKey(
				CursesKey.Tab,
				CursesKeyModifiers.Shift
			)
		);
		AddCapability(
			terminal,
			StringCapability.KeyCursorUp,
			CursesInputEvent.FromKey( CursesKey.Up )
		);
		AddCapability(
			terminal,
			StringCapability.KeyCursorDown,
			CursesInputEvent.FromKey( CursesKey.Down )
		);
		AddCapability(
			terminal,
			StringCapability.KeyCursorLeft,
			CursesInputEvent.FromKey( CursesKey.Left )
		);
		AddCapability(
			terminal,
			StringCapability.KeyCursorRight,
			CursesInputEvent.FromKey( CursesKey.Right )
		);
		AddCapability(
			terminal,
			StringCapability.KeyHome,
			CursesInputEvent.FromKey( CursesKey.Home )
		);
		AddCapability(
			terminal,
			StringCapability.KeyEnd,
			CursesInputEvent.FromKey( CursesKey.End )
		);
		AddCapability(
			terminal,
			StringCapability.KeyEnter,
			CursesInputEvent.FromKey( CursesKey.Enter )
		);
		AddCapability(
			terminal,
			StringCapability.KeyPreviousPage,
			CursesInputEvent.FromKey( CursesKey.PageUp )
		);
		AddCapability(
			terminal,
			StringCapability.KeyNextPage,
			CursesInputEvent.FromKey( CursesKey.PageDown )
		);
		AddCapability(
			terminal,
			StringCapability.KeyInsertCharacter,
			CursesInputEvent.FromKey( CursesKey.Insert )
		);
		AddCapability(
			terminal,
			StringCapability.KeyDeleteCharacter,
			CursesInputEvent.FromKey( CursesKey.Delete )
		);

		for ( int number = 0; number <= 63; number++ ) {
			if ( !Enum.TryParse(
				$"KeyF{number}",
				out StringCapability capability
			) ) {
				continue;
			}

			AddCapability(
				terminal,
				capability,
				CursesInputEvent.FromKey(
					CursesKey.Function,
					functionKeyNumber: number
				)
			);
		}

		keySequences.Sort(
			static ( left, right ) =>
				right.Bytes.Length.CompareTo( left.Bytes.Length )
		);
	}

	internal async ValueTask<CursesInputEvent> ReadAsync(
		CancellationToken cancellationToken = default) {
		while ( true ) {
			cancellationToken.ThrowIfCancellationRequested();

			if ( 0 == bufferedBytes.Count ) {
				if ( !await ReadMoreAsync(
					cancellationToken
				).ConfigureAwait( false ) ) {
					return CursesInputEvent.EndOfInput();
				}
			}

			if ( TryDecodeKeySequence(
				out CursesInputEvent? keyEvent,
				out bool needsMore
			) ) {
				return keyEvent!;
			}

			if ( needsMore ) {
				bool appended = EscapeByte == bufferedBytes[ 0 ]
					? await ReadMoreWithinEscapeWindowAsync(
						cancellationToken
					).ConfigureAwait( false )
					: await ReadMoreAsync(
						cancellationToken
					).ConfigureAwait( false )
				;

				if ( appended ) {
					continue;
				}

				if ( EscapeByte == bufferedBytes[ 0 ] ) {
					Consume( 1 );
					return CursesInputEvent.FromKey( CursesKey.Escape );
				}

				return await DecodeFallbackAsync(
					cancellationToken
				).ConfigureAwait( false );
			}

			return await DecodeFallbackAsync(
				cancellationToken
			).ConfigureAwait( false );
		}
	}

	private async ValueTask<CursesInputEvent> DecodeFallbackAsync(
		CancellationToken cancellationToken) {
		byte first = bufferedBytes[ 0 ];

		switch ( first ) {
			case EscapeByte:
				Consume( 1 );
				return CursesInputEvent.FromKey( CursesKey.Escape );

			case 0x09:
				Consume( 1 );
				return CursesInputEvent.FromKey( CursesKey.Tab );

			case 0x0A:
			case 0x0D:
				Consume( 1 );
				return CursesInputEvent.FromKey( CursesKey.Enter );

			case 0x20:
				Consume( 1 );
				return CursesInputEvent.FromKey( CursesKey.Space );

			case 0x7F:
				Consume( 1 );
				return CursesInputEvent.FromKey( CursesKey.Backspace );
		}

		if ( 0x20 > first ) {
			Consume( 1 );
			return CreateControlKey( first );
		}

		while ( true ) {
			byte[] source = bufferedBytes.ToArray();
			OperationStatus status = Rune.DecodeFromUtf8(
				source,
				out Rune rune,
				out int bytesConsumed
			);

			if ( OperationStatus.Done == status ) {
				Consume( bytesConsumed );
				return CursesInputEvent.FromText( rune );
			}

			if ( OperationStatus.NeedMoreData == status && !endOfInput ) {
				if ( await ReadMoreAsync(
					cancellationToken
				).ConfigureAwait( false ) ) {
					continue;
				}
			}

			Consume( 1 );
			return CursesInputEvent.FromText( new Rune( '\uFFFD' ) );
		}
	}

	private static CursesInputEvent CreateControlKey( byte value ) {
		char character = value switch {
			0 => '@',
			>= 1 and <= 26 => (char)( 'A' + value - 1 ),
			28 => '\\',
			29 => ']',
			30 => '^',
			31 => '_',
			_ => (char)( '@' + value )
		};

		return CursesInputEvent.FromKey(
			CursesKey.Character,
			CursesKeyModifiers.Control,
			new Rune( character )
		);
	}

	private bool TryDecodeKeySequence(
		out CursesInputEvent? inputEvent,
		out bool needsMore) {
		KeySequence? exact = null;
		needsMore = false;

		foreach ( KeySequence sequence in keySequences ) {
			if ( bufferedBytes.Count >= sequence.Bytes.Length ) {
				if ( BufferStartsWith( sequence.Bytes ) ) {
					exact ??= sequence;
				}
				continue;
			}

			if ( SequenceStartsWithBuffer( sequence.Bytes ) ) {
				needsMore = true;
			}
		}

		if ( null == exact ) {
			inputEvent = null;
			return false;
		}

		if ( needsMore && EscapeByte == bufferedBytes[ 0 ] ) {
			inputEvent = null;
			return false;
		}

		Consume( exact.Bytes.Length );
		inputEvent = exact.InputEvent;
		needsMore = false;
		return true;
	}

	private bool BufferStartsWith( IReadOnlyList<byte> bytes ) {
		if ( bufferedBytes.Count < bytes.Count ) {
			return false;
		}

		for ( int index = 0; index < bytes.Count; index++ ) {
			if ( bufferedBytes[ index ] != bytes[ index ] ) {
				return false;
			}
		}

		return true;
	}

	private bool SequenceStartsWithBuffer( IReadOnlyList<byte> bytes ) {
		if ( bufferedBytes.Count >= bytes.Count ) {
			return false;
		}

		for ( int index = 0; index < bufferedBytes.Count; index++ ) {
			if ( bufferedBytes[ index ] != bytes[ index ] ) {
				return false;
			}
		}

		return true;
	}

	private async ValueTask<bool> ReadMoreAsync(
		CancellationToken cancellationToken) {
		if ( endOfInput ) {
			return false;
		}

		Task<int> readTask = EnsurePendingRead( cancellationToken );
		int count = await CompletePendingReadAsync(
			readTask
		).ConfigureAwait( false );

		return 0 < count;
	}

	private async ValueTask<bool> ReadMoreWithinEscapeWindowAsync(
		CancellationToken cancellationToken) {
		if ( endOfInput ) {
			return false;
		}

		Task<int> readTask = EnsurePendingRead( cancellationToken );
		if ( readTask.IsCompleted ) {
			return 0 < await CompletePendingReadAsync(
				readTask
			).ConfigureAwait( false );
		}

		if ( TimeSpan.Zero == escapeSequenceTimeout ) {
			return false;
		}

		Task delayTask = Task.Delay(
			escapeSequenceTimeout,
			cancellationToken
		);
		Task completed = await Task.WhenAny(
			readTask,
			delayTask
		).ConfigureAwait( false );

		if ( ReferenceEquals( completed, delayTask ) ) {
			cancellationToken.ThrowIfCancellationRequested();
			return false;
		}

		return 0 < await CompletePendingReadAsync(
			readTask
		).ConfigureAwait( false );
	}

	private Task<int> EnsurePendingRead(
		CancellationToken cancellationToken) {
		pendingRead ??= input.ReadAsync(
			readBuffer,
			cancellationToken
		).AsTask();

		return pendingRead;
	}

	private async ValueTask<int> CompletePendingReadAsync(
		Task<int> readTask) {
		int count;

		try {
			count = await readTask.ConfigureAwait( false );
		} finally {
			if ( ReferenceEquals( pendingRead, readTask ) && readTask.IsCompleted ) {
				pendingRead = null;
			}
		}

		if ( 0 == count ) {
			endOfInput = true;
			return 0;
		}

		for ( int index = 0; index < count; index++ ) {
			bufferedBytes.Add( readBuffer[ index ] );
		}

		return count;
	}

	private void AddCapability(
		TerminalDescription terminal,
		StringCapability capability,
		CursesInputEvent inputEvent) {
		string? value = terminal.GetString( capability );
		if ( string.IsNullOrEmpty( value ) ) {
			return;
		}

		byte[] bytes = EncodeCapability( value, capability );
		if ( 0 == bytes.Length ) {
			return;
		}

		foreach ( KeySequence existing in keySequences ) {
			if ( existing.Bytes.AsSpan().SequenceEqual( bytes ) ) {
				return;
			}
		}

		keySequences.Add(
			new KeySequence(
				bytes,
				inputEvent
			)
		);
	}

	private static byte[] EncodeCapability(
		string value,
		StringCapability capability) {
		foreach ( char character in value ) {
			if ( 0x00FF < character ) {
				throw new InvalidOperationException(
					$"Terminal key capability '{capability}' contains data outside the reversible 8-bit terminfo range."
				);
			}
		}

		return Encoding.Latin1.GetBytes( value );
	}

	private void Consume( int count ) {
		if ( 0 >= count || count > bufferedBytes.Count ) {
			throw new ArgumentOutOfRangeException( nameof( count ) );
		}

		bufferedBytes.RemoveRange( 0, count );
	}

	private sealed class KeySequence {
		internal KeySequence(
			byte[] bytes,
			CursesInputEvent inputEvent) {
			ArgumentNullException.ThrowIfNull( bytes );
			ArgumentNullException.ThrowIfNull( inputEvent );

			Bytes = bytes;
			InputEvent = inputEvent;
		}

		internal byte[] Bytes {
			get;
		}

		internal CursesInputEvent InputEvent {
			get;
		}
	}
}