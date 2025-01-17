using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleAppFramework;

namespace EcAuthConsoleApp.Commands
{
    [RegisterCommands]
    internal class SumCommand
    {
        /// <summary>Sum parameters.</summary>
        /// <param name="x">left value.</param>
        /// <param name="y">right value.</param>
        public void Sum(int x, int y)
        {
            Console.WriteLine(x + y);
        }
    }
}
