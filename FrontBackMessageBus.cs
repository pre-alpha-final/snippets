#region Frontend

public class MessageBusProxy
{
	private readonly LoadMessagesHandler _loadMessagesHandler;
	private long _ticks = DateTime.UtcNow.Ticks;

	public MessageBusProxy(LoadMessagesHandler loadMessagesHandler)
	{
		_loadMessagesHandler = loadMessagesHandler;
		Task.Run(LoadMessages);
	}

	private async Task LoadMessages()
	{
		while (true)
		{
			var messages = await _loadMessagesHandler.Handle(_ticks);
			await HandleMessages(messages);
			_ticks = messages
				.Select(e => e.Ticks)
				.DefaultIfEmpty(_ticks - 1)
				.Max() + 1;
		}
	}

	private async Task HandleMessages(List<Message> messages)
	{
		foreach (var message in messages.OrderBy(e => e.Ticks))
		{
			var type = Type.GetType(message.Type);
			var pubSubMessage = JsonConvert.DeserializeObject(message.Payload, type);
			dynamic hubWrapper = Activator.CreateInstance(typeof(HubWrapper<>).MakeGenericType(type));
			await hubWrapper.PublishAsync(pubSubMessage);
		}
	}

	private class HubWrapper<T>
	{
		public async Task PublishAsync(object data)
		{
			await Hub.Default.PublishAsync((T)data);
		}
	}
}

public class LoadMessagesHandler
{
	private readonly IMessageBusApi _messageBusApi;

	public LoadMessagesHandler(IMessageBusApi messageBusApi)
	{
		_messageBusApi = messageBusApi;
	}

	public async Task<List<Message>> Handle(long ticks)
	{
		var getMessagesResponse = (await _messageBusApi.GetMessages(ticks))?.Content;

		return getMessagesResponse?.Messages ?? new List<Message>();
	}
}

public interface IMessageBusApi
{
	[Get("/api/messagebus/{ticks}")]
	Task<ApiResponse<GetMessagesResponse>> GetMessages([Query] long ticks);
}

public class Message
{
	public long Ticks { get; set; }
	public Guid? ForUser { get; set; }
	public string Type { get; set; }
	public string Payload { get; set; }
}

#endregion

#region Backend

public class GetMessagesHandler : IRequestHandler<GetMessagesRequest, GetMessagesResponse>
{
	private readonly IMessageBusRepository _messageBusRepository;
	private readonly IUserInfoFactory _userInfoFactory;

	public GetMessagesHandler(IMessageBusRepository messageBusRepository, IUserInfoFactory userInfoFactory)
	{
		_messageBusRepository = messageBusRepository;
		_userInfoFactory = userInfoFactory;
	}

	public async Task<GetMessagesResponse> Handle(GetMessagesRequest request, CancellationToken cancellationToken)
	{
		var userId = (await _userInfoFactory.Create(cancellationToken)).User.Id;
		List<Message> messages = new();
		for (var i = 0; i < 30; i++)
		{
			messages = await _messageBusRepository.GetMessages(request.Ticks, userId);
			if (messages.Any())
			{
				break;
			}
			await Task.Delay(1000, cancellationToken);
		}

		return new GetMessagesResponse
		{
			Messages = messages,
		};
	}
}

public interface IMessageBusRepository
{
	Task AddMessage(Message message);
	Task<List<Message>> GetMessages(long ticks, UserId forUser);
}

public class MessageBusRepository : IMessageBusRepository
{
	private readonly SemaphoreSlim _sync = new(1, 1);
	private readonly Dictionary<long, Message> _messages = new();

	public MessageBusRepository()
	{
		Task.Run(Trim);
	}

	public async Task AddMessage(Message message)
	{
		using var sync = await _sync.DisposableWaitAsync(TimeSpan.MaxValue);

		var timestamp = EnsureContinuity(DateTime.UtcNow.Ticks);
		message.Ticks = timestamp;
		_messages.Add(timestamp, message);
	}

	public async Task<List<Message>> GetMessages(long ticks, Guid forUser)
	{
		using var sync = await _sync.DisposableWaitAsync(TimeSpan.MaxValue);

		return _messages
			.Where(e => e.Key >= ticks)
			.Where(e => e.Value.ForUser == null || e.Value.ForUser == forUser)
			.Select(e => e.Value)
			.ToList();
	}

	private long EnsureContinuity(long ticks)
	{
		var lastTicks = _messages.Keys.DefaultIfEmpty(long.MinValue).Max();
		while (ticks <= lastTicks)
		{
			ticks++;
		}

		return ticks;
	}

	private async Task Trim()
	{
		while (true)
		{
			var timestamp = DateTime.UtcNow.Ticks;
			await Task.Delay(TimeSpan.FromMinutes(1));
			using var sync = await _sync.DisposableWaitAsync(TimeSpan.MaxValue);
			var messagesToRemove = _messages.Where(e => e.Key < timestamp);
			foreach (var messageToRemove in messagesToRemove)
			{
				_messages.Remove(messageToRemove.Key);
			}
		}
	}
}

public static class SemaphoreSlimExtensions
{
	public static int SafeRelease(this SemaphoreSlim semaphoreSlim)
	{
		try
		{
			return semaphoreSlim.Release();
		}
		catch
		{
			// Ignore
		}

		return -1;
	}

	// IMPORTANT
	// Needs to be awaited in using() otherwise returns a task
	// instead of Disposable -> no Dispose = stuck
	public static async Task<IDisposable> DisposableWaitAsync(this SemaphoreSlim semaphoreSlim, TimeSpan timeout)
	{
		var milliseconds = (int)timeout.TotalMilliseconds;
		if (timeout == TimeSpan.MaxValue)
		{
			milliseconds = int.MaxValue; // Max for WaitAsync
		}
		await semaphoreSlim.WaitAsync(milliseconds).ConfigureAwait(false);

		return new Disposable(semaphoreSlim);
	}

	private class Disposable : IDisposable
	{
		private readonly SemaphoreSlim _semaphoreSlim;
		private bool _isDisposed;

		public Disposable(SemaphoreSlim semaphoreSlim)
		{
			_semaphoreSlim = semaphoreSlim;
		}

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}
			_isDisposed = true;

			_semaphoreSlim.SafeRelease();
		}
	}
}

#endregion
