static class StringExtensions
{
	public static string Capitalize(this string str)
	=> str switch
	{
		"" => "",
		[ var c ] => Char.ToUpper(c).ToString(),
		_ => Char.ToUpper(str[0]) + str[1..]
	};
}
