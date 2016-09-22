using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using FluentAssertions;
using NUnit.Framework;

namespace WebApi.Pagination
{
    [TestFixture]
    public class PaginationTest
    {
        [Test]
        public void Subset()
        {
            var source = new List<int> {1, 2, 3, 4, 5}.AsQueryable();

            long firstIndex;
            var result = source.Paginate(new RangeItemHeaderValue(from: 1, to: 2), out firstIndex).ToList();

            result.Should().Equal(2, 3);
            firstIndex.Should().Be(1);
        }

        [Test]
        public void Skip()
        {
            var source = new List<int> {1, 2, 3, 4, 5}.AsQueryable();

            long firstIndex;
            var result = source.Paginate(new RangeItemHeaderValue(from: 2, to: null), out firstIndex).ToList();

            result.Should().Equal(3, 4, 5);
            firstIndex.Should().Be(2);
        }

        [Test]
        public void Tail()
        {
            var source = new List<int> {1, 2, 3, 4, 5}.AsQueryable();

            long firstIndex;
            var result = source.Paginate(new RangeItemHeaderValue(from: null, to: 2), out firstIndex).ToList();

            result.Should().Equal(4, 5);
            firstIndex.Should().Be(3);
        }

        [Test]
        public void TailOverflow()
        {
            var source = new List<int> {1, 2}.AsQueryable();

            long firstIndex;
            var result = source.Paginate(new RangeItemHeaderValue(from: null, to: 4), out firstIndex).ToList();

            result.Should().Equal(1, 2);
            firstIndex.Should().Be(0);
        }

        [Test]
        public void Exception()
        {
            var source = new List<int> {1, 2, 3, 4, 5}.AsQueryable();

            long firstIndex;
            source.Invoking(x => x.Paginate(new RangeItemHeaderValue(from: null, to: null), out firstIndex))
                .ShouldThrow<ArgumentException>();
        }
    }
}