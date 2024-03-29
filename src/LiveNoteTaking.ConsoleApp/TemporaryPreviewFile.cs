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
			null => throw ObjectConstructionException.FromInvalidMemberValue(GetType(), nameof(_content), content),
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
		<button onclick=""EXPORT('md')"" class=""export-button"" style=""position: fixed; bottom: 0; right: 95px;"">Markdown export</button>
		<button onclick=""EXPORT('html')"" class=""export-button"" style=""position: fixed; bottom: 0; right: 1px;"">HTML export</button>
	</body>
</html>";

			return new(content, previewFilePath);
		}
		catch (ObjectConstructionException objectConstructionException)
		{
			AddMethodParametersValuesAsDebuggingInformation(objectConstructionException);
			throw;
		}
		catch (Exception developerMistake)
		{
			var objectConstructionException = ObjectConstructionException.FromDeveloperMistake(typeof(TemporaryPreviewFile), developerMistake);
			AddMethodParametersValuesAsDebuggingInformation(objectConstructionException);
			throw objectConstructionException;
		}

		void AddMethodParametersValuesAsDebuggingInformation(ObjectConstructionException objectConstructionException)
		{
			objectConstructionException.AddDebuggingInformation(nameof(portNumber), portNumber);
			objectConstructionException.AddDebuggingInformation(nameof(markdownRenderingUrl), markdownRenderingUrl);
		}
	}

	public void Dispose()
	{
		if (File.Exists(Path))
		{
			File.Delete(Path);
		}
	}
}
