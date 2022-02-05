using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteKun
{
    internal class Command
    {
        public Command() { }
        public Command(string type) { Type = type; }
        public string Type { get; private set; }
    }
}
