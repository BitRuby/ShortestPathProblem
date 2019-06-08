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
        private static int[,] solution;
        private static int counter = 0;
        #endregion

        #region SetupServer
        private static void SetupServer()
        {
            Console.WriteLine("Setting up server...");
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);
            generatedMatrix = matrix.GenerateMatrix(config.GetVertices());
            solution = matrix.Initialize(config.GetVertices());
            Console.WriteLine("Generated graph vertices: {0}", config.GetVertices());
            Console.WriteLine("Waiting for {0} clients to send matrix", config.GetClients());
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
            TempStorage temp2 = tempStorage.Find(x => x.Socket == current);
            if (temp2.ReceiveFlag && temp2.Socket == current)
            {
                try
                {
                    received = current.EndReceive(AR);
                    temp2.Offset += received;
                }
                catch (SocketException)
                {
                    Console.WriteLine("Client forcefully disconnected");
                    current.Close();
                    clientSockets.Remove(current);
                    return;
                }
                if (temp2.Offset == temp2.Length)
                {
                    temp2.ReceivedMessage = (Message)Message.Deserialize(new MemoryStream(buffer));
                    Console.WriteLine("Received matrix from client (IP: {0})", ((IPEndPoint)(current.RemoteEndPoint)).Address.ToString());
                    int L = (int)Math.Floor((double)Math.Pow(config.GetVertices() * 2, 0.5));
                    for (int i = 0; i <= L - 1; i++)
                    {
                        for (int j = temp2.ReceivedMessage.RangeFrom; j <= temp2.ReceivedMessage.RangeTo; j++)
                        {
                            solution[i, j] = temp2.ReceivedMessage.Mat[i, j];
                        }
                    }
                    Console.WriteLine("Received solution has been merged. ");
                    counter++;
                    if (counter == config.GetClients())
                    {
                        matrix.SaveMatrixToFile(generatedMatrix, "Matrix.txt");
                        matrix.SaveMatrixToFile(solution, "NewMatrix.txt");
                        Console.WriteLine("Generated matrix saved to file Matrix.txt and calculated matrix to NewMatrix.txt. Press Enter to Exit");
                        return;
                    }
                    temp2.ReceiveFlag = false;
                    current.BeginReceive(buffer, 0, SIZE, SocketFlags.None, ReceiveCallback, current);
                    return;
                }
                if (temp2.Offset + PACKET_SIZE > temp2.Length)
                {
                    current.BeginReceive(buffer, temp2.Offset, temp2.Length - temp2.Offset, SocketFlags.Partial, ReceiveCallback, current);
                }
                else
                {
                    current.BeginReceive(buffer, temp2.Offset, PACKET_SIZE, SocketFlags.Partial, ReceiveCallback, current);
                }
            }
            else
            {
                Message m = (Message)Message.Deserialize(new MemoryStream(buffer));
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
                        Send(response2, current, Message.Serialize(response2).Length);
                        Console.WriteLine("Message matrix as chunks has been send to client (IP: {0}).", ((IPEndPoint)(current.RemoteEndPoint)).Address.ToString());
                        break;
                    case 3:
                        Console.WriteLine(m.Text.ToString());
                        Console.WriteLine("Received length of message from client (IP: {0})", ((IPEndPoint)(current.RemoteEndPoint)).Address.ToString());
                        TempStorage temp1 = tempStorage.Find(x => x.Socket == current);
                        temp1.Length = m.Length;
                        temp1.ReceiveFlag = true;
                        Message response3 = new Message(null, 4, "Request for matrix");
                        Send(response3, current);
                        break;
                }
                current.BeginReceive(buffer, 0, SIZE, SocketFlags.None, ReceiveCallback, current);
            }
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
