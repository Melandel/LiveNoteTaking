record EnhancedMarkdownText
{
	public override string ToString() => _encapsulated;
	public static implicit operator string(EnhancedMarkdownText obj) => obj._encapsulated;

	readonly string _encapsulated;

	public static EnhancedMarkdownText Empty => new("");

	EnhancedMarkdownText(string encapsulated)
	{
		_encapsulated = encapsulated switch
		{
			null => throw ObjectConstructionException.WhenConstructingAMemberFor<EnhancedMarkdownText>(nameof(_encapsulated), _encapsulated, "@member must not be null"),
			_ => encapsulated
		};
	}

	public bool Contains(string pattern)
	=> _encapsulated.Contains(pattern);

	public string[] Split(string separator)
	=> _encapsulated.Split(separator);

	public string[] Split(params char[]? separator)
	=> _encapsulated.Split(separator);

	public int Length
	=> _encapsulated.Length;

	public EnhancedMarkdownText Substring(int startIndex, int length)
	=> From(_encapsulated.Substring(startIndex, length));

	public bool LooksLike(EnhancedMarkdownText other)
	{
		Random rnd = new Random();
		var numberOfChallenges = 3;
		for(var i = 0; i < numberOfChallenges; i++)
		{
			var patternLength = 5;
			var randomStartIndex = rnd.Next(0, other.Length-patternLength-1);

			var substringInOther = other.Substring(randomStartIndex, patternLength);

			if (!_encapsulated.Contains(substringInOther))
			{
				return false;
			}
		}
		return true;
	}

	public static EnhancedMarkdownText From(string encapsulated)
	{
		try
		{
			return new(encapsulated);
		}
		catch (ObjectConstructionException objectConstructionException)
		{
			objectConstructionException.EnrichConstructionFailureContextWith<EnhancedMarkdownText>(encapsulated);
			throw;
		}
		catch (Exception developerMistake)
		{
			throw ObjectConstructionException.WhenConstructingAnInstanceOf<EnhancedMarkdownText>(developerMistake, encapsulated);
		}
	}

	public record DiagrammingEnhancement
	{
		public const string DiagramEndToken = "```";
		public record D2 { public const string StartToken = "```d2"; }
		public record Plantuml
		{
			public record StartToken
			{
				public const string Common = "```puml";
				public const string MindMap = "```puml_mindmap";
			}
		}
		public record Mermaid { public const string StartToken = "```mmd"; }
	}

	public record ExpandableSectionEnhancement
	{
		public const string StartToken = "{<";
		public const string EndToken = ">}";
	}

	public record MultiColumnsEnhancement
	{
		public const string StartToken = "{|";
		public const string AddToken = ".|";
		public const string EndToken = "|}";
	}

	public record DataRowsEnhancement
	{
		public const string StartToken = "```data";
		public const string EndToken = "```";
	}
}
