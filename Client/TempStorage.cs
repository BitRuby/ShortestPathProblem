using System;
using System.Net.Sockets;

namespace ShortestPathResolver
{
    class TempStorage
    {
        public int Offset { get; set; } = 0;
        public int Length { get; set; } = 0;
        public Boolean ReceiveFlag { get; set; } = false;
        public Message ReceivedMessage { get; set; } = null;
        public Socket Socket { get; set; } = null;
        public int rangeFrom { get; set; } = 0;
        public int rangeTo { get; set; } = 0;
        public TempStorage(Socket socket)
        {
            this.Socket = socket;
        }
    }
}
