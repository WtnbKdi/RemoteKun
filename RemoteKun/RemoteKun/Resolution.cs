using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteKun
{
    internal class Resolution
    {
        // 画面解像度 デフォルト1920x1080
        public int X { get; } = 1920;
        public int Y { get; } = 1080;
        public Resolution() { }
        public Resolution(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}
