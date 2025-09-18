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

                            // Le serveur commence toujours
                            bool tourDuServeur = true;

                            while (true)
                            {
                                if (tourDuServeur)
                                {
                                    // ===== TOUR DU SERVEUR =====
                                    do
                                    {
                                        //Serveur recoit ok du client et send attaque
                                        BattleShip.message = ReceiveMessage();
                                        if (!BattleShip.AnalyseRequete())
                                        {
                                            if (!BattleShip.rejouer)
                                                SendMessage(BattleShip.message);
                                            goto EndGame; // Sortir de la boucle principale
                                        }

                                        BattleShip.message.SetMessageAttaque('A', Jeux.SelectCase(BattleShip.plateau));
                                        SendMessage(BattleShip.message);

                                        //Serveur recoit resultat du client et send ok
                                        BattleShip.message = ReceiveMessage();
                                        if (!BattleShip.AnalyseRequete())
                                        {
                                            if (!BattleShip.rejouer)
                                                SendMessage(BattleShip.message);
                                            goto EndGame; // Sortir de la boucle principale
                                        }
                                        SendMessage(BattleShip.message);

                                        // Si le serveur a touché (rejouerTour = true), il garde la main
                                        // Si le serveur a raté (rejouerTour = false), c'est au tour du client
                                    } while (BattleShip.rejouerTour);

                                    // Le serveur a raté, c'est maintenant au tour du client
                                    tourDuServeur = false;
                                }
                                else
                                {
                                    // ===== TOUR DU CLIENT =====
                                    do
                                    {
                                        //Serveur recoit attaque du client et send resultat
                                        BattleShip.message = ReceiveMessage();
                                        if (!BattleShip.AnalyseRequete())
                                        {
                                            if (!BattleShip.rejouer)
                                                SendMessage(BattleShip.message);
                                            goto EndGame; // Sortir de la boucle principale
                                        }
                                        SendMessage(BattleShip.message);

                                        // Si le client a touché (rejouerTour = true), il garde la main
                                        // Si le client a raté (rejouerTour = false), c'est au tour du serveur
                                    } while (BattleShip.rejouerTour);

                                    // Le client a raté, c'est maintenant au tour du serveur
                                    tourDuServeur = true;
                                }
                            }

                        EndGame:
                            // Gestion de la fin de partie et demande de rejouer
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
            byte[] data = Encoding.ASCII.GetBytes(SerialisationModel.Serialiser(message));

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
            int bytesRec;
            StringBuilder receivedDataBuilder = new StringBuilder();
            do
            {
                bytesRec = handler.Receive(bytes);
                receivedDataBuilder.Append(Encoding.ASCII.GetString(bytes, 0, bytesRec));
            } while (bytesRec == 32);

            string json = receivedDataBuilder.ToString();

            return SerialisationModel.Deserialiser(json);
        }
    }
}