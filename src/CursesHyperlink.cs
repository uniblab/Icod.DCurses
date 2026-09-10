namespace Icod.DCurses;

using System.Text;

/// <summary>
/// Represents one terminal-independent hyperlink semantic attached to retained curses content.
/// </summary>
/// <remarks>
/// The target is stored as an absolute, already URI-encoded ASCII URI. Percent escapes are
/// canonicalized to uppercase hexadecimal. This type never dereferences or activates the target.
/// <see cref="Icod.Terminal.TerminalSession"/> remains the OSC 8 protocol authority and revalidates
/// the value before physical output.
/// </remarks>
public sealed record CursesHyperlink {
	private const int MaximumUriLength = 2_083;
	private const int MaximumIdentifierLength = 128;

	/// <summary>Initializes one hyperlink semantic.</summary>
	/// <param name="uri">A non-empty absolute, already URI-encoded ASCII target.</param>
	/// <param name="identifier">
	/// An optional identifier containing only RFC 3986 unreserved ASCII characters.
	/// Null and empty identifiers are canonicalized to no identifier.
	/// </param>
	public CursesHyperlink(
		string uri,
		string? identifier = null
	) {
		Uri = NormalizeUri( uri );
		Identifier = NormalizeIdentifier( identifier );
	}

	/// <summary>Gets the canonical hyperlink target.</summary>
	public string Uri {
		get;
	}

	/// <summary>Gets the optional hyperlink identifier.</summary>
	public string? Identifier {
		get;
	}

	private static string NormalizeUri( string uri ) {
		ArgumentNullException.ThrowIfNull( uri );
		if ( 0 == uri.Length ) {
			throw new ArgumentException(
				"A curses hyperlink target may not be empty.",
				nameof( uri )
			);
		}
		if ( MaximumUriLength < uri.Length ) {
			throw new ArgumentException(
				"A curses hyperlink target may not exceed 2083 ASCII bytes.",
				nameof( uri )
			);
		}

		int colon = uri.IndexOf( ':' );
		if ( 1 > colon || !IsAsciiLetter( uri[ 0 ] ) ) {
			throw new ArgumentException(
				"A curses hyperlink target must be an absolute URI with a valid scheme.",
				nameof( uri )
			);
		}
		for ( int index = 1; index < colon; index++ ) {
			char character = uri[ index ];
			if ( !IsAsciiLetterOrDigit( character )
				&& '+' != character
				&& '-' != character
				&& '.' != character ) {
				throw new ArgumentException(
					"A curses hyperlink URI contains an invalid scheme character.",
					nameof( uri )
				);
			}
		}

		StringBuilder normalized = new( uri.Length );
		int fragmentCount = 0;
		for ( int index = 0; index < uri.Length; index++ ) {
			char character = uri[ index ];
			if ( 0x21 > character || 0x7E < character || '\\' == character ) {
				throw new ArgumentException(
					"A curses hyperlink URI must contain only encoded printable ASCII URI characters.",
					nameof( uri )
				);
			}
			if ( '#' == character ) {
				fragmentCount++;
				if ( 1 < fragmentCount ) {
					throw new ArgumentException(
						"A curses hyperlink URI may contain at most one fragment delimiter.",
						nameof( uri )
					);
				}
			}
			if ( '%' != character ) {
				normalized.Append( character );
				continue;
			}
			if ( index + 2 >= uri.Length
				|| !IsHexDigit( uri[ index + 1 ] )
				|| !IsHexDigit( uri[ index + 2 ] ) ) {
				throw new ArgumentException(
					"A curses hyperlink URI contains an invalid percent escape.",
					nameof( uri )
				);
			}

			normalized.Append( '%' );
			normalized.Append( char.ToUpperInvariant( uri[ index + 1 ] ) );
			normalized.Append( char.ToUpperInvariant( uri[ index + 2 ] ) );
			index += 2;
		}

		string result = normalized.ToString();
		if ( !System.Uri.TryCreate(
			result,
			UriKind.Absolute,
			out _
		) ) {
			throw new ArgumentException(
				"A curses hyperlink target must be a well-formed absolute URI.",
				nameof( uri )
			);
		}
		return result;
	}

	private static string? NormalizeIdentifier( string? identifier ) {
		if ( string.IsNullOrEmpty( identifier ) ) {
			return null;
		}
		if ( MaximumIdentifierLength < identifier.Length ) {
			throw new ArgumentException(
				"A curses hyperlink identifier may not exceed 128 ASCII bytes.",
				nameof( identifier )
			);
		}

		foreach ( char character in identifier ) {
			if ( !IsUnreservedAscii( character ) ) {
				throw new ArgumentException(
					"A curses hyperlink identifier may contain only RFC 3986 unreserved ASCII characters.",
					nameof( identifier )
				);
			}
		}
		return identifier;
	}

	private static bool IsUnreservedAscii( char character ) {
		return IsAsciiLetterOrDigit( character )
			|| '-' == character
			|| '.' == character
			|| '_' == character
			|| '~' == character
		;
	}

	private static bool IsAsciiLetterOrDigit( char character ) {
		return IsAsciiLetter( character )
			|| ( '0' <= character && '9' >= character );
	}

	private static bool IsAsciiLetter( char character ) {
		return ( 'A' <= character && 'Z' >= character )
			|| ( 'a' <= character && 'z' >= character );
	}

	private static bool IsHexDigit( char character ) {
		return ( '0' <= character && '9' >= character )
			|| ( 'A' <= character && 'F' >= character )
			|| ( 'a' <= character && 'f' >= character );
	}
}
