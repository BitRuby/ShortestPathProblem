﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Xml.Linq;

namespace Server
{

    public class Matrix
    {
        public int[,] Mat;
    }
    public class ConfigJsonItems
    {
        public int Clients;
        public int Vertices;
    }
    public class Message
    {
        public int type; // 0 - Error not enough clients connected, 1 - Send matrix 
        public String message;
        public int length;
        public Matrix matrix;
        public int from;
        public int to;
    }

    public class Config
    {
        ConfigJsonItems items = new ConfigJsonItems();
        public Config()
        {
            using (StreamReader r = new StreamReader("config.json"))
            {
                string json = r.ReadToEnd();
                ConfigJsonItems items = JsonConvert.DeserializeObject<ConfigJsonItems>(json);
                this.items = items;
            }

        }
        public int getVertices()
        {
            return this.items.Vertices;
        }
        public int getClients()
        {
            return this.items.Clients;
        }
    }


    class Server
    {
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 102400000;
        private const int PORT = 100;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];

        private static Matrix m = new Matrix();
        private static Matrix mNew = new Matrix();
        private static int responses = 0;

        /// <summary>
        /// Set the server settings that declared as object fields
        /// </summary>
        private static void SetupServer()
        {
            Console.WriteLine("Setting up server...");
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);
            Config c = new Config();
            Server s = new Server();
            m.Mat = s.GenerateMatrix(c.getVertices());
            mNew.Mat = new int[c.getVertices(), c.getVertices()];
            Console.WriteLine("Generated graph vertices: {0}", c.getVertices());
            Console.WriteLine("Waiting for {0} clients to send matrix", c.getClients());
            Console.WriteLine("Server setup complete");
        }

        /// <summary>
        /// Close all connected client (we do not need to shutdown the server socket as its connections
        /// are already closed with the clients).
        /// </summary>
        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            { 
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            serverSocket.Close();
        }
        /// <summary>
        /// Accept connection
        /// </summary>
        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException) // I cannot seem to avoid this (on exit when properly closing sockets)
            {
                return;
            }

            clientSockets.Add(socket);
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Client connected (IP: {0})", ((IPEndPoint)(socket.RemoteEndPoint)).Address.ToString());
            Config c = new Config();
            Console.WriteLine(c.getClients() - clientSockets.Count + " clients left to start calculate");
            serverSocket.BeginAccept(AcceptCallback, null);
        }
        /// <summary>
        ///  Send data as chunks
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="buffer"></param>
        private static void SendChunks(Socket socket, int length, byte[] buffer)
        {
            int size = 8;
            int offset = 0;
            // int len = buffer.Length;
            Console.WriteLine("rozmiar length {0}", length);
            Console.WriteLine("rozmiar danych {0}", buffer.Length);
            while (offset < length)
            {
                if(offset + size > length)
                {
                    offset += socket.Send(buffer, offset, length - offset, SocketFlags.Partial);
                    Console.WriteLine("Dupa z rozmiaru offset {0}", offset);
                }
                else
                {
                    offset += socket.Send(buffer, offset, size, SocketFlags.Partial);
                }  
                //Console.WriteLine("Data sed: {0}", offset);
            }
            Console.WriteLine("Data send to client {0}.", ((IPEndPoint)(socket.RemoteEndPoint)).Address.ToString());
        }
        /// <summary>
        /// Function sends response to client
        /// </summary>
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
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                current.Close();
                clientSockets.Remove(current);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);
            Message msg = new Message();
            msg.matrix = new Matrix();
            msg = JsonConvert.DeserializeObject<Message>(text);
            switch (msg.type)
            {
                case 0: // First request from client

                    Server s = new Server();
                    Config c = new Config();
                    Message me = new Message();
                    if (c.getClients() > clientSockets.Count)
                    {
                        me.type = 0;
                        me.message = "Not enough clients connected to server.";
                        string jsonmess = JsonConvert.SerializeObject(me, Formatting.Indented);
                        byte[] data = Encoding.ASCII.GetBytes(jsonmess);
                        current.Send(data);

                    }
                    else
                    {
                        me.type = 1;
                        me.matrix = m;
                        string json = JsonConvert.SerializeObject(me, Formatting.Indented);
                        byte[] data = Encoding.ASCII.GetBytes(json);
                        me.message = "Server sending length of matrix to clients";
                        me.length = data.Length;

                        me.matrix = null;
                        me.type = 2;
                        int j = 0;
                        foreach (Socket value in clientSockets)
                        {
                            me.from = s.calculateRanges(c.getClients(), c.getVertices(), j)[0];
                            me.to = s.calculateRanges(c.getClients(), c.getVertices(), j)[1];
                            json = JsonConvert.SerializeObject(me, Formatting.Indented);
                            data = Encoding.ASCII.GetBytes(json);
                            value.Send(data);
                            Console.WriteLine("Information send to client {0}.", ((IPEndPoint)(value.RemoteEndPoint)).Address.ToString());
                            j++;
                        }
                        Message msa = new Message();
                        msa.type = 1;
                        msa.matrix = m;
                        string jsonz = JsonConvert.SerializeObject(msa, Formatting.Indented);
                        byte[] dataz = Encoding.ASCII.GetBytes(jsonz);
                        j = 0;
                        Console.WriteLine("Serialized object size: {0} BYTE", me.length);
                        foreach (Socket value in clientSockets.ToList())
                        {
                            SendChunks(value, me.length, dataz);
                            j++;
                        }
                        Console.WriteLine("Generated Matrix Send to Clients");
                    }
                    break;
                case 1: // Response with calulcated array
                    Console.WriteLine("Received array from client (IP: {0})", ((IPEndPoint)(current.RemoteEndPoint)).Address.ToString());
                    int L = (int)Math.Floor((double)Math.Pow(msg.matrix.Mat.Length, 0.5));

                    for (int i = 0; i <=L-1; i++)
                    {
                        for (int j = msg.from; j <= msg.to; j++)
                        {
                            mNew.Mat[i,j] = msg.matrix.Mat[i,j];
                        }
                    }
                    responses++;
                    Config sc = new Config();
                    if (responses == sc.getClients())
                    {
                        Console.WriteLine("Merging all solutions");
                        Server sxaz = new Server();
                        sxaz.SaveMatrixToFile(m.Mat, "Matrix.txt");
                        sxaz.SaveMatrixToFile(mNew.Mat, "NewMatrix.txt");
                        Console.WriteLine("Generated matrix saved to file Matrix.txt and calculated matrix to NewMatrix.txt. Press Enter to Exit");
                    }
                    break;
                case 2: // Exit request
                    Console.WriteLine("Client disconnected (IP: {0})", ((IPEndPoint)(current.RemoteEndPoint)).Address.ToString());
                    current.Shutdown(SocketShutdown.Both);
                    current.Close();
                    clientSockets.Remove(current);
                    return;
            }
            current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
        }

        private int[] calculateRanges(int clients, int vertices, int n)
        {
            int[] ar = new int[2];
            ar[0] = n * (vertices / clients);
            ar[1] = (ar[0] + (vertices / clients)) - 1;
            if (n + 1 == clients) ar[1] += vertices % clients;
            return ar;
        }



        /// <summary>
        /// Generates graph matrix 
        /// </summary>
        private int[,] GenerateMatrix(int nodes)
        {
            int[,] m = new int[nodes, nodes];
            Random rnd = new Random();
            int tempValue;
            int connectivity = 0;
            int cst = 99999;
            for (int i = 0; i < nodes; i++)
            {
                for (int j = 0; j < nodes; j++)
                {
                    if (i > j) continue;
                    if (i == j)
                    {
                        m[i, j] = 0;
                    }
                    else
                    {
                        if (connectivity == 0 || rnd.Next(0, 3) != 0)
                        {
                            connectivity = 1;
                            tempValue = rnd.Next(1, 50);
                            m[i, j] = tempValue;
                            m[j, i] = tempValue;
                        }
                        else
                        {
                            m[i, j] = cst;
                            m[j, i] = cst;
                        }
                    }
                }
                connectivity = 0;
            }
            return m;
        }
        /// <summary>
        /// Saves calculated matrix
        /// </summary>
        void SaveMatrixToFile(int[,] matrix, string name)
        {
            using (StreamWriter file = new StreamWriter(@name, false))
            {
                for (int j = 0; j < matrix.GetLength(0); j++)
                {
                    for (int i = 0; i < matrix.GetLength(1); i++)
                    {
                        file.Write("{0}\t", matrix[i, j]);
                    }
                    file.Write("\n");
                }
            }
        }

        static void Main(string[] args)
        {

            Console.Title = "Server";
            SetupServer();
            Console.ReadLine(); // When we press enter close everything
            CloseAllSockets();
        }
    }
}
