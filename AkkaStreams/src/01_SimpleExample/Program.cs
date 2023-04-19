namespace SimpleExample
{
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Akka;
    using Akka.Actor;
    using Akka.Streams;
    using Akka.Streams.Dsl;

    class Program
    {
        static async Task Main(string[] args)
        {
            var actorSystem = ActorSystem.Create("StreamExample");

            var ints = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 19 };

            // all streams start with one or more sources
            Source<int, NotUsed> source = Source.From(ints);

            // create a Flow that accepts an int and produces an int
            // this Flow filters out any odd-numbered integers
            Flow<int, int, NotUsed> flow = Flow.Create<int>().Where(x => x % 2 == 0).Async();

            // create a Sink to write our integer output to the console
            Sink<int, Task<IImmutableList<int>>> sink = Sink.Seq<int>();

            // create an instance of the materializer from the ActorSystem
            // this will create the underlying actors as children of the /user root actor
            IMaterializer materializer = actorSystem.Materializer();

            // connect all of the stream stages together and return the materialized Task
            // created by out Sink<int, Task<IImmutableList<int>>> stage
            Task<IImmutableList<int>> allNumsTask = source.Via(flow).RunWith(sink, materializer);

            IImmutableList<int> allNums = await allNumsTask;

            foreach(var i in allNums)
            {
                System.Console.WriteLine($"{i}");
            }
        }
    }
}
