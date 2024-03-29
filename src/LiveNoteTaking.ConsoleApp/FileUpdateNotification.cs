class FileUpdateNotification
{
	public MarkdownFileAbsolutePath? MarkdownFilePath { get; }
	public EnhancedMarkdownText? EnhancedMarkdownText { get; }

	public FileUpdateNotification(
		MarkdownFileAbsolutePath? markdownFilePath,
		EnhancedMarkdownText? enhancedMarkdownText)
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
	}

	public static FileUpdateNotification FromMarkdownFilePath(string filepath)
	{
		var markdownFilePath = MarkdownFileAbsolutePath.FromString(filepath);
		EnhancedMarkdownText? markdownText = null;

		return new(markdownFilePath, markdownText);
	}

	public static FileUpdateNotification FromMarkdownText(string markdownText)
	{
		MarkdownFileAbsolutePath? markdownFilePath = null;
		EnhancedMarkdownText enhancedMarkdownText = EnhancedMarkdownText.From(markdownText);

		return new(markdownFilePath, enhancedMarkdownText);
	}
}

