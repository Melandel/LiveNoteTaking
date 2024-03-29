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
			null => throw ObjectConstructionException.FromInvalidMemberValue(GetType(), nameof(DiagramDescription), diagramDescription),
			_ => diagramDescription
		};
		GeneratedSvg = generatedSvg switch
		{
			null => throw ObjectConstructionException.FromInvalidMemberValue(GetType(), nameof(GeneratedSvg), generatedSvg),
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
			AddMethodParametersValuesAsDebuggingInformation(objectConstructionException);
			throw;
		}
		catch (Exception developerMistake)
		{
			var objectConstructionException = ObjectConstructionException.FromDeveloperMistake(typeof(CachedDiagramWithoutSyntaxError), developerMistake);
			AddMethodParametersValuesAsDebuggingInformation(objectConstructionException);
			throw objectConstructionException;
		}

		void AddMethodParametersValuesAsDebuggingInformation(ObjectConstructionException objectConstructionException)
		{
			objectConstructionException.AddDebuggingInformation(nameof(diagramDescription), diagramDescription);
			objectConstructionException.AddDebuggingInformation(nameof(generatedSvg), generatedSvg);
		}
	}
}
