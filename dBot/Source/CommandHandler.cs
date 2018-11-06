using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace DBot.Source
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient Client;


        public CommandHandler(DiscordSocketClient dc)
        {
            this.Client = dc;

        }

    }
}
