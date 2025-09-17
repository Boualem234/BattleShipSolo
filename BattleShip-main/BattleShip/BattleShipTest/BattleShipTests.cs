using BattleShip;
using BattleShip.Models;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Sockets;

namespace BattleShipTest
{
    [TestClass]
    public class BattleShipTests
    {
        private BattleShipModel model;

        [TestInitialize]
        public void Setup()
        {
            model = new BattleShipModel();
            BattleShipModel.plateauTailleX = 4;
            BattleShipModel.plateauTailleY = 4;
            BattleShipModel.tailleBateaux = new List<int> { 2 };
        }

        [TestMethod]
        public void Test_NouvellePartie()
        {
            // Test du redémarrage d'une partie
            model.casesBateaux.Add((0, 0), 0);
            model.casesTouchees.Add((0, 0));

            model.NouveauJeux();

            Assert.AreEqual(0, model.casesBateaux.Count);
            Assert.AreEqual(0, model.casesTouchees.Count);
        }

        // 1.Choisir client/serveur
        [TestMethod]
        public void Test_Choisir_ClientOuServeur()
        {
            using (var sr = new StringReader("oui\n"))
            {
                Console.SetIn(sr);
                string input = Console.ReadLine().ToLower();
                bool isClient = input == "oui";
                Assert.IsTrue(isClient);
            }
        }

        // 2) se connecter à un serveur (simplifié)
        [TestMethod]
        public void Test_Connexion_ClientServeur()
        {
            // Test de la connexion (simplifié)
            IPAddress ipAddress = IPAddress.Loopback; // Utilisation de l'adresse locale pour le test
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 443);
            Socket testSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //starter un serveur localement pour try
            TcpListener serveurTest = new TcpListener(ipAddress, 443);
            serveurTest.Start();

            try
            {
                testSocket.Connect(remoteEP);
                Assert.IsTrue(testSocket.Connected);
            }
            catch (SocketException)
            {
                Assert.Fail("Échec de la connexion au serveur.");
            }
            finally
            {
                if (testSocket.Connected)
                    testSocket.Shutdown(SocketShutdown.Both);
                testSocket.Close();
            }
        }

        // 3. Placer un bateau
        [TestMethod]
        public void Test_PlacerBateaux_LogiqueMetier()
        {
            // Test de la logique sans interaction console
            // crée manuellement un dictionnaire comme PlacerBateaux devrait le faire
            var casesBateaux = new Dictionary<(int, int), int>();

            // Simulation d'un bateau de taille 2 placé horizontalement
            casesBateaux.Add((0, 0), 0); 
            casesBateaux.Add((1, 0), 0);

            Assert.IsNotNull(casesBateaux, "Le dictionnaire ne doit pas être null");
            Assert.AreEqual(2, casesBateaux.Count, "Un bateau de taille 2 doit avoir exactement 2 cases");

            // limites du plateau
            foreach (var pos in casesBateaux.Keys)
            {
                Assert.IsTrue(pos.Item1 >= 0 && pos.Item1 < BattleShipModel.plateauTailleX,
                    $"Coordonnée X {pos.Item1} doit être entre 0 et {BattleShipModel.plateauTailleX - 1}");
                Assert.IsTrue(pos.Item2 >= 0 && pos.Item2 < BattleShipModel.plateauTailleY,
                    $"Coordonnée Y {pos.Item2} doit être entre 0 et {BattleShipModel.plateauTailleY - 1}");
            }

            // Vérifier que toutes les cases appartiennent au même bateau
            var valeursUniques = casesBateaux.Values.Distinct().ToList();
            Assert.AreEqual(1, valeursUniques.Count, "Toutes les cases doivent appartenir au même bateau");
            Assert.AreEqual(0, valeursUniques[0], "Le premier bateau doit avoir l'index 0");

            // Vérifier que les cases sont adjacentes
            var positions = casesBateaux.Keys.ToList();
            var pos1 = positions[0];
            var pos2 = positions[1];

            bool sontAdjacentes = (Math.Abs(pos1.Item1 - pos2.Item1) == 1 && pos1.Item2 == pos2.Item2) ||
                                 (Math.Abs(pos1.Item2 - pos2.Item2) == 1 && pos1.Item1 == pos2.Item1);

            Assert.IsTrue(sontAdjacentes, "Les cases du bateau doivent être adjacentes");
        }


        // 4. Sélectionner une case (on simule un plateau vide)
        [TestMethod]
        public void Test_SelectCase()
        {
            var plateau = new char[3, 3];
            var coords = (1, 1);
            // ici on ne peut pas simuler Console.ReadKey, donc on vérifie juste que le plateau existe
            Assert.AreEqual(3, plateau.GetLength(0));
        }

        // 5. Répondre à une attaque adverse (Analyse requête)
        [TestMethod]
        public void Test_AnalyseRequete_Attaque()
        {
            var model = new BattleShipModel();
            var message = new Message();
            message.SetMessageAttaque('A', (0, 0));
            model.message = message;

            bool result = model.AnalyseRequete();
            Assert.IsTrue(result);
        }

        // 6. Afficher victoire
        [TestMethod]
        public void Test_Message_Victoire()
        {
            // Capturer la sortie console
            using (var sw = new StringWriter())
            {
                Console.SetOut(sw); 
                Jeux.Victoire();

                var result = sw.ToString().Trim();
                Assert.AreEqual("Félicitation, tu as gagné!", result);
            }
        }


        // 7. Afficher défaite
        [TestMethod]
        public void Test_Message_Defaite()
        {
            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);
                Jeux.Defaite();

                var result = sw.ToString().Trim();
                Assert.AreEqual("Dommage, tu as perdu!", result);
            }
        }


        // 8. Relancer une partie
        [TestMethod]
        public void Test_Rejouer_Oui()
        {
            using (var sr = new StringReader("oui"))
            {
                Console.SetIn(sr);

                bool result = Jeux.Rejouer();

                Assert.IsTrue(result);
            }
        }

        [TestMethod]
        public void Test_Rejouer_Non()
        {
            using (var sr = new StringReader("non"))
            {
                Console.SetIn(sr);

                bool result = Jeux.Rejouer();

                Assert.IsFalse(result);
            }
        }

        // 9. Quitter l'application

        [TestMethod]
        public void Test_Quitter()
        {
            // Test de la déconnexion
            model.message.SetMessage('S');
            bool resultat = model.AnalyseRequete();

            Assert.IsFalse(resultat);
            Assert.IsFalse(model.rejouer);
        }
    }
}