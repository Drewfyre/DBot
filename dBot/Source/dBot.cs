using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DBot.Source.Modules.ReactionRoles;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace DBot.Source
{
    public class DBot
    {
        private BotCredentials _Credentials { get; }
        private DiscordSocketClient _Client { get; }
        private CommandService _CommandService { get; }
        private IServiceProvider _ServiceProvider;

        public DBot()
        {
            this._Credentials = new BotCredentials();

            this._Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = false,
                ConnectionTimeout = int.MaxValue,
                LogLevel = LogSeverity.Warning,
                MessageCacheSize = 50,
                DefaultRetryMode = RetryMode.AlwaysRetry,
            });

            this._CommandService = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Sync,
            });

            this._ServiceProvider
                = new ServiceCollection()
                .AddSingleton(this._Client)
                .AddSingleton(this._CommandService)
                .BuildServiceProvider();

        }

        public async Task RunAsync()
        {
            await this.LoginAsync().ConfigureAwait(false);
        }

        public async Task RunAndBlockAsync()
        {
            await this.RegisterCommandsAsync();
            await RunAsync().ConfigureAwait(false);
            await Task.Delay(-1).ConfigureAwait(false);
        }

        private async Task LoginAsync()
        {
            Support.Log($"Logging in as: {this._Credentials.ClientID}...");
            await this._Client.LoginAsync(TokenType.Bot, this._Credentials.Token).ConfigureAwait(false);
            await this._Client.StartAsync().ConfigureAwait(false);

            Support.Log("Successfully logged in!");
            this._Client.JoinedGuild += Client_JoinedGuild;
            this._Client.LeftGuild += Client_LeftGuild;
            this._Client.ReactionAdded += _SocketClient_ReactionAdded;
            this._Client.ReactionRemoved += _SocketClient_ReactionRemoved;
        }

        private async Task RegisterCommandsAsync()
        {
            this._Client.MessageReceived += HandleCommandAsync;

            await this._CommandService.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;

            if (message is null || message.Author.IsBot) return;

            int argPos = 0;

            if (message.HasStringPrefix("!", ref argPos) || message.HasMentionPrefix(this._Client.CurrentUser, ref argPos))
            {
                var context = new SocketCommandContext(this._Client, message);

                var result = await this._CommandService.ExecuteAsync(context, argPos, this._ServiceProvider);
                

                if (!result.IsSuccess)
                    Console.WriteLine(result.ErrorReason);
            }
        }

        private Task Client_LeftGuild(SocketGuild arg)
        {
            Support.Log($"Left server {arg.Name}");

            return null;
        }

        private async Task Client_JoinedGuild(SocketGuild arg)
        {
            Support.Log($"Joined server {arg.Name}");


            return;
        }

        private async Task _SocketClient_ReactionRemoved(Discord.Cacheable<Discord.IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            ulong MainMsgID = arg3.MessageId;
            ReactionRoleCommands Rc = new ReactionRoleCommands(this._Client);

            string SQL = $@"select [role] FROM reaction_roles 
                            WHERE rmID = (SELECT rmID FROM reaction_messages WHERE messageID = {MainMsgID})
                            AND rID = (SELECT rID FROM reactions WHERE reaction_text = '{arg3.Emote.Name}')";
            SQLiteCommand Cmd = new SQLiteCommand(SQL, Rc._DbService.Conn);
            Rc._DbService.Conn.Open();
            SQLiteDataReader Reader = Cmd.ExecuteReader();

            if (Reader.HasRows)
            {
                Reader.Read();

                var Chann = arg3.Channel as SocketGuildChannel;
                var Guild = Chann.Guild;
                var Role = Guild.Roles.FirstOrDefault(x => x.Name == Reader.GetString(0));
                if (Role != default(SocketRole))
                {
                    var user = arg3.User.Value;
                    await (user as IGuildUser).RemoveRoleAsync(Role);
                }
            }

            Rc._DbService.Conn.Close();
        }

        private async Task _SocketClient_ReactionAdded(Discord.Cacheable<Discord.IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            if(arg3.UserId == this._Client.CurrentUser.Id
                || arg3.User.Value.IsBot == true)
            {
                return;
            }

            ulong MainMsgID = arg3.MessageId;
            ReactionRoleCommands Rc = new ReactionRoleCommands(this._Client);

            string SQL = $@"select [role] FROM reaction_roles 
                            WHERE rmID = (SELECT rmID FROM reaction_messages WHERE messageID = {MainMsgID})
                            AND rID = (SELECT rID FROM reactions WHERE reaction_text = '{arg3.Emote.Name}')";
            SQLiteCommand Cmd = new SQLiteCommand(SQL, Rc._DbService.Conn);
            Rc._DbService.Conn.Open();
            SQLiteDataReader Reader = Cmd.ExecuteReader();

            if (Reader.HasRows)
            {
                Reader.Read();

                var Chann = arg3.Channel as SocketGuildChannel;
                var Guild = Chann.Guild;
                var Role = Guild.Roles.FirstOrDefault(x => x.Name == Reader.GetString(0));
                if (Role != default(SocketRole))
                {
                    var user = arg3.User.Value;
                    await (user as IGuildUser).AddRoleAsync(Role);
                }
            }

            Rc._DbService.Conn.Close();
        }
    }
}
