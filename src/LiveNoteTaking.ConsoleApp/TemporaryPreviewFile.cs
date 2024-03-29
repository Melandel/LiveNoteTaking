using System.Diagnostics;
using System.Text;

record TemporaryPreviewFile : IDisposable
{
	public override string ToString() => Path;
	public static implicit operator string(TemporaryPreviewFile obj) => obj.Path;

	readonly string _content;
	public string Path { get; }
	TemporaryPreviewFile(string content, string path)
	{
		_content = content switch
		{
			null => throw ObjectConstructionException.WhenConstructingAMemberFor<TemporaryPreviewFile>(nameof(content), content, "@member must not be null"),
			_ => content
		};
		Path = path;
	}

	public void Save()
	{
		if (File.Exists(Path))
		{
			File.Delete(Path);
		}
		using var fileStream = File.Create(Path);
		var fileContent = new UTF8Encoding(true).GetBytes(_content);
		fileStream.Write(fileContent, 0, fileContent.Length);
	}

	public static TemporaryPreviewFile Create(
		MarkdownFileAbsolutePath markdownFileAbsolutePath,
		int portNumber,
		string markdownRenderingUrl,
		string reloadUrl,
		string documentExportUrl,
		string cascadingStyleSheetsRules)
	{
		try
		{
		// 👇 give access to resources on local storage
			var previewFilePath =  System.IO.Path.Combine(markdownFileAbsolutePath.DirectoryAbsolutePath, $"{portNumber}.html");
			var downloadableExportFilename =  System.IO.Path.GetFileNameWithoutExtension(markdownFileAbsolutePath);

			var content = @$"<!DOCTYPE html>
<html>
	<head>
		<title>{System.IO.Path.GetFileName(markdownFileAbsolutePath)}</title>
		<meta http-equiv=""content-type"" content=""text/html; charset=utf-8"">
		<style>{cascadingStyleSheetsRules}</style>
		<script>
			var source = new EventSource(""{markdownRenderingUrl}"");
			source.onmessage = function(event) {{
				document.getElementById(""markdownviewer"").innerHTML = event.data;
			}}

			function RELOAD()
			{{
				var xhr = new XMLHttpRequest();
				xhr.responseType = ""blob"";
				xhr.open(""POST"", ""{reloadUrl}"", true); // true for asynchronous
				xhr.setRequestHeader('Content-Type', 'application/json');
				xhr.send(JSON.stringify({{}}));
			}}

			function EXPORT(fileformat)
			{{
				var xhr = new XMLHttpRequest();
				xhr.responseType = ""blob"";
				xhr.open(""GET"", ""{documentExportUrl}"" + fileformat, true); // true for asynchronous
				xhr.onload = function() {{
					if (xhr.readyState == 4 && xhr.status == 200)
					{{
						const href = URL.createObjectURL(xhr.response);
						const aElement = document.createElement('a');
						aElement.setAttribute('download', ""{downloadableExportFilename}."" + fileformat);
						aElement.href = href;
						aElement.setAttribute('target', '_blank');
						aElement.click();
						URL.revokeObjectURL(href);
					}}
				}}
				xhr.send(null);
			}}
		</script>
	</head>
	<body>
		<div id=""markdownviewer""></div>
		<button title=""Export as HTML"" onclick=""EXPORT('html')"" class=""export-button"" style=""position: fixed; bottom: 0; right: 92px;"">📥 html</button>
		<button title=""Export as Markdown"" onclick=""EXPORT('md')"" class=""export-button"" style=""position: fixed; bottom: 0; right: 35px;"">📥 md</button>
		<button title=""Reset caches and reload preview"" onclick=""RELOAD()"" class=""export-button"" style=""position: fixed; bottom: 0; right: 1px;"">🔄</button>
	</body>
</html>";

			return new(content, previewFilePath);
		}
		catch (ObjectConstructionException objectConstructionException)
		{
			objectConstructionException.EnrichConstructionFailureContextWith<TemporaryPreviewFile>(markdownFileAbsolutePath, portNumber, markdownRenderingUrl, reloadUrl, documentExportUrl, cascadingStyleSheetsRules);
			throw;
		}
		catch (Exception developerMistake)
		{
			throw ObjectConstructionException.WhenConstructingAnInstanceOf<TemporaryPreviewFile>(developerMistake, markdownFileAbsolutePath, portNumber, markdownRenderingUrl, reloadUrl, documentExportUrl, cascadingStyleSheetsRules);
		}
	}

	public void Dispose()
	{
		if (File.Exists(Path))
		{
		Console.WriteLine("Disposing tmp file...");
		var v = Stopwatch.GetTimestamp();
			File.Delete(Path);
		var w = Stopwatch.GetElapsedTime(v);
		Console.WriteLine($"Tmp file diposed in {w.Milliseconds}ms!");
		}
	}
}
