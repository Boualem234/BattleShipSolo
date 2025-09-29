using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Models;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;

namespace BattleShip
{

    public enum Colors
    {
        Green,
        Blue,
        Red,
        Yellow
    }
    public static class Jeux
    {
        public static Colors boatColor;
        public static bool isClient;

        /// <summary>
        /// Recharge le plateau
        /// </summary>
        /// <param name="plateau"></param>
        public static void ChargerPlateau(char[,] plateau)
        {
            Console.Clear();
            Console.WriteLine(LoadPlateau(plateau));
        }

        /// <summary>
        /// Affiche un message
        /// </summary>
        /// <param name="message"></param>
        public static void Message(string message, bool clear = true)
        {
            if (clear)
                Console.Clear();
            Console.WriteLine(message);
        }

        /// <summary>
        /// Affiche le message de victoire
        /// </summary>
        public static void Victoire()
        {
            if (!Console.IsOutputRedirected)
            {
                Console.Clear();
            }
            Console.WriteLine("Félicitation, tu as gagné!");
        }

        /// <summary>
        /// Affiche le message de défaite
        /// </summary>
        public static void Defaite()
        {
            if (!Console.IsOutputRedirected)
            {
                Console.Clear();
            }
            Console.WriteLine("Dommage, tu as perdu!");
        }

        /// <summary>
        /// Demande si le joueur veut rejouer
        /// </summary>
        /// <returns></returns>
        public static bool Rejouer()
        {
            Console.WriteLine("Veux-tu rejouer? (oui/non)");
            switch (Console.ReadLine().ToLower())
            {
                case "oui":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Sélectionne une case sur le plateau
        /// </summary>
        /// <param name="plateau"></param>
        /// <returns></returns>
        public static (int, int) SelectCase(char[,] plateau)
        {
            BattleShipModel model = new BattleShipModel();
            (int, int) coordonnees;

            model.plateau = plateau;

            while (true)
            {
                Console.Clear();
                Console.WriteLine(LoadPlateau(plateau));
                coordonnees = SelectionneCoordonnees(BattleShipModel.plateauTailleX, BattleShipModel.plateauTailleY, 3, 2);

                if (!model.GetCaseStatue(coordonnees))
                {
                    return coordonnees;
                }
            }
        }

        /// <summary>
        /// Place les bateaux sur le plateau selon les tailles données
        /// </summary>
        /// <param name="tailleBateaux"></param>
        /// <returns></returns>
        public static Dictionary<(int, int), int> PlacerBateaux(List<int> tailleBateaux)
        {
            Dictionary<(int, int), int> casesBateaux = new Dictionary<(int, int), int>();
            List<(int, int)> bateauCases = new List<(int, int)>();

            for (int i = 0; i < tailleBateaux.Count; i++)
            {
                while (true)
                {
                    bateauCases = SetBateauCases(tailleBateaux[i], casesBateaux);
                    if (bateauCases.Count > 0)
                    {
                        foreach ((int, int) coordonnees in bateauCases)
                        {
                            casesBateaux.Add(coordonnees, i);
                        }
                        break;
                    }
                }
            }
            return casesBateaux;
        }

        /// <summary>
        /// Sélectionne les cases d'un bateau
        /// </summary>
        /// <param name="nbCases"></param>
        /// <param name="casesBateaux"></param>
        /// <returns></returns>
        private static List<(int, int)> SetBateauCases(int nbCases, Dictionary<(int, int), int> casesBateaux)
        {
            List<(int, int)> bateauCases = new List<(int, int)>();
            (int, int) coordonneesBase, coordonneesSens;
            BattleShipModel model = new BattleShipModel();
            bool valideUp, valideDown, valideLeft, valideRight;

            //Load les bateaux déjà placés
            model.NouveauJeux();
            model.casesBateaux = casesBateaux;
            foreach (KeyValuePair<(int, int), int> caseBateau in casesBateaux)
                model.SetCase(caseBateau.Key, 'B');

            //Selectionne la premiere coordonnée du bateau
            Console.Clear();
            Console.WriteLine(LoadPlateau(model.plateau));
            while (true)
            {
                coordonneesBase = SelectionneCoordonnees(BattleShipModel.plateauTailleX, BattleShipModel.plateauTailleY, 3, 2);
                if (!model.Checkbateaux(coordonneesBase))
                {
                    valideUp = coordonneesBase.Item2 - (nbCases - 1) >= 0;
                    valideLeft = coordonneesBase.Item1 - (nbCases - 1) >= 0;
                    valideRight = coordonneesBase.Item1 + (nbCases - 1) < BattleShipModel.plateauTailleX;
                    valideDown = coordonneesBase.Item2 + (nbCases - 1) < BattleShipModel.plateauTailleY;
                    for (int i = 1; i < nbCases; i++)
                    {
                        if (valideUp && model.Checkbateaux((coordonneesBase.Item1, coordonneesBase.Item2 - i))) { valideUp = false; }
                        if (valideLeft && model.Checkbateaux((coordonneesBase.Item1 - i, coordonneesBase.Item2))) { valideLeft = false; }
                        if (valideRight && model.Checkbateaux((coordonneesBase.Item1 + i, coordonneesBase.Item2))) { valideRight = false; }
                        if (valideDown && model.Checkbateaux((coordonneesBase.Item1, coordonneesBase.Item2 + i))) { valideDown = false; }
                    }

                    if (valideUp || valideDown || valideLeft || valideRight)
                    {
                        bateauCases.Add(coordonneesBase);
                        model.SetCase(coordonneesBase, 'B');
                        break;
                    }
                }
            }

            //Ajoute les possibles cases dans le tableau
            for (int i = 1; i < nbCases; i++)
            {
                if (valideUp) { model.SetCase((coordonneesBase.Item1, coordonneesBase.Item2 - i), 'V'); }
                if (valideLeft) { model.SetCase((coordonneesBase.Item1 - i, coordonneesBase.Item2), 'V'); }
                if (valideRight) { model.SetCase((coordonneesBase.Item1 + i, coordonneesBase.Item2), 'V'); }
                if (valideDown) { model.SetCase((coordonneesBase.Item1, coordonneesBase.Item2 + i), 'V'); }
            }

            //Selectionne les autres cases du bateau
            Console.Clear();
            Console.WriteLine(LoadPlateau(model.plateau));
            while (true)
            {
                coordonneesSens = SelectionneCoordonnees(BattleShipModel.plateauTailleX, BattleShipModel.plateauTailleY, 3, 2, true);

                //Si on appuie sur Echap (annule le placement du bateau)
                if (coordonneesSens.Item1 == -1 && coordonneesSens.Item2 == -1)
                {
                    bateauCases.Clear();
                    return bateauCases;
                }

                if (model.GetCaseValue(coordonneesSens) == 'V')
                {
                    for (int i = 1; i < nbCases; i++)
                    {
                        if (coordonneesSens.Item1 > coordonneesBase.Item1) { bateauCases.Add((coordonneesBase.Item1 + i, coordonneesBase.Item2)); }
                        if (coordonneesSens.Item1 < coordonneesBase.Item1) { bateauCases.Add((coordonneesBase.Item1 - i, coordonneesBase.Item2)); }
                        if (coordonneesSens.Item2 > coordonneesBase.Item2) { bateauCases.Add((coordonneesBase.Item1, coordonneesBase.Item2 + i)); }
                        if (coordonneesSens.Item2 < coordonneesBase.Item2) { bateauCases.Add((coordonneesBase.Item1, coordonneesBase.Item2 - i)); }
                    }
                    break;
                }
            }

            return bateauCases;
        }

        /// <summary>
        /// Permet de sélectionner des coordonnées avec les flèches du clavier
        /// </summary>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <param name="bondX"></param>
        /// <param name="bondY"></param>
        /// <param name="exit"></param>
        /// <returns></returns>
        private static (int, int) SelectionneCoordonnees(int maxX, int maxY, int bondX, int bondY, bool exit = false)
        {
            bool boucle = true;
            int x = 0;
            int y = 0;

            while (boucle)
            {
                Console.SetCursorPosition(x, y);
                switch (Console.ReadKey(intercept: true).Key.ToString())
                {
                    case "UpArrow":
                        if (y - bondY >= 0)
                            y -= bondY;
                        break;
                    case "DownArrow":
                        if (y < (maxY - 1) * bondY)
                            y += bondY;
                        break;
                    case "RightArrow":
                        if (x < (maxX - 1) * bondX)
                            x += bondX;
                        break;
                    case "LeftArrow":
                        if (x - bondX >= 0)
                            x -= bondX;
                        break;
                    case "Enter":
                        boucle = false;
                        break;
                    case "Escape":
                        if (exit)
                        {
                            return (-1, -1);
                        }
                        break;
                }
            }
            return (x / bondX, y / bondY);
        }

        /// <summary>
        /// Charge le plateau dans une string
        /// </summary>
        /// <param name="plateau"></param>
        /// <returns></returns>
        private static string LoadPlateau(char[,] plateau)
        {
            string Stringplateau = "";

            for (int y = 0; y < plateau.GetLength(1); y++)
            {
                //Ajoute les 💧|💧|💧|💧
                for (int x = 0; x < plateau.GetLength(0); x++)
                {
                    Stringplateau += ConvertEmoji(plateau[x, y]);
                    if (x + 1 != plateau.GetLength(0))
                        Stringplateau += "|";
                }

                Stringplateau += "\n";

                //Ajoute les --+--+--+--
                if (y + 1 != plateau.GetLength(1))
                    for (int i = 0; i < (plateau.GetLength(0) * 2) - 1; i++)
                    {
                        if (i % 2 == 0)
                            Stringplateau += "--";
                        else
                            Stringplateau += "+";
                    }

                Stringplateau += "\n";
            }

            return Stringplateau;
        }

        /// <summary>
        /// Convertit une lettre en emoji
        /// </summary>
        /// <param name="lettre"></param>
        /// <returns></returns>
        private static string ConvertEmoji(char lettre)
        {
            switch (lettre)
            {
                case ' ':
                    return "💧";
                case 'T':
                    return "💥";
                case 'M':
                    return "🌫️";
                case 'B':
                    return boatColor switch
                    {
                        Colors.Red => "🟥",
                        Colors.Blue => "🟦",
                        Colors.Green => "🟩",
                        Colors.Yellow => "🟨",
                        _ => "⛵"
                    };
                case 'V':
                    return "✅";
                default:
                    return "??";
            }
        }

        public static void SaveColors(Colors colorClient, Colors colorServer)
        {
            // dossier "Documents\Color_BattleShip" de l'utilisateur courant
            string folderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Color_BattleShip"
            );

            // chemin complet du fichier
            string path = Path.Combine(folderPath, "colors.json");

            var colorsObj = new
            {
                ColorClient = colorClient.ToString(),
                ColorServer = colorServer.ToString()
            };

            string json = JsonConvert.SerializeObject(colorsObj, Formatting.Indented);

            Directory.CreateDirectory(folderPath);
            File.WriteAllText(path, json);

            Console.WriteLine($"✅ Couleurs sauvegardées dans : {path}");
        }

        public static void LoadColor()
        {
            string folderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Color_BattleShip"
            );
            string path = Path.Combine(folderPath, "colors.json");

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var obj = JsonConvert.DeserializeObject<dynamic>(json);
                string colorStr = isClient ? (string)obj.ColorClient : (string)obj.ColorServer;

                if (Enum.TryParse(colorStr, out Colors color))
                {
                    Console.WriteLine($"✅ Couleur chargée depuis le fichier : {color}");
                    boatColor = color;
                }
                else
                {
                    Console.WriteLine("❌ Couleur invalide dans le fichier, couleur par défaut utilisée (Vert).");
                    boatColor = Colors.Green;
                }
            }
            else
            {
                SetColor();
            }
        }

        private static void SetColor()
        {
            Colors color;
            bool valid = false;

            while (!valid)
            {
                Console.WriteLine("Quelle couleur pour vous ?");
                Console.WriteLine("1. Rouge");
                Console.WriteLine("2. Bleu");
                Console.WriteLine("3. Vert");
                Console.WriteLine("4. Jaune");
                Console.WriteLine("Choisissez un chiffre entre 1 et 4 : ");

                string? input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        color = Colors.Red;
                        valid = true;
                        break;
                    case "2":
                        color = Colors.Blue;
                        valid = true;
                        break;
                    case "3":
                        color = Colors.Green;
                        valid = true;
                        break;
                    case "4":
                        color = Colors.Yellow;
                        valid = true;
                        break;
                    default:
                        Console.WriteLine("Choix invalide. Veuillez réessayer.\n");
                        continue; // On redemande
                }

                boatColor = color;
            }
        }


    }
}
