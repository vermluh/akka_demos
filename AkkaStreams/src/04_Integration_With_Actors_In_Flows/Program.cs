namespace _04_Integration_With_Actors_In_Flows
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

            IActorRef myActorRef = actorSystem.ActorOf(Props.Create(() => new MyActor()), "myactor");

            // create 10,000 integers
            Source<int, NotUsed> source = Source.From(Enumerable.Range(0, 100));

            // allow up to 10 parallel asks of our actor
            var askFlow = Flow.Create<int>().SelectAsyncUnordered(10, async i => await myActorRef.Ask<(int value, int mod)>(i, TimeSpan.FromSeconds(1)));

            // create a flow that does the following:
            // int --> is even number? --> keep only even numbers --> count total even numbers
            IAsyncEnumerable<int> graph = source.Via(askFlow).Where(d => d.mod == 0).Select(x => x.value).RunAsAsyncEnumerable(materializer);

            await foreach (var i in graph)
            {
                Console.WriteLine(i);
            }
        }
    }

    // create a simple actor that performs some modulus
    public class MyActor : ReceiveActor
    {
        public MyActor()
        {
            // returns a (i, i mod 2) ValueTuple
            Receive<int>(i => Sender.Tell((i, i % 2)));
        }
    }
}
