using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace ShortestPathResolver
{
    class Server
    {
        #region Static Variables
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int PORT = 100;
        private const int SIZE = 512000000;
        private static Config config = new Config();
        private static Matrix matrix = new Matrix();
        private static byte[] buffer = new byte[SIZE];
        private static int[,] generatedMatrix; 
        #endregion

        #region SetupServer
        private static void SetupServer()
        {
            Console.WriteLine("Setting up server...");
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);
            generatedMatrix = matrix.GenerateMatrix(config.GetVertices());
            Console.WriteLine("Generated graph vertices: {0}", config.GetVertices());
            Console.WriteLine("Waiting for {0} clients to send matrix", config.GetVertices());
            Console.WriteLine("Server setup complete");
        }
        #endregion

        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();

            }
            serverSocket.Close();
        }

        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;
            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            clientSockets.Add(socket);
            socket.BeginReceive(buffer, 0, SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Client connected (IP: {0})", ((IPEndPoint)(socket.RemoteEndPoint)).Address.ToString());
            serverSocket.BeginAccept(AcceptCallback, null);
        }
        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;

            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                current.Close();
                clientSockets.Remove(current);
                return;
            }
            Message m = (Message) Message.Deserialize(new MemoryStream(buffer));

            switch(m.Type)
            {
                case 1:
                    if (config.GetClients() > clientSockets.Count)
                    {
                        Message response = new Message(null, 1, "Not enough clients connected to server. ");
                        Send(response, current);
                    }
                    else
                    {
                        int j = 0;
                        Message response = new Message(generatedMatrix, 2, "Generated matrix");
                        foreach (Socket value in clientSockets)
                        {
                            response.RangeFrom = matrix.CalculateRanges(config.GetClients(), config.GetVertices(), j)[0];
                            response.RangeTo = matrix.CalculateRanges(config.GetClients(), config.GetVertices(), j)[1];
                            Send(response, value);
                            j++;
                        }
                        Console.WriteLine("Generated matrix has been send to cliens. ");
                    }
                    break;
                //case 2: 
                //    Console.WriteLine("Received array from client (IP: {0})", ((IPEndPoint)(current.RemoteEndPoint)).Address.ToString());
                //    int L = (int)Math.Floor((double)Math.Pow(msg.matrix.Mat.Length, 0.5));

                //    for (int i = 0; i <= L - 1; i++)
                //    {
                //        for (int j = msg.from; j <= msg.to; j++)
                //        {
                //            mNew.Mat[i, j] = msg.matrix.Mat[i, j];
                //        }
                //    }
                //    responses++;
                //    Config sc = new Config();
                //    if (responses == sc.getClients())
                //    {
                //        Console.WriteLine("Merging all solutions");
                //        Server sxaz = new Server();
                //        sxaz.SaveMatrixToFile(m.Mat, "Matrix.txt");
                //        sxaz.SaveMatrixToFile(mNew.Mat, "NewMatrix.txt");
                //        Console.WriteLine("Generated matrix saved to file Matrix.txt and calculated matrix to NewMatrix.txt. Press Enter to Exit");
                //    }
                //    break;
                //case 3: 
                //    Console.WriteLine("Client disconnected (IP: {0})", ((IPEndPoint)(current.RemoteEndPoint)).Address.ToString());
                //    current.Shutdown(SocketShutdown.Both);
                //    current.Close();
                //    clientSockets.Remove(current);
                //    return;
            }

            current.BeginReceive(buffer, 0, SIZE, SocketFlags.None, ReceiveCallback, current);
        }

        private static void Send(Message m, Socket socket)
        {
            byte[] buffer = Message.Serialize(m);
            socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        #region Main
        static void Main(string[] args)
        {
            Console.Title = "Server";
            SetupServer();
            Console.ReadLine();
            CloseAllSockets();
        } 
        #endregion
    }
}
