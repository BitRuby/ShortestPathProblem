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
        private const int PACKET_SIZE = 512;
        private static byte[] buffer = new byte[SIZE];
        private static TempStorage tempStorage = new TempStorage(ClientSocket);
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

        private static void Send(Message m, Socket socket, int length)
        {
            int offset = 0;
            byte[] buffer = Message.Serialize(m);
            while (offset < length)
            {
                if (offset + PACKET_SIZE > length)
                {
                    offset += socket.Send(buffer, offset, length - offset, SocketFlags.Partial);
                }
                else
                {
                    offset += socket.Send(buffer, offset, PACKET_SIZE, SocketFlags.Partial);
                }
            }
        }

        private static Message Receive()
        {
            int received = ClientSocket.Receive(buffer, SocketFlags.None);
            if (received == 0) return null;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            return (Message) Message.Deserialize(new MemoryStream(data));
        }

        private static Message Receive(int length)
        {
            int received = 0;
            //Console.WriteLine("R: {0}; L: {1}", received, length);
            while (received < length)
            {
                if (received + PACKET_SIZE > length)
                {
                    received += ClientSocket.Receive(buffer, received, length - received, SocketFlags.Partial);
                }
                else
                {
                    received += ClientSocket.Receive(buffer, received, PACKET_SIZE, SocketFlags.Partial);
                }
            }
            if (received == 0) return null;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            return (Message)Message.Deserialize(new MemoryStream(data));
        }

        private static void RequestLoop()
        {
            Message m = new Message(null, 1, "Helo message. ");
            Send(m);
            while (true)
            {
                ReceiveResponse();
            }
        }

        private static void ReceiveResponse()
        {
            Message receive;
            if (tempStorage.ReceiveFlag)
            {
                receive = Receive(tempStorage.Length);
            }
            else
            {
                receive = Receive();
            }
            if (receive == null) return;
            DoSth(receive);
        }

        private static void DoSth(Message receive)
        {
            switch (receive.Type)
            {
                case 1:
                    Console.WriteLine(receive.Text);
                    break;
                case 2:
                    Console.WriteLine(receive.Text.ToString());
                    tempStorage.Length = receive.Length;
                    tempStorage.ReceiveFlag = true;
                    Message m = new Message(null, 2, "Matrix request. ");
                    Send(m);
                    break;
                case 3:
                    Console.WriteLine(receive.Text.ToString());
                    tempStorage.ReceiveFlag = false;
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
