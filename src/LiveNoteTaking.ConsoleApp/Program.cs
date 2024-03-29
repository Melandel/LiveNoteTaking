using Markdig;
using Markdown.ColorCode.CSharpToColoredHtml;

try
{
	return args switch
	{
		[ "-h" or "--html", var pathToFileContainingEnhancedMarkdown ] => await BuildHtmlFromEnhancedMarkdown(MarkdownFileAbsolutePath.FromString(pathToFileContainingEnhancedMarkdown)),
		[ var pathToFileContainingEnhancedMarkdown ] => StartHtmlRenderingServer(MarkdownFileAbsolutePath.FromString(pathToFileContainingEnhancedMarkdown)),
#if DEBUG
		       //                                                                   👇 ...../csproj/bin/Debug/netX.Y                 ->     👇  gitRepoRoot/demofile.md
		[ ] => StartHtmlRenderingServer(MarkdownFileAbsolutePath.FromString(Path.GetFullPath($"{System.AppDomain.CurrentDomain.BaseDirectory}/../../../../../example.md"))),
#endif
		_ => throw new Exception($"Expecting either <filepath>, either \"--snapshot <filepath>\", but got {string.Join(' ', args)} instead.")
	};
}
catch (Exception ex)
{
	Console.Error.WriteLine(ex.Message);
	return 1;
}

async Task<int> BuildHtmlFromEnhancedMarkdown(MarkdownFileAbsolutePath pathToFileContainingEnhancedMarkdown)
{
	var input = new MarkdownFileLivePreviewInput(pathToFileContainingEnhancedMarkdown);
	var standardMarkdownText = await ReturnStandardMarkdownFromEnhancedMarkdown(input);
	var markdigPipeline = new Markdig.MarkdownPipelineBuilder()
		.UseAdvancedExtensions()
		.UseColorCodeWithCSharpToColoredHtml()
		.Build();
	var markdownAsHtml = Markdig.Markdown.ToHtml(standardMarkdownText, markdigPipeline);
	var html = @$"<!DOCTYPE html>
<html>
	<head>
		<meta http-equiv=""content-type"" content=""text/html; charset=utf-8"">
		<title>{Path.GetFileNameWithoutExtension(input.MarkdownFileAbsolutePath)}</title>
		<style>{input.CascadingStyleSheetsRules}</style>
	</head>
	<body>
		<div id=""markdownviewer"">{markdownAsHtml}</div>
	</body>
</html>";

	Console.Out.WriteLine(html);
	return 0;
}

async Task<string> ReturnStandardMarkdownFromEnhancedMarkdown(MarkdownFileLivePreviewInput input)
{
	var markdownToHtmlConverter = EnhancedMarkdownToStandardMarkdownConverter.Create(
		input.NumberOfItemsInHtmlGenerationCacheTriggeringCleanup,
		input.AcceptableDataFreshnessInSecondsAfterHtmlGenerationCacheCleanup);

	var enhancedMarkdownText = EnhancedMarkdownText.From(File.ReadAllText(input.MarkdownFileAbsolutePath));
	var standardMarkdownText = await markdownToHtmlConverter.Convert(enhancedMarkdownText);
	return standardMarkdownText;
}

int StartHtmlRenderingServer(MarkdownFileAbsolutePath pathToFileContainingEnhancedMarkdown)
{
	var input = new MarkdownFileLivePreviewInput(pathToFileContainingEnhancedMarkdown);
	using var notificationMechanism = NotificationMechanism<FileUpdateNotification>.Create();

	using var markdownRenderingServer = MarkdownRenderingServer.CreateUsingRandomPort(
		input.MarkdownFileAbsolutePath,
		input.CascadingStyleSheetsRules,
		input.NumberOfItemsInHtmlGenerationCacheTriggeringCleanup,
		input.AcceptableDataFreshnessInSecondsAfterHtmlGenerationCacheCleanup,
		input.NumberOfMarkdownFileReadRetries,
		input.WaitTimeBeforeEachMarkdownFileReadRetryInMilliseconds,
		notificationMechanism);
	_ = markdownRenderingServer.RunAsync();
	Console.WriteLine($"Markdown rendering server: {markdownRenderingServer.ServerSideEventEndpointRoute}");

	using var previewFile = TemporaryPreviewFile.Create(
		input.MarkdownFileAbsolutePath,
		markdownRenderingServer.Port,
		markdownRenderingServer.ServerSideEventEndpointRoute,
		markdownRenderingServer.ReloadRoute,
		markdownRenderingServer.DocumentExportRoute,
		input.CascadingStyleSheetsRules);
	Console.WriteLine($"Temporary preview file: {previewFile}");

	previewFile.Save();
	_ = WebBrowser.OpenInsideNewTab(previewFile.Path);

	using var fileUpdateNotifier = FileUpdateNotifier.Create(
		input.MarkdownFileAbsolutePath,
		notificationMechanism);
	fileUpdateNotifier.Start();

	var sb = new System.Text.StringBuilder();
	while (true)
	{
		var chunk = Console.ReadLine();
		if (chunk == "")
		{
			return 0;
		}
		else if (chunk == "<LiveNote>")
		{
			var enhancedMarkdownText = System.Text.Json.JsonSerializer.Deserialize<string>(sb.ToString())!;
			var fileUpdateNotification = FileUpdateNotification.FromMarkdownText(enhancedMarkdownText);

			notificationMechanism.SendNotification(fileUpdateNotification);

			sb = new System.Text.StringBuilder();
		}
		else
		{
			sb.Append(chunk);
		}
	}
}
