record MarkdownFileLivePreviewInput(MarkdownFileAbsolutePath MarkdownFileAbsolutePath)
{
	public int NumberOfItemsInHtmlGenerationCacheTriggeringCleanup => 50;
	public int AcceptableDataFreshnessInSecondsAfterHtmlGenerationCacheCleanup => 8;
	public int NumberOfMarkdownFileReadRetries => 3;
	public int WaitTimeBeforeEachMarkdownFileReadRetryInMilliseconds => 100;
	public string CascadingStyleSheetsRules => WebBrowser.Name switch
	{ // 👇 https://www.reddit.com/r/firefox/comments/logdzx/does_firefox_use_a_different_color_profile/
		"Mozilla Firefox" => _cascadingStyleSheetsRules,
			_ => _cascadingStyleSheetsRules
				.Replace("rgba(0,0,0,.75)", "rgb(65, 80, 118)")  // text color
				.Replace("rgba(0,0,0,.5)", "rgb(127, 130, 130)") // quote color
				.Replace("hsla(210,18%,87%,1)", "hsl(210, 17%, 89%)") // header lines
	};
	readonly string _cascadingStyleSheetsRules = @"
html {
	background-color: #f3f3f3;
}
#markdownviewer {
	padding: 10px 10% 193px;
	font-family: Lato,Helvetica Neue,Helvetica,sans-serif;
	font-size: 16px! important;
	line-height: 1.5;
	word-wrap: break-word;
	margin: unset;
	margin-top: 0px;
	margin-bottom: 16px;
	max-width: none;
	color: rgba(0,0,0,.75);
	background: #f3f3f3;
	tab-size: 4;
}

#markdownviewer div p:first-child {
	margin-top: 0;
}

#markdownviewer h1, h2, h3, h4, h5, h6 {
	margin-top: 24px;
	margin-bottom: 16px;
	font-weight: 600;
	line-height: 1.25;
}

#markdownviewer h1 {
	padding-bottom: .3em;
	font-size: 2em;
	border-bottom: 1px solid hsla(210,18%,87%,1);
}

#markdownviewer h2 {
	padding-bottom: .3em;
	font-size: 1.5em;
	border-bottom: 1px solid hsla(210,18%,87%,1);
}

#markdownviewer div {
	max-width: fit-content;
}

#markdownviewer pre {
	margin: 0;
}

#markdownviewer code:not(pre code) {
	background-color: rgba(0,0,0,.05);
	border-radius: 3px;
	padding: 2px 4px;
	font-family: Roboto Mono,Lucida Sans Typewriter,Lucida Console,monaco,Courrier,monospace;
	font-size: .85em;
}

#markdownviewer div:has(> pre) {
	margin: 18px 0px;
	max-width: min-content;
	padding: 0.5em;
	overflow: auto;
	font-size: 85%;
	line-height: 1.45;
	border-radius: 3px;
}

#markdownviewer li input {
	width: auto;
	margin-bottom: 0px;
	height: auto;
}

#markdownviewer table {
	border-spacing: 0;
	border-collapse: collapse;
	margin: 42px 0px;
}
#markdownviewer table th, table td {
    padding: 8px 12px;
    border: 1px solid #d0d7de;
}
#markdownviewer table th, table td:nth-child(even) {
    background: transparent;
}
#markdownviewer table th, table td:nth-child(odd) {
    background: transparent;
}

#markdownviewer table td:first-of-type {
	border-left: 0;
}
#markdownviewer table td:last-of-type {
	border-right: 0;
}
#markdownviewer table tr:last-of-type td {
	border-bottom: 0;
}
#markdownviewer table tr:first-of-type th {
	border-top: 0;
}
#markdownviewer table tr:first-of-type th:first-of-type {
	border-left: 0;
}
#markdownviewer table tr:first-of-type th:last-of-type {
	border-right: 0;
}

#markdownviewer a {
	color: #0969da;
}

#markdownviewer blockquote {
	padding: 0 15px;
	color: rgba(0,0,0,.5);
	border-left: 4px solid #ddd;
}

#markdownviewer img {
	max-width: 100%;
	padding: 5px;
}

#markdownviewer .contains-task-list {
	list-style-type: none;
	padding-left: 0;
}

#markdownviewer div[class^='generated-diagram-'], div[class*=' generated-diagram-'] {
	margin: 5px 0px;
	border: black 1px dotted;
}

#markdownviewer div[class^='generated-diagram-']:only-child, div[class*=' generated-diagram-']:only-child {
	margin: 0 auto;
}

#markdownviewer details {
	background-color: rgba(0,0,0,.05);
	margin: 10px 0px;
	display: flow-root;
}

#markdownviewer details > summary {
	list-style-type: '▶️ ';
	background-color: #9BC2CB;
	font-weight: bold;
	padding: 10px;
	cursor: pointer;
}

#markdownviewer .summary-color-2 {
	background-color: #8ACBB9;
}

#markdownviewer .summary-color-3 {
	background-color: #ECB4A7;
}

#markdownviewer details[open] > summary {
	list-style-type: '🔽 ';
}

#markdownviewer details > :not(:first-child) {
	margin: 10px 0px 8px 10px;
}

#markdownviewer .contains-vertical-columns {
	width: 100%;
	display: grid;
	grid-template-columns: 1fr 1fr;
	grid-gap: 20px;
	max-width: 100%;
}

#markdownviewer .vertical-columns-ratio-50-50 {
	grid-template-columns: 1fr 1fr;
}

#markdownviewer .vertical-columns-ratio-40-60 {
	grid-template-columns: 40fr 60fr;
}

#markdownviewer .contains-vertical-columns-50-50 {
	width: 100%;
	display: grid;
	grid-template-columns: 1fr 1fr;
	grid-gap: 20px;
}

#markdownviewer .vertical-column {
	min-width: 0;
	max-width: unset;
	margin: unset;
	padding: unset;
}

#markdownviewer .vertical-column-title {
	font-style: italic;
	text-decoration: underline;
}

#markdownviewer svg {
	max-width: 100%;
	max-height: 100%;
}

#markdownviewer svg:has(> svg) {
	min-width: 100%;
	min-height: 100%;
	width: fit-content;
	height: fit-content;
}

.export-button {
	background: revert!important;
	color: revert!important;
	border: revert!important;
	font: revert!important;
}
";
}
