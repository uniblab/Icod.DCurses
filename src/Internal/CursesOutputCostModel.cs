namespace Icod.DCurses.Internal;

using System.Text;
using Icod.TermInfo;

/// <summary>
/// Computes deterministic encoded-byte costs for candidate refresh output.
/// </summary>
internal sealed class CursesOutputCostModel {
	private readonly Encoding applicationEncoding;

	internal CursesOutputCostModel(
		Encoding applicationEncoding
	) {
		ArgumentNullException.ThrowIfNull( applicationEncoding );
		this.applicationEncoding = applicationEncoding;
	}

	internal int GetApplicationTextByteCount(
		string value
	) {
		ArgumentNullException.ThrowIfNull( value );
		return this.applicationEncoding.GetByteCount( value );
	}

	internal static int GetTerminalStringByteCount(
		string value,
		int affectedLines = 1
	) {
		ArgumentNullException.ThrowIfNull( value );
		if ( 0 >= affectedLines ) {
			throw new ArgumentOutOfRangeException( nameof( affectedLines ) );
		}

		int byteCount = 0;
		TermInfoOutput.TPuts(
			value,
			affectedLines,
			_ => byteCount = checked( byteCount + 1 )
		);
		return byteCount;
	}
}
