using Baubit.Collections;

namespace Baubit.Test.Collections.ConcurrentList
{
    public class Test
    {
        [Theory]
        [InlineData(1000, 2)]
        //[InlineData(10000, 2)]
        //[InlineData(100000, 2)]
        public void CanReadAndWriteConcurrently(int maxItems, int maxDegreeOfParallelism)
        {
            var concurrentList = new ConcurrentList<int>();
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };
            var addAction = () => { Parallel.For(0, maxItems, parallelOptions, concurrentList.Add); };

            var removeAction = () =>
            {
                int removedCount = 0;
                while (removedCount < maxItems)
                {
                    while (concurrentList.Count < 1)
                    {
                        Thread.Sleep(5);
                    }
                    Parallel.For(0, concurrentList.Count, parallelOptions, i => { concurrentList.RemoveAt(0); removedCount++; });
                }
            };

            Parallel.Invoke(addAction, removeAction);

            Assert.Empty(concurrentList);
        }
    }
}
