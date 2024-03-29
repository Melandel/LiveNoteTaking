class MarkdownPartitionExtractor
{
	public MarkdownPartition ExtractFrom(EnhancedMarkdownText markdownWithCustomizedSyntaxes)
	{
		var lines = markdownWithCustomizedSyntaxes.Split(Environment.NewLine);
		var declarativeDiagrammingSnippetMarkers = new List<int>();
		var plantumlSnippetMarkers = new List<int>();
		var mermaidSnippetMarkers = new List<int>();
		var dataRowsSnippetMarkers = new List<int>();
		var zeroIndexBaseOffset = 1;
		for (var i = 0; i < lines.Length; i++)
		{

			if (lines[i].StartsWith(EnhancedMarkdownText.DiagrammingEnhancement.D2.StartToken) && declarativeDiagrammingSnippetMarkers.Count % 2 == 0)
			{
				declarativeDiagrammingSnippetMarkers.Add(i + zeroIndexBaseOffset);
				continue;
			}

			if (lines[i].StartsWith(EnhancedMarkdownText.DiagrammingEnhancement.Plantuml.StartToken.Common) && plantumlSnippetMarkers.Count % 2 == 0)
			{
				plantumlSnippetMarkers.Add(i + zeroIndexBaseOffset);
				continue;
			}

			if (lines[i].StartsWith(EnhancedMarkdownText.DiagrammingEnhancement.Mermaid.StartToken) && mermaidSnippetMarkers.Count % 2 == 0)
			{
				mermaidSnippetMarkers.Add(i + zeroIndexBaseOffset);
				continue;
			}

			if (lines[i].StartsWith(EnhancedMarkdownText.DataRowsEnhancement.StartToken) && dataRowsSnippetMarkers.Count % 2 == 0)
			{
				dataRowsSnippetMarkers.Add(i + zeroIndexBaseOffset);
				continue;
			}

			if (lines[i].Trim() == "```")
			{
				if (declarativeDiagrammingSnippetMarkers.Count % 2 == 1)
				{
					declarativeDiagrammingSnippetMarkers.Add(i + zeroIndexBaseOffset);
				}
				else if (plantumlSnippetMarkers.Count % 2 == 1)
				{
					plantumlSnippetMarkers.Add(i + zeroIndexBaseOffset);
				}
				else if (mermaidSnippetMarkers.Count % 2 == 1)
				{
					mermaidSnippetMarkers.Add(i + zeroIndexBaseOffset);
				}
				else if (dataRowsSnippetMarkers.Count % 2 == 1)
				{
					dataRowsSnippetMarkers.Add(i + zeroIndexBaseOffset);
				}
				continue;
			}
		}

		var numberOfFirstLine = 1;
		var numberOfLastLine = lines.Length;

		if (declarativeDiagrammingSnippetMarkers.Count == 0 && plantumlSnippetMarkers.Count == 0 && mermaidSnippetMarkers.Count == 0 && dataRowsSnippetMarkers.Count == 0)
		{
			return MarkdownPartition.FromSinglePart(MarkdownPart.FromLines(MarkdownPartType.StandardMarkdown, numberOfLastLine, numberOfLastLine, lines));
		}

		var parts = new SortedList<int, MarkdownPart>();

		var startsWithD2Snippet = declarativeDiagrammingSnippetMarkers.FirstOrDefault() == numberOfFirstLine;
		var startsWithPlantumlSnippet = plantumlSnippetMarkers.FirstOrDefault() == numberOfFirstLine;
		var startsWithMermaidSnippet = mermaidSnippetMarkers.FirstOrDefault() == numberOfFirstLine;
		var startsWithDataRowsSnippet = dataRowsSnippetMarkers.FirstOrDefault() == numberOfFirstLine;
		if (startsWithD2Snippet)
		{
			var firstLineOfFirstDiagramSnippet = declarativeDiagrammingSnippetMarkers[0];
			var lastLineOfFirstDiagramSnippet = declarativeDiagrammingSnippetMarkers[1];
			parts.Add(
				numberOfFirstLine,
				MarkdownPart.FromLines(
					MarkdownPartType.DeclarativeDiagrammingSnippet,
					firstLineOfFirstDiagramSnippet,
					lastLineOfFirstDiagramSnippet,
					lines[(firstLineOfFirstDiagramSnippet - zeroIndexBaseOffset)..(lastLineOfFirstDiagramSnippet - zeroIndexBaseOffset + 1)]));

			declarativeDiagrammingSnippetMarkers.RemoveRange(0,2);
		}
		else if (startsWithPlantumlSnippet)
		{
			var firstLineOfFirstDiagramSnippet = plantumlSnippetMarkers[0];
			var lastLineOfFirstDiagramSnippet = plantumlSnippetMarkers[1];
			parts.Add(
				numberOfFirstLine,
				MarkdownPart.FromLines(
					MarkdownPartType.PlantumlSnippet,
					firstLineOfFirstDiagramSnippet,
					lastLineOfFirstDiagramSnippet,
					lines[(firstLineOfFirstDiagramSnippet - zeroIndexBaseOffset)..(lastLineOfFirstDiagramSnippet - zeroIndexBaseOffset + 1)]));

			plantumlSnippetMarkers.RemoveRange(0,2);
		}
		else if (startsWithMermaidSnippet)
		{
			var firstLineOfFirstDiagramSnippet = mermaidSnippetMarkers[0];
			var lastLineOfFirstDiagramSnippet = mermaidSnippetMarkers[1];
			parts.Add(
				numberOfFirstLine,
				MarkdownPart.FromLines(
					MarkdownPartType.MermaidSnippet,
					firstLineOfFirstDiagramSnippet,
					lastLineOfFirstDiagramSnippet,
					lines[(firstLineOfFirstDiagramSnippet - zeroIndexBaseOffset)..(lastLineOfFirstDiagramSnippet - zeroIndexBaseOffset + 1)]));

			mermaidSnippetMarkers.RemoveRange(0,2);
		}
		else if (startsWithDataRowsSnippet)
		{
			var firstLineOfFirstBlockSnippet = dataRowsSnippetMarkers[0];
			var lastLineOfFirstBlockSnippet = dataRowsSnippetMarkers[1];
			parts.Add(
				numberOfFirstLine,
				MarkdownPart.FromLines(
					MarkdownPartType.DataRowsSnippet,
					firstLineOfFirstBlockSnippet,
					lastLineOfFirstBlockSnippet,
					lines[(firstLineOfFirstBlockSnippet - zeroIndexBaseOffset)..(lastLineOfFirstBlockSnippet - zeroIndexBaseOffset + 1)]));

			dataRowsSnippetMarkers.RemoveRange(0,2);
		}
		else
		{
			var firstLineOfEnhancedMarkdown = (declarativeDiagrammingSnippetMarkers, plantumlSnippetMarkers, mermaidSnippetMarkers, dataRowsSnippetMarkers) switch
			{
				([_,..], [], [], []) => declarativeDiagrammingSnippetMarkers[0],
				([], [_,..], [], []) => plantumlSnippetMarkers[0],
				([], [], [_,..], []) => mermaidSnippetMarkers[0],
				([], [], [], [_,..]) => dataRowsSnippetMarkers[0],
				_ => new[]
				{
					declarativeDiagrammingSnippetMarkers switch
					{
						var m when m.Any() => declarativeDiagrammingSnippetMarkers[0],
						_ => int.MaxValue
					},
					plantumlSnippetMarkers switch
					{
						var m when m.Any() => plantumlSnippetMarkers[0],
						_ => int.MaxValue
					},
					mermaidSnippetMarkers switch
					{
						var m when m.Any() => mermaidSnippetMarkers[0],
						_ => int.MaxValue
					},
					dataRowsSnippetMarkers switch
					{
						var m when m.Any() => dataRowsSnippetMarkers[0],
						_ => int.MaxValue
					},
				}.Min(),
			};
			parts.Add(
				numberOfFirstLine,
				MarkdownPart.FromLines(
					MarkdownPartType.StandardMarkdown,
					numberOfFirstLine,
					firstLineOfEnhancedMarkdown-1,
					lines[..(firstLineOfEnhancedMarkdown-1 - zeroIndexBaseOffset + 1)]));
		}

		var endsWithD2Snippet = declarativeDiagrammingSnippetMarkers.LastOrDefault() == numberOfLastLine;
		var endsWithPlantumlSnippet = plantumlSnippetMarkers.LastOrDefault() == numberOfLastLine;
		var endsWithMermaidSnippet = mermaidSnippetMarkers.LastOrDefault() == numberOfLastLine;
		var endsWithDataRowsSnippet = dataRowsSnippetMarkers.LastOrDefault() == numberOfLastLine;
		if (endsWithD2Snippet)
		{
			var firstLineOfLastDiagramSnippet = declarativeDiagrammingSnippetMarkers[^2];
			var lastLineOfLastEnhancedMarkdown = declarativeDiagrammingSnippetMarkers[^1];
			parts.Add(
				firstLineOfLastDiagramSnippet,
				MarkdownPart.FromLines(
					MarkdownPartType.DeclarativeDiagrammingSnippet,
					firstLineOfLastDiagramSnippet,
					lastLineOfLastEnhancedMarkdown,
					lines[(firstLineOfLastDiagramSnippet - zeroIndexBaseOffset)..]));

			declarativeDiagrammingSnippetMarkers.RemoveRange(declarativeDiagrammingSnippetMarkers.Count-2, 2);
		}
		else if (endsWithPlantumlSnippet)
		{
			var firstLineOfLastDiagramSnippet = plantumlSnippetMarkers[^2];
			var lastLineOfLastDiagramSnippet = plantumlSnippetMarkers[^1];
			parts.Add(
				firstLineOfLastDiagramSnippet,
				MarkdownPart.FromLines(
					MarkdownPartType.PlantumlSnippet,
					firstLineOfLastDiagramSnippet,
					lastLineOfLastDiagramSnippet,
					lines[(firstLineOfLastDiagramSnippet - zeroIndexBaseOffset)..]));

			plantumlSnippetMarkers.RemoveRange(plantumlSnippetMarkers.Count-2, 2);
		}
		else if (endsWithMermaidSnippet)
		{
			var firstLineOfLastDiagramSnippet = mermaidSnippetMarkers[^2];
			var lastLineOfLastDiagramSnippet = mermaidSnippetMarkers[^1];
			parts.Add(
				firstLineOfLastDiagramSnippet,
				MarkdownPart.FromLines(
					MarkdownPartType.MermaidSnippet,
					firstLineOfLastDiagramSnippet,
					lastLineOfLastDiagramSnippet,
					lines[(firstLineOfLastDiagramSnippet - zeroIndexBaseOffset)..]));

			mermaidSnippetMarkers.RemoveRange(mermaidSnippetMarkers.Count-2, 2);
		}
		else if (endsWithDataRowsSnippet)
		{
			var firstLineOfBlockSnippet = dataRowsSnippetMarkers[^2];
			var lastLineOfBlockSnippet = dataRowsSnippetMarkers[^1];
			parts.Add(
				firstLineOfBlockSnippet,
				MarkdownPart.FromLines(
					MarkdownPartType.DataRowsSnippet,
					firstLineOfBlockSnippet,
					lastLineOfBlockSnippet,
					lines[(firstLineOfBlockSnippet - zeroIndexBaseOffset)..]));

			dataRowsSnippetMarkers.RemoveRange(dataRowsSnippetMarkers.Count-2, 2);
		}
		else
		{
			var lastDiagramSnippetStop = (declarativeDiagrammingSnippetMarkers, plantumlSnippetMarkers, mermaidSnippetMarkers, dataRowsSnippetMarkers) switch
			{
				([_,..], [], [], []) => declarativeDiagrammingSnippetMarkers[^1],
				([], [_,..], [], []) => plantumlSnippetMarkers[^1],
				([], [], [_,..], []) => mermaidSnippetMarkers[^1],
				([], [], [], [_,..]) => dataRowsSnippetMarkers[^1],
				([..], [..], [..], [..]) => new[]
				{
					declarativeDiagrammingSnippetMarkers switch
					{
						var m when m.Any() => declarativeDiagrammingSnippetMarkers[^1],
						_ => int.MinValue
					},
					plantumlSnippetMarkers switch
					{
						var m when m.Any() => plantumlSnippetMarkers[^1],
						_ => int.MinValue
					},
					mermaidSnippetMarkers switch
					{
						var m when m.Any() => mermaidSnippetMarkers[^1],
						_ => int.MinValue
					},
					dataRowsSnippetMarkers switch
					{
						var m when m.Any() => dataRowsSnippetMarkers[^1],
						_ => int.MinValue
					},
				}.Max(),
				_ => throw new InvalidOperationException()
			};
			parts.Add(
				lastDiagramSnippetStop + 1,
				MarkdownPart.FromLines(
					MarkdownPartType.StandardMarkdown,
					lastDiagramSnippetStop + 1,
					lines.Length,
					lines[(lastDiagramSnippetStop + 1 - zeroIndexBaseOffset)..]));
		}

		var onlyOneEnhancementInWholeMarkdown = declarativeDiagrammingSnippetMarkers.Count == 0 && plantumlSnippetMarkers.Count == 0 && mermaidSnippetMarkers.Count == 0 && dataRowsSnippetMarkers.Count == 0;
		if (onlyOneEnhancementInWholeMarkdown)
		{
			var numberBetweenFirstLineNumberAndLastLineNumber = numberOfFirstLine + 1;
			parts.Add(
				numberBetweenFirstLineNumberAndLastLineNumber,
				MarkdownPart.FromLines(
					MarkdownPartType.StandardMarkdown,
					parts[0].LastLineNumber+1,
					parts[1].FirstLineNumber-1,
					lines[(parts[0].LastLineNumber+1 - zeroIndexBaseOffset)..(parts[1].FirstLineNumber-1 - zeroIndexBaseOffset + 1)]));

			return MarkdownPartition.From(parts.Values);
		}

		if (declarativeDiagrammingSnippetMarkers.Count > 0)
		{
			for (var i = 0; i < declarativeDiagrammingSnippetMarkers.Count; i += 2)
			{
				var firstLineOfSnippet = declarativeDiagrammingSnippetMarkers[i];
				var lastLineOfSnippet = declarativeDiagrammingSnippetMarkers[i+1];
				parts.Add(
					firstLineOfSnippet,
					MarkdownPart.FromLines(
						MarkdownPartType.DeclarativeDiagrammingSnippet,
						firstLineOfSnippet,
						lastLineOfSnippet,
						lines[(firstLineOfSnippet - zeroIndexBaseOffset)..(lastLineOfSnippet - zeroIndexBaseOffset + 1)]));
			}
		}

		if (plantumlSnippetMarkers.Count > 0)
		{
			for (var i = 0; i < plantumlSnippetMarkers.Count; i += 2)
			{
				var firstLineOfSnippet = plantumlSnippetMarkers[i];
				var lastLineOfSnippet = plantumlSnippetMarkers[i+1];
				parts.Add(
					firstLineOfSnippet,
					MarkdownPart.FromLines(
						MarkdownPartType.PlantumlSnippet,
						firstLineOfSnippet,
						lastLineOfSnippet,
						lines[(firstLineOfSnippet - zeroIndexBaseOffset)..(lastLineOfSnippet - zeroIndexBaseOffset + 1)]));
			}
		}

		if (mermaidSnippetMarkers.Count > 0)
		{
			for (var i = 0; i < mermaidSnippetMarkers.Count; i += 2)
			{
				var firstLineOfSnippet = mermaidSnippetMarkers[i];
				var lastLineOfSnippet = mermaidSnippetMarkers[i+1];
				parts.Add(
					firstLineOfSnippet,
					MarkdownPart.FromLines(
						MarkdownPartType.MermaidSnippet,
						firstLineOfSnippet,
						lastLineOfSnippet,
						lines[(firstLineOfSnippet - zeroIndexBaseOffset)..(lastLineOfSnippet - zeroIndexBaseOffset + 1)]));
			}
		}

		if (dataRowsSnippetMarkers.Count > 0)
		{
			for (var i = 0; i < dataRowsSnippetMarkers.Count; i += 2)
			{
				var firstLineOfSnippet = dataRowsSnippetMarkers[i];
				var lastLineOfSnippet = dataRowsSnippetMarkers[i+1];
				parts.Add(
					firstLineOfSnippet,
					MarkdownPart.FromLines(
						MarkdownPartType.DataRowsSnippet,
						firstLineOfSnippet,
						lastLineOfSnippet,
						lines[(firstLineOfSnippet - zeroIndexBaseOffset)..(lastLineOfSnippet - zeroIndexBaseOffset + 1)]));
			}
		}

		for (var i = 0; i < parts.Count-2; i++)
		{
			var lastLineNumber = parts.GetValueAtIndex(i).LastLineNumber;
			var firstLineNumberOfNextPart = parts.GetValueAtIndex(i+1).FirstLineNumber;
			if (firstLineNumberOfNextPart == lastLineNumber +1)
			{
				continue;
			}

			parts.Add(
				lastLineNumber+1,
				MarkdownPart.FromLines(
					MarkdownPartType.StandardMarkdown,
					lastLineNumber+1,
					firstLineNumberOfNextPart-1,
					lines[(lastLineNumber + 1 - zeroIndexBaseOffset)..(firstLineNumberOfNextPart-1 - zeroIndexBaseOffset + 1)]));
		}

		return MarkdownPartition.From(parts.Values);
	}
}
