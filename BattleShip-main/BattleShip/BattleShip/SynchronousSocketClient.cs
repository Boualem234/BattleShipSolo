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
            // Demander l'IP et établir la connexion UNE SEULE FOIS
            try
            {
                // Connexion initiale
                IPHostEntry ipHostInfo = Dns.GetHostEntry(DemandeIp());
                IPAddress ipAddress = ipHostInfo.AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 443);
                sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sender.Connect(remoteEP);
                Console.WriteLine("Connexion établie avec le serveur.");

                // Boucle des parties - GARDER LA MÊME CONNEXION
                do
                {
                    if (!JouerUnePartie())
                    {
                        Console.WriteLine("Erreur pendant la partie, arrêt du client.");
                        break;
                    }

                    // BattleShip.rejouer est défini par la logique de fin de partie
                    if (BattleShip.rejouer)
                    {
                        Console.WriteLine("Nouvelle partie va commencer...");
                    }

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
            finally
            {
                // Fermer la connexion seulement à la toute fin
                CloseConnection();
            }
        }

        private static bool JouerUnePartie()
        {
            try
            {
                BattleShip.NouveauJeux();

                //Client recoit parametres et send ok
                Jeux.Message("En attente des paramètres...");
                BattleShip.message = ReceiveMessage();
                BattleShip.AnalyseRequete();
                SendMessage(BattleShip.message);

                //Client recoit ok, place bateaux et send ok
                Jeux.Message("En attente du placement des bateaux de l'adversaire...");
                BattleShip.message = ReceiveMessage();
                if (!BattleShip.AnalyseRequete())
                {
                    Console.WriteLine("Pas reçu le OK, problème de communication.");
                    return false;
                }

                BattleShip.casesBateaux = Jeux.PlacerBateaux(BattleShipModel.tailleBateaux);
                BattleShip.message.SetMessage('O');
                SendMessage(BattleShip.message);

                //Executions des attaques jusqu'a la victoire ou la defaite
                Jeux.Message("En attente de l'attaque de l'adversaire...");

                bool tourDuClient = false;

                while (true)
                {
                    if (!tourDuClient)
                    {
                        // tout du serveur
                        do
                        {
                            //Client analyse attaque du serveur et send resultat
                            BattleShip.message = ReceiveMessage();
                            if (!BattleShip.AnalyseRequete())
                            {
                                SendMessage(BattleShip.message);
                                return true; // Fin de partie normale
                            }

                            SendMessage(BattleShip.message);

                        } while (BattleShip.rejouerTour);

                        tourDuClient = true;
                    }
                    else
                    {
                        // tour du client
                        do
                        {
                            //Client recoit ok et send attaque
                            BattleShip.message = ReceiveMessage();
                            if (!BattleShip.AnalyseRequete())
                            {
                                SendMessage(BattleShip.message);
                                return true; // Fin de partie normale
                            }

                            BattleShip.message.SetMessageAttaque('A', Jeux.SelectCase(BattleShip.plateau));
                            SendMessage(BattleShip.message);

                            //Client recoit resultat et send ok
                            BattleShip.message = ReceiveMessage();
                            if (!BattleShip.AnalyseRequete())
                            {
                                SendMessage(BattleShip.message);
                                return true; // Fin de partie normale
                            }

                            SendMessage(BattleShip.message);

                        } while (BattleShip.rejouerTour);

                        tourDuClient = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur pendant la partie : {ex.Message}");
                return false;
            }
        }

        private static void CloseConnection()
        {
            try
            {
                if (sender != null)
                {
                    if (sender.Connected)
                    {
                        sender.Shutdown(SocketShutdown.Both);
                    }
                    sender.Close();
                    Console.WriteLine("Connexion fermée.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la fermeture : {ex.Message}");
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