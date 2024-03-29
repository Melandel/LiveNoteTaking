record CachedDiagramWithoutSyntaxError
{
	public override string ToString() => GeneratedSvg.ToString();
	public static implicit operator StandardMarkdownText(CachedDiagramWithoutSyntaxError obj) => obj.GeneratedSvg;

	public EnhancedMarkdownText DiagramDescription { get; init; }
	public StandardMarkdownText GeneratedSvg { get; init; }
	CachedDiagramWithoutSyntaxError(
		EnhancedMarkdownText diagramDescription,
		StandardMarkdownText generatedSvg)
	{
		DiagramDescription = diagramDescription switch
		{
			null => throw ObjectConstructionException.WhenConstructingAMemberFor<CachedDiagramWithoutSyntaxError>(nameof(diagramDescription), diagramDescription, "@member must not be null"),
			_ => diagramDescription
		};
		GeneratedSvg = generatedSvg switch
		{
			null => throw ObjectConstructionException.WhenConstructingAMemberFor<CachedDiagramWithoutSyntaxError>(nameof(generatedSvg), generatedSvg, "@member must not be null"),
			_ => generatedSvg
		};
	}

	public static CachedDiagramWithoutSyntaxError From(EnhancedMarkdownText diagramDescription, StandardMarkdownText generatedSvg)
	{
		try
		{
			return new(diagramDescription, generatedSvg);
		}
		catch (ObjectConstructionException objectConstructionException)
		{
			objectConstructionException.EnrichConstructionFailureContextWith<CachedDiagramWithoutSyntaxError>(diagramDescription, generatedSvg);
			throw;
		}
		catch (Exception developerMistake)
		{
			throw ObjectConstructionException.WhenConstructingAnInstanceOf<CachedDiagramWithoutSyntaxError>(developerMistake, diagramDescription, generatedSvg);
		}
	}
}
