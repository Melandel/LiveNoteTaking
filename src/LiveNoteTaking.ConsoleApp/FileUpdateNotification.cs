record FileUpdateNotification
{
	public MarkdownFileAbsolutePath? MarkdownFilePath { get; }
	public EnhancedMarkdownText? EnhancedMarkdownText { get; }
	public bool ResetCache { get; init; }

	public FileUpdateNotification(
		MarkdownFileAbsolutePath? markdownFilePath,
		EnhancedMarkdownText? enhancedMarkdownText,
		bool resetCache)
	{
		if (markdownFilePath is null && enhancedMarkdownText is null)
		{
			throw ObjectConstructionException.FromInvalidValuesCombination(
				GetType(),
				nameof(MarkdownFilePath), markdownFilePath,
				nameof(EnhancedMarkdownText), enhancedMarkdownText,
				"At least one property must have a value");
		}
		MarkdownFilePath = markdownFilePath;
		EnhancedMarkdownText = enhancedMarkdownText;
		ResetCache = resetCache;
	}

	public static FileUpdateNotification FromMarkdownFilePath(string filepath)
	{
		var markdownFilePath = MarkdownFileAbsolutePath.FromString(filepath);
		EnhancedMarkdownText? markdownText = null;

		return new(markdownFilePath, markdownText, resetCache: false);
	}

	public static FileUpdateNotification FromMarkdownText(string markdownText, bool resetCache = false)
	{
		MarkdownFileAbsolutePath? markdownFilePath = null;
		EnhancedMarkdownText enhancedMarkdownText = EnhancedMarkdownText.From(markdownText);

		return new(markdownFilePath, enhancedMarkdownText, resetCache);
	}
}

