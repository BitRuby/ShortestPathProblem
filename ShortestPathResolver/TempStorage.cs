using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public Stopwatch Sw { get; set; } = new Stopwatch();
        public List<TimeSpan> TimeLog { get; set; } = new List<TimeSpan>();
        public int centralPoint;
        public TempStorage(Socket socket)
        {
            this.Socket = socket;
        }
    }
}
