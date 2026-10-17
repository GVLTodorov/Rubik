// Records the README demo in a real Chromium: Swagger UI opens, the UI button in its top bar opens
// the React app, Scramble plays, and once the cube settles Solve plays the solution stage by stage.
// Every wait targets a specific element, so a broken flow fails with a TimeoutException and a
// non-zero exit code: this doubles as an end-to-end smoke test of the running app.
//
// Each tab is recorded to its own .webm (Swagger, then the app). The paths are printed in order
// as VIDEO_PATH=... lines; .github/workflows/demo.yml joins them into docs/demo.gif.
//
//   dotnet run --project backend/Rubik.Demo -- http://localhost:5080 artifacts/demo-video

using System.Text.RegularExpressions;
using Microsoft.Playwright;

var apiUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5080";
var outputDir = args.Length > 1 ? args[1] : "artifacts/demo-video";
Directory.CreateDirectory(outputDir);

const int DefaultTimeoutMs = 30_000;
// The longest solutions are about 170 moves at 250 ms each.
const int SolveTimeoutMs = 120_000;
var viewport = new ViewportSize { Width = 1024, Height = 1500 };

Console.WriteLine($"Recording demo against {apiUrl}, output dir {outputDir}");

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
var context = await browser.NewContextAsync(new BrowserNewContextOptions
{
    ViewportSize = viewport,
    RecordVideoDir = outputDir,
    RecordVideoSize = new RecordVideoSize { Width = viewport.Width, Height = viewport.Height },
});
context.SetDefaultTimeout(DefaultTimeoutMs);
// Expect's own default (5 s) is about as long as a 20-move scramble takes to play.
Assertions.SetDefaultExpectTimeout(DefaultTimeoutMs);

Console.WriteLine("Opening Swagger UI...");
var swagger = await context.NewPageAsync();
LogDiagnostics(swagger, "swagger");
await swagger.GotoAsync($"{apiUrl}/swagger");
await swagger.Locator(".opblock").First.WaitForAsync();
var uiButton = swagger.Locator(".rubik-links a", new() { HasText = "UI" });
await uiButton.WaitForAsync();
await swagger.WaitForTimeoutAsync(2_000);
await uiButton.HoverAsync();
await swagger.WaitForTimeoutAsync(800);

Console.WriteLine("Clicking the UI button...");
var app = await swagger.RunAndWaitForPopupAsync(() => uiButton.ClickAsync());
LogDiagnostics(app, "app");
// Closing Swagger ends its recording at the click, so the two videos play back to back.
await swagger.CloseAsync();

var stage = app.Locator("main.stage");
var scrambleButton = app.GetByRole(AriaRole.Button, new() { Name = "Scramble", Exact = true });
var solveButton = app.GetByRole(AriaRole.Button, new() { Name = "Solve", Exact = true });
await Assertions.Expect(scrambleButton).ToBeEnabledAsync();
await app.WaitForTimeoutAsync(1_500);

Console.WriteLine("Scrambling...");
await scrambleButton.ClickAsync();
await Assertions.Expect(stage).ToHaveAttributeAsync("data-animating", "true");
await Assertions.Expect(stage).ToHaveAttributeAsync("data-animating", "false");
await Assertions.Expect(solveButton).ToBeEnabledAsync();
await app.WaitForTimeoutAsync(1_500);

Console.WriteLine("Solving...");
await solveButton.ClickAsync();
var status = app.Locator(".solution-status");
await Assertions.Expect(status).ToHaveTextAsync(
    new Regex(@"^Solved in \d+ moves\.$"),
    new() { Timeout = SolveTimeoutMs });
Console.WriteLine(await status.TextContentAsync());
await app.WaitForTimeoutAsync(3_000);

await context.CloseAsync(); // flushes the video files
Console.WriteLine($"VIDEO_PATH={await swagger.Video!.PathAsync()}");
Console.WriteLine($"VIDEO_PATH={await app.Video!.PathAsync()}");

// Surfaces browser-side failures in the CI log; without this a broken page only shows up as a
// Playwright timeout.
static void LogDiagnostics(IPage page, string label)
{
    page.Console += (_, msg) => Console.WriteLine($"[{label} console/{msg.Type}] {msg.Text}");
    page.PageError += (_, error) => Console.WriteLine($"[{label} page error] {error}");
    page.RequestFailed += (_, request) => Console.WriteLine($"[{label} request failed] {request.Url}: {request.Failure}");
}
