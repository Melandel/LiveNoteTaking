public class MarkdownFileAbsolutePath {
	readonly string _fullPath;
	public static implicit operator string(MarkdownFileAbsolutePath obj) => obj._fullPath;
	MarkdownFileAbsolutePath(string fullPath) {
		_fullPath = fullPath switch
		{
			_ when Path.GetExtension(fullPath) != ".md" => throw ObjectConstructionException.FromInvalidMemberValue(GetType(), nameof(_fullPath), fullPath, "Path does not end with \".md\" extension."),
			_ when !File.Exists(fullPath) => throw ObjectConstructionException.FromInvalidMemberValue(GetType(), nameof(_fullPath), fullPath, "File with this path does not exist."),
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
			AddMethodParametersValuesAsDebuggingInformation(objectConstructionException);
			throw;
		}
		catch (Exception developerMistake)
		{
			var objectConstructionException = ObjectConstructionException.FromDeveloperMistake(typeof(MarkdownFileAbsolutePath), developerMistake);
			AddMethodParametersValuesAsDebuggingInformation(objectConstructionException);
			throw objectConstructionException;
		}

		void AddMethodParametersValuesAsDebuggingInformation(ObjectConstructionException objectConstructionException)
		{
			objectConstructionException.AddDebuggingInformation(nameof(path), path);
		}
	}
}
