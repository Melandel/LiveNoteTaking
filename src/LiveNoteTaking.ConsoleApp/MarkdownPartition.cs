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
			throw ObjectConstructionException.FromInvalidMemberValue(GetType(), nameof(_parts), parts, "must have at least one item");
		}

		var firstLineOfEachPart = parts.Values.Select(p => p.FirstLineNumber).ToArray();
		var lastLineOfEachPart = parts.Values.Select(p => p.LastLineNumber).ToArray();
		for (var i = 0; i < parts.Count-1; i++)
		{
			var lastLineOfCurrentPart = lastLineOfEachPart[i];
			var firstLineOfConsecutivePart = firstLineOfEachPart[i+1];

			if (lastLineOfCurrentPart >= firstLineOfConsecutivePart)
			{
				throw ObjectConstructionException.FromInvalidMemberValue(GetType(), nameof(_parts), parts, $"two consecutive parts cannot cross each other such as {{{i}: {firstLineOfEachPart[i]}-{lastLineOfCurrentPart}}} and {{{i+1}: {firstLineOfConsecutivePart}-{lastLineOfEachPart[i+1]}}})");
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
			AddMethodParametersValuesAsDebuggingInformation(objectConstructionException);
			throw;
		}
		catch (Exception developerMistake)
		{
			var objectConstructionException = ObjectConstructionException.FromDeveloperMistake(typeof(MarkdownPartition), developerMistake);
			AddMethodParametersValuesAsDebuggingInformation(objectConstructionException);
			throw objectConstructionException;
		}

		void AddMethodParametersValuesAsDebuggingInformation(ObjectConstructionException objectConstructionException)
		{
			objectConstructionException.AddDebuggingInformation(nameof(parts), parts);
		}
	}
}
