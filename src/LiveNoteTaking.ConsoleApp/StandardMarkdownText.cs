using System.Dynamic;
using System.Text;

record StandardMarkdownText
{
	public override string ToString() => _encapsulated.ToString();
	public static implicit operator string(StandardMarkdownText obj) => obj._encapsulated.ToString();

	readonly StringBuilder _encapsulated;


	StandardMarkdownText(StringBuilder encapsulated)
	{
		_encapsulated = encapsulated switch
		{
			null => throw ObjectConstructionException.WhenConstructingAMemberFor<StandardMarkdownText>(nameof(encapsulated), encapsulated, "@member must not be null"),
			_ => encapsulated
		};
	}

	public bool Contains(string pattern)
	=> _encapsulated.ToString().Contains(pattern);

	public static StandardMarkdownText Empty
	=> new(new StringBuilder());

	public static StandardMarkdownText From(string str)
	=> From(new StringBuilder(str));

	public static StandardMarkdownText From(StringBuilder stringBuilder)
	{
		try
		{
			return new(stringBuilder);
		}
		catch (ObjectConstructionException objectConstructionException)
		{
			objectConstructionException.EnrichConstructionFailureContextWith<StandardMarkdownText>(stringBuilder);
			throw;
		}
		catch (Exception developerMistake)
		{
			throw ObjectConstructionException.WhenConstructingAnInstanceOf<StandardMarkdownText>(developerMistake, stringBuilder);
		}
	}

	public class Table {
		public override string ToString() => _text;
		public static implicit operator string(Table obj) => obj._text;
		readonly string _text;
		Table(string text)
		{
			_text = text;
		}

		public static Table From(dynamic expandoObjectOrListOfExpandoObjects)
		{
			var sb = new StringBuilder();
			dynamic dyn = expandoObjectOrListOfExpandoObjects;

			if (dyn is List<ExpandoObject> items)
			{
				if (!items.Any())
				{
					return new("`(nothing)`");
				}

				var dynItem = (IDictionary<string, object>)items[0]!;
				var numberOfColumns = dynItem.Count + 1;
				sb.AppendLine($"| {(items.Count == 1 ? "1row" : $"{items.Count}rows")} | {string.Join(" | ", dynItem.Keys)} |");
				sb.AppendLine($"| {string.Join(" | ", Enumerable.Repeat("--", numberOfColumns))} |");
				for(var i = 0; i < items.Count; i++)
				{
					dynamic item = (IDictionary<string, object>)items[i]!;
					sb.Append($"| #{i+1} |");
					foreach (var kvp in item)
					{
						if (kvp.Value is string)
						{
							sb.Append($" `` {(kvp.Value ?? "(null)")} `` |");
						}
						else if (kvp.Value is bool boolValue)
						{
							sb.Append($" {(boolValue ? "true" : "false")} |");
						}
						else
						{
							sb.Append($" {(kvp.Value?.ToString().Replace("|", "\\|") ?? "`(null)`")} |");
						}
					}
					sb.AppendLine();
				}
				return new(sb.ToString());
			}

			var dict = (IDictionary<string, object>)expandoObjectOrListOfExpandoObjects!;
			var isOutputFromDbCommandLineWithMultipleItems = dict.ContainsKey("count") && dict.ContainsKey("_");
			if (isOutputFromDbCommandLineWithMultipleItems)
			{
				List<dynamic> itms = dyn._;
				if (!itms.Any())
				{
					return new("");
				}

				var dynItem = (IDictionary<string, object>)itms[0];
				var numberOfColumns = dynItem.Count + 1;
				sb.AppendLine($"| {dyn.count}rows | {string.Join(" | ", dynItem.Keys)} |");
				sb.AppendLine($"| {string.Join(" | ", Enumerable.Repeat("--", numberOfColumns))} |");
				for(var i = 0; i < itms.Count; i++)
				{
					dynamic item = (IDictionary<string, object>)itms[i];
					sb.Append($"| #{i+1} |");
					foreach (var kvp in item)
					{
						if (kvp.Value is string)
						{
							sb.Append($" `` {(kvp.Value ?? "(null)")} `` |");
						}
						else if (kvp.Value is bool boolValue)
						{
							sb.Append($" {(boolValue ? "true" : "false")} |");
						}
						else
						{
							sb.Append($" {(kvp.Value?.ToString().Replace("|", "\\|") ?? "`(null)`")} |");
						}
					}
					sb.AppendLine();
				}

				if (dyn.count != itms.Count)
				{
					sb.AppendLine($"| #... | {string.Join(" | ", Enumerable.Repeat("-", numberOfColumns-1))} |");
				}
				return new(sb.ToString());
			}

			sb.AppendLine($"| {string.Join(" | ", dict.Keys)} |");
			sb.AppendLine($"| {string.Join(" | ", Enumerable.Repeat("--", dict.Count))} |");
			sb.AppendLine($"| {string.Join(" | ", dict.Values)} |");
			return new(sb.ToString());
		}
	}
}
