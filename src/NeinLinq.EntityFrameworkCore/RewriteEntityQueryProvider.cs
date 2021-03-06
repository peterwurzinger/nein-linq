﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query.Internal;

#pragma warning disable EF1001 // Internal EF Core API usage.

namespace NeinLinq
{
    /// <summary>
    /// Proxy for query provider.
    /// </summary>
    public class RewriteEntityQueryProvider : RewriteQueryProvider, IAsyncQueryProvider
    {
        /// <summary>
        /// Create a new rewrite query provider.
        /// </summary>
        /// <param name="provider">The actual query provider.</param>
        /// <param name="rewriter">The rewriter to rewrite the query.</param>
        public RewriteEntityQueryProvider(IQueryProvider provider, ExpressionVisitor rewriter)
            : base(provider, rewriter)
        {
        }

        /// <inheritdoc />
        public override IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            // create query and make proxy again for rewritten query chaining
            var query = Provider.CreateQuery<TElement>(expression);
            return new RewriteEntityQueryable<TElement>(query, this);
        }

        /// <inheritdoc />
        public override IQueryable CreateQuery(Expression expression)
        {
            // create query and make proxy again for rewritten query chaining
            var query = Provider.CreateQuery(expression);
            return (IQueryable?)Activator.CreateInstance(
                typeof(RewriteEntityQueryable<>).MakeGenericType(query.ElementType),
                query, this) ?? throw new InvalidOperationException();
        }

        /// <inheritdoc />
        public virtual TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            // execute query with rewritten expression; async, if possible
            if (Provider is IAsyncQueryProvider asyncProvider)
                return asyncProvider.ExecuteAsync<TResult>(Rewrite(expression), cancellationToken);
            if (typeof(TResult).IsGenericType)
            {
                // TODO: there is a better solution for that, right?
                var resultDefinition = typeof(TResult).GetGenericTypeDefinition();
                if (resultDefinition == typeof(Task<>))
                    return Execute<TResult>(executeTask, expression);
                if (resultDefinition == typeof(IAsyncEnumerable<>))
                    return Execute<TResult>(executeAsyncEnumerable, expression);
            }
            return Provider.Execute<TResult>(Rewrite(expression));
        }

        private TResult Execute<TResult>(MethodInfo method, Expression expression)
        {
            return (TResult)(method.MakeGenericMethod(typeof(TResult).GetGenericArguments()[0])
                .Invoke(this, new object[] { expression }) ?? throw new InvalidOperationException("Execute returns null."));
        }

        private static readonly MethodInfo executeTask = typeof(RewriteEntityQueryProvider)
            .GetMethod("ExecuteTask", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Method ExecuteTask is missing.");

        private Task<TResult> ExecuteTask<TResult>(Expression expression)
        {
            return Task.FromResult(Provider.Execute<TResult>(Rewrite(expression)));
        }

        private static readonly MethodInfo executeAsyncEnumerable = typeof(RewriteEntityQueryProvider)
            .GetMethod("ExecuteAsyncEnumerable", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Method ExecuteAsyncEnumerable is missing.");

        private IAsyncEnumerable<TResult> ExecuteAsyncEnumerable<TResult>(Expression expression)
        {
            return new RewriteQueryEnumerable<TResult>(Provider.Execute<IEnumerable<TResult>>(Rewrite(expression)));
        }
    }
}

#pragma warning restore EF1001 // Internal EF Core API usage.
