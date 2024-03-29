using System.Collections;

record MarkdownPartition : IReadOnlyCollection<MarkdownPart>
{
	public override string ToString() => System.Text.Json.JsonSerializer.Serialize(_parts, new System.Text.Json.JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default });
	public static implicit operator MarkdownPart[](MarkdownPartition obj) => obj._parts.Values.ToArray();

	readonly SortedList<int, MarkdownPart> _parts;
	public IEnumerator<MarkdownPart> GetEnumerator() => ((IEnumerable<MarkdownPart>)_parts.Values).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _parts.Values.GetEnumerator();

	public int Count => ((IReadOnlyCollection<MarkdownPart>)_parts).Count;

	MarkdownPartition(SortedList<int, MarkdownPart> parts)
	{
		if (parts.Count == 0)
		{
			throw ObjectConstructionException.WhenConstructingAMemberFor<MarkdownPartition>(nameof(parts), parts, "@member must have at least one item");
		}

		var firstLineOfEachPart = parts.Values.Select(p => p.FirstLineNumber).ToArray();
		var lastLineOfEachPart = parts.Values.Select(p => p.LastLineNumber).ToArray();
		for (var i = 0; i < parts.Count-1; i++)
		{
			var lastLineOfCurrentPart = lastLineOfEachPart[i];
			var firstLineOfConsecutivePart = firstLineOfEachPart[i+1];

			if (lastLineOfCurrentPart >= firstLineOfConsecutivePart)
			{
				throw ObjectConstructionException.WhenConstructingAMemberFor<MarkdownPartition>(nameof(parts), parts, $"two consecutive @members cannot cross each other such as {{{i}: {firstLineOfEachPart[i]}-{lastLineOfCurrentPart}}} and {{{i+1}: {firstLineOfConsecutivePart}-{lastLineOfEachPart[i+1]}}})");
			}
		}

		_parts = parts;
	}

	public static MarkdownPartition FromSinglePart(MarkdownPart part) => From(new[] { part });
	public static MarkdownPartition From(IEnumerable<MarkdownPart> parts)
	{
		try
		{
			var sortedByFirstLineNumber = new SortedList<int, MarkdownPart>(
				parts.ToDictionary(p => p.FirstLineNumber, p => p));
			return new(sortedByFirstLineNumber);
		}
		catch (ObjectConstructionException objectConstructionException)
		{
			objectConstructionException.EnrichConstructionFailureContextWith<MarkdownPartition>(parts);
			throw;
		}
		catch (Exception developerMistake)
		{
			throw ObjectConstructionException.WhenConstructingAnInstanceOf<MarkdownPartition>(developerMistake, parts);
		}
	}
}
