using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Namespace
{
	/*
	 * WARNING
	 * Pass in a yieldable enumerable of tasks and not a materialized list.
	 * Otherwise You'll be passing a list of already running tasks
	 * which defeats the whole purpose.
	 * 
	 * Linq queries are fine, just don't ToList, e.g.
	 * await new TaskThrottle(itemIds.Select(e => ProcessItem(e)), 10).Execute();
	 */
	public class TaskThrottle
	{
		private readonly IEnumerable<Task> _tasks;
		private readonly SemaphoreSlim _semaphoreSlim;

		public List<string> Errors { get; set; }

		public TaskThrottle(IEnumerable<Task> tasks, int maxConcurrentCount)
		{
			if (tasks is IList)
			{
				throw new ArgumentException("Passing materialized list of tasks doesn't make sense, as all of them are already running");
			}

			_tasks = tasks;
			_semaphoreSlim = new SemaphoreSlim(maxConcurrentCount - 1, maxConcurrentCount);
		}

		public async Task Execute()
		{
			var proxyTasks = new List<Task>();
			var originalTasks = new List<Task>();
			using (var enumerator = _tasks.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					var task = enumerator.Current;
					originalTasks.Add(task);
					proxyTasks.Add(Task.Run(async () =>
					{
						try
						{
							await task;
						}
						catch (Exception e)
						{
							// ignore
						}
						finally
						{
							_semaphoreSlim.Release();
						}
					}));
					await _semaphoreSlim.WaitAsync();
				}

				while (proxyTasks.All(e => e.IsCompleted) == false)
				{
					await Task.Delay(100);
				}

				Errors = originalTasks
					.Where(e => e.Exception != null)
					.Select(e => $"Error getting item: '{e.Exception.Message}'")
					.ToList();
			}
		}
	}
}
