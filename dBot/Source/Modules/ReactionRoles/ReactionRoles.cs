using DBot.Source.Services;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBot.Source.Modules.ReactionRoles
{
    public class ReactionRolesService : IService
    {
        private readonly DiscordSocketClient _SocketClient;
        public readonly DbService _DbService;

        private Dictionary<string, Dictionary<string, string>> Reactions;

        public ReactionRolesService(DiscordSocketClient Client)
        {
            this._SocketClient = Client;
            this._SocketClient.ReactionAdded += _SocketClient_ReactionAdded;
            this._SocketClient.ReactionRemoved += _SocketClient_ReactionRemoved;

            this._DbService = new DbService();
            this._DbService.Conn.Open();
            SQLiteCommand Cmd = new SQLiteCommand("SELECT * FROM reaction_messages", this._DbService.Conn);
            SQLiteDataReader Reader = Cmd.ExecuteReader();
            this._DbService.Conn.Close();
            this.Reactions = new Dictionary<string, Dictionary<string, string>>();

            while(Reader.Read())
            {
                string MessageID = Reader["messageID"].ToString();

                var ReactionRoles = new Dictionary<string, string>();

                SQLiteCommand Cmd2 = new SQLiteCommand("SELECT r.reaction_text, rr.role FROM reaction_roles rr JOIN reactions r ON rr.rID = r.rID WHERE rmID = " + Convert.ToInt64(MessageID), this._DbService.Conn);
                SQLiteDataReader Reader2 = Cmd2.ExecuteReader();

                while(Reader2.Read())
                {
                    ReactionRoles.Add(Reader2["reaction_text"].ToString(), Reader2["role"].ToString());
                }

                this.Reactions.Add(MessageID, ReactionRoles);
            }
        }

        private async Task _SocketClient_ReactionRemoved(Discord.Cacheable<Discord.IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            string MessageID = arg1.Value.Id.ToString();
            string Reaction = arg3.Emote.Name;
            

            if (this.Reactions.ContainsKey(MessageID)
                && this.Reactions[MessageID].ContainsKey(Reaction))
            {
                //var Role = arg1.Value.Channel.
            }
        }

        private async Task _SocketClient_ReactionAdded(Discord.Cacheable<Discord.IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            
        }
    }
}
