using BattleShip.Models;
using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BattleShip
{
    public class SynchronousSocketListener
    {
        private static BattleShipModel BattleShip = new BattleShipModel();

        private static Socket handler;

        private static byte[] bytes = new byte[32];

        public static void StartListening()
        {
            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 443);
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                //serveur attend
                listener.Bind(localEndPoint);
                listener.Listen(1);

                while (true)
                {
                    Console.Clear();
                    Console.WriteLine("Serveur en attente de connexions...");
                    handler = listener.Accept();
                    try
                    {
                        do
                        {
                            //Serveur set parametres et send parametres
                            BattleShip.SetParametres();
                            BattleShip.NouveauJeux();
                            BattleShip.message.SetMessageParametres(BattleShipModel.plateauTailleX, BattleShipModel.plateauTailleY, BattleShipModel.tailleBateaux);

                            SendMessage(BattleShip.message);

                            //Serveur recoit ok, place bateaux et send ok
                            BattleShip.message = ReceiveMessage();
                            if (!BattleShip.AnalyseRequete())
                                break;

                            BattleShip.casesBateaux = Jeux.PlacerBateaux(BattleShipModel.tailleBateaux);

                            BattleShip.message.SetMessage('O');
                            SendMessage(BattleShip.message);

                            //Executions des attaques jusqu'a la victoire ou la defaite
                            Jeux.Message("En attente du placement des bateaux de l'adversaire...");
                            while (true)
                            {
                                //Serveur recoit ok et send attaque
                                BattleShip.message = ReceiveMessage();
                                if (!BattleShip.AnalyseRequete())
                                {
                                    if (!BattleShip.rejouer)
                                        SendMessage(BattleShip.message);
                                    break;
                                }

                                BattleShip.message.SetMessageAttaque('A', Jeux.SelectCase(BattleShip.plateau));
                                SendMessage(BattleShip.message);

                                //Serveur recoit resultat et send ok
                                BattleShip.message = ReceiveMessage();
                                if (!BattleShip.AnalyseRequete())
                                {
                                    if (!BattleShip.rejouer)
                                        SendMessage(BattleShip.message);
                                    break;
                                }
                                SendMessage(BattleShip.message);

                                //Serveur recoit attaque et send resultat
                                BattleShip.message = ReceiveMessage();
                                if (!BattleShip.AnalyseRequete())
                                {
                                    if (!BattleShip.rejouer)
                                        SendMessage(BattleShip.message);
                                    break;
                                }
                                SendMessage(BattleShip.message);
                            }
                            if (BattleShip.rejouer && BattleShip.message.statut != 'Y')
                            {
                                SendMessage(BattleShip.message);
                                BattleShip.message = ReceiveMessage();
                            }
                        } while (BattleShip.rejouer);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors du traitement du client : {ex.Message}");
                    }
                    finally
                    {
                        // Fermer la connexion avec le client
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        Console.WriteLine("Connexion fermée.");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erreur critique : {e.Message}");
            }
            finally
            {
                listener.Close();
                Console.WriteLine("Serveur arrêté.");
            }
        }

        private static void SendMessage(Message message)
        {
            byte[] data = Encoding.ASCII.GetBytes(SerialisationModel.Serialiser(message) + "\n");

            int offset = 0;
            while (offset < data.Length)
            {
                int chunkSize = Math.Min(32, data.Length - offset);
                handler.Send(data, offset, chunkSize, SocketFlags.None);
                offset += chunkSize;
            }
        }

        private static Message ReceiveMessage()
        {
            StringBuilder receivedDataBuilder = new StringBuilder();
            int bytesRec;

            while (true)
            {
                bytesRec = handler.Receive(bytes);
                string chunk = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                receivedDataBuilder.Append(chunk);

                if (chunk.Contains("\n"))
                    break;
            }

            string json = receivedDataBuilder.ToString().TrimEnd('\n'); // Enlève le \n
            return SerialisationModel.Deserialiser(json);
        }
    }
}