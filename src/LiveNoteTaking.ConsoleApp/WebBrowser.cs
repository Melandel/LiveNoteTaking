using System.Diagnostics;
using MintPlayer.PlatformBrowser;

class WebBrowser
{
	public static string Name => (PlatformBrowser.GetDefaultBrowser().GetAwaiter().GetResult())!.Name;
	public static string Path => (PlatformBrowser.GetDefaultBrowser().GetAwaiter().GetResult())!.ExecutablePath;
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
