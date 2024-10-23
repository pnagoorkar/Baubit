using Baubit.IO;
using Baubit.Reflection;
using System.Reflection;
using System.Text;

namespace Baubit.Test.IO.KMP
{
    public class Test
    {
        private static string LoremIpsum; 
        static Test()
        {
            LoremIpsum = Assembly.GetExecutingAssembly().ReadResource("Baubit.Test.IO.KMP.loremIpsum.txt").GetAwaiter().GetResult().Value;
        }
        [Fact]
        public async Task CanSearchForOneOccurrenceOfOneTriad()
        {
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(LoremIpsum));
            using StreamReader reader = new StreamReader(stream);

            var kmpTriad = new KMPTriad("eiusmod tempor ", " ut labore", 1);

            await reader.SearchAsync(CancellationToken.None, kmpTriad);

            Assert.NotEmpty(kmpTriad.KMPResults);
            Assert.Equal("incididunt", kmpTriad.KMPResults.First().Value);
        }

        [Fact]
        public async Task CanSearchForAllOccurrenceOfOneTriad()
        {
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(LoremIpsum));
            using StreamReader reader = new StreamReader(stream);

            var kmpTriad = new KMPTriad("o", "i");

            await reader.SearchAsync(CancellationToken.None, kmpTriad);

            Assert.NotEmpty(kmpTriad.KMPResults);
            Assert.Equal(16, kmpTriad.KMPResults.Count);
        }

        [Fact]
        public async Task CanSearchForAllOccurrenceOfTwoTriads()
        {
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(LoremIpsum));
            using StreamReader reader = new StreamReader(stream);

            var kmpTriad1 = new KMPTriad("o", "i");
            var kmpTriad2 = new KMPTriad("i", "i");

            await reader.SearchAsync(CancellationToken.None, kmpTriad1, kmpTriad2);

            Assert.NotEmpty(kmpTriad1.KMPResults);
            Assert.Equal(16, kmpTriad1.KMPResults.Count);

            Assert.NotEmpty(kmpTriad2.KMPResults);
            Assert.Equal(41, kmpTriad2.KMPResults.Count);
        }

        [Fact]
        public async Task CanSearchForAllOccurrenceOfOneTriadWhenPrefixIsSameAsSuffix()
        {
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(LoremIpsum));
            using StreamReader reader = new StreamReader(stream);

            var kmpTriad = new KMPTriad("i", "i");

            await reader.SearchAsync(CancellationToken.None, kmpTriad);

            Assert.NotEmpty(kmpTriad.KMPResults);
            Assert.Equal(41, kmpTriad.KMPResults.Count);
        }
    }
}
