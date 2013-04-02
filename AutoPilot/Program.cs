using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EveCom;
using System.Threading;
using EveComFramework;

namespace AutoPilot
{
    class Program
    {
        static Move move;

        static void Main(string[] args)
        {
            using (new EVEFrameLock())
            {
            }
            move = new Move();
            move.AutoPilot();
            while (move.Busy)
            {
                Thread.Sleep(100);
            }
        }
    }
}
