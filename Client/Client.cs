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
        public int[] range;
    }

    class Client
    {
        private static readonly Socket ClientSocket = new Socket
                   (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private const int PORT = 100;

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

        /// <summary>
        /// Function saves received data to buffer and deserialize it then passes to process function
        /// </summary>
        private static void ReceiveResponse()
        {
            var buffer = new byte[400000];
            int received = ClientSocket.Receive(buffer, SocketFlags.None);
            if (received == 0) return;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = Encoding.ASCII.GetString(data);  
            Message msg = new Message();
            msg = JsonConvert.DeserializeObject<Message>(text);
            DoSth(msg);
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
                    msg.matrix.Mat = Calculate(msg.matrix.Mat, msg.range[0], msg.range[1]);
                    string request = JsonConvert.SerializeObject(msg, Formatting.Indented);
                    SendString(request);
                    Console.WriteLine("Calculated array has been send.");
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
            Console.WriteLine("Range({0},{1})", start, end);
            int V = end;
            for (int k = start; k < end; k++)
            {
                for (int i = start; i < end; i++)
                {
                    for (int j = start; j < end; j++)
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
