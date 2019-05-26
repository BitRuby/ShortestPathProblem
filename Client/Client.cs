using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Client
{

    public class Matrix
    {
        public int[,] Mat;
    }

    public class Message
    {
        public int type; // 0 - Ask for matrix, 1 - Response, 2 - Exit
        public String message;
        public Matrix matrix;
        public int length;
        public int from;
        public int to;
    }

    class Client
    {
        private static readonly Socket ClientSocket = new Socket
                   (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private const int PORT = 100;
        private static Boolean receivedData = false;
        private static int lengthData = 0;

        static void Main()
        {
            Console.Title = "Client";
            ConnectToServer();
            RequestLoop();
            Exit();
        }
        /// <summary>
        /// Function connecting to server Socket
        /// </summary>
        private static void ConnectToServer()
        {
            int attempts = 0;

            while (!ClientSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection attempt " + attempts);
                    // Change IPAddress.Loopback to a remote IP to connect to a remote host.
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


        /// <summary>
        /// This loop allowes to receive and send messages
        /// </summary>
        private static void RequestLoop()
        {
            SendRequest();
            while (true)
            {
                ReceiveResponse();
            }
        }

        /// <summary>
        /// Close socket and exit program.
        /// </summary>
        private static void Exit()
        {
            Message msg = new Message();
            msg.type = 2;
            string request = JsonConvert.SerializeObject(msg, Formatting.Indented);
            SendString(request);
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
            Environment.Exit(0);
        }


        /// <summary>
        /// Client user input
        /// </summary>
        private static void SendRequest()
        {
            Message msg = new Message();
            msg.type = 0;
            string request = JsonConvert.SerializeObject(msg, Formatting.Indented);
            SendString(request);
        }

        /// <summary>
        /// Sends a string to the server with ASCII encoding.
        /// </summary>
        private static void SendString(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        private static void ReceiveAsChunks(int length)
        {
            Console.WriteLine("Stared");
            int size = 8;
            var buffer = new byte[length+1000];
            int received = 0;
            Console.WriteLine("rozmiar length {0}", length);
            while (received <= length)
            {
                if(received + size > length)
                {
                    received += ClientSocket.Receive(buffer, received, length-received, SocketFlags.Partial);
                    Console.WriteLine("dupa z rozmiaru received {0}", received);
                    break; // to do wyjebania 
                }
                else
                {
                    received += ClientSocket.Receive(buffer, received, size, SocketFlags.Partial);
                }
               
               // Console.WriteLine("Data received: {0}", received);

            }
            Console.WriteLine("cwel");
            if (received == 0) return;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = Encoding.ASCII.GetString(data);
            Message msg = new Message();
            msg = JsonConvert.DeserializeObject<Message>(text, new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
            Console.WriteLine(msg);
            DoSth(msg);
        }

        /// <summary>
        /// Function saves received data to buffer and deserialize it then passes to process function
        /// </summary>
        private static void ReceiveResponse()
        {
            if (receivedData == true)
            {
                ReceiveAsChunks(lengthData);
            }
            else
            {
                long size = 102400000;
                var buffer = new byte[size];
                int received = ClientSocket.Receive(buffer, SocketFlags.None);
                if (received == 0) return;
                var data = new byte[received];
                Array.Copy(buffer, data, received);
                string text = Encoding.ASCII.GetString(data);
                Console.WriteLine("Data received: {0}", text);
                Message msg = new Message();
                msg = JsonConvert.DeserializeObject<Message>(text, new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
                if (msg.type == 2)
                {
                    lengthData = msg.length;
                    receivedData = true;
                }
                DoSth(msg);
            }
        }
        /// <summary>
        /// Function process received information 
        /// </summary>
        private static void DoSth(Message msg)
        {
            switch(msg.type)
            {
                case 0:
                    Console.WriteLine(msg.message);
                    break;
                case 1:
                    Console.WriteLine("Range({0},{1})", msg.from, msg.to);
                    msg.matrix.Mat = Calculate(msg.matrix.Mat, msg.from, msg.to);
                    string request = JsonConvert.SerializeObject(msg, Formatting.Indented);
                    SendString(request);
                    Console.WriteLine("Calculated matrix has been send.");
                    Console.WriteLine("Nothing to do");
                    Exit();
                    break;
            }
        }


        // <summary>
        // Floyd-Warshall algorithm accepts matrix and calculating shortest path.
        // </summary>
        private static int[,] Calculate(int[,] dist, int start, int end)
        {
            Console.WriteLine("Calculating...");
            int V = end;
            int L = (int)Math.Floor((double)Math.Pow(dist.Length, 0.5));
            for (int k = 0; k <= L-1; ++k)
            {
                for (int i = 0; i <= L-1; ++i)
                {
                    for (int j = start; j <= end; ++j)
                    {
                        if (dist[i, j] > dist[i, k] + dist[k, j])
                            dist[i, j] = dist[i, k] + dist[k, j];
                    }
                }
            }
            return dist;
        }
    }
}
