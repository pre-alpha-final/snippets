using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CustomerExposureDataProvider;

public class CustomerExposureDataProvider
{
	private const int ParallelFactor = 10;
	private const int BufferSize = 10;
	private readonly IExposureDbContextOptionsProvider _exposureDbContextOptionsProvider;

	public CustomerExposureDataProvider(IExposureDbContextOptionsProvider exposureDbContextOptionsProvider)
	{
		_exposureDbContextOptionsProvider = exposureDbContextOptionsProvider;
	}

	public IEnumerable<CustomerExposure> Get(IEnumerable<CustomerExposureId> customerExposureIds)
	{
		var context = new Context();
		while (true)
		{
			if (context.DataQueue.Count < BufferSize && context.HasMoreData && context.IsGettingData == false)
			{
				context.IsGettingData = true;
				_ = Task.Run(async () =>
				{
					var customerExposures = await GetCustomerExposures(
						customerExposureIds.Skip(context.Page++ * ParallelFactor).Take(ParallelFactor));
					if (customerExposures.Count == 0)
					{
						context.HasMoreData = false;
					}
					foreach (var customerExposure in customerExposures)
					{
						context.DataQueue.Enqueue(customerExposure);
					}
					context.IsGettingData = false;
				});
			}

			if (context.DataQueue.TryDequeue(out var nextCustomerExposure) == false && context.HasMoreData == false)
			{
				yield break;
			}

			if (nextCustomerExposure != null)
			{
				yield return nextCustomerExposure;
			}
		}
	}

	private async Task<List<CustomerExposure>> GetCustomerExposures(IEnumerable<CustomerExposureId> customerExposureIds)
	{
		var tasks = new List<Task<CustomerExposure>>();
		foreach (var customerExposureId in customerExposureIds)
		{
			tasks.Add(Task.Run(async () =>
			{
				try
				{
					await using var exposureDbContext = new ExposureDbContext(_exposureDbContextOptionsProvider.Create("exposure"));
					var exposure = await exposureDbContext.CustomerExposureReadOnly
						.Where(e => e.Id == customerExposureId)
						.FirstOrDefaultAsync();
					var calculationTransactions = await exposureDbContext.CalculationTransactionsReadOnly
						.Where(e => e.CustomerExposureId == customerExposureId)
						.Where(e => e.IsValidForProgram)
						.Include(e => e.Product)
						.Include(e => e.Distributor)
						.Include(e => e.PayTo)
						.Include(e => e.Parent)
						.Include(e => e.ShipFrom)
						.Include(e => e.ShipTo)
						.ToListAsync();
					exposure.CalculationTransactions = calculationTransactions;

					return exposure;
				}
				catch
				{
					return null;
				}
			}));
		}
		await Task.WhenAll(tasks);

		return tasks.Select(e => e.Result).Where(e => e != null).ToList();
	}

	private class Context
	{
		public int Page { get; set; }
		public bool HasMoreData { get; set; } = true;
		public bool IsGettingData { get; set; }
		public ConcurrentQueue<CustomerExposure> DataQueue { get; } = new();
	}
}
