namespace _03_Graph_Lifecycles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Akka;
    using Akka.Actor;
    using Akka.Streams;
    using Akka.Streams.Dsl;
    using Akka.Util;

    class Program
    {
        static async Task Main(string[] args)
        {
            ActorSystem actorSystem = ActorSystem.Create("StreamsExample");

            IMaterializer materializer = actorSystem.Materializer();

            // a source representing a range of integers
            Source<int, NotUsed> source1 = Source.From(Enumerable.Range(1, 10));

            // a source representing a single string value
            Source<string, NotUsed> source2 = Source.Repeat("a");

            // Let's combine these two sources such that we create 10 int / string tuples
            IAsyncEnumerable<(int i, string s)> merged1 = source1.Zip(source2).RunAsAsyncEnumerable(materializer);

            await foreach(var (i, s) in merged1)
            {
                Console.WriteLine($"{i}-->{s}");
            }

            // -----------------------------------------------------------------------------------------------------
            Console.WriteLine("----------------------------------------------------------");
            // create a source that will be materialized into an IActorRef
            Source<string, IActorRef> actorSource = Source.ActorRef<string>(1000, OverflowStrategy.DropHead);
            var (preMaterializedRef, standAloneSrc) = actorSource.PreMaterialize(materializer);

            // materialize the rest of the stream into an IAsyncEnumerable
            IAsyncEnumerable<string> strResponses = standAloneSrc.Via(Flow.Create<string>()
                .Select(x => x.ToLowerInvariant()))
                .RunAsAsyncEnumerable(materializer);

            // send some messages to our head actor to drive the stream
            preMaterializedRef.Tell("HIT1");
            preMaterializedRef.Tell("HIT2");
            preMaterializedRef.Tell("HIT3");
            //preMaterializedRef.GracefulStop(TimeSpan.FromSeconds(1));

            // need to timeout our IAsyncEnumerable otherwise it will run forever (by design)
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

            await foreach (var str in strResponses.WithCancellation(cts.Token))
            {
                Console.WriteLine(str);
            }

            // -----------------------------------------------------------------------------------------------------
            Console.WriteLine("----------------------------------------------------------");
            // create another source that will be materialized into an IActorRef
            Source<string, IActorRef> actorSource2 = Source.ActorRef<string>(1000, OverflowStrategy.DropTail);
            var (preMaterializedRef2, standAloneSrc2) = actorSource2.PreMaterialize(materializer);

            // going to use this as part of our KillSwitch
            cts = new CancellationTokenSource();

            // materialize the rest of the stream into an IAsyncEnumerable
            IAsyncEnumerable<string> strResponses2 = standAloneSrc2.Via(Flow.Create<string>()
                .Select(x => x.ToLowerInvariant()))
                .Via(cts.Token.AsFlow<string>(true))
                //.Via(KillSwitches.AsFlow<string>(cts.Token, cancelGracefully:true))
                .RunAsAsyncEnumerable(materializer);

            // send some messages to our head actor to drive the stream
            preMaterializedRef2.Tell("HIT1");
            preMaterializedRef2.Tell("HIT2");
            preMaterializedRef2.Tell("HIT3");
            preMaterializedRef2.Tell("HIT4");
            preMaterializedRef2.Tell("HIT5");

            var count = 0;
            await foreach (var str in strResponses2)
            {
                Console.WriteLine(str);
                if (++count == 3)
                {
                    cts.Cancel(); // shut down the stream
                }
            }

            Console.WriteLine("Stream completed! (cancelled)");

            // -----------------------------------------------------------------------------------------------------
            Console.WriteLine("----------------------------------------------------------");
            // create a source that will be materialized into an IActorRef
            Source<string, IActorRef> actorSource3 = Source.ActorRef<string>(1000, OverflowStrategy.DropTail);
            var (preMaterializedRef3, standAloneSrc3) = actorSource3.PreMaterialize(materializer);

            // materialize the rest of the stream into an IAsyncEnumerable
            IAsyncEnumerable<string> strResponses3 = standAloneSrc3.Via(Flow.Create<string>()
                .Select(x => x.ToLowerInvariant()))
                .RunAsAsyncEnumerable(materializer);

            // send some messages to our head actor to drive the stream
            preMaterializedRef3.Tell("HIT1");
            preMaterializedRef3.Tell("HIT2");
            preMaterializedRef3.Tell("HIT3");
            preMaterializedRef3.Tell(PoisonPill.Instance);

            await foreach (var str in strResponses3)
            {
                Console.WriteLine(str);
            }

            Console.WriteLine("Stream completed! (poison pill)");

            // -----------------------------------------------------------------------------------------------------
            Console.WriteLine("----------- Handling failed stages -----------");
            // create a source of integers - including one bad apple (zero)
            Source<int, NotUsed> numbers = Source.From(new[] { 9, 8, 7, 6, 0, 5, 4, 3, 2, 1 });

            //IAsyncEnumerable<string> integerDivision = numbers.Via(Flow.Create<int>()
            //    .Select(x => $"1/{x} is {1 / x} w/ integer division"))
            //    .RunAsAsyncEnumerable(materializer);
            //await foreach (var d in integerDivision)
            //{
            //    Console.WriteLine(d);
            //}

            IAsyncEnumerable<string> integerDivision2 = numbers.Via(Flow.Create<int>()
                .Select(x => $"1/{x} is {1 / x} w/ integer division"))
                .Recover(ex => {
                    if (ex is DivideByZeroException)
                    {
                        return new Option<string>("Whoops - attempted to divide by zero!");
                    }

                    // otherwise just return nothing - a gap will appear in the output
                    return Option<string>.None;
                })
                .RunAsAsyncEnumerable(materializer);
            await foreach (var d in integerDivision2)
            {
                Console.WriteLine(d);
            }
        }
    }
}
