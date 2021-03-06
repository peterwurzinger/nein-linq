﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NeinLinq.Fakes.EntityAsyncQuery;
using Xunit;

namespace NeinLinq.Tests.EntityAsyncQuery
{
    public class RealTest : IDisposable
    {
        private readonly Context db;

        public RealTest()
        {
            using (var init = new Context())
            {
                init.ResetDatabase();

                init.Dummies.AddRange(new[]
                {
                    new Dummy
                    {
                        Name = "Asdf",
                        Number = 123.45m,
                        Other = new OtherDummy
                        {
                            Name = "Asdf"
                        }
                    },
                    new Dummy
                    {
                        Name = "Qwer",
                        Number = 67.89m,
                        Other = new OtherDummy
                        {
                            Name = "Qwer"
                        }
                    },
                    new Dummy
                    {
                        Name = "Narf",
                        Number = 3.14m,
                        Other = new OtherDummy
                        {
                            Name = "Narf"
                        }
                    }
                });
                init.SaveChanges();
            }

            db = new Context();
        }

        [Fact]
        public async Task AsNoTrackingShouldSucceed()
        {
            var rewriter = new Rewriter();
            var query = db.Dummies.EntityRewrite(rewriter);

            var result = await query.AsNoTracking().ToListAsync();

            Assert.True(rewriter.VisitCalled);
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task IncludeShouldSucceed()
        {
            var rewriter = new Rewriter();
            var query = db.Dummies.EntityRewrite(rewriter);

            var result = await query.Include(d => d.Other).ToListAsync();

            Assert.True(rewriter.VisitCalled);
            Assert.All(result, r => Assert.Equal(r.Name, r.Other.Name));
        }

        [Fact]
        public async Task SubQueryShouldSucceed()
        {
            var innerRewriter = new Rewriter();
            var dummies = db.Dummies.EntityRewrite(innerRewriter);

            var outerRewriter = new Rewriter();
            var query = from dummy in db.Dummies.EntityRewrite(outerRewriter)
                        where dummies.Any(d => d.Id < dummy.Id)
                        select dummy;

            var result = await query.ToListAsync();

            Assert.True(outerRewriter.VisitCalled);
            Assert.True(innerRewriter.VisitCalled);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task SubQueryShouldSucceedWithProjection()
        {
            var innerRewriter = new Rewriter();
            var dummies = db.Dummies.EntityRewrite(innerRewriter).Select(d => new { d.Id });

            var outerRewriter = new Rewriter();
            var query = from dummy in db.Dummies.EntityRewrite(outerRewriter)
                        where dummies.Any(d => d.Id < dummy.Id)
                        select dummy;

            var result = await query.ToListAsync();

            Assert.True(outerRewriter.VisitCalled);
            Assert.True(innerRewriter.VisitCalled);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task SubQueryShouldSucceedWithInclude()
        {
            var innerRewriter = new Rewriter();
            var dummies = db.Dummies.EntityRewrite(innerRewriter);

            var outerRewriter = new Rewriter();
            var query = from dummy in db.Dummies.EntityRewrite(outerRewriter)
                        where dummies.Any(d => d.Id < dummy.Id)
                        select dummy;

            var result = await query.Include(d => d.Other).ToListAsync();

            Assert.True(outerRewriter.VisitCalled);
            Assert.True(innerRewriter.VisitCalled);
            Assert.All(result, r => Assert.Equal(r.Name, r.Other.Name));
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task SubQueryShouldSucceedWithAsNoTracking()
        {
            var innerRewriter = new Rewriter();
            var dummies = db.Dummies.EntityRewrite(innerRewriter);

            var outerRewriter = new Rewriter();
            var query = from dummy in db.Dummies.EntityRewrite(outerRewriter)
                        where dummies.Any(d => d.Id < dummy.Id)
                        select dummy;

            var result = await query.AsNoTracking().ToListAsync();

            Assert.True(outerRewriter.VisitCalled);
            Assert.True(innerRewriter.VisitCalled);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task ToListAsyncShouldWork()
        {
            await db.Dummies.AsQueryable().ToListAsync();
        }

        [Fact]
        public async Task ToListAsyncShouldSucceed()
        {
            var rewriter = new Rewriter();
            var query = db.Dummies.EntityRewrite(rewriter);

            var result = await query.ToListAsync();

            Assert.True(rewriter.VisitCalled);
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task SumAsyncShouldWork()
        {
            await db.Dummies.AsQueryable().SumAsync(d => d.Number);
        }

        [Fact]
        public async Task SumAsyncShouldSucceed()
        {
            var rewriter = new Rewriter();
            var query = db.Dummies.EntityRewrite(rewriter);

            var result = await query.SumAsync(d => d.Number);

            Assert.True(rewriter.VisitCalled);
            Assert.Equal(194.48m, result, 2);
        }

#pragma warning disable EF1001 // Internal EF Core API usage.

        [Fact]
        public async Task ExecuteAsyncShouldSucceed()
        {
            var rewriter = new Rewriter();
            var query = db.Dummies.EntityRewrite(rewriter);

            var enumerator = ((Microsoft.EntityFrameworkCore.Query.Internal.IAsyncQueryProvider)query.Provider).ExecuteAsync<IAsyncEnumerable<Dummy>>(query.Expression).GetAsyncEnumerator();

            var result = await enumerator.MoveNextAsync();

            Assert.True(rewriter.VisitCalled);
            Assert.True(result);
        }

#pragma warning restore EF1001 // Internal EF Core API usage.

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
