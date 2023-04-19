using System.Linq;

namespace AkkaNetworking
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using Akka.Actor;

    class Program
    {
        static async Task Main(string[] args)
        {
            using (var system = ActorSystem.Create("echo-server-system"))
            {
                //var port = 9001;
                //var actor = system.ActorOf(Props.Create(() => new EchoService(new IPEndPoint(IPAddress.Any, port))), "echo-service");

                ///**
                // *  Now you should be able to connect with current TCP actor using i.e. telnet command:
                // *  $> telnet 127.0.0.1 9001
                // */

                //Console.WriteLine("TCP server is listening on *:{0}", port);
                //Console.WriteLine("ENTER to exit...");
                //Console.ReadLine();

                //// Close connection to avoid error message in console
                //await actor.Ask(new EchoService.StopServer());

                // 1
                //var actor = system.ActorOf(Props.Create(() => new TelnetClient("192.168.60.2", 50000)), "ioc-client");
                //Console.ReadLine();

                // 2
                //var iocClient = system.ActorOf(Props.Create(() => new IOCClient("192.168.60.2", 50000)), "ioc-client");
                //var iocClient = system.ActorOf(Props.Create(() => new IOCClient("127.0.0.1", 8888)), "ioc-client");
                var iocClient = system.ActorOf(Props.Create(() => new IOCClient("192.168.60.1", 50000)), "ioc-client");
                while (true)
                {
                    var input = Console.ReadLine();
                    if (input.StartsWith("/"))
                    {
                        var cmd = input.ToLowerInvariant();
                        if (cmd == "/exit")
                        {
                            Console.WriteLine("exiting");
                            break;
                        }
                    }
                    else
                    {
                        iocClient.Tell(input);
                    }
                }

                Console.WriteLine("closing client");
                iocClient.Tell(new IOCClient.CloseClient());
                await Task.Delay(500);
                system.Terminate().Wait();
            }
        }
    }
}
