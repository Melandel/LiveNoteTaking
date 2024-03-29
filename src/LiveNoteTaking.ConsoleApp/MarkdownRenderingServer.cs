using System.Diagnostics;
using System.Text;
using Markdig;
using Markdown.ColorCode.CSharpToColoredHtml;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

class MarkdownRenderingServer : IDisposable
{
	readonly WebApplication _app;
	readonly string _cascadingStyleSheetsRules;
	readonly MarkdownFileAbsolutePath? _markdownFilePath;
	readonly MarkdownPipeline _markdigPipeline;
	public int Port { get; }

	public string ServerSideEventEndpointRoute => $"http://localhost:{Port}/";
	public string ReloadRoute => $"http://localhost:{Port}/reload/";
	public string DocumentExportRoute => $"http://localhost:{Port}/export/";
	MarkdownToHtmlConversionResult? _latestMarkdownToHtmlConversionResult;

	// 👇 https://superuser.com/a/188070
	static readonly int[] RestrictedPortsOnChromium = new[]
	{
		1,      // tcpmux
		7,      // echo
		9,      // discard
		11,     // systat
		13,     // daytime
		15,     // netstat
		17,     // qotd
		19,     // chargen
		20,     // ftp data
		21,     // ftp access
		22,     // ssh
		23,     // telnet
		25,     // smtp
		37,     // time
		42,     // name
		43,     // nicname
		53,     // domain
		69,     // tftp
		77,     // priv-rjs
		79,     // finger
		87,     // ttylink
		95,     // supdup
		101,    // hostriame
		102,    // iso-tsap
		103,    // gppitnp
		104,    // acr-nema
		109,    // pop2
		110,    // pop3
		111,    // sunrpc
		113,    // auth
		115,    // sftp
		117,    // uucp-path
		119,    // nntp
		123,    // NTP
		135,    // loc-srv /epmap
		137,    // netbios
		139,    // netbios
		143,    // imap2
		161,    // snmp
		179,    // BGP
		389,    // ldap
		427,    // SLP (Also used by Apple Filing Protocol)
		465,    // smtp+ssl
		512,    // print / exec
		513,    // login
		514,    // shell
		515,    // printer
		526,    // tempo
		530,    // courier
		531,    // chat
		532,    // netnews
		540,    // uucp
		548,    // AFP (Apple Filing Protocol)
		554,    // rtsp
		556,    // remotefs
		563,    // nntp+ssl
		587,    // smtp (rfc6409)
		601,    // syslog-conn (rfc3195)
		636,    // ldap+ssl
		989,    // ftps-data
		990,    // ftps
		993,    // ldap+ssl
		995,    // pop3+ssl
		1719,   // h323gatestat
		1720,   // h323hostcall
		1723,   // pptp
		2049,   // nfs
		3659,   // apple-sasl / PasswordServer
		4045,   // lockd
		5060,   // sip
		5061,   // sips
		6000,   // X11
		6566,   // sane-port
		6665,   // Alternate IRC [Apple addition]
		6666,   // Alternate IRC [Apple addition]
		6667,   // Standard IRC [Apple addition]
		6668,   // Alternate IRC [Apple addition]
		6669,   // Alternate IRC [Apple addition]
		6697,   // IRC + TLS
		10080  // Amanda
	};

	// 👇 https://superuser.com/questions/188058/which-ports-are-considered-unsafe-by-chrome#comment512837_188070
	static readonly int[] BlockedPortsOnFirefox = new[]
	{
		1, // tcpmux
		7, // echo
		9, // discard
		11, // systat
		13, // daytime
		15, // netstat
		17, // qotd
		19, // chargen
		20, // ftp data
		21, // ftp control
		22, // ssh
		23, // telnet
		25, // smtp
		37, // time
		42, // name
		43, // nicname
		53, // domain
		77, // priv-rjs
		79, // finger
		87, // ttylink
		95, // supdup
		101, // hostriame
		102, // iso-tsap
		103, // gppitnp
		104, // acr-nema
		109, // POP2
		110, // POP3
		111, // sunrpc
		113, // auth
		115, // sftp
		117, // uucp-path
		119, // NNTP
		123, // NTP
		135, // loc-srv / epmap
		139, // netbios
		143, // IMAP2
		179, // BGP
		389, // LDAP
		465, // SMTP+SSL
		512, // print / exec
		513, // login
		514, // shell
		515, // printer
		526, // tempo
		530, // courier
		531, // chat
		532, // netnews
		540, // uucp
		556, // remotefs
		563, // NNTP+SSL
		587, // submission
		601, // syslog
		636, // LDAP+SSL
		993, // IMAP+SSL
		995, // POP3+SSL
		2049, // nfs
		4045, // lockd
		6000  // X11
	};

	MarkdownRenderingServer(
		int port,
		MarkdownFileAbsolutePath markdownFilePath,
		string cascadingStyleSheetsRules,
		int numberOfItemsInHtmlGenerationCacheTriggeringCleanup,
		int acceptableDataFreshnessInSecondsAfterHtmlGenerationCacheCleanup,
		int numberOfMarkdownFileReadRetries,
		int waitTimeBeforeEachMarkdownFileReadRetryInMilliseconds,
		NotificationMechanism<FileUpdateNotification> updateNotificationMechanism)
	{
		_cascadingStyleSheetsRules = cascadingStyleSheetsRules;
		_markdownFilePath = markdownFilePath;
		_markdigPipeline = new Markdig.MarkdownPipelineBuilder()
			.UseAdvancedExtensions()
			.UseColorCodeWithCSharpToColoredHtml()
			.Build();
		Port = port;
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			// 👇 give access to resources by relative path on local storage
			WebRootPath = Path.GetDirectoryName(markdownFilePath.DirectoryAbsolutePath),
		});

		builder.WebHost.UseKestrel(options =>
		{
			options.ListenLocalhost(port);
		});

		builder.Services.AddLogging(logging =>
		{
			logging.ClearProviders();
			logging.AddConsole();
			logging.SetMinimumLevel(LogLevel.Warning);
		});

		builder.Services.AddCors(options =>
		{
			options.AddPolicy("CorsPolicy",
				builder => builder
					.AllowAnyMethod()
					.AllowCredentials()
					.SetIsOriginAllowed((host) => true)
					.AllowAnyHeader());
		});

		_app = builder.Build();
		_app.UseCors("CorsPolicy");
		// 👇 give access to resources on local storage
		_app.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true });

		_app.MapGet(
			"/",
			async (HttpContext httpContext) =>
			{
				var responseHeaders = httpContext.Response.Headers;
				responseHeaders.Append("Access-Control-Allow-Origin", "*");  // 👈 prevent Cross-Origin Resource Sharing issues for local filesystem ressources
				responseHeaders.Append("Content-Type", "text/event-stream"); // 👈 use Server-Side Event technology
				responseHeaders.Append("Cache-Control", "no-cache");         // 👈 prevent cache usage

				updateNotificationMechanism.SendNotification(FileUpdateNotification.FromMarkdownFilePath(markdownFilePath));
				var markdownToHtmlConverter = EnhancedMarkdownToStandardMarkdownConverter.Create(
					numberOfItemsInHtmlGenerationCacheTriggeringCleanup,
					acceptableDataFreshnessInSecondsAfterHtmlGenerationCacheCleanup);

				await foreach (var notif in updateNotificationMechanism.ReadNotifications())
				{
					var enhancedMarkdownText = notif switch
					{
						{ EnhancedMarkdownText: not null } => notif.EnhancedMarkdownText,
						_ => ReadEnhancedMarkdownText(notif.MarkdownFilePath!, numberOfMarkdownFileReadRetries, waitTimeBeforeEachMarkdownFileReadRetryInMilliseconds) // 👈 read file now and not before to prevent skippable reads
					};

					if (enhancedMarkdownText is null)
					{
						continue;
					}

					try
					{
						var standardMarkdownText = await markdownToHtmlConverter.Convert(enhancedMarkdownText, notif.ResetCache);
						var html = Markdig.Markdown.ToHtml(standardMarkdownText, _markdigPipeline);

						_latestMarkdownToHtmlConversionResult = new(html, standardMarkdownText);

						var responseBody = FormatResponseForServerSentEvent(html);
						await httpContext.Response.WriteAsync(responseBody);
					}
					catch (Exception ex)
					{
						Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
						Console.Error.WriteLine(ex.StackTrace);
					}
				}
			});

		_app.MapPost(
			"/",
				([FromBody] EnhancedMarkdownRenderingRequest enhancedMarkdownRenderingRequest) =>
				{
					var notification = FileUpdateNotification.FromMarkdownText(enhancedMarkdownRenderingRequest.MarkdownText);
					updateNotificationMechanism.SendNotification(notification);
				});

		_app.MapPost(
			"/reload",
				([FromBody] ReloadMarkdownRenderingRequest reloadMarkdownRenderingRequest) =>
				{
					updateNotificationMechanism.SendNotification(updateNotificationMechanism.MostRecentNotificationMessage with { ResetCache = true });
				});

		_app.MapGet(
			"/export/{fileformat}",
			(HttpContext httpContext, string fileformat) =>
			{
				httpContext.Response.Headers.Append("Access-Control-Allow-Origin", "*");  // 👈 prevent Cross-Origin Resource Sharing issues for local filesystem ressources
				var exportedFile = fileformat switch
				{
					"html" => ExportLastGenerationAsHtml(),
					"md" => ExportLastGenerationAsMarkdown(),
					_ => throw new NotSupportedException(fileformat)
				};

				return Results.File(exportedFile.FileContents, exportedFile.ContentType, exportedFile.FileDownloadName);
			});
	}

	public static MarkdownRenderingServer CreateUsingRandomPort(
		MarkdownFileAbsolutePath markdownFilePath,
		string cascadingStyleSheetsRules,
		int numberOfItemsInHtmlGenerationCacheTriggeringCleanup,
		int acceptableDataFreshnessInSecondsAfterHtmlGenerationCacheCleanup,
		int numberOfMarkdownFileReadRetries,
		int waitTimeBeforeEachMarkdownFileReadRetryInMilliseconds,
		NotificationMechanism<FileUpdateNotification> updateNotificationMechanism)
	{
		var port = new Random().Next(5000, 5300);
		while (RestrictedPortsOnChromium.Contains(port) || BlockedPortsOnFirefox.Contains(port))
		{
			port = new Random().Next(5000, 5300);
		}

		return new(
			port,
			markdownFilePath,
			cascadingStyleSheetsRules,
			numberOfItemsInHtmlGenerationCacheTriggeringCleanup,
			acceptableDataFreshnessInSecondsAfterHtmlGenerationCacheCleanup,
			numberOfMarkdownFileReadRetries,
			waitTimeBeforeEachMarkdownFileReadRetryInMilliseconds,
			updateNotificationMechanism);
	}

	static EnhancedMarkdownText? ReadEnhancedMarkdownText(
		MarkdownFileAbsolutePath markdownFilePath,
		int numberOfMarkdownFileReadRetries,
		int waitTimeBeforeEachMarkdownFileReadRetryInMilliseconds)
	{
		var numberOfRemainingRetries = numberOfMarkdownFileReadRetries;
		while(numberOfRemainingRetries > 0)
		{
			try
			{
				var markdownText = File.ReadAllText(markdownFilePath);
				return markdownText switch
				{
					null => null,
					_ => EnhancedMarkdownText.From(markdownText)
				};
			}
			catch
			{
				Thread.Sleep(waitTimeBeforeEachMarkdownFileReadRetryInMilliseconds);
			}
			finally
			{
				numberOfRemainingRetries--;
			}
		}
		return null;
	}

	static string FormatResponseForServerSentEvent(string markdownAsHtml)
	{
		var formattedHtml = String.Join('\n', markdownAsHtml.Split('\n').Select(line => $"data: {line}".TrimEnd('\r')));

		var responseBuilder = new StringBuilder();
		responseBuilder.Append("retry: 60000").Append("\n");
		responseBuilder.Append(formattedHtml);
		responseBuilder.Append("\n\n");

		return responseBuilder.ToString();
	}

	ExportedFile ExportLastGenerationAsHtml()
	{
		var html = BuildHtmlPageToExport(_latestMarkdownToHtmlConversionResult!);
		return new ExportedFile(
			Encoding.UTF8.GetBytes(html),
			"text/html;charset=utf-8",
			$"{Path.GetFileNameWithoutExtension(_markdownFilePath!)}.html");
	}

	string BuildHtmlPageToExport(MarkdownToHtmlConversionResult markdownToHtmlConversionResult)
	{
		var htmlPage = @$"<!DOCTYPE html>
<html>
	<head>
		<meta http-equiv=""content-type"" content=""text/html; charset=utf-8"">
		<title>{Path.GetFileNameWithoutExtension(_markdownFilePath!)}</title>
		<style>{_cascadingStyleSheetsRules}</style>
	</head>
	<body>
		<div id=""markdownviewer"">{_latestMarkdownToHtmlConversionResult!.GeneratedHtml}</div>
	</body>
</html>";

		return htmlPage;
	}

	ExportedFile ExportLastGenerationAsMarkdown()
	{
		var standardMarkdown = _latestMarkdownToHtmlConversionResult!.GeneratedStandardMarkdown;
		return new ExportedFile(
			Encoding.UTF8.GetBytes(standardMarkdown),
			"text/markdown;charset=utf-8",
			Path.GetFileName(_markdownFilePath!));
	}

	public Task RunAsync() => _app.RunAsync();

	public void Dispose() {
		Console.WriteLine("Disposing web server...");
		var v = Stopwatch.GetTimestamp();
		((IDisposable)_app).Dispose();
		var w = Stopwatch.GetElapsedTime(v);
		Console.WriteLine($"Web server diposed in {w.Milliseconds}ms!");
	}
}
