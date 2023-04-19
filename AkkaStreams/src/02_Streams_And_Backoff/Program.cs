namespace _02_Streams_And_Backoff
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Akka;
    using Akka.Actor;
    using Akka.Streams;
    using Akka.Streams.Dsl;

    class Program
    {
        static async Task Main(string[] args)
        {
            ActorSystem actorSystem = ActorSystem.Create("StreamsExcample2");
            IMaterializer materializer = actorSystem.Materializer();

            // fast upstream producer - instantly produces 10000 requests
            Source<int, NotUsed> source = Source.From(Enumerable.Range(0, 10000));

            // group into batches of 100
            Source<IEnumerable<int>, NotUsed> batchingSource = source.Via(Flow.Create<int>().Grouped(100));

            var start = DateTime.UtcNow;

            // simulate a slower downstream consumer - can only process 10 events per second
            var slowSink = batchingSource.Via(Flow.Create<IEnumerable<int>>().Delay(TimeSpan.FromMilliseconds(1000), DelayOverflowStrategy.Backpressure)
                .Select(x => (x.Sum(), DateTime.UtcNow - start))); // sum each group
            
            var output = slowSink.RunAsAsyncEnumerable(materializer);

            await foreach (var i in output)
            {
                Console.WriteLine($"{i}");
            }
        }
    }
}
