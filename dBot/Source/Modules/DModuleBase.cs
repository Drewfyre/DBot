using DBot.Source.Services;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace DBot.Source.Modules
{
    public class DModuleBase : ModuleBase
    {
        public DModuleBase() : base()
        {

        }
    }

    public class DModuleBase<TService> : DModuleBase where TService : IService
    {
        public TService _Service { get; set; }

        public DModuleBase() : base()
        {

        }
    }
}
