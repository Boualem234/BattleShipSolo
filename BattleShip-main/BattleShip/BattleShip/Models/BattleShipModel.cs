using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BattleShip.Models
{
    public class BattleShipModel
    {
        //Attributs statiques
        public static int plateauTailleX { get; set; }
        public static int plateauTailleY;
        public static List<int> tailleBateaux = new List<int>() { };

        //Attributs
        public char[,] plateau = new char[plateauTailleX, plateauTailleY];
        public Dictionary<(int, int), int> casesBateaux = new Dictionary<(int, int), int>();
        public List<(int, int)> casesTouchees = new List<(int, int)>();
        public Message message = new Message();
        public bool rejouer = false;

        /// <summary>
        /// Analyse la requete recu et effectue les actions necessaires
        /// </summary>
        /// <returns></returns>
        public bool AnalyseRequete()
        {
            switch (message.statut)
            {
                case 'P':
                    plateauTailleX = message.coordonnes[0];
                    plateauTailleY = message.coordonnes[1];
                    tailleBateaux.Clear();
                    for (int i = 2; i < message.coordonnes.Count(); i++)
                    {
                        tailleBateaux.Add(message.coordonnes[i]);
                    }
                    NouveauJeux();
                    message.SetMessage('O');
                    return true;
                case 'A':
                    if (Checkbateaux((message.coordonnes[0], message.coordonnes[1])))
                    {
                        casesTouchees.Add((message.coordonnes[0], message.coordonnes[1]));
                        if (CheckVictoire())
                        {
                            message.SetMessage('W');
                            Jeux.Defaite();
                            Jeux.Message("En attente de la demande de rejouer...", false);
                            return true;
                        }
                        message.SetMessageAttaque('T', (message.coordonnes[0], message.coordonnes[1]));
                        return true;
                    }
                    else
                    {
                        message.SetMessageAttaque('M', (message.coordonnes[0], message.coordonnes[1]));
                        return true;
                    }
                case 'T':
                    SetCase((message.coordonnes[0], message.coordonnes[1]), 'T');
                    Jeux.ChargerPlateau(plateau);
                    Jeux.Message("En attente de l'attaque de l'adversaire...", false);
                    message.SetMessage('O');
                    return true;
                case 'M':
                    SetCase((message.coordonnes[0], message.coordonnes[1]), 'M');
                    Jeux.ChargerPlateau(plateau);
                    Jeux.Message("En attente de l'attaque de l'adversaire...", false);
                    message.SetMessage('O');
                    return true;
                case 'O':
                    return true;
                case 'W':
                    Jeux.Victoire();
                    if (Jeux.Rejouer())
                    {
                        message.SetMessage('R');
                        rejouer = true;
                    }
                    else
                    {
                        message.SetMessage('S');
                        rejouer = false;
                    }
                    return false;
                case 'R':
                    rejouer = true;
                    NouveauJeux();
                    message.SetMessage('Y');
                    return false;
                case 'S':
                    rejouer = false;
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Verifie si tous les bateaux ont ete touches
        /// </summary>
        /// <returns></returns>
        private bool CheckVictoire()
        {
            foreach (KeyValuePair<(int, int), int> bateau in casesBateaux)
            {
                if (!casesTouchees.Contains(bateau.Key))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Demande les parametres de la partie et les set
        /// </summary>
        public void SetParametres()
        {
            int tailleX, tailleY;
            List<int> TailleBateaux = new List<int>();

            //Set la taille x du plateau
            do
            {
                Console.Clear();
                Console.Write("Taille du plateau sur le x: ");
            } while (!int.TryParse(Console.ReadLine(), out tailleX) || tailleX < 4 || tailleX >= 21);


            //Set la taille y du plateau
            do
            {
                Console.Clear();
                Console.Write("Taille du plateau sur le y: ");
            } while (!int.TryParse(Console.ReadLine(), out tailleY) || tailleY < 4 || tailleY >= 21);

            //Set la taille des bateaux
            tailleBateaux.Clear();
            for(int l = 0; l <= Math.Min(tailleX,tailleY)/2; l++) 
            {
                Console.Clear();
                Console.Write($"Taille du bateau #{TailleBateaux.Count() + 1} (0 = stop): ");
                if (int.TryParse(Console.ReadLine(), out int taille))
                {
                    if (taille != 0)
                    {
                        if (taille > 1 && taille <= Math.Max(tailleX, tailleY))
                            TailleBateaux.Add(taille);
                    }
                    else
                    {
                        if (TailleBateaux.Count() > 0) { break; }
                    }
                }
            }

            //Set les settings
            plateauTailleX = tailleX;
            plateauTailleY = tailleY;
            foreach (int taille in TailleBateaux)
            {
                tailleBateaux.Add(taille);
            }
        }

        /// <summary>
        /// Génère le plateau de jeu vide
        /// </summary>
        private void GeneratePlateau()
        {
            plateau = new char[plateauTailleX, plateauTailleY];
            for (int x = 0; x < plateauTailleX; x++)
            {
                for (int y = 0; y < plateauTailleY; y++)
                {
                    plateau[x, y] = ' ';
                }
            }
        }

        /// <summary>
        /// Set la valeur d'une case
        /// </summary>
        /// <param name="position"></param>
        /// <param name="value"></param>
        public void SetCase((int, int) position, char value)
        {
            if (position.Item1 < plateauTailleX && position.Item1 >= 0 && position.Item2 < plateauTailleY && position.Item2 >= 0)
                plateau[position.Item1, position.Item2] = value;
        }

        /// <summary>
        /// Reset le plateau et les bateaux
        /// </summary>
        public void NouveauJeux()
        {
            casesBateaux.Clear();
            casesTouchees.Clear();
            GeneratePlateau();
        }

        /// <summary>
        /// Retourne si un bateau est present 
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool Checkbateaux((int, int) position)
        {
            if (casesBateaux.ContainsKey(position))
                return true;
            return false;
        }

        /// <summary>
        /// Retourne si la case est vide ou non
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool GetCaseStatue((int, int) position)
        {
            if (plateau[position.Item1, position.Item2] == ' ') return false;
            else return true;
        }

        /// <summary>
        /// Retourne la valeur de la case
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public char GetCaseValue((int, int) position)
        {
            return plateau[position.Item1, position.Item2];
        }
    }
}
