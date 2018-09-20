﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace NeinLinq
{
    /// <summary>
    /// Proxy for rewritten queries.
    /// </summary>
    public class RewriteQueryable<T> : IOrderedQueryable<T>
    {
        private readonly IQueryable queryable;
        private readonly RewriteQueryProvider provider;

        /// <summary>
        /// Create a new query to rewrite.
        /// </summary>
        /// <param name="queryable">The actual query.</param>
        /// <param name="provider">The provider to rewrite the query.</param>
        public RewriteQueryable(IQueryable queryable, RewriteQueryProvider provider)
        {
            if (queryable == null)
                throw new ArgumentNullException(nameof(queryable));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            this.queryable = queryable;
            this.provider = provider;
        }

        /// <summary>
        /// Rewrites the entire query expression.
        /// </summary>
        /// <returns>A rewritten query.</returns>
        public IQueryable<T> UnwrapQuery()
        {
            var expression = provider.Rewriter.Visit(queryable.Expression);
            return provider.Provider.CreateQuery<T>(expression);
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            // rewrite on enumeration
            var enumerable = UnwrapQuery();
            return enumerable.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public Type ElementType => queryable.ElementType;

        /// <inheritdoc />
        public Expression Expression => queryable.Expression;

        /// <inheritdoc />
        public IQueryProvider Provider => provider; // replace query provider
    }
}