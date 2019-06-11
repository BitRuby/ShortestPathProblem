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
        private static int PACKET_SIZE;
        private static int[,] solution;
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

        private static void Send(Message m, int length)
        {
            int offset = 0;
            byte[] buffer = Message.Serialize(m);
            while (offset < length)
            {
                if (offset + PACKET_SIZE > length)
                {
                    offset += ClientSocket.Send(buffer, offset, length - offset, SocketFlags.Partial);
                }
                else
                {
                    offset += ClientSocket.Send(buffer, offset, PACKET_SIZE, SocketFlags.Partial);
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
                    tempStorage.rangeFrom = receive.RangeFrom;
                    tempStorage.rangeTo = receive.RangeTo;
                    tempStorage.ReceiveFlag = true;
                    PACKET_SIZE = receive.PacketSize;
                    Message m1 = new Message(null, 2, "Matrix request. ");
                    Send(m1);
                    Console.WriteLine("Matrix request has been send. ");
                    break;
                case 3:
                    Console.WriteLine(receive.Text.ToString());
                    tempStorage.Sw.Start();
                    solution = Matrix.Calculate(receive.Mat, tempStorage.rangeFrom, tempStorage.rangeTo);
                    tempStorage.Sw.Stop();
                    tempStorage.Length = Message.Serialize(new Message(solution, 3, "Response with matrix", tempStorage.rangeFrom, tempStorage.rangeTo)).Length;
                    Message m2 = new Message(null, 3, "Length message. ", 0, 0, tempStorage.Length);
                    m2.TimeLog.Add(tempStorage.Sw.Elapsed);
                    Send(m2);
                    tempStorage.ReceiveFlag = false;
                    Console.WriteLine("Message length has been send to server. ");
                    break;
                case 4:
                    Console.WriteLine(receive.Text.ToString());
                    Message m3 = new Message(solution, 4, "Response with matrix", tempStorage.rangeFrom, tempStorage.rangeTo);
                    Send(m3, tempStorage.Length);
                    Console.WriteLine("Calculated matrix has been send. ");
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
