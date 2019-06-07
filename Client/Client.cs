using System;
using System.IO;
using System.Net;
using System.Net.Sockets;


namespace ShortestPathResolver
{
    class Client
    {
        #region Static Variables
        private static readonly Socket ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private const int PORT = 100;
        private const int SIZE = 512000000;
        private static byte[] buffer = new byte[SIZE];
        #endregion

        private static void ConnectToServer()
        {
            int attempts = 0;

            while (!ClientSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection attempt " + attempts);
                    IPAddress localIPAddress = IPAddress.Parse("127.0.0.1");
                    ClientSocket.Connect(new IPEndPoint(localIPAddress, PORT));
                }
                catch (SocketException)
                {
                    Console.Clear();
                }
            }
            Console.Clear();
            Console.WriteLine("Connected");
        }

        private static void Exit()
        {
            Message m = new Message(null, 2);
            Send(m);
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
            Environment.Exit(0);
        }

        private static void Send(Message m)
        {
            byte[] buffer = Message.Serialize(m);
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        private static Message Receive()
        {
            Message m = new Message();
            int received = ClientSocket.Receive(buffer, SocketFlags.None);
            if (received == 0) return null;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            return (Message) Message.Deserialize(new MemoryStream(data));
        }

        private static void RequestLoop()
        {
            Message m = new Message(null, 1);
            Send(m);
            while (true)
            {
                ReceiveResponse();
            }
        }

        private static void ReceiveResponse()
        {
            Message receive = Receive();
            if (receive == null) return;
            else DoSth(receive);
        }

        private static void DoSth(Message receive)
        {
            switch (receive.Type)
            {
                case 1:
                    Console.WriteLine(receive.Text);
                    break;
                case 2:
                    Console.WriteLine("Matrix has been received");
                    break;
                case 3:
                    break;
            }
        }

        #region Main
        static void Main()
        {
            Console.Title = "Client";
            ConnectToServer();
            RequestLoop();
            Exit();
        } 
        #endregion
    }
}
