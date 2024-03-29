public class MarkdownFileAbsolutePath {
	readonly string _fullPath;
	public static implicit operator string(MarkdownFileAbsolutePath obj) => obj._fullPath;
	MarkdownFileAbsolutePath(string fullPath) {
		_fullPath = fullPath switch
		{
			_ when Path.GetExtension(fullPath) != ".md" => throw ObjectConstructionException.WhenConstructingAMemberFor<MarkdownFileAbsolutePath>(nameof(fullPath), fullPath, "@member must end with \".md\" extension."),
			_ when !File.Exists(fullPath) => throw ObjectConstructionException.WhenConstructingAMemberFor<MarkdownFileAbsolutePath>(nameof(fullPath), fullPath, "@member must be an existing path."),
			_ => fullPath
		};
	}

	public string DirectoryAbsolutePath => Path.GetDirectoryName(_fullPath)!;

	public static MarkdownFileAbsolutePath FromString(string path)
	{
		try
		{
			var fullPath = Path.GetFullPath(path);
			return new(fullPath);
		}
		catch (ObjectConstructionException objectConstructionException)
		{
			objectConstructionException.EnrichConstructionFailureContextWith<MarkdownFileAbsolutePath>(path as object);
			throw;
		}
		catch (Exception developerMistake)
		{
			throw ObjectConstructionException.WhenConstructingAnInstanceOf<MarkdownFileAbsolutePath>(developerMistake, path);
		}
	}
}
