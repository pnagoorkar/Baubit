namespace Baubit.Test.Collections.ObservableConcurrentList
{
    public class Test
    {
        [Theory]
        [InlineData(1000)]
        public void CanAddAndRemoveConcurrentlyWithNotificationsOfEach(int numberOfItems)
        {
            int itemsAdded = 0, 
                itemsRemoved = 0;

            var list = new Baubit.Collections.ObservableConcurrentList<int>();

            list.OnCollectionChanged += @event =>
            {
                switch (@event.ChangeType)
                {
                    case Baubit.Collections.CollectionChangeType.Added:
                        itemsAdded++;
                        Parallel.For(0, @event.Sender.Count, i => list.RemoveAt(0));
                        break;
                    case Baubit.Collections.CollectionChangeType.Removed:
                        itemsRemoved++;
                        break;
                    default:
                        break;
                }
            };

            Parallel.For(0, numberOfItems, list.Add);

            while (itemsAdded != numberOfItems && itemsRemoved != numberOfItems)
            {
                Thread.Sleep(1);
            }

            Assert.Equal(numberOfItems, itemsAdded);
            Assert.Equal(numberOfItems, itemsRemoved);
        }
    }
}
