using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TrucXanhServer
{
    class SocketState
    {
        public Socket ClientSocket { get; set; }
        public byte[] Buffer { get; set; }

        public SocketState(Socket clientSocket)
        {
            ClientSocket = clientSocket;
        }
    }
}
