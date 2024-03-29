record MarkdownPart
{
	const int zeroIndexBaseOffset = 1;
	public MarkdownPartType Type { get; }
	public int FirstLineNumber { get; }
	public int LastLineNumber { get; }
	public EnhancedMarkdownText Content { get; private set; }
	MarkdownPart(
		MarkdownPartType type,
		int firstLineNumber,
		int lastLineNumber,
		EnhancedMarkdownText content)
	{
		Type = type;
		FirstLineNumber = firstLineNumber;
		LastLineNumber = lastLineNumber;
		Content = content;
	}

	public void ReplaceContentWith(EnhancedMarkdownText newContent) => Content = newContent;

	public static MarkdownPart FromLines(
		MarkdownPartType type,
		int firstLineNumberInOriginalDocument,
		int lastLineNumberInOriginalDocument,
		IEnumerable<string> lines)
	{
		var content = EnhancedMarkdownText.From(String.Join(Environment.NewLine, lines));
		return new(type, firstLineNumberInOriginalDocument, lastLineNumberInOriginalDocument, content);
	}
}
