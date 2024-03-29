class FileUpdateNotifier : IDisposable
{
	string _currentlyWatchedFilePath;
	string CurrentlyWatchedFilePath => _currentlyWatchedFilePath;
	FileSystemWatcher _encapsulated;
	readonly NotificationMechanism<FileUpdateNotification> _notificationMechanism;
	FileUpdateNotifier(
		string currentlyWatchedFilePath,
		FileSystemWatcher fileSystemWatcher,
		NotificationMechanism<FileUpdateNotification> notificationMechanism)
	{
		_currentlyWatchedFilePath = currentlyWatchedFilePath;
		_encapsulated = fileSystemWatcher;
		_notificationMechanism = notificationMechanism;
	}

	public void Start()
	{
		_encapsulated.Changed += OnChanged;
		_encapsulated.Renamed += OnRenamed;
		_encapsulated.Error += OnError;
	}

	void OnChanged(object sender, FileSystemEventArgs e)
	{
		_notificationMechanism.SendNotification(FileUpdateNotification.FromMarkdownFilePath(CurrentlyWatchedFilePath));
	}

	void OnError(object sender, ErrorEventArgs e) =>
		PrintException(e.GetException());

	void PrintException(Exception? ex)
	{
		if (ex is null)
		{
			return;
		}

		Console.WriteLine($"Message: {ex.Message}");
		Console.WriteLine("Stacktrace:");
		Console.WriteLine(ex.StackTrace);
		Console.WriteLine();
		PrintException(ex.InnerException);
	}

	public static FileUpdateNotifier Create(
		string filepath,
		NotificationMechanism<FileUpdateNotification> notificationMechanism)
	{
		try
		{
			var folderName = Path.GetDirectoryName(filepath)!;
			var fileName = Path.GetFileName(filepath);
			var filesystemWatcher = new FileSystemWatcher(path: folderName, filter: fileName)
			{
				NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastWrite | NotifyFilters.FileName,
				IncludeSubdirectories = true,
				EnableRaisingEvents = true
			};
			return new(filepath, filesystemWatcher, notificationMechanism);
		}
		catch (ObjectConstructionException objectConstructionException)
		{
			objectConstructionException.EnrichConstructionFailureContextWith<FileUpdateNotifier>(filepath, notificationMechanism);
			throw;
		}
		catch (Exception developerMistake)
		{
			throw ObjectConstructionException.WhenConstructingAnInstanceOf<FileUpdateNotifier>(developerMistake, filepath, notificationMechanism);
		}
	}

	void OnRenamed(object sender, RenamedEventArgs e)
	{
		_currentlyWatchedFilePath = e.FullPath;
		_encapsulated.Dispose();

		var folderName = Path.GetDirectoryName(_currentlyWatchedFilePath)!;
		var fileName = Path.GetFileName(_currentlyWatchedFilePath);
		_encapsulated = new FileSystemWatcher(path: folderName, filter: fileName)
		{
			NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastWrite | NotifyFilters.FileName,
			IncludeSubdirectories = true,
			EnableRaisingEvents = true
		};
		_encapsulated.Changed += OnChanged;
		_encapsulated.Renamed += OnRenamed;
		_encapsulated.Error += OnError;
	}

	public void Dispose()
	{
		((IDisposable)_encapsulated).Dispose();
	}
}
