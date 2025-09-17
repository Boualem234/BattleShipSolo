using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BattleShip.Models
{
    public static class SerialisationModel
    {
        public static string Serialiser(Message message)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(message);
        }

        public static Message Deserialiser(string jsonSave)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Message>(jsonSave);
        }
    }
}
