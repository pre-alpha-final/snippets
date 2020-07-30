using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Foo
{
	public class AsyncQueryable<T> : EnumerableQuery<T>, IQueryable<T>, IAsyncEnumerable<T>
	{
		public AsyncQueryable(IEnumerable<T> enumerable) 
			: base(enumerable)
		{
		}

		public AsyncQueryable(Expression expression) 
			: base(expression)
		{
		}

		public IAsyncEnumerator<T> GetEnumerator()
		{
			return new AsyncQueryableEnumerator<T>(this.AsEnumerable().GetEnumerator());
		}

		IQueryProvider IQueryable.Provider => new EfAsyncQueryProvider<T>(this);
	}

	public class AsyncQueryableEnumerator<T> : IAsyncEnumerator<T>
	{
		private readonly IEnumerator<T> _source;
		public T Current => _source.Current;

		public AsyncQueryableEnumerator(IEnumerator<T> source)
		{
			_source = source;
		}

		public void Dispose()
		{
			_source.Dispose();
		}

		public Task<bool> MoveNext(CancellationToken cancellationToken)
		{
			try
			{
				return Task.FromResult(_source.MoveNext());
			}
			catch (Exception e)
			{
				return Task.FromResult(false);
			}
		}
	}

	public class EfAsyncQueryProvider<T> : IAsyncQueryProvider
	{
		private readonly IQueryProvider _source;

		public EfAsyncQueryProvider(IQueryProvider source)
		{
			_source = source;
		}
		
		public IQueryable CreateQuery(Expression expression)
		{
			return new AsyncQueryable<T>(expression);
		}

		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
		{
			return new AsyncQueryable<TElement>(expression);
		}

		public object Execute(Expression expression)
		{
			return _source.Execute(expression);
		}

		public TResult Execute<TResult>(Expression expression)
		{
			return _source.Execute<TResult>(expression);
		}

		public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
		{
			return new AsyncQueryable<TResult>(expression);
		}

		public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			return Task.FromResult(Execute<TResult>(expression));
		}
	}

	public static class IEnumerableExtensions
	{
		public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source)
		{
			return new AsyncQueryable<T>(source);
		}
	}
}
