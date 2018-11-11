using Discord;
using Discord.Commands;
using Discord.Rest;
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

        private List<long> ReactionMessages;

        public ReactionRoleCommands(DiscordSocketClient Client)
        {
            this._SocketClient = Client;

            using (SQLiteConnection conn = new SQLiteConnection(Support.DbConnectionString))
            {
                conn.Open();
                SQLiteCommand Cmd = new SQLiteCommand("SELECT * FROM reaction_messages", conn);
                SQLiteDataReader Reader = Cmd.ExecuteReader();

                this.ReactionMessages = new List<long>();

                while (Reader.Read())
                {
                    this.ReactionMessages.Add(Reader.GetInt64(0));
                }

                conn.Close();
            }

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

            using (SQLiteConnection conn = new SQLiteConnection(Support.DbConnectionString))
            {

                conn.Open();
                string SQL = $@"INSERT INTO reaction_messages (message, messageID, DOEOM) VALUES ('{Message}', {(long)Answer.Id}, {Support.UnixTimeStamp()})";
                SQLiteCommand Cmd = new SQLiteCommand(SQL, conn);
                Cmd.ExecuteNonQuery();
                Cmd.Dispose();
                conn.Close();
            }
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
            string[] CustomeReaction = Params[0].Replace("<:", "").Replace(">", "").Split(":");

            if (!Context.Guild.Roles.Any(R => R.Name == Role))
            {
                await Context.Channel.SendMessageAsync($"The role <{Role}> doesn't exist. Please specify a correct role.");
                await Context.Message.DeleteAsync();
                return;
            }

            IMessage MainMsg = await Context.Channel.GetMessageAsync(ulong.Parse(Params[1]));
            string foo = MainMsg.GetType().ToString();

            try
            {
                SocketUserMessage SocketMsg = (SocketUserMessage)await Context.Channel.GetMessageAsync(ulong.Parse(Params[1]));

                IEmote e;

                if (Params[0].StartsWith('<'))
                {
                    e = Context.Guild.Emotes.FirstOrDefault(E => E.Name == CustomeReaction[0] && E.Id.ToString() == CustomeReaction[1]);
                }
                else
                {
                    e = new Emoji(Params[0]);
                }

                await ((SocketUserMessage)SocketMsg).AddReactionAsync(e);
            }
            catch (Exception exc)
            {
                try
                {
                    RestUserMessage RestMsg = (RestUserMessage)await Context.Channel.GetMessageAsync(ulong.Parse(Params[1]));


                    IEmote e;

                    if (Params[0].StartsWith('<'))
                    {
                        e = Context.Guild.Emotes.FirstOrDefault(E => E.Name == CustomeReaction[0] && E.Id.ToString() == CustomeReaction[1]);
                    }
                    else
                    {
                        e = new Emoji(Params[0]);
                    }

                    await ((RestUserMessage)RestMsg).AddReactionAsync(e);
                }
                catch (Exception)
                {
                    Support.Log($"Could not add reaction <{Params[0]}> to base message <{Params[1]}>");
                    return;
                }
            }


            if (MainMsg == null)
            {
                await Context.Channel.SendMessageAsync($"The base message <{Params[1]}> doesn't exist. Please specify a correct base message.");
                await Context.Message.DeleteAsync();
                return;
            }
            
            long rID = 0;

            using (SQLiteConnection conn = new SQLiteConnection(Support.DbConnectionString))
            {

                conn.Open();
                string SQL = $@"SELECT rID FROM reactions WHERE gID = {Context.Guild.Id} AND reaction_text = '{Params[0]}'";
                SQLiteCommand Cmd = new SQLiteCommand(SQL, conn);
                SQLiteDataReader Reader = Cmd.ExecuteReader();


                if (Reader.HasRows)
                {
                    Reader.Read();
                    rID = Reader.GetInt64(0);
                    Reader.Close();
                    Reader = null;
                    Cmd.Dispose();
                }
                else
                {
                    Cmd.Dispose();
                    SQL = $@"INSERT INTO reactions (reaction_text, gID) VALUES('{Params[0]}', {Context.Guild.Id});";
                    SQLiteCommand Cmd2 = new SQLiteCommand(SQL, conn);
                    Cmd2.ExecuteNonQuery();
                    Cmd2.Dispose();
                    SQL = @"select last_insert_rowid()";
                    SQLiteCommand Cmd3 = new SQLiteCommand(SQL, conn);
                    rID = (long)Cmd3.ExecuteScalar();
                    Cmd3.Dispose();
                }

                Cmd.Dispose();
                conn.Close();
            }

            using (SQLiteConnection conn = new SQLiteConnection(Support.DbConnectionString))
            {
                conn.Open();
                
                string SQL = $@"INSERT INTO reaction_roles (rmID, rID, [role], DOEOM) SELECT rm.rmID, {rID}, '{Role}', {Support.UnixTimeStamp()} FROM reaction_messages rm WHERE rm.messageID = {Params[1]}";
                SQLiteCommand Cmd = new SQLiteCommand(SQL, conn);
                Cmd.ExecuteNonQuery();
                Cmd.Dispose();
                conn.Close();
            }

            await Context.Message.DeleteAsync();
        }
    }
}
