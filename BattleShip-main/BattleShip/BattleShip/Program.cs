using BattleShip.Models;
using System.Text;

namespace BattleShip
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("Est-ce que tu veux être le client (oui)? ");
            switch (Console.ReadLine().ToLower())
            {
                case "oui":
                    SynchronousSocketClient.StartClient();
                    break;
                default:
                    SynchronousSocketListener.StartListening();
                    break;
            }
        }
    }
}
