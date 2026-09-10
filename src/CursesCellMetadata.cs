namespace Icod.DCurses;

/// <summary>
/// Represents terminal-independent semantic metadata associated with retained logical content.
/// </summary>
/// <remarks>
/// Version 1.1 introduces hyperlinks as the first semantic metadata kind. The type is deliberately
/// distinct from <see cref="CursesStyle"/>, which remains presentation-only. The 1.1 constructor
/// requires a hyperlink because no other metadata kind exists yet, while the property is nullable so
/// future semantic kinds can be added without later weakening the published return-nullability contract.
/// </remarks>
public sealed record CursesCellMetadata {
	/// <summary>Initializes semantic metadata containing one hyperlink.</summary>
	/// <param name="hyperlink">The hyperlink semantic.</param>
	public CursesCellMetadata( CursesHyperlink hyperlink ) {
		ArgumentNullException.ThrowIfNull( hyperlink );
		Hyperlink = hyperlink;
	}

	/// <summary>Gets the optional hyperlink semantic.</summary>
	/// <remarks>Instances created by the 1.1 constructor always contain a hyperlink.</remarks>
	public CursesHyperlink? Hyperlink {
		get;
	}
}
