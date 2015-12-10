using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WebApi.Pagination
{
    public class MockDataSource<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _data;

        private readonly int _returnDataAfterAttempts;

        public MockDataSource(IEnumerable<T> data, int returnDataAfterAttempts)
        {
            _data = data;
            _returnDataAfterAttempts = returnDataAfterAttempts;
        }

        public int AccessCounter { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            if (++AccessCounter == _returnDataAfterAttempts)
                return _data.GetEnumerator();
            else return Enumerable.Empty<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}