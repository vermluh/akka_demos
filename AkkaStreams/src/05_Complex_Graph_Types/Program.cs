namespace _05_Complex_Graph_Types
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
            ActorSystem actorSystem = ActorSystem.Create("StreamsExample");

            IMaterializer materializer = actorSystem.Materializer();
            //--------------------------------------------------------------------
            // ****Fan-In Stages****
            // a source representing a range of integers
            Source<int, NotUsed> source1 = Source.From(Enumerable.Range(1, 10));
            // a source representing a single string value
            Source<string, NotUsed> source2 = Source.Single("a");
            // combine these two sources such that we create 10 int / string tuples
            IAsyncEnumerable<(int i, string s)> merged1 = source1.Zip(source2).RunAsAsyncEnumerable(materializer);
            await foreach(var (i, s) in merged1)
            {
                Console.WriteLine($"{i}-->{s}");
            }

            // REPEAT this value each time we're pulled
            Source<string, NotUsed> source3 = Source.Repeat("a");
            // let's combine these two sources such that we create 10 int / string tuples
            IAsyncEnumerable<(int i, string s)> merged2 = source1.Zip(source3).RunAsAsyncEnumerable(materializer);
            await foreach (var (i, s) in merged2)
            {
                Console.WriteLine($"{i}-->{s}");
            }

            //--------------------------------------------------------------------
            // ****Fan-Out Stages****

        }
    }
}
