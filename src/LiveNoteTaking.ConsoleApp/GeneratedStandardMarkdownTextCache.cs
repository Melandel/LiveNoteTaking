using System.Collections.Concurrent;

class GeneratedStandardMarkdownTextCache
{
	static readonly DateTime FirstInstantiationDate = DateTime.Now;
	const int ApproximateMaximumDurationForFirstRenderingInSeconds = 10;
	readonly ConcurrentDictionary<EnhancedMarkdownText, GeneratedStandardMarkdownText> _cache;
	readonly int _numberOfItemsTriggeringCleanup;
	readonly int _acceptableDataFreshnessInSecondsAfterCleanup;
	public CachedDiagramWithoutSyntaxError? MostRecentlyCachedDiagramWithoutSyntaxError { get; private set; }
	GeneratedStandardMarkdownTextCache(
		ConcurrentDictionary<EnhancedMarkdownText, GeneratedStandardMarkdownText> cache,
		int numberOfItemsTriggeringCleanup,
		int acceptableDataFreshnessInSecondsAfterCleanup)
	{
		_cache = cache;
		_numberOfItemsTriggeringCleanup = numberOfItemsTriggeringCleanup;
		_acceptableDataFreshnessInSecondsAfterCleanup = acceptableDataFreshnessInSecondsAfterCleanup;
	}

	public int Count => _cache.Count;

	public bool ContainsKey(EnhancedMarkdownText markdown)
		=> _cache.ContainsKey(markdown);

	public void AddDiagram(EnhancedMarkdownText markdown, StandardMarkdownText generatedDiagram)
	{
		if (DateTime.Now > FirstInstantiationDate.AddSeconds(ApproximateMaximumDurationForFirstRenderingInSeconds)) // 👈 Skip setting MostRecentlyCached diagram during first rendering
		{
			MostRecentlyCachedDiagramWithoutSyntaxError = CachedDiagramWithoutSyntaxError.From(markdown, generatedDiagram);
		}
		this[markdown] = generatedDiagram;
	}

	public void AddStandardMarkdownText(EnhancedMarkdownText markdown, StandardMarkdownText generatedMermaidDiagram)
	{
		this[markdown] = generatedMermaidDiagram;
	}

	public StandardMarkdownText this[EnhancedMarkdownText markdown]
	{
		get {
			var cached = _cache[markdown];
			cached.LatestReadDatetime = DateTime.Now;
			return cached.StandardMarkdown;
		}
		private set {
				if (_cache.Count >= _numberOfItemsTriggeringCleanup)
				{
					RemoveOldUnusedElements();
				}
			_cache[markdown] = GeneratedStandardMarkdownText.Create(value);
		}
	}

	void RemoveOldUnusedElements()
	{
		var someSecondsAgo = DateTime.Now.AddSeconds(-10);
		foreach (var kvp in _cache)
		{
			var generatedHtmlCache = kvp.Value;
			if (generatedHtmlCache.LatestReadDatetime <= someSecondsAgo)
			{
				_cache.Remove(kvp.Key, out _);
			}
		}
	}

	public static GeneratedStandardMarkdownTextCache Create(int numberOfItemsTriggeringCleanup, int acceptableDataFreshnessInSecondsAfterCleanup)
	{
		var cache = new ConcurrentDictionary<EnhancedMarkdownText, GeneratedStandardMarkdownText>();
		return new(cache, numberOfItemsTriggeringCleanup, acceptableDataFreshnessInSecondsAfterCleanup);
	}

	public void Reset()
	{
		_cache.Clear();
		MostRecentlyCachedDiagramWithoutSyntaxError = null;
	}

	class GeneratedStandardMarkdownText
	{
		public StandardMarkdownText StandardMarkdown { get; }
		public DateTime LatestReadDatetime { get; set; }
		GeneratedStandardMarkdownText(
			StandardMarkdownText standardMarkdown,
			DateTime latestReadDatetime)
		{
			StandardMarkdown = standardMarkdown;
			LatestReadDatetime = latestReadDatetime;
		}
		public static GeneratedStandardMarkdownText Create(StandardMarkdownText standardMarkdownText) => new(standardMarkdownText, DateTime.Now);
	}
}
