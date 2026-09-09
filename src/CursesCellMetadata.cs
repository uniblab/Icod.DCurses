namespace Icod.DCurses;

/// <summary>
/// Represents terminal-independent semantic metadata associated with retained logical content.
/// </summary>
/// <remarks>
/// Version 1.1 introduces hyperlinks as the first semantic metadata kind. The type is deliberately
/// distinct from <see cref="CursesStyle"/>, which remains presentation-only.
/// </remarks>
public sealed record CursesCellMetadata {
	/// <summary>Initializes semantic metadata containing one hyperlink.</summary>
	/// <param name="hyperlink">The hyperlink semantic.</param>
	public CursesCellMetadata( CursesHyperlink hyperlink ) {
		ArgumentNullException.ThrowIfNull( hyperlink );
		Hyperlink = hyperlink;
	}

	/// <summary>Gets the hyperlink semantic.</summary>
	public CursesHyperlink Hyperlink {
		get;
	}
}
