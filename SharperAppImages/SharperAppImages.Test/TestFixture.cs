using System.Reflection;
using PathLib;
using SharperAppImages;

namespace Integration.Tests;

public static class TestFixture
{
    private static readonly Lazy<IPath> LazyTestData = new(() => new CompatPath(AppDomain.CurrentDomain.BaseDirectory) / "TestData");
    
    public static IPath TestData => LazyTestData.Value;
}