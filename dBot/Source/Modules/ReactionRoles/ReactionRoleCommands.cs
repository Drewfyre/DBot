using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBot.Source.Modules.ReactionRoles
{
    [Group]
    public class ReactionRoleCommands : DModuleBase
    {
        private readonly DiscordSocketClient _SocketClient;
        public readonly DbService _DbService;

        private List<long> ReactionMessages;

        public ReactionRoleCommands(DiscordSocketClient Client)
        {
            this._SocketClient = Client;

            this._DbService = new DbService();
            this._DbService.Conn.Open();
            SQLiteCommand Cmd = new SQLiteCommand("SELECT * FROM reaction_messages", this._DbService.Conn);
            SQLiteDataReader Reader = Cmd.ExecuteReader();

            this.ReactionMessages = new List<long>();

            while (Reader.Read())
            {
                this.ReactionMessages.Add(Reader.GetInt64(0));
            }

            this._DbService.Conn.Close();
        }

        [Command("add_reaction_msg")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        [RequireBotPermission(Discord.GuildPermission.Administrator)]
        public async Task AddReactionMessage(params string[] Params)
        {
            //string Message = Context.Message.Content.Replace("!add_reaction_msg", "").Trim();
            if (Params.Length <= 0)
                return;

            System.Drawing.Color c = (System.Drawing.Color)(new ColorConverter().ConvertFromString(Params[0]));

            string Message = string.Join(" ", Params.Skip(1));
            
            await Context.Message.DeleteAsync();

            var Eb = new EmbedBuilder();
            Eb.WithDescription(Message);
            Eb.WithColor(c.R, c.G, c.B);
            var Answer = await Context.Channel.SendMessageAsync("", false, Eb.Build());
            this.ReactionMessages.Add((long)Answer.Id);

            this._DbService.Conn.Open();
            string SQL = $@"INSERT INTO reaction_messages (message, messageID, DOEOM) VALUES ('{Message}', {(long)Answer.Id}, {Support.UnixTimeStamp()})";
            SQLiteCommand Cmd = new SQLiteCommand(SQL, this._DbService.Conn);
            Cmd.ExecuteNonQuery();
            this._DbService.Conn.Close();
        }

        [Command("add_reaction_role")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.Administrator)]
        public async Task AddReactionRole(params string[] Params)
        {
            if (Params.Length <= 0)
                return;

            string Role = string.Join(" ", Params.Skip(2));

            if(!Context.Guild.Roles.Any(R => R.Name == Role))
            {
                await Context.Channel.SendMessageAsync($"The role <{Role}> doesn't exist. Please specify a correct role.");
                await Context.Message.DeleteAsync();
                return;
            }

            var MainMsg = await Context.Channel.GetMessageAsync(ulong.Parse(Params[1]));

            if (MainMsg == null)
            {
                await Context.Channel.SendMessageAsync($"The base message <{Params[1]}> doesn't exist. Please specify a correct base message.");
                await Context.Message.DeleteAsync();
                return;
            }

            this._DbService.Conn.Open();
            string SQL = $@"SELECT rID FROM reactions WHERE gID = {Context.Guild.Id} AND reaction_text = '{Params[0]}'";
            SQLiteCommand Cmd = new SQLiteCommand(SQL, this._DbService.Conn);
            SQLiteDataReader Reader = Cmd.ExecuteReader();

            long rID = 0;

            if(Reader.HasRows)
            {
                Reader.Read();
                rID = Reader.GetInt64(0);
            }
            else
            {
                SQL = $@"INSERT INTO reactions (reaction_text, gID) VALUES('{Params[0]}', {Context.Guild.Id});";
                Cmd = new SQLiteCommand(SQL, this._DbService.Conn);
                Cmd.ExecuteNonQuery();
                SQL = @"select last_insert_rowid()";
                Cmd = new SQLiteCommand(SQL, this._DbService.Conn);
                rID = (long)Cmd.ExecuteScalar();
            }

            SQL = $@"INSERT INTO reaction_roles (rmID, rID, [role], DOEOM) SELECT rm.rmID, {rID}, '{Role}', {Support.UnixTimeStamp()} FROM reaction_messages rm WHERE rm.messageID = {Params[1]}";
            Cmd = new SQLiteCommand(SQL, this._DbService.Conn);
            Cmd.ExecuteNonQuery();
            this._DbService.Conn.Close();

            await Context.Message.DeleteAsync();
            await (MainMsg as SocketUserMessage).AddReactionAsync(new Emoji(Params[0]));
        }
    }
}
