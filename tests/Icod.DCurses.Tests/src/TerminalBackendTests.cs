using System.Reflection;
using System.Text;
using Icod.DCurses.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class TerminalBackendTests
{
    [Fact]
    public async Task InMemoryBackendOperatesWithoutConsoleOrNativeInterop()
    {
        FakeTerminalInput input = new(Encoding.UTF8.GetBytes("q"));
        FakeTerminalOutput output = new();
        FakeTerminalModeController modes = new();
        TerminalDescription capabilities =
            new TerminalDescriptionBuilder("memory-terminal").Build();

        TerminalBackend backend = new(
            new TerminalEndpoint("memory-input", true),
            new TerminalEndpoint("memory-output", true),
            capabilities,
            input,
            output,
            new FakeTerminalDimensionProvider(new TerminalSize(132, 43)),
            modes);

        Assert.True(backend.IsInteractive);
        Assert.Same(capabilities, backend.Capabilities);

        byte[] inputBuffer = new byte[8];
        int count = await backend.Input.ReadAsync(inputBuffer);

        Assert.Equal(1, count);
        Assert.Equal((byte)'q', inputBuffer[0]);

        byte[] bytes = Encoding.UTF8.GetBytes("frame");
        await backend.Output.WriteAsync(bytes);
        await backend.Output.FlushAsync();

        Assert.Equal(bytes, output.Bytes);
        Assert.Equal(1, output.FlushCount);

        TerminalSize size = backend.Dimensions
            .GetDimensions()
            .GetRequiredValue();

        Assert.Equal(132, size.Columns);
        Assert.Equal(43, size.Rows);

        ITerminalModeState state = backend.Modes
            .CaptureMode()
            .GetRequiredValue();

        TerminalBackendMutationResult restored = backend.Modes.RestoreMode(
            state,
            TerminalModeApplyTiming.Immediately);

        Assert.True(restored.Succeeded);
        Assert.Equal(1, modes.CaptureCount);
        Assert.Equal(1, modes.RestoreCount);
    }

    [Fact]
    public void TerminalSizeRequiresPositiveDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TerminalSize(0, 24));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TerminalSize(80, 0));
    }

    [Fact]
    public void UnavailableResultDoesNotExposeDefaultAsAValue()
    {
        TerminalBackendResult<TerminalSize> result =
            TerminalBackendResult<TerminalSize>.Unavailable("No size.");

        Assert.False(result.IsAvailable);
        Assert.Equal(TerminalBackendStatus.Unavailable, result.Status);
        Assert.Throws<InvalidOperationException>(
            () => result.GetRequiredValue());
    }

    [Fact]
    public void PublicBackendBoundaryDoesNotExposeCommandFrameworkTerminalTypes()
    {
        Assembly assembly = typeof(TerminalBackend).Assembly;

        foreach (Type type in assembly.GetExportedTypes())
        {
            Assert.DoesNotContain(
                "Icod.CommandFramework.Terminal",
                GetPublicSignatureTypeNames(type));
        }
    }

    private static string GetPublicSignatureTypeNames(Type type)
    {
        List<Type> referencedTypes = [];

        if (type.BaseType is not null)
        {
            referencedTypes.Add(type.BaseType);
        }

        referencedTypes.AddRange(type.GetInterfaces());

        foreach (ConstructorInfo constructor in type.GetConstructors())
        {
            referencedTypes.AddRange(
                constructor.GetParameters().Select(parameter => parameter.ParameterType));
        }

        foreach (MethodInfo method in type.GetMethods(
            BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.Public
            | BindingFlags.DeclaredOnly))
        {
            referencedTypes.Add(method.ReturnType);
            referencedTypes.AddRange(
                method.GetParameters().Select(parameter => parameter.ParameterType));
        }

        foreach (PropertyInfo property in type.GetProperties(
            BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.Public
            | BindingFlags.DeclaredOnly))
        {
            referencedTypes.Add(property.PropertyType);
        }

        return string.Join(
            Environment.NewLine,
            referencedTypes.SelectMany(ExpandType)
                .Select(referencedType => referencedType.FullName ?? referencedType.Name));
    }

    private static IEnumerable<Type> ExpandType(Type type)
    {
        yield return type;

        if (type.HasElementType && type.GetElementType() is Type elementType)
        {
            foreach (Type nested in ExpandType(elementType))
            {
                yield return nested;
            }
        }

        if (type.IsGenericType)
        {
            foreach (Type argument in type.GetGenericArguments())
            {
                foreach (Type nested in ExpandType(argument))
                {
                    yield return nested;
                }
            }
        }
    }

    private sealed class FakeTerminalInput : ITerminalInput
    {
        private readonly Queue<byte> bytes;

        public FakeTerminalInput(IEnumerable<byte> bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            this.bytes = new Queue<byte>(bytes);
        }

        public ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (buffer.Length == 0)
            {
                return ValueTask.FromResult(0);
            }

            int count = 0;
            while (count < buffer.Length && bytes.Count > 0)
            {
                buffer.Span[count++] = bytes.Dequeue();
            }

            return ValueTask.FromResult(count);
        }
    }

    private sealed class FakeTerminalOutput : ITerminalOutput
    {
        private readonly List<byte> bytes = [];

        public byte[] Bytes => bytes.ToArray();

        public int FlushCount { get; private set; }

        public ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bytes.AddRange(buffer.ToArray());
            return ValueTask.CompletedTask;
        }

        public ValueTask FlushAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FlushCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeTerminalDimensionProvider : ITerminalDimensionProvider
    {
        private readonly TerminalSize size;

        public FakeTerminalDimensionProvider(TerminalSize size)
        {
            this.size = size;
        }

        public TerminalBackendResult<TerminalSize> GetDimensions()
        {
            return TerminalBackendResult<TerminalSize>.Available(size);
        }
    }

    private sealed class FakeTerminalModeController : ITerminalModeController
    {
        public int CaptureCount { get; private set; }

        public int RestoreCount { get; private set; }

        public TerminalBackendResult<ITerminalModeState> CaptureMode()
        {
            CaptureCount++;
            return TerminalBackendResult<ITerminalModeState>.Available(
                new FakeTerminalModeState());
        }

        public TerminalBackendMutationResult RestoreMode(
            ITerminalModeState state,
            TerminalModeApplyTiming timing)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (!Enum.IsDefined(timing))
            {
                throw new ArgumentOutOfRangeException(nameof(timing));
            }

            Assert.IsType<FakeTerminalModeState>(state);
            RestoreCount++;
            return TerminalBackendMutationResult.Success();
        }
    }

    private sealed class FakeTerminalModeState : ITerminalModeState
    {
    }
}
