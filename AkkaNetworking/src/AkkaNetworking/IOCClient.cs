namespace AkkaNetworking
{
    using System;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Akka.Actor;
    using Akka.IO;

    public class IOCClient : UntypedActor
    {
        public IOCClient(string host, int port)
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(host), port);//new DnsEndPoint(host, port);
            Context.System.Tcp().Tell(new Tcp.Connect(endpoint));
        }

        protected override void OnReceive(object message)
        {
            if (message is Tcp.Connected)
            {
                var connected = message as Tcp.Connected;
                Console.WriteLine("Connected to {0}", connected.RemoteAddress);

                // Register self as connection handler
                Sender.Tell(new Tcp.Register(Self));
                Become(Connected(Sender));
            }
            else if (message is Tcp.CommandFailed)
            {
                Console.WriteLine("Connection failed");
            }
            else
            {
                Unhandled(message);
            }
        }

        private UntypedReceive Connected(IActorRef connection)
        {
            return message =>
            {
                if (message is Tcp.Received)  // data received from network
                {
                    var received = message as Tcp.Received;
                    var receivedString = Encoding.ASCII.GetString(received.Data.ToArray()).Trim();
                    Console.WriteLine(receivedString);
                }
                else if (message is string)   // data received from console
                {
                    connection.Tell(Tcp.Write.Create(ByteString.FromString((string)message + "\n")));
                }
                else if (message is CloseClient)
                {
                    Console.WriteLine("closing client...");
                    connection.Tell(Tcp.Close.Instance);
                }
                else if (message is Tcp.ConnectionClosed)
                {
                    Context.Stop(Self);
                    Console.WriteLine("Connection closed");
                }
                else
                {
                    Unhandled(message);
                }
            };
        }

        public class CloseClient { }
    }
}
