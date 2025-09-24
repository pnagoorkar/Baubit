using Baubit.Collections;
using Baubit.Identity;

namespace Baubit.Test.Identity.GuidV7Generator
{
    public class Test
    {
        [Fact]
        public void CanCreateDefault()
        {
            var guidV7Generator = Baubit.Identity.GuidV7Generator.CreateNew();
            Assert.NotNull(guidV7Generator);
        }
        [Fact]
        public void CanCreateUsingGuidV7()
        {
            var dateTimeOffset = DateTimeOffset.UtcNow;
            var reference = Guid.CreateVersion7(dateTimeOffset);
            var guidV7Generator = Baubit.Identity.GuidV7Generator.CreateNew(reference);
            Assert.NotNull(guidV7Generator);
            Assert.Equal(dateTimeOffset.ToUnixTimeMilliseconds(), guidV7Generator.LastIssuedUnixMs);
        }
        [Fact]
        public void CanNotCreateWithoutGuidV7()
        {
            Assert.Throws<InvalidOperationException>(() => Baubit.Identity.GuidV7Generator.CreateNew(Guid.NewGuid()));
        }
        [Fact]
        public void CanProgressSeedUsingDateTimeOffset()
        {
            var guidV7Generator = Baubit.Identity.GuidV7Generator.CreateNew();
            var guid1 = guidV7Generator.GetNext();
            var lastMs1 = guidV7Generator.LastIssuedUnixMs;
            var dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(lastMs1 + 1);
            var guid2 = guidV7Generator.GetNext(dateTimeOffset);
            var lastMs2 = guidV7Generator.LastIssuedUnixMs;

            Assert.True(lastMs1 + 1 == lastMs2);
        }
        [Fact]
        public void CanProgressSeedUsingGuidV7()
        {
            var initTime = DateTimeOffset.UtcNow;
            var initTimestamp = initTime.ToUnixTimeMilliseconds();
            var initGuid = Guid.CreateVersion7(initTime);
            var generator1 = Baubit.Identity.GuidV7Generator.CreateNew(initGuid);
            var generator2 = Baubit.Identity.GuidV7Generator.CreateNew(initGuid);

            // generators created with the same seed as initTime
            Assert.Equal(generator1.LastIssuedUnixMs, generator2.LastIssuedUnixMs);
            Assert.Equal(initTimestamp, generator1.LastIssuedUnixMs);
            Assert.Equal(initTimestamp, generator2.LastIssuedUnixMs);

            var driftTime = initTime.AddHours(1);

            var driftGuid = Guid.CreateVersion7(driftTime);

            generator2.InitializeFrom(driftGuid); // tell the generator we want generated ids to have a timestamp AFTER the drifted time
            var driftTimestamp = driftTime.ToUnixTimeMilliseconds();

            Assert.Equal(driftTimestamp, generator2.LastIssuedUnixMs); // generator2 seed reflects the drifted time

            var guid1 = generator1.GetNext(initTime);
            var guid2 = generator2.GetNext(initTime);

            Baubit.Identity.GuidV7Generator.TryGetUnixMs(guid1, out var guid1Timestamp);
            Baubit.Identity.GuidV7Generator.TryGetUnixMs(guid2, out var guid2Timestamp);

            // undrifted generator generates a guid with a timestamp 1 ms after the init time because the last issued guid was at initTimestamp
            Assert.Equal(guid1Timestamp, initTimestamp + 1); 
            // drifted generator generates a guid with a time stamp 1 ms after the drifted time because we told it we want ids generated after the driftTimestamp
            Assert.Equal(guid2Timestamp, driftTimestamp + 1); 
        }
        [Fact]
        public void CannotProgressSeedWithoutGuidV7()
        {
            var guidV7Generator = Baubit.Identity.GuidV7Generator.CreateNew();
            Assert.Throws<InvalidOperationException>(() => guidV7Generator.InitializeFrom(Guid.NewGuid()));
        }
        [Theory]
        [InlineData(100000)]
        public void IdsAreMonotonicallyUnique(int numOfIds)
        {
            var guidV7Generator = Baubit.Identity.GuidV7Generator.CreateNew();
            var monotonicIds = new ConcurrentList<Guid>();

            var parallelLoopResult = Parallel.For(0, numOfIds, _ =>
            {
                monotonicIds.Add(guidV7Generator.GetNext());
            });

            Assert.Null(parallelLoopResult.LowestBreakIteration);
            Assert.Equal(numOfIds, monotonicIds.Count);
            var timeStamps = new ConcurrentList<long>();
            foreach (var monotonicId in monotonicIds)
            {
                Baubit.Identity.GuidV7Generator.TryGetUnixMs(monotonicId, out var ms);
                timeStamps.Add(ms);
            }
            Assert.Equal(numOfIds, timeStamps.Distinct().Count());
        }
        [Theory]
        [InlineData(100000)]
        public void IdsAreMonotonicallyUniqueEvenWhenCreatedWithReferenceOfAnother(int numOfIds)
        {
            var monotonicIds = new ConcurrentList<Guid>();
            var dateTimeOffset = DateTimeOffset.UtcNow;
            var reference = Guid.CreateVersion7(dateTimeOffset);
            var guidV7Generator = Baubit.Identity.GuidV7Generator.CreateNew(reference);
            var parallelLoopResult = Parallel.For(0, numOfIds, _ =>
            {
                monotonicIds.Add(guidV7Generator.GetNext());
            });

            Assert.Null(parallelLoopResult.LowestBreakIteration);
            Assert.Equal(numOfIds, monotonicIds.Count);
            var timeStamps = new ConcurrentList<long>();
            foreach (var monotonicId in monotonicIds)
            {
                Baubit.Identity.GuidV7Generator.TryGetUnixMs(monotonicId, out var ms);
                timeStamps.Add(ms);
            }
            Assert.Equal(numOfIds, timeStamps.Distinct().Count());
            Baubit.Identity.GuidV7Generator.TryGetUnixMs(reference, out var refTimestamp);
            Assert.True(timeStamps.Min() > refTimestamp); // the earliest generated guid is still - in time - later than the reference
        }
    }
}
