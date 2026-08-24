using System.Reflection;
using Icod.DCurses;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    public void ProjectReferenceLoadsDCursesAssembly()
    {
        Assembly assembly = typeof(LibraryMarker).Assembly;

        Assert.Equal(LibraryMarker.Name, assembly.GetName().Name);
    }
}
