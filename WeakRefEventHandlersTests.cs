using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace RceServer.Tests
{
	public class FoobarEventHandlers
	{
		public class Handlers
		{
			public Func<Task> OnFoo { get; set; }
			public Func<string, Task> OnBar { get; set; }
		}

		public void OnFoo()
		{
			GetEventHandlersList().ForEach(eventHandler =>
			{
				if (eventHandler.OnFoo != null)
				{
					Task.Run(eventHandler.OnFoo);
				}
			});
		}

		public void OnBar(string text)
		{
			GetEventHandlersList().ForEach(eventHandler =>
			{
				if (eventHandler.OnBar != null)
				{
					Task.Run(() => eventHandler.OnBar(text));
				}
			});
		}

		#region Boilerplate

		private readonly ConcurrentDictionary<Guid, WeakReference<Handlers>> _eventHandlersWeakRefDictionary =
			new ConcurrentDictionary<Guid, WeakReference<Handlers>>();

		private List<Handlers> GetEventHandlersList()
		{
			var eventHandlersList = new List<Handlers>();
			foreach (var entry in _eventHandlersWeakRefDictionary)
			{
				if (entry.Value.TryGetTarget(out var eventHandlers))
				{
					eventHandlersList.Add(eventHandlers);
				}
				else
				{
					_eventHandlersWeakRefDictionary.TryRemove(entry.Key, out _);
				}
			}

			return eventHandlersList;
		}

		public Handlers AddAsWeakRef(Handlers handlers)
		{
			_eventHandlersWeakRefDictionary.GetOrAdd(Guid.NewGuid(), new WeakReference<Handlers>(handlers));

			return handlers;
		}

		#endregion
	}

	public class Foo
	{
		public FoobarEventHandlers FoobarEventHandlers { get; }
			= new FoobarEventHandlers();

		public void DoFoo()
		{
			// Do foo
			// ...

			// Notify with OnFoo
			FoobarEventHandlers.OnFoo();
		}
	}

	public class WeakRefEventHandlersTests
	{
		[Fact]
		public async Task WeakRefEventHandlers_WhenNoStrongReference_DoesNotRunHandler()
		{
			var results = new List<string>();

			var foo = new Foo();
			foo.FoobarEventHandlers.AddAsWeakRef(new FoobarEventHandlers.Handlers
			{
				OnFoo = async () => results.Add("Handler 1")
			});

			await Task.Delay(1);
			GC.Collect();

			for (var i = 0; i < 3; i++)
			{
				foo.DoFoo();
				await Task.Delay(100);
			}

			Assert.Empty(results);
		}

		[Fact]
		public async Task WeakRefEventHandlers_WhenGivenStrongReference_RunsHandler()
		{
			var results = new List<string>();

			var foo = new Foo();
			var handler = foo.FoobarEventHandlers.AddAsWeakRef(new FoobarEventHandlers.Handlers
			{
				OnFoo = async () => results.Add("Handler 1")
			});

			await Task.Delay(1);
			GC.Collect();

			for (var i = 0; i < 3; i++)
			{
				foo.DoFoo();
				await Task.Delay(100);
			}

			Assert.Equal(3, results.Count);
		}

		[Fact]
		public async Task WeakRefEventHandlers_WhenHandlerNullified_DoesNotRunHandler()
		{
			var results = new List<string>();

			var foo = new Foo();
			var handler = foo.FoobarEventHandlers.AddAsWeakRef(new FoobarEventHandlers.Handlers
			{
				OnFoo = async () => results.Add("Handler 1")
			});
			handler = null;

			await Task.Delay(1);
			GC.Collect();

			for (var i = 0; i < 3; i++)
			{
				foo.DoFoo();
				await Task.Delay(100);
			}

			Assert.Empty(results);
		}
	}
}
