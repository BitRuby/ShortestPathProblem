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
        private const int PACKET_SIZE = 512;
        private static Config config = new Config();
        private static Matrix matrix = new Matrix();
        private static List<TempStorage> tempStorage = new List<TempStorage>();
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
            tempStorage.Add(new TempStorage(socket));
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
            switch (m.Type)
            {
                case 1:
                    Console.WriteLine(m.Text.ToString());
                    if (config.GetClients() > clientSockets.Count)
                    {
                        Message response1 = new Message(null, 1, "Not enough clients connected to server. ");
                        Send(response1, current);
                    }
                    else
                    {
                        int j = 0;
                        Message response1 = new Message(null, 2, "Length message and ranges to calculate. ", 0, 0,
                            Message.Serialize(new Message(generatedMatrix, 3, "Generated matrix")).Length);
                        foreach (Socket value in clientSockets)
                        {
                            response1.RangeFrom = matrix.CalculateRanges(config.GetClients(), config.GetVertices(), j)[0];
                            response1.RangeTo = matrix.CalculateRanges(config.GetClients(), config.GetVertices(), j)[1];
                            Send(response1, value);
                            j++;
                        }
                        Console.WriteLine("Message length and ranges has been send to clients. ");
                    }
                    break;
                case 2:
                    Console.WriteLine(m.Text.ToString());
                    Message response2 = new Message(generatedMatrix, 3, "Generated matrix");
                    Send(response2, current, Message.Serialize(new Message(generatedMatrix, 3, "Generated matrix")).Length);
                    Console.WriteLine("Message matrix as chunks has been send to client (IP: {0}).", ((IPEndPoint)(current.RemoteEndPoint)).Address.ToString());
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
            TempStorage temp = tempStorage.Find(x => x.Socket == current);
            if (temp.ReceiveFlag && temp.Socket == current)
            {
                if (temp.Offset == temp.Length)
                {
                    temp.ReceivedMessage = (Message)Message.Deserialize(new MemoryStream(buffer));
                    temp.ReceiveFlag = false;
                    current.BeginReceive(buffer, 0, SIZE, SocketFlags.None, ReceiveCallback, current);
                    return;
                }
                if (temp.Offset + PACKET_SIZE > temp.Length)
                {
                    current.BeginReceive(buffer, temp.Offset, temp.Length - temp.Offset, SocketFlags.Partial, ReceiveCallback, current);
                }
                else
                {
                    current.BeginReceive(buffer, temp.Offset, PACKET_SIZE, SocketFlags.Partial, ReceiveCallback, current);
                }
            }
            else current.BeginReceive(buffer, 0, SIZE, SocketFlags.None, ReceiveCallback, current);
        }

        private static void Send(Message m, Socket socket)
        {
            byte[] buffer = Message.Serialize(m);
            socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
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
