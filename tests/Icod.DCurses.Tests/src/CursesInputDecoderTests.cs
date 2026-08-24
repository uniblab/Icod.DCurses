using System.Text;
using Icod.DCurses.Internal;
using Icod.DCurses.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class CursesInputDecoderTests {
	[Fact]
	public async Task FragmentedTerminfoSequenceDecodesAsOneKey() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "fragmented" )
			.SetString(
				StringCapability.KeyCursorUp,
				"\u001b[A"
			)
			.Build();
		ScriptedTerminalInput input = new(
			[
				[ 0x1B ],
				Encoding.ASCII.GetBytes( "[" ),
				Encoding.ASCII.GetBytes( "A" )
			]
		);
		CursesInputDecoder decoder = new(
			input,
			terminal,
			TimeSpan.FromMilliseconds( 50 )
		);

		CursesInputEvent inputEvent = await decoder.ReadAsync();

		Assert.Equal( CursesInputEventKind.Key, inputEvent.Kind );
		Assert.Equal( CursesKey.Up, inputEvent.Key );
	}

	[Fact]
	public async Task MultipleKeysFromOneReadRemainBuffered() {
		ScriptedTerminalInput input = new(
			[
				Encoding.UTF8.GetBytes( "ab" )
			]
		);
		CursesInputDecoder decoder = CreateDecoder( input );

		CursesInputEvent first = await decoder.ReadAsync();
		CursesInputEvent second = await decoder.ReadAsync();

		Assert.Equal( new Rune( 'a' ), first.Character );
		Assert.Equal( new Rune( 'b' ), second.Character );
		Assert.Equal( CursesInputEventKind.Text, first.Kind );
		Assert.Equal( CursesInputEventKind.Text, second.Kind );
	}

	[Fact]
	public async Task FragmentedUtf8DecodesUnicodeRunes() {
		byte[] smile = Encoding.UTF8.GetBytes( "🙂" );
		ScriptedTerminalInput input = new(
			[
				smile[ 0..1 ],
				smile[ 1..3 ],
				smile[ 3..4 ]
			]
		);
		CursesInputDecoder decoder = CreateDecoder( input );

		CursesInputEvent inputEvent = await decoder.ReadAsync();

		Assert.Equal( CursesInputEventKind.Text, inputEvent.Kind );
		Assert.Equal( new Rune( 0x1F642 ), inputEvent.Character );
	}

	[Fact]
	public async Task IsolatedEscapeUsesBoundedAmbiguityDelay() {
		using CancellationTokenSource cancellation = new();
		EscapeThenBlockTerminalInput input = new();
		CursesInputDecoder decoder = new(
			input,
			new TerminalDescriptionBuilder( "escape" )
				.SetString(
					StringCapability.KeyCursorUp,
					"\u001b[A"
				)
				.Build(),
			TimeSpan.FromMilliseconds( 10 )
		);

		CursesInputEvent inputEvent = await decoder.ReadAsync( cancellation.Token );
		cancellation.Cancel();

		Assert.Equal( CursesInputEventKind.Key, inputEvent.Kind );
		Assert.Equal( CursesKey.Escape, inputEvent.Key );
	}

	[Fact]
	public async Task SelectedTerminfoFunctionKeyAndShiftTabAreDecoded() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "functions" )
			.SetString(
				StringCapability.KeyBackTab,
				"\u001b[Z"
			)
			.SetString(
				StringCapability.KeyF37,
				"\u001b[37~"
			)
			.Build();
		ScriptedTerminalInput input = new(
			[
				Encoding.Latin1.GetBytes(
					"\u001b[Z\u001b[37~"
				)
			]
		);
		CursesInputDecoder decoder = new(
			input,
			terminal,
			TimeSpan.FromMilliseconds( 50 )
		);

		CursesInputEvent shiftTab = await decoder.ReadAsync();
		CursesInputEvent function = await decoder.ReadAsync();

		Assert.Equal( CursesKey.Tab, shiftTab.Key );
		Assert.Equal( CursesKeyModifiers.Shift, shiftTab.Modifiers );
		Assert.Equal( CursesKey.Function, function.Key );
		Assert.Equal( 37, function.FunctionKeyNumber );
	}

	[Fact]
	public async Task NavigationCapabilitiesComeFromTerminalDescription() {
		(
			StringCapability Capability,
			string Sequence,
			CursesKey Key
		)[] mappings =
		[
			( StringCapability.KeyHome, "\u001b[H", CursesKey.Home ),
			( StringCapability.KeyEnd, "\u001b[F", CursesKey.End ),
			( StringCapability.KeyPreviousPage, "\u001b[5~", CursesKey.PageUp ),
			( StringCapability.KeyNextPage, "\u001b[6~", CursesKey.PageDown ),
			( StringCapability.KeyInsertCharacter, "\u001b[2~", CursesKey.Insert ),
			( StringCapability.KeyDeleteCharacter, "\u001b[3~", CursesKey.Delete ),
			( StringCapability.KeyCursorLeft, "\u001b[D", CursesKey.Left ),
			( StringCapability.KeyCursorRight, "\u001b[C", CursesKey.Right ),
			( StringCapability.KeyCursorDown, "\u001b[B", CursesKey.Down )
		];

		TerminalDescriptionBuilder builder = new( "navigation" );
		StringBuilder inputText = new();

		foreach ( var mapping in mappings ) {
			builder.SetString(
				mapping.Capability,
				mapping.Sequence
			);
			inputText.Append( mapping.Sequence );
		}

		CursesInputDecoder decoder = new(
			new ScriptedTerminalInput(
				[
					Encoding.Latin1.GetBytes(
						inputText.ToString()
					)
				]
			),
			builder.Build(),
			TimeSpan.FromMilliseconds( 50 )
		);

		foreach ( var mapping in mappings ) {
			CursesInputEvent inputEvent = await decoder.ReadAsync();
			Assert.Equal( mapping.Key, inputEvent.Key );
		}
	}

	[Fact]
	public async Task ControlCombinationIsRepresentedWithoutLeakingControlByte() {
		CursesInputDecoder decoder = CreateDecoder(
			new ScriptedTerminalInput(
				[
					[ 0x03 ]
				]
			)
		);

		CursesInputEvent inputEvent = await decoder.ReadAsync();

		Assert.Equal( CursesInputEventKind.Key, inputEvent.Kind );
		Assert.Equal( CursesKey.Character, inputEvent.Key );
		Assert.Equal( CursesKeyModifiers.Control, inputEvent.Modifiers );
		Assert.Equal( new Rune( 'C' ), inputEvent.Character );
	}

	[Fact]
	public async Task SingleByteSemanticKeysAndEndOfInputAreRepresented() {
		CursesInputDecoder decoder = CreateDecoder(
			new ScriptedTerminalInput(
				[
					[ 0x0D, 0x20, 0x09, 0x7F ]
				]
			)
		);

		Assert.Equal( CursesKey.Enter, ( await decoder.ReadAsync() ).Key );
		Assert.Equal( CursesKey.Space, ( await decoder.ReadAsync() ).Key );
		Assert.Equal( CursesKey.Tab, ( await decoder.ReadAsync() ).Key );
		Assert.Equal( CursesKey.Backspace, ( await decoder.ReadAsync() ).Key );

		CursesInputEvent end = await decoder.ReadAsync();
		Assert.Equal( CursesInputEventKind.EndOfInput, end.Kind );
	}

	private static CursesInputDecoder CreateDecoder(
		ITerminalInput input) {
		return new CursesInputDecoder(
			input,
			new TerminalDescriptionBuilder( "text" ).Build(),
			TimeSpan.FromMilliseconds( 50 )
		);
	}

	private sealed class ScriptedTerminalInput
		: ITerminalInput {
		private readonly Queue<byte[]> chunks;

		internal ScriptedTerminalInput(
			IEnumerable<byte[]> chunks) {
			ArgumentNullException.ThrowIfNull( chunks );
			this.chunks = new Queue<byte[]>(
				chunks.Select(
					static value => value.ToArray()
				)
			);
		}

		public ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();

			if ( 0 == chunks.Count ) {
				return ValueTask.FromResult( 0 );
			}

			byte[] chunk = chunks.Dequeue();
			if ( chunk.Length > buffer.Length ) {
				throw new InvalidOperationException(
					"The scripted chunk exceeds the decoder read buffer."
				);
			}

			chunk.AsSpan().CopyTo( buffer.Span );
			return ValueTask.FromResult( chunk.Length );
		}
	}

	private sealed class EscapeThenBlockTerminalInput
		: ITerminalInput {
		private int readCount;

		public async ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default) {
			if ( 1 == Interlocked.Increment( ref readCount ) ) {
				buffer.Span[ 0 ] = 0x1B;
				return 1;
			}

			await Task.Delay(
				Timeout.InfiniteTimeSpan,
				cancellationToken
			).ConfigureAwait( false );
			return 0;
		}
	}
}