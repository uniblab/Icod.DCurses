namespace Icod.DCurses.Terminal;

using Icod.TermInfo;

/// <summary>
/// Identifies the outcome of a controlled terminal-backend operation.
/// </summary>
public enum TerminalBackendStatus
{
    /// <summary>The requested operation completed and produced its value.</summary>
    Available,

    /// <summary>The backend exists, but the requested value is unavailable.</summary>
    Unavailable,

    /// <summary>The backend does not support the requested operation.</summary>
    Unsupported,

    /// <summary>The backend operation failed in a controlled manner.</summary>
    Failed
}

/// <summary>
/// Represents a controlled terminal-backend query result.
/// </summary>
/// <typeparam name="T">The value type returned when the operation is available.</typeparam>
public sealed class TerminalBackendResult<T>
{
    private TerminalBackendResult(
        TerminalBackendStatus status,
        T? value,
        string? message)
    {
        Status = status;
        Value = value;
        Message = message;
    }

    /// <summary>Gets the operation status.</summary>
    public TerminalBackendStatus Status { get; }

    /// <summary>
    /// Gets the available value, or the default value when the operation did not
    /// produce one.
    /// </summary>
    public T? Value { get; }

    /// <summary>Gets a controlled diagnostic message, when present.</summary>
    public string? Message { get; }

    /// <summary>Gets whether the result contains an available value.</summary>
    public bool IsAvailable => TerminalBackendStatus.Available == Status;

    /// <summary>
    /// Returns the available value.
    /// </summary>
    /// <returns>The available value.</returns>
    /// <exception cref="InvalidOperationException">
    /// The result does not contain an available value.
    /// </exception>
    public T GetRequiredValue()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(
                Message ?? "The terminal-backend value is not available.");
        }

        return Value!;
    }

    /// <summary>Creates an available result.</summary>
    /// <param name="value">The available backend value.</param>
    /// <returns>An available backend result.</returns>
    public static TerminalBackendResult<T> Available(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new TerminalBackendResult<T>(
            TerminalBackendStatus.Available,
            value,
            null);
    }

    /// <summary>Creates an unavailable result.</summary>
    /// <param name="message">Optional controlled diagnostic detail.</param>
    /// <returns>An unavailable backend result.</returns>
    public static TerminalBackendResult<T> Unavailable(string? message = null)
    {
        return CreateWithoutValue(
            TerminalBackendStatus.Unavailable,
            message,
            "The terminal-backend value is unavailable.");
    }

    /// <summary>Creates an unsupported result.</summary>
    /// <param name="message">Optional controlled diagnostic detail.</param>
    /// <returns>An unsupported backend result.</returns>
    public static TerminalBackendResult<T> Unsupported(string? message = null)
    {
        return CreateWithoutValue(
            TerminalBackendStatus.Unsupported,
            message,
            "The terminal-backend operation is unsupported.");
    }

    /// <summary>Creates a failed result.</summary>
    /// <param name="message">Optional controlled diagnostic detail.</param>
    /// <returns>A failed backend result.</returns>
    public static TerminalBackendResult<T> Failed(string? message = null)
    {
        return CreateWithoutValue(
            TerminalBackendStatus.Failed,
            message,
            "The terminal-backend operation failed.");
    }

    private static TerminalBackendResult<T> CreateWithoutValue(
        TerminalBackendStatus status,
        string? message,
        string fallback)
    {
        return new TerminalBackendResult<T>(
            status,
            default,
            string.IsNullOrWhiteSpace(message) ? fallback : message.Trim());
    }
}

/// <summary>
/// Represents the controlled outcome of mutating terminal-backend state.
/// </summary>
public sealed class TerminalBackendMutationResult
{
    private TerminalBackendMutationResult(
        TerminalBackendStatus status,
        string? message)
    {
        Status = status;
        Message = message;
    }

    /// <summary>Gets the mutation status.</summary>
    public TerminalBackendStatus Status { get; }

    /// <summary>Gets a controlled diagnostic message, when present.</summary>
    public string? Message { get; }

    /// <summary>Gets whether the mutation succeeded.</summary>
    public bool Succeeded => TerminalBackendStatus.Available == Status;

    /// <summary>Creates a successful mutation result.</summary>
    /// <returns>A successful terminal-backend mutation result.</returns>
    public static TerminalBackendMutationResult Success()
    {
        return new TerminalBackendMutationResult(
            TerminalBackendStatus.Available,
            null);
    }

    /// <summary>Creates an unavailable mutation result.</summary>
    /// <param name="message">Optional controlled diagnostic detail.</param>
    /// <returns>An unavailable terminal-backend mutation result.</returns>
    public static TerminalBackendMutationResult Unavailable(string? message = null)
    {
        return Create(
            TerminalBackendStatus.Unavailable,
            message,
            "The terminal-backend mutation is unavailable.");
    }

    /// <summary>Creates an unsupported mutation result.</summary>
    /// <param name="message">Optional controlled diagnostic detail.</param>
    /// <returns>An unsupported terminal-backend mutation result.</returns>
    public static TerminalBackendMutationResult Unsupported(string? message = null)
    {
        return Create(
            TerminalBackendStatus.Unsupported,
            message,
            "The terminal-backend mutation is unsupported.");
    }

    /// <summary>Creates a failed mutation result.</summary>
    /// <param name="message">Optional controlled diagnostic detail.</param>
    /// <returns>A failed terminal-backend mutation result.</returns>
    public static TerminalBackendMutationResult Failed(string? message = null)
    {
        return Create(
            TerminalBackendStatus.Failed,
            message,
            "The terminal-backend mutation failed.");
    }

    private static TerminalBackendMutationResult Create(
        TerminalBackendStatus status,
        string? message,
        string fallback)
    {
        return new TerminalBackendMutationResult(
            status,
            string.IsNullOrWhiteSpace(message) ? fallback : message.Trim());
    }
}

/// <summary>
/// Describes one logical terminal endpoint without exposing native descriptors or
/// handles.
/// </summary>
public sealed class TerminalEndpoint
{
    /// <summary>
    /// Initializes endpoint metadata.
    /// </summary>
    /// <param name="displayName">A nonempty diagnostic display name.</param>
    /// <param name="isInteractive">
    /// Whether the endpoint behaves as an interactive terminal endpoint.
    /// </param>
    public TerminalEndpoint(
        string displayName,
        bool isInteractive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        DisplayName = displayName;
        IsInteractive = isInteractive;
    }

    /// <summary>Gets the diagnostic display name.</summary>
    public string DisplayName { get; }

    /// <summary>Gets whether the endpoint behaves as an interactive terminal.</summary>
    public bool IsInteractive { get; }
}

/// <summary>
/// Represents positive terminal dimensions in character cells.
/// </summary>
public readonly record struct TerminalSize
{
    /// <summary>
    /// Initializes terminal dimensions.
    /// </summary>
    /// <param name="columns">The positive number of columns.</param>
    /// <param name="rows">The positive number of rows.</param>
    public TerminalSize(
        int columns,
        int rows)
    {
        if (columns <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(columns),
                "Terminal columns must be positive.");
        }

        if (rows <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rows),
                "Terminal rows must be positive.");
        }

        Columns = columns;
        Rows = rows;
    }

    /// <summary>Gets the terminal width in columns.</summary>
    public int Columns { get; }

    /// <summary>Gets the terminal height in rows.</summary>
    public int Rows { get; }
}

/// <summary>
/// Identifies when restoration of a captured host terminal mode should occur.
/// </summary>
public enum TerminalModeApplyTiming
{
    /// <summary>Apply the captured state immediately.</summary>
    Immediately,

    /// <summary>Apply the captured state after pending output has drained.</summary>
    AfterOutputDrained,

    /// <summary>
    /// Apply the captured state after pending output has drained and unread input
    /// has been discarded.
    /// </summary>
    AfterOutputDrainedAndInputDiscarded
}

/// <summary>
/// Opaque backend-specific terminal mode state captured for later restoration.
/// </summary>
/// <remarks>
/// Implementations may wrap POSIX termios state, Windows console modes, or
/// deterministic test state. Consumers must not depend on the representation.
/// </remarks>
public interface ITerminalModeState
{
}

/// <summary>
/// Supplies raw terminal input bytes to the curses input decoder.
/// </summary>
public interface ITerminalInput
{
    /// <summary>
    /// Reads terminal input bytes asynchronously.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The number of bytes read, or zero when the input endpoint has reached EOF.
    /// </returns>
    ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Receives terminal output bytes emitted by the curses refresh engine.
/// </summary>
public interface ITerminalOutput
{
    /// <summary>Writes terminal output bytes asynchronously.</summary>
    /// <param name="buffer">The terminal output bytes.</param>
    /// <param name="cancellationToken">Cancellation for the write operation.</param>
    /// <returns>A value task representing the write operation.</returns>
    ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default);

    /// <summary>Flushes buffered terminal output asynchronously.</summary>
    /// <param name="cancellationToken">Cancellation for the flush operation.</param>
    /// <returns>A value task representing the flush operation.</returns>
    ValueTask FlushAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Supplies live terminal dimensions.
/// </summary>
public interface ITerminalDimensionProvider
{
    /// <summary>Queries the current terminal dimensions.</summary>
    /// <returns>The controlled terminal-dimension result.</returns>
    TerminalBackendResult<TerminalSize> GetDimensions();
}

/// <summary>
/// Captures and restores host terminal mode without exposing its native
/// representation.
/// </summary>
public interface ITerminalModeController
{
    /// <summary>Captures the current host terminal mode.</summary>
    /// <returns>The controlled captured-mode result.</returns>
    TerminalBackendResult<ITerminalModeState> CaptureMode();

    /// <summary>
    /// Restores a previously captured host terminal mode.
    /// </summary>
    /// <param name="state">The captured state.</param>
    /// <param name="timing">When the state should be applied.</param>
    /// <returns>The controlled restoration result.</returns>
    TerminalBackendMutationResult RestoreMode(
        ITerminalModeState state,
        TerminalModeApplyTiming timing);
}

/// <summary>
/// Composes the platform-neutral terminal services consumed by DCurses.
/// </summary>
public sealed class TerminalBackend
{
    /// <summary>
    /// Initializes a terminal backend from independent terminal services.
    /// </summary>
    /// <param name="inputEndpoint">The terminal input endpoint metadata.</param>
    /// <param name="outputEndpoint">The terminal output endpoint metadata.</param>
    /// <param name="capabilities">The active terminal capability description.</param>
    /// <param name="input">The terminal input byte source.</param>
    /// <param name="output">The terminal output byte sink.</param>
    /// <param name="dimensions">The live terminal-dimension provider.</param>
    /// <param name="modes">The host terminal-mode controller.</param>
    public TerminalBackend(
        TerminalEndpoint inputEndpoint,
        TerminalEndpoint outputEndpoint,
        TerminalDescription capabilities,
        ITerminalInput input,
        ITerminalOutput output,
        ITerminalDimensionProvider dimensions,
        ITerminalModeController modes)
    {
        ArgumentNullException.ThrowIfNull(inputEndpoint);
        ArgumentNullException.ThrowIfNull(outputEndpoint);
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(dimensions);
        ArgumentNullException.ThrowIfNull(modes);

        InputEndpoint = inputEndpoint;
        OutputEndpoint = outputEndpoint;
        Capabilities = capabilities;
        Input = input;
        Output = output;
        Dimensions = dimensions;
        Modes = modes;
    }

    /// <summary>Gets the input endpoint metadata.</summary>
    public TerminalEndpoint InputEndpoint { get; }

    /// <summary>Gets the output endpoint metadata.</summary>
    public TerminalEndpoint OutputEndpoint { get; }

    /// <summary>
    /// Gets the immutable terminfo capability description for the terminal.
    /// </summary>
    public TerminalDescription Capabilities { get; }

    /// <summary>Gets the terminal input service.</summary>
    public ITerminalInput Input { get; }

    /// <summary>Gets the terminal output service.</summary>
    public ITerminalOutput Output { get; }

    /// <summary>Gets the terminal dimension provider.</summary>
    public ITerminalDimensionProvider Dimensions { get; }

    /// <summary>Gets the host terminal mode controller.</summary>
    public ITerminalModeController Modes { get; }

    /// <summary>
    /// Gets whether both input and output endpoints are interactive terminal
    /// endpoints.
    /// </summary>
    public bool IsInteractive =>
        InputEndpoint.IsInteractive
        && OutputEndpoint.IsInteractive;
}
