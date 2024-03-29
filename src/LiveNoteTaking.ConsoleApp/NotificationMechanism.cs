using System.Threading.Channels;

class NotificationMechanism<TNotificationMessage> : IDisposable
{
	const int ChannelCapacity = 1; // 👈 Always process the most recent notification
	readonly Channel<TNotificationMessage> _channel;
	public TNotificationMessage MostRecentNotificationMessage;
	NotificationMechanism(Channel<TNotificationMessage> channel)
	{
		_channel = channel;
		MostRecentNotificationMessage = default(TNotificationMessage)!;
	}

	public static NotificationMechanism<TNotificationMessage> Create()
	{
		var channelOptions = new BoundedChannelOptions(ChannelCapacity)
		{
			FullMode = BoundedChannelFullMode.DropOldest
		};
		var channel = Channel.CreateBounded<TNotificationMessage>(channelOptions);
		return new(channel);
	}

	public void SendNotification(TNotificationMessage message)
	{
		_channel.Writer.WriteAsync(message);
		MostRecentNotificationMessage = message;
	}

	public IAsyncEnumerable<TNotificationMessage> ReadNotifications()
	{
		return _channel.Reader.ReadAllAsync();
	}

	public void Dispose()
	{
		if (_channel.Writer.TryComplete()) // Dispose writer
		{
			_channel.Reader.Completion.ContinueWith(o => { });  // Dispose reader
		}
	}
}
