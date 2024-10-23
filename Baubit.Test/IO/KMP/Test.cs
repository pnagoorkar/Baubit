using Baubit.IO;
using System.Reflection;

namespace Baubit.Test.IO.KMP
{
    public class Test
    {
        private static string loremIpsumTextResourceName = "Baubit.Test.IO.KMP.loremIpsum.txt";

        [Fact]
        public async Task CanSearchForOneOccurrenceOfOneTriad()
        {
            using StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(loremIpsumTextResourceName)!);

            var kmpTriad = new KMPTriad("eiusmod tempor ", " ut labore", 1);

            await reader.SearchAsync(CancellationToken.None, kmpTriad);

            Assert.NotEmpty(kmpTriad.KMPResults);
            Assert.Equal("incididunt", kmpTriad.KMPResults.First().Value);
        }

        [Fact]
        public async Task CanSearchForAllOccurrenceOfOneTriad()
        {
            using StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(loremIpsumTextResourceName)!);

            var kmpTriad = new KMPTriad("o", "i");

            await reader.SearchAsync(CancellationToken.None, kmpTriad);

            Assert.NotEmpty(kmpTriad.KMPResults);
            Assert.Equal(16, kmpTriad.KMPResults.Count);
        }

        [Fact]
        public async Task CanSearchForAllOccurrenceOfTwoTriads()
        {
            using StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(loremIpsumTextResourceName)!);

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
            using StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(loremIpsumTextResourceName)!);

            var kmpTriad = new KMPTriad("i", "i");

            await reader.SearchAsync(CancellationToken.None, kmpTriad);

            Assert.NotEmpty(kmpTriad.KMPResults);
            Assert.Equal(41, kmpTriad.KMPResults.Count);
        }
    }
}
