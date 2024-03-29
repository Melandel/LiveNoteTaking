static class DependencyInjection
{
	public static IServiceCollection ToSomeString(this IServiceCollection serviceCollection)
	=> serviceCollection.AddTransient<FileUpdateNotifier>();
}
