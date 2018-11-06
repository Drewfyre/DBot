using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DBot.Source
{
    public class BotCredentials
    {
        public string Token { get; private set; }
        public string ClientID { get; private set; }
        public string DbConnectionString { get; private set; }
        public ulong[] Owners { get; private set; }
        public ulong ReactionRoleChannel { get; private set; }

        public BotCredentials()
        {
            try
            {
                JObject credentials = Support.readJsonFile("credentials.json");

                this.Token = credentials["token"].ToString();
                this.ClientID = credentials["client_id"].ToString();
                this.Owners = credentials["owner_ids"].ToObject<ulong[]>();
                this.ReactionRoleChannel = Convert.ToUInt64(credentials["reaction_role_channel_id"]);
            }
            catch (Exception exc)
            {
                Support.Log($"Error in BotCredentials\n{exc.Message}");
            }            
        }
    }
}
