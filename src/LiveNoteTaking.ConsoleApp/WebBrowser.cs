using System.Diagnostics;
using MintPlayer.PlatformBrowser;

class WebBrowser
{
	static readonly string MyViebWebBrowser = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"tools/Vieb/Vieb.exe");
	static bool IsViebInstalled => File.Exists(MyViebWebBrowser);

	public static string Name => IsViebInstalled ? System.IO.Path.GetFileName(MyViebWebBrowser) : (PlatformBrowser.GetDefaultBrowser().GetAwaiter().GetResult())!.Name;
	public static string Path => IsViebInstalled ? MyViebWebBrowser : (PlatformBrowser.GetDefaultBrowser().GetAwaiter().GetResult())!.ExecutablePath;
	public static Task OpenInsideNewTab(string htmlPreviewFilePath)
	{
		return Task.Run(() =>
		{
			using var process = new Process();
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.FileName = Path;
			process.StartInfo.Arguments = htmlPreviewFilePath;

			// 👇 Steal web browser stdout & stderr to prevent writing to the console
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.ErrorDataReceived += (sender, data) => { };
			process.Start();
			var output = process.StandardOutput.ReadToEnd();
		});
	}
}
