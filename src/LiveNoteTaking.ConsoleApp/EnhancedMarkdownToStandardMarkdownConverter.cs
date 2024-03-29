using System.Diagnostics;
using System.Dynamic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WebMarkupMin.Core;

class EnhancedMarkdownToStandardMarkdownConverter
{
	public int NumberOfCachedItems => _cachedGeneratedStandardMarkdown.Count;
	const string AnsiColorCodesPattern = @"\x1B\[[^@-~]*[@-~]";
	static readonly string PlantumlJar = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "tools/plantuml/plantuml.jar");
	readonly GeneratedStandardMarkdownTextCache _cachedGeneratedStandardMarkdown;
	EnhancedMarkdownToStandardMarkdownConverter(GeneratedStandardMarkdownTextCache cachedGeneratedStandardMarkdown)
	{
		_cachedGeneratedStandardMarkdown = cachedGeneratedStandardMarkdown;
	}

	public async Task<StandardMarkdownText> Convert(EnhancedMarkdownText markdownText, bool resetCache = false)
	{
		if (resetCache)
		{
			_cachedGeneratedStandardMarkdown.Reset();
		}

		return await InlineCustomMarkdownSyntaxes(markdownText);
	}

	async Task<StandardMarkdownText> InlineCustomMarkdownSyntaxes(EnhancedMarkdownText markdownWithCustomizedSyntax)
	{
		var orderedStandardMarkdownSlices = (await GenerateStandardMarkdownTexts(markdownWithCustomizedSyntax)).Values;

		var standardMarkdown = new StringBuilder();
		foreach (var markdown in orderedStandardMarkdownSlices)
		{
			standardMarkdown.AppendLine(markdown);
		}

		return StandardMarkdownText.From(standardMarkdown);
	}

	async Task<SortedList<int, StandardMarkdownText>> GenerateStandardMarkdownTexts(EnhancedMarkdownText markdownWithCustomizedSyntax)
	{
		var markdownPartitionExtractor = new MarkdownPartitionExtractor();
		var markdownPartition = markdownPartitionExtractor.ExtractFrom(markdownWithCustomizedSyntax);

		var orderedMarkdownParts = new SortedList<int, StandardMarkdownText>();
		var concurrencyLock = new object();

		await Task.WhenAll(
			markdownPartition
			.Select(part => Task.Run(async () => {
				var markdownText = part.Type switch
				{
					MarkdownPartType.DeclarativeDiagrammingSnippet => await BuildSvgFromD2DiagramSnippetWithTransparentBackground(part.Content),
					MarkdownPartType.PlantumlSnippet => await BuildSvgFromPlantumlSnippetWithTransparentBackground(part.Content),
					MarkdownPartType.MermaidSnippet => await BuildSvgFromMermaidSnippetWithTransparentBackground(part.Content),
					MarkdownPartType.DataRowsSnippet => BuildMarkdownTableFromDataRowsSnippet(part.Content),
					_ => InlineCustomizedMarkdownSyntax(part.Content)
				};

				lock (concurrencyLock) { orderedMarkdownParts.Add(part.FirstLineNumber, markdownText); }
			}))
			.ToArray()
		);
		return orderedMarkdownParts;
	}

	StandardMarkdownText BuildMarkdownTableFromDataRowsSnippet(EnhancedMarkdownText dataRowsSnippet)
	{
		var dataAsString = string.Join(Environment.NewLine, dataRowsSnippet.Split(Environment.NewLine)[1..^1]);
		dynamic data = dataAsString.TrimStart() switch
		{
			[ '{', .. ] => JsonConvert.DeserializeObject<ExpandoObject>(dataAsString, new ExpandoObjectConverter()),
			[ '[', .. ] => JsonConvert.DeserializeObject<List<ExpandoObject>>(dataAsString, new ExpandoObjectConverter()),
			_ => throw new NotImplementedException()
		};
		var markdownTable = StandardMarkdownText.Table.From(data);
		return StandardMarkdownText.From(markdownTable);
	}

	StandardMarkdownText InlineCustomizedMarkdownSyntax(EnhancedMarkdownText markdownWithCustomizedSyntax)
	{
		if (!_cachedGeneratedStandardMarkdown.ContainsKey(markdownWithCustomizedSyntax))
		{
			var lines = markdownWithCustomizedSyntax.Split(Environment.NewLine).ToArray();
			var standardMarkdownBuilder = new StringBuilder();
			foreach (var line in lines)
			{
				var standardMarkdown = InlineCustomTokens(line);
				standardMarkdownBuilder.AppendLine(standardMarkdown);
			}

			_cachedGeneratedStandardMarkdown.AddStandardMarkdownText(markdownWithCustomizedSyntax, StandardMarkdownText.From(standardMarkdownBuilder));
		}

		return _cachedGeneratedStandardMarkdown[markdownWithCustomizedSyntax];
	}

	string InlineCustomTokens(string line)
	=> line switch
	{
		"" => "",
		_ when line.StartsWith(EnhancedMarkdownText.ExpandableSectionEnhancement.StartToken) => InlineSpoilerStart(line),
		_ when line.EndsWith(EnhancedMarkdownText.ExpandableSectionEnhancement.EndToken) => InlineSpoilerEnd(),
		_ when line.StartsWith(EnhancedMarkdownText.MultiColumnsEnhancement.StartToken) => InlineMultiColumnStart(line),
		_ when line.StartsWith(EnhancedMarkdownText.MultiColumnsEnhancement.AddToken) => InlineMultiColumnAdd(line),
		_ when line.StartsWith(EnhancedMarkdownText.MultiColumnsEnhancement.EndToken) => InlineMultiColumnEnd(),
		_ => line
	};

	string InlineSpoilerEnd() => $"</details>{Environment.NewLine}";
	string InlineMultiColumnEnd() => $"</div></div>{Environment.NewLine}";

	string InlineMultiColumnAdd(string trimmedLineWithCustomizedSyntax)
	{
		var multiColumnAdd = new StringBuilder("</div>");
		multiColumnAdd.Append(@"<div class=""vertical-column"">");
		if (trimmedLineWithCustomizedSyntax.Length > EnhancedMarkdownText.MultiColumnsEnhancement.AddToken.Length)
		{
			var title = trimmedLineWithCustomizedSyntax[(EnhancedMarkdownText.MultiColumnsEnhancement.AddToken.Length)..].Trim().Capitalize();
			if (!string.IsNullOrEmpty(title))
			{
				multiColumnAdd.Append($@"<div class=""vertical-column-title"">{title}</div>");
			}
		}
		return multiColumnAdd.AppendLine().ToString();
	}

	string InlineMultiColumnStart(string trimmedLineWithCustomizedSyntax)
	{
		var multiColumnStart = new StringBuilder();
		multiColumnStart.Append(@"<div class=""contains-vertical-columns""");
		var title = "";
		if (trimmedLineWithCustomizedSyntax.Length > EnhancedMarkdownText.MultiColumnsEnhancement.StartToken.Length)
		{
			var tokensText = trimmedLineWithCustomizedSyntax[(EnhancedMarkdownText.MultiColumnsEnhancement.StartToken.Length)..];
			if (!Char.IsWhiteSpace(tokensText[0]))
			{
				var tokens = tokensText.Split(new char[0], StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
				var weights = tokens.First();
				if (weights.Any())
				{
					multiColumnStart.Append(@" style=""grid-template-columns:");
					foreach (var weight in weights)
					{
						multiColumnStart.Append($" {weight}fr");
					}
					multiColumnStart.Append(";\"");
				}
				title = String.Join(' ', tokens.Skip(1)).Trim().Capitalize();
			}
			else
			{
				title = String.Join(' ', tokensText).Trim().Capitalize();
			}
		}
		multiColumnStart.Append(@">");
		multiColumnStart.Append(@"<div class=""vertical-column"">");
		if (!string.IsNullOrEmpty(title))
		{
			multiColumnStart.Append($@"<div class=""vertical-column-title"">{title}</div>");
		}

		return multiColumnStart.AppendLine().ToString();
	}

	string InlineSpoilerStart(string trimmedLineWithCustomizedSyntax)
	{
		var collapsibleSpoilerStart = new StringBuilder("<details>");

		if (trimmedLineWithCustomizedSyntax.Length > EnhancedMarkdownText.ExpandableSectionEnhancement.StartToken.Length)
		{
			var spoilerTitle = trimmedLineWithCustomizedSyntax[(EnhancedMarkdownText.ExpandableSectionEnhancement.StartToken.Length+1)..].Trim('<', ' ');
			collapsibleSpoilerStart.Append($"<summary");
			collapsibleSpoilerStart.Append(trimmedLineWithCustomizedSyntax switch
			{
			_ when trimmedLineWithCustomizedSyntax.Contains("{<<<") => @" class=""summary-color-3"">",
			_ when trimmedLineWithCustomizedSyntax.Contains("{<<") => @" class=""summary-color-2"">",
			_ => @">",
			});
			collapsibleSpoilerStart.Append($"{spoilerTitle}</summary>");
		}

		return collapsibleSpoilerStart.AppendLine().ToString();
	}

	async Task<StandardMarkdownText> BuildSvgFromD2DiagramSnippetWithTransparentBackground(EnhancedMarkdownText d2DiagramSnippet)
	{
		var sw = new Stopwatch();
		if (!_cachedGeneratedStandardMarkdown.ContainsKey(d2DiagramSnippet))
		{
				var diagramDescription = new StringBuilder();
				diagramDescription.AppendLine("style.fill: transparent");
				diagramDescription.AppendLine("layers.*.style.fill: transparent");
				diagramDescription.AppendLine("**.style.text-transform: capitalize");
				var firstLine = d2DiagramSnippet.Split(Environment.NewLine).First();
				if (firstLine.Length > EnhancedMarkdownText.DiagrammingEnhancement.D2.StartToken.Length)
				{
					var title = firstLine[(EnhancedMarkdownText.DiagrammingEnhancement.D2.StartToken.Length+1)..].Trim();
					if (!string.IsNullOrWhiteSpace(title))
					{
						diagramDescription.AppendLine($@"
title: {{
	label: {title}
	near: top-center
	shape: text
	style.font-size: 40
	style.underline: true
}}");
					}
				}
				diagramDescription.AppendLine(string.Join(Environment.NewLine, d2DiagramSnippet.Split(Environment.NewLine)[1..^1]));
			var isAnimated = d2DiagramSnippet.Contains("layers: {") || d2DiagramSnippet.Contains("scenarios: {") || d2DiagramSnippet.Contains("steps: {");
			if (isAnimated)
			{
				var inputFile = GenerateNewTmpD2FilePath();
				using (var fw = new StreamWriter(inputFile))
				{
					fw.WriteLine(diagramDescription);
				}

				var outputFile = GenerateNewTmpSvgFilePath();
				using var process = new Process();
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.FileName = "d2.exe";
				process.StartInfo.Arguments = $@"--force-appendix --scale 0.7 --pad 42 -t 300 -l tala --animate-interval 1200 ""{inputFile}"" ""{outputFile}""";

				var stdErr = new StringBuilder();
				process.ErrorDataReceived += (sender, args) => {
					stdErr.AppendLine(args.Data);
				};

				try
				{
					process.Start();
					process.BeginErrorReadLine();

					await process.WaitForExitAsync();
					process.WaitForExit();

					var d2DiagramAsSvg = File.ReadAllText(outputFile);

					var diagramHasIncorrectSyntax = string.IsNullOrWhiteSpace(d2DiagramAsSvg);
					if (diagramHasIncorrectSyntax)
					{
						return _cachedGeneratedStandardMarkdown.MostRecentlyCachedDiagramWithoutSyntaxError switch
						{
							null => EncapsulateDiagramWithParentDiv("d2", BuildD2SyntaxErrorSnippet(stdErr)),
							var latestCorrectCachedDiagram when latestCorrectCachedDiagram.DiagramDescription.LooksLike(d2DiagramSnippet) => latestCorrectCachedDiagram.GeneratedSvg,
							_ => EncapsulateDiagramWithParentDiv("d2", BuildD2SyntaxErrorSnippet(stdErr))
						};
					}
					_cachedGeneratedStandardMarkdown.AddDiagram(d2DiagramSnippet, EncapsulateDiagramWithParentDiv("d2", BuildD2DiagramAsSvg(d2DiagramAsSvg)));
				}
				catch (Exception ex)
				{
					return BuildD2HtmlFromD2Incident(d2DiagramSnippet, ex.Message);
				}
			}
			else
			{
				using var process = new Process();
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.FileName = "d2.exe";
				process.StartInfo.Arguments = "--force-appendix --scale 0.7 --pad 42 -t 300 -l tala -";

				var stdOutput = new StringBuilder();
				process.OutputDataReceived += (sender, args) => {
					stdOutput.AppendLine(args.Data);
				};

				var stdErr = new StringBuilder();
				process.ErrorDataReceived += (sender, args) => {
					stdErr.AppendLine(args.Data);
				};

				try
				{
					sw.Start();
					process.Start();
					process.BeginOutputReadLine();
					process.BeginErrorReadLine();

					process.StandardInput.WriteLine(diagramDescription);
					process.StandardInput.Flush();
					process.StandardInput.Close();
					await process.WaitForExitAsync();
					process.WaitForExit();
				}
				catch (Exception ex)
				{
					return BuildD2HtmlFromD2Incident(d2DiagramSnippet, ex.Message);
				}

				var d2DiagramAsSvg = stdOutput.ToString();
				var diagramHasIncorrectSyntax = string.IsNullOrWhiteSpace(d2DiagramAsSvg);
				if (diagramHasIncorrectSyntax)
				{
					return _cachedGeneratedStandardMarkdown.MostRecentlyCachedDiagramWithoutSyntaxError switch
					{
						null => EncapsulateDiagramWithParentDiv("d2", BuildD2SyntaxErrorSnippet(stdErr)),
						var latestCorrectCachedDiagram when latestCorrectCachedDiagram.DiagramDescription.LooksLike(d2DiagramSnippet) => latestCorrectCachedDiagram.GeneratedSvg,
						_ => EncapsulateDiagramWithParentDiv("d2", BuildD2SyntaxErrorSnippet(stdErr))
					};
				}
				_cachedGeneratedStandardMarkdown.AddDiagram(d2DiagramSnippet, EncapsulateDiagramWithParentDiv("d2", BuildD2DiagramAsSvg(d2DiagramAsSvg)));
			}
		}

		sw.Stop();
		Console.Error.WriteLine($"d2 diagram generation: {sw.Elapsed:c}ms.");
		return _cachedGeneratedStandardMarkdown[d2DiagramSnippet];
	}

	StandardMarkdownText BuildD2HtmlFromD2Incident(string d2DiagramSnippet, string incidentMessage)
	{
		var html = new StringBuilder();
		html.AppendLine("<div class=\"generated-diagram-d2\">");
		html.AppendLine();
		html.AppendLine(InsertCommentIntoD2Snippet(d2DiagramSnippet, $"☠️ {incidentMessage}"));
		html.AppendLine();
		html.AppendLine("</div>");
		html.AppendLine();

		return StandardMarkdownText.From(html);
	}

	string InsertCommentIntoD2Snippet(string d2DiagramSnippet, string message)
	=> InsertCommentIntoCodeSnippet(d2DiagramSnippet, message, "#");

	StandardMarkdownText EncapsulateDiagramWithParentDiv(string diagramType, StandardMarkdownText diagramAsSvgOrErrorAsMarkdownCodeSnippet)
	{
		var div = new StringBuilder();
		div.Append($"<div class=\"generated-diagram-{diagramType}\"");
		var widthMatch = new Regex(@"width=""?(\d+)""?").Match(diagramAsSvgOrErrorAsMarkdownCodeSnippet.ToString());
		var heightMatch = new Regex(@"height=""?(\d+)""?").Match(diagramAsSvgOrErrorAsMarkdownCodeSnippet.ToString());
		if (!diagramAsSvgOrErrorAsMarkdownCodeSnippet.Contains("width=100%") && widthMatch.Success && heightMatch.Success)
		{
			var widthInPixels = widthMatch.Groups[1].Value;
			var heightInPixels = heightMatch.Groups[1].Value;
			div.Append($" style=\"");

			var maxHeight = "80vh";
			div.Append($"max-height:{maxHeight};");

			var height = $"min({maxHeight}, {heightInPixels}px)";
			div.Append($"height:{height};");

			var maxWidth = "100%";
			div.Append($"max-width:{maxWidth};");

			var aspectRatio = $"{widthInPixels}/{heightInPixels}";
			div.Append($"aspect-ratio:{aspectRatio};");

			var width = $"min({maxWidth}, {widthInPixels}px, {aspectRatio}*{height})";
			div.Append($"width:{width};");
			div.Append("\"");
		}
		div.AppendLine(">");

		div.AppendLine(diagramAsSvgOrErrorAsMarkdownCodeSnippet);

		div.AppendLine("</div>");
		div.AppendLine();

		return StandardMarkdownText.From(div);
	}

	StandardMarkdownText BuildD2DiagramAsSvg(string d2DiagramAsSvg)
	{
			var minifiedDiagramSvg = new HtmlMinifier().Minify(d2DiagramAsSvg).MinifiedContent;
			return StandardMarkdownText.From(minifiedDiagramSvg);
	}

	StandardMarkdownText BuildD2SyntaxErrorSnippet(StringBuilder stdErr)
	{
			var errorAsMarkdownBlockSnippet = new StringBuilder(Environment.NewLine);
			errorAsMarkdownBlockSnippet.AppendLine("```");
			errorAsMarkdownBlockSnippet.AppendLine("🔧 D2 diagram does not build");
			errorAsMarkdownBlockSnippet.AppendLine();
			errorAsMarkdownBlockSnippet.AppendLine(stdErr.ToString().Trim());
			errorAsMarkdownBlockSnippet.AppendLine("```");

			return StandardMarkdownText.From(errorAsMarkdownBlockSnippet);
	}

	async Task<StandardMarkdownText> BuildSvgFromPlantumlSnippetWithTransparentBackground(EnhancedMarkdownText plantumlDiagramSnippet)
	{
		if (!_cachedGeneratedStandardMarkdown.ContainsKey(plantumlDiagramSnippet))
		{
			using var process = new Process();
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.FileName = "java";
			process.StartInfo.Arguments = $"-Dfile.encoding=UTF8 -jar {PlantumlJar} -tsvg -stdrpt:1 -pipe";

			var stdOutput = new StringBuilder();
			process.OutputDataReceived += (sender, args) => {
				stdOutput.AppendLine(args.Data);
			};

			var stdErr = new StringBuilder();
			process.ErrorDataReceived += (sender, args) => {
				stdErr.AppendLine(args.Data);
			};

			var diagramDescription = new StringBuilder();
			var firstLine = plantumlDiagramSnippet.Split(Environment.NewLine).First();
			var isMindMapDiagram = firstLine.Contains(EnhancedMarkdownText.DiagrammingEnhancement.Plantuml.StartToken.MindMap);
			if (firstLine.Length > EnhancedMarkdownText.DiagrammingEnhancement.Plantuml.StartToken.Common.Length)
			{
				if (isMindMapDiagram)
				{
					diagramDescription.AppendLine($"@startmindmap");

					var title = firstLine.Length switch
					{
						var l when l > EnhancedMarkdownText.DiagrammingEnhancement.Plantuml.StartToken.MindMap.Length => firstLine[(EnhancedMarkdownText.DiagrammingEnhancement.Plantuml.StartToken.MindMap.Length + 1)..].Trim(),
						_ => ""
					};
					if (!string.IsNullOrWhiteSpace(title))
					{
						diagramDescription.AppendLine($"title <u>{title}</u>");
					}
				}
				else
				{
					var title = firstLine[(EnhancedMarkdownText.DiagrammingEnhancement.Plantuml.StartToken.Common.Length+1)..].Trim();
					if (!string.IsNullOrWhiteSpace(title))
					{
						diagramDescription.AppendLine($"title <u>{title}</u>");
					}
				}
			}
			diagramDescription.AppendLine(string.Join(Environment.NewLine, plantumlDiagramSnippet.Split(Environment.NewLine)[1..^1]));
			diagramDescription.AppendLine("skinparam backgroundColor transparent");

			if (isMindMapDiagram)
			{
					diagramDescription.AppendLine($"@endmindmap");
			}
			try
			{
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				process.StandardInput.WriteLine(diagramDescription);
				process.StandardInput.Flush();
				process.StandardInput.Close();
				await process.WaitForExitAsync();
				process.WaitForExit();

				if (process.ExitCode == 1)
				{
					var error = stdErr.ToString();
					if (error.Contains("java") || error.Contains(PlantumlJar))
					{
						return BuildPlantumlHtmlFromPlantumlIncident(plantumlDiagramSnippet, error, diagramDescription.ToString());
					}
				}
			}
			catch (Exception ex)
			{
				return BuildPlantumlHtmlFromPlantumlIncident(plantumlDiagramSnippet, ex.Message, diagramDescription.ToString());
			}

			var plantumlErrorMessage = stdErr.ToString();
			var diagramHasIncorrectSyntax = !string.IsNullOrWhiteSpace(plantumlErrorMessage);
			if (diagramHasIncorrectSyntax)
			{
				return _cachedGeneratedStandardMarkdown.MostRecentlyCachedDiagramWithoutSyntaxError switch
				{
					null => EncapsulateDiagramWithParentDiv("plantuml", BuildPlantumlSyntaxErrorSnippet(plantumlErrorMessage, diagramDescription.ToString())),
					var latestCorrectCachedDiagram when latestCorrectCachedDiagram.DiagramDescription.LooksLike(plantumlDiagramSnippet) => latestCorrectCachedDiagram.GeneratedSvg,
					_ => EncapsulateDiagramWithParentDiv("plantuml", BuildPlantumlSyntaxErrorSnippet(plantumlErrorMessage, diagramDescription.ToString()))
				};
			}
			_cachedGeneratedStandardMarkdown.AddDiagram(plantumlDiagramSnippet, EncapsulateDiagramWithParentDiv("plantuml", BuildPlantumlDiagramAsSvg(stdOutput)));
		}

		return _cachedGeneratedStandardMarkdown[plantumlDiagramSnippet];
	}

	StandardMarkdownText BuildPlantumlHtmlFromPlantumlIncident(string plantumlDiagramSnippet, string incidentMessage, string diagramDescription)
	{
		var html = new StringBuilder();
		html.AppendLine("<div class=\"generated-diagram-plantuml\">");
		html.AppendLine();
		html.AppendLine(InsertCommentIntoPlantumlSnippet(plantumlDiagramSnippet, $"☠️ {incidentMessage}{Environment.NewLine}{diagramDescription}"));
		html.AppendLine();
		html.AppendLine("</div>");
		html.AppendLine();

		return StandardMarkdownText.From(html);
	}

	string InsertCommentIntoPlantumlSnippet(string plantumlDiagramSnippet, string message)
	=> InsertCommentIntoCodeSnippet(plantumlDiagramSnippet, message, "'");

	StandardMarkdownText BuildPlantumlDiagramAsSvg(StringBuilder plantumlStdOutput)
	{
		var rawSvg = plantumlStdOutput.ToString();
		var minifiedDiagramSvg = new HtmlMinifier().Minify(rawSvg).MinifiedContent;
		return StandardMarkdownText.From(string.IsNullOrEmpty(minifiedDiagramSvg) ? rawSvg : minifiedDiagramSvg);
	}

	StandardMarkdownText BuildPlantumlSyntaxErrorSnippet(string plantumlErrorMessage, string diagramDescription)
	{
		var errorAsMarkdownBlockSnippet = new StringBuilder(Environment.NewLine);
		errorAsMarkdownBlockSnippet.AppendLine("```");
		errorAsMarkdownBlockSnippet.AppendLine("🔧 Plantuml diagram does not build");
		errorAsMarkdownBlockSnippet.AppendLine();
		var stdError = plantumlErrorMessage;
		errorAsMarkdownBlockSnippet.AppendLine(stdError);
		errorAsMarkdownBlockSnippet.AppendLine(diagramDescription);
		errorAsMarkdownBlockSnippet.AppendLine("```");
		errorAsMarkdownBlockSnippet.AppendLine();

		return StandardMarkdownText.From(errorAsMarkdownBlockSnippet);
	}

	async Task<StandardMarkdownText> BuildSvgFromMermaidSnippetWithTransparentBackground(EnhancedMarkdownText mermaidDiagramSnippet)
	{
		if (!_cachedGeneratedStandardMarkdown.ContainsKey(mermaidDiagramSnippet))
		{
			var outputFile = GenerateNewTmpSvgFilePath();
			using var process = new Process();
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.FileName = "cmd.exe";
			process.StartInfo.Arguments = String.Format("/C \"mmdc --input - --backgroundColor transparent --output \"\"{0}\"\"", outputFile);

			process.OutputDataReceived += (sender, args) => { };

			var stdErr = new StringBuilder();
			process.ErrorDataReceived += (sender, args) => {
				stdErr.AppendLine(args.Data);
			};

			try
			{
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				var diagramDescription = string.Join(Environment.NewLine, (mermaidDiagramSnippet.Split(Environment.NewLine)[1..^1]));
				process.StandardInput.WriteLine(diagramDescription);
				process.StandardInput.Flush();
				process.StandardInput.Close();
				await process.WaitForExitAsync();
				process.WaitForExit();

				if (process.ExitCode == 1)
				{
					var error = stdErr.ToString();
					if ((error.Contains("node") && !error.Contains("node_modules")) || error.Contains("mmdc"))
					{
						return BuildMermaidHtmlFromMermaidIncident(mermaidDiagramSnippet, error);
					}
				}
			}
			catch (Exception ex)
			{
				return BuildMermaidHtmlFromMermaidIncident(mermaidDiagramSnippet, ex.Message);
			}

			var mermaidDiagramAsSvg = outputFile switch
			{
				var path when File.Exists(path) => File.ReadAllText(path),
				_ => null
			};
			var diagramHasIncorrectSyntax = string.IsNullOrWhiteSpace(mermaidDiagramAsSvg);
			if (diagramHasIncorrectSyntax)
			{
				return _cachedGeneratedStandardMarkdown.MostRecentlyCachedDiagramWithoutSyntaxError switch
				{
					null => EncapsulateDiagramWithParentDiv("mermaid", BuildMermaidSyntaxErrorSnippet(stdErr)),
					var latestCorrectCachedDiagram when latestCorrectCachedDiagram.DiagramDescription.LooksLike(mermaidDiagramSnippet) => latestCorrectCachedDiagram.GeneratedSvg,
					_ => EncapsulateDiagramWithParentDiv("mermaid", BuildMermaidSyntaxErrorSnippet(stdErr))
				};
			}
			_cachedGeneratedStandardMarkdown.AddDiagram(mermaidDiagramSnippet, EncapsulateDiagramWithParentDiv("mermaid", BuildMermaidDiagramAsSvg(mermaidDiagramAsSvg!)));
		}

		return _cachedGeneratedStandardMarkdown[mermaidDiagramSnippet];
	}

	StandardMarkdownText BuildMermaidHtmlFromMermaidIncident(string mermaidlDiagramSnippet, string incidentMessage)
	{
		var html = new StringBuilder();
		html.AppendLine("<div class=\"generated-diagram-mermaid\">");
		html.AppendLine();
		html.AppendLine(InsertCommentIntoMermaidSnippet(mermaidlDiagramSnippet, $"☠️ {incidentMessage}"));
		html.AppendLine();
		html.AppendLine("</div>");
		html.AppendLine();

		return StandardMarkdownText.From(html);
	}

	string InsertCommentIntoMermaidSnippet(string mermaidDiagramSnippet, string message)
	=> InsertCommentIntoCodeSnippet(mermaidDiagramSnippet, message, "%%");

	string InsertCommentIntoCodeSnippet(string mermaidDiagramSnippet, string message, string commentPrefix)
	{
		var codeSnippetLines = mermaidDiagramSnippet.Split(Environment.NewLine);
		var codeSnippetWithCommentedMessage = new string[] { codeSnippetLines[0] }
			.Append($"{commentPrefix} {message}")
			.Append(Environment.NewLine)
			.Concat(codeSnippetLines[1..]);
		return string.Join(Environment.NewLine, codeSnippetWithCommentedMessage);
	}


	StandardMarkdownText BuildMermaidSyntaxErrorSnippet(StringBuilder stdErr)
	{
			var errorAsMarkdownBlockSnippet = new StringBuilder(Environment.NewLine);
			errorAsMarkdownBlockSnippet.AppendLine("```");
			errorAsMarkdownBlockSnippet.AppendLine("🔧 Mermaid diagram does not build");
			errorAsMarkdownBlockSnippet.AppendLine();
			var stdErrorWithoutAnsiColorCodes = Regex.Replace(stdErr.ToString(), AnsiColorCodesPattern, "").Trim();
			errorAsMarkdownBlockSnippet.AppendLine(stdErrorWithoutAnsiColorCodes);
			errorAsMarkdownBlockSnippet.AppendLine("```");

			return StandardMarkdownText.From(errorAsMarkdownBlockSnippet);
	}

	StandardMarkdownText BuildMermaidDiagramAsSvg(string mermaidDiagramAsSvg)
	{
		var minifiedDiagramSvg = new HtmlMinifier().Minify(mermaidDiagramAsSvg).MinifiedContent;
		return StandardMarkdownText.From(minifiedDiagramSvg);
	}

	string GenerateNewTmpD2FilePath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"tmp/{Guid.NewGuid()}.d2");
	string GenerateNewTmpSvgFilePath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"tmp/{Guid.NewGuid()}.svg");

	public static EnhancedMarkdownToStandardMarkdownConverter Create(int numberOfItemsInHtmlGenerationCacheTriggeringCleanup, int acceptableDataFreshnessInSecondsAfterHtmlGenerationCacheCleanup)
		=> new(GeneratedStandardMarkdownTextCache.Create(numberOfItemsInHtmlGenerationCacheTriggeringCleanup, acceptableDataFreshnessInSecondsAfterHtmlGenerationCacheCleanup));
}
