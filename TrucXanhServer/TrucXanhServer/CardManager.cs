using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucXanhServer
{
    class CardManager
    {
        public string urlImg { get; set; }
        public string data { get; set; }
        public bool open { get; set; }
        public int slot { get; set; }

        public CardManager(string url, string data, bool open, int slot)
        {
            this.urlImg = url;
            this.data = data;
            this.open = open;
            this.slot = slot;
        }
    }
}
