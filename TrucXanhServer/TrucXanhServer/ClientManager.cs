using System.Net.Sockets;

namespace TrucXanhServer
{
    class ClientManager
    {
        public Socket clientSocket { get; set; }
        public string name { get; set; }
        public int points { get; set; }
        public int order { get; set; }

        public ClientManager(Socket socket, string name, int points, int order)
        {
            this.name = name;
            this.clientSocket = socket;
            this.points = points;
            this.order = order;
        }
    }
}
