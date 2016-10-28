using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace WebApi.Pagination
{
    [TestFixture]
    public class LongPollingTest
    {
        [Test]
        public async Task ToListLongPoll()
        {
            var source = new MockDataSource<int>(
                new[] {1, 2, 3},
                returnDataAfterAttempts: 2);

            var result = await source.AsQueryable().ToListLongPollAsync(maxAttempts: 2, delayMs: 0);
            result.Should().Equal(1, 2, 3);
            source.AccessCounter.Should().Be(2);
        }

        [Test]
        public async Task ToListLongPollTimeout()
        {
            var source = new MockDataSource<int>(
                new[] {1, 2, 3},
                returnDataAfterAttempts: 3);

            var result = await source.AsQueryable().ToListLongPollAsync(maxAttempts: 2, delayMs: 0);
            result.Should().BeEmpty();
            source.AccessCounter.Should().Be(2);
        }
    }
}