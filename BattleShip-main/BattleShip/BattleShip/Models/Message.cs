using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BattleShip.Models
{
    public class Message
    {
        public string MessageRequete
        {
            get
            {
                string message = $"{statut}";
                foreach (int coordonne in coordonnes)
                    message += $"{coordonne.ToString()},";
                return message;
            }
        }
        //A=attaquer M=manquer T=toucher W=win P=parametre O=OK R=rejouer S=stop C=Color
        [JsonIgnore]
        public char statut;
        [JsonIgnore]
        public List<int> coordonnes = new List<int>();

        //Constructeurs
        public Message() { }

        [JsonConstructor]
        public Message(string MessageRequete)
        {
            DecrypteMessage(MessageRequete);
        }

        /// <summary>
        /// Decrypte le message recu et rempli les attributs
        /// </summary>
        /// <param name="message"></param>
        public void DecrypteMessage(string message)
        {
            string messageTemporaire = "";

            coordonnes.Clear();
            //Load le statut
            statut = message[0];

            //Load les coordonnees
            for (int i = 1; i < message.Length; i++)
            {
                if (message[i] != ',')
                {
                    messageTemporaire += message[i];
                }
                else
                {
                    coordonnes.Add(int.Parse(messageTemporaire));
                    messageTemporaire = "";
                }
            }
        }

        /// <summary>
        /// Remplit les attributs pour un message d'attaque
        /// </summary>
        /// <param name="statut"></param>
        /// <param name="coordonne"></param>
        public void SetMessageAttaque(char statut, (int, int) coordonne)
        {
            coordonnes.Clear();
            this.statut = statut;
            coordonnes.Add(coordonne.Item1);
            coordonnes.Add(coordonne.Item2);
        }

        /// <summary>
        /// Remplit les attributs pour un message de parametres
        /// </summary>
        /// <param name="tailleX"></param>
        /// <param name="tailleY"></param>
        /// <param name="tailleBateaux"></param>
        public void SetMessageParametres(int tailleX, int tailleY, List<int> tailleBateaux)
        {
            coordonnes.Clear();
            statut = 'P';
            coordonnes.Add(tailleX);
            coordonnes.Add(tailleY);
            foreach (int taille in tailleBateaux)
                coordonnes.Add(taille);
        }

        /// <summary>
        /// Remplit le statut pour un message simple (O, W, R)
        /// </summary>
        /// <param name="statut"></param>
        public void SetMessage(char statut)
        {
            coordonnes.Clear();
            this.statut = statut;
        }
    }
}
