using BattleShip.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BattleShip
{
    public class SynchronousSocketClient
    {
        private static BattleShipModel BattleShip = new BattleShipModel();

        private static Socket sender;

        private static byte[] bytes = new byte[32];

        public static void StartClient()
        {
            while (true)
            {
                BattleShip.NouveauJeux();
                try
                {
                    //client connecte
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(DemandeIp());
                    IPAddress ipAddress = ipHostInfo.AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, 443);
                    sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    sender.Connect(remoteEP);


                    do
                    {
                        //Client recoit parametres et send ok
                        Jeux.Message("En attente des paramètres...");
                        BattleShip.message = ReceiveMessage();
                        BattleShip.AnalyseRequete();

                        SendMessage(BattleShip.message);

                        //Client recoit ok, place bateaux et send ok
                        Jeux.Message("En attente du placement des bateaux de l'adversaire...");
                        BattleShip.message = ReceiveMessage();
                        if (!BattleShip.AnalyseRequete())
                            Console.WriteLine("pas recu le ok donc pas cool");

                        BattleShip.casesBateaux = Jeux.PlacerBateaux(BattleShipModel.tailleBateaux);

                        BattleShip.message.SetMessage('O');
                        SendMessage(BattleShip.message);

                        //Executions des attaques jusqu'a la victoire ou la defaite
                        Jeux.Message("En attente de l'attaque de l'adversaire...");

                        // Le serveur commence toujours
                        bool tourDuClient = false;

                        while (true)
                        {
                            if (!tourDuClient)
                            {
                                // ===== TOUR DU SERVEUR =====
                                do
                                {
                                    //Client analyse attaque du serveur et send resultat
                                    BattleShip.message = ReceiveMessage();
                                    if (!BattleShip.AnalyseRequete())
                                    {
                                        SendMessage(BattleShip.message);
                                        goto EndGame; // Sortir de la boucle principale
                                    }

                                    SendMessage(BattleShip.message);

                                    // Si le serveur a touché (rejouerTour = true), il garde la main
                                    // Si le serveur a raté (rejouerTour = false), c'est au tour du client
                                } while (BattleShip.rejouerTour);

                                // Le serveur a raté, c'est maintenant au tour du client
                                tourDuClient = true;
                            }
                            else
                            {
                                // ===== TOUR DU CLIENT =====
                                do
                                {
                                    //Client recoit ok et send attaque
                                    BattleShip.message = ReceiveMessage();
                                    if (!BattleShip.AnalyseRequete())
                                    {
                                        SendMessage(BattleShip.message);
                                        goto EndGame; // Sortir de la boucle principale
                                    }

                                    BattleShip.message.SetMessageAttaque('A', Jeux.SelectCase(BattleShip.plateau));
                                    SendMessage(BattleShip.message);

                                    //Client recoit resultat et send ok
                                    BattleShip.message = ReceiveMessage();
                                    if (!BattleShip.AnalyseRequete())
                                    {
                                        SendMessage(BattleShip.message);
                                        goto EndGame; // Sortir de la boucle principale
                                    }

                                    SendMessage(BattleShip.message);

                                    // Si le client a touché (rejouerTour = true), il garde la main
                                    // Si le client a raté (rejouerTour = false), c'est au tour du serveur
                                } while (BattleShip.rejouerTour);

                                // Le client a raté, c'est maintenant au tour du serveur
                                tourDuClient = false;
                            }
                        }

                    EndGame:
                        // Point de sortie pour les fins de partie
                        break;

                    } while (BattleShip.rejouer);
                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }
        }

        private static void SendMessage(Message message)
        {
            byte[] data = Encoding.ASCII.GetBytes(SerialisationModel.Serialiser(message) + "\n");

            int offset = 0;
            while (offset < data.Length)
            {
                int chunkSize = Math.Min(32, data.Length - offset);
                sender.Send(data, offset, chunkSize, SocketFlags.None);
                offset += chunkSize;
            }
        }

        private static Message ReceiveMessage()
        {
            StringBuilder receivedDataBuilder = new StringBuilder();
            int bytesRec;

            while (true)
            {
                bytesRec = sender.Receive(bytes);
                string chunk = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                receivedDataBuilder.Append(chunk);

                if (chunk.Contains("\n"))
                    break;
            }

            string json = receivedDataBuilder.ToString().TrimEnd('\n'); // Enlève le \n
            return SerialisationModel.Deserialiser(json);
        }

        private static string DemandeIp()
        {
            Console.Clear();
            Console.WriteLine("Quelle est l'IP de ton host? ");
            return Console.ReadLine();
        }
    }
}