using BattleShip.Models;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BattleShip
{
    public class SynchronousSocketListener
    {
        private static BattleShipModel BattleShip = new BattleShipModel();
        private static Socket handler;
        private static readonly byte[] buffer = new byte[32];

        public static void StartListening()
        {
            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 443);
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(1);

                Console.WriteLine("Serveur démarré sur le port 443.");

                while (true)
                {
                    Console.Clear();
                    Console.WriteLine("En attente de connexions...");

                    handler = listener.Accept();
                    Console.WriteLine("Client connecté.");

                    try
                    {
                        do
                        {
                            bool gameEnded = false;

                            // === PHASE 1 : Envoi des paramètres ===
                            BattleShip.SetParametres();
                            BattleShip.NouveauJeux();
                            BattleShip.message.SetMessageParametres(BattleShipModel.plateauTailleX, BattleShipModel.plateauTailleY, BattleShipModel.tailleBateaux);
                            SendMessage(BattleShip.message);

                            // === PHASE 2 : Réception de l'OK ===
                            BattleShip.message = ReceiveMessage();
                            if (!BattleShip.AnalyseRequete())
                                break;

                            // === PHASE 3 : Placement des bateaux ===
                            BattleShip.casesBateaux = Jeux.PlacerBateaux(BattleShipModel.tailleBateaux);
                            BattleShip.message.SetMessage('O');
                            SendMessage(BattleShip.message);

                            Jeux.Message("En attente du placement des bateaux de l'adversaire...");

                            // === PHASE 4 : Boucle de jeu ===
                            bool tourDuServeur = true;

                            while (!gameEnded)
                            {
                                if (tourDuServeur)
                                {
                                    // === TOUR DU SERVEUR ===
                                    do
                                    {
                                        BattleShip.message = ReceiveMessage();
                                        if (!BattleShip.AnalyseRequete())
                                        {
                                            if (!BattleShip.rejouer)
                                                SendMessage(BattleShip.message);
                                            gameEnded = true;
                                            break;
                                        }

                                        BattleShip.message.SetMessageAttaque('A', Jeux.SelectCase(BattleShip.plateau));
                                        SendMessage(BattleShip.message);

                                        BattleShip.message = ReceiveMessage();
                                        if (!BattleShip.AnalyseRequete())
                                        {
                                            if (!BattleShip.rejouer)
                                                SendMessage(BattleShip.message);
                                            gameEnded = true;
                                            break;
                                        }

                                        SendMessage(BattleShip.message);

                                    } while (BattleShip.rejouerTour);

                                    tourDuServeur = false;
                                }
                                else
                                {
                                    // === TOUR DU CLIENT ===
                                    do
                                    {
                                        BattleShip.message = ReceiveMessage();
                                        if (!BattleShip.AnalyseRequete())
                                        {
                                            if (!BattleShip.rejouer)
                                                SendMessage(BattleShip.message);
                                            gameEnded = true;
                                            break;
                                        }

                                        SendMessage(BattleShip.message);

                                    } while (BattleShip.rejouerTour);

                                    tourDuServeur = true;
                                }
                            }

                            // === FIN DE PARTIE ===
                            if (BattleShip.rejouer && BattleShip.message.statut != 'O')
                            {
                                SendMessage(BattleShip.message);
                                BattleShip.message = ReceiveMessage();
                            }

                        } while (BattleShip.rejouer);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Erreur client] {ex.Message}");
                    }
                    finally
                    {
                        try
                        {
                            handler?.Shutdown(SocketShutdown.Both);
                            handler?.Close();
                            Console.WriteLine("Connexion client fermée proprement.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Fermeture socket] {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Erreur serveur] {e.Message}");
            }
            finally
            {
                try
                {
                    listener?.Close();
                    Console.WriteLine("Serveur arrêté.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Fermeture listener] {ex.Message}");
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
                bytesRec = handler.Receive(buffer);
                string chunk = Encoding.ASCII.GetString(buffer, 0, bytesRec);
                receivedDataBuilder.Append(chunk);

                if (chunk.Contains("\n"))
                    break;
            }

            string json = receivedDataBuilder.ToString().TrimEnd('\n'); // Enlève le \n
            return SerialisationModel.Deserialiser(json);
        }
    }
}
