using System.Reflection;

namespace ShareSmallBiz.Api.OpenApi;

/// <summary>
/// True while the build generates openapi/ by booting the app under GetDocument.Insider.
/// That run has no secrets or database, so startup checks and side effects are skipped.
/// </summary>
internal static class OpenApiGeneration
{
    public static bool IsRunning { get; } = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
}
