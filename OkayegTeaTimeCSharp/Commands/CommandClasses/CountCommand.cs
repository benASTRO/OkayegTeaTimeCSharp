﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OkayegTeaTimeCSharp.Commands.CommandEnums;
using OkayegTeaTimeCSharp.Utils;
using OkayegTeaTimeCSharp.Twitch;
using OkayegTeaTimeCSharp.Twitch.Bot;
using TwitchLib.Client.Models;

namespace OkayegTeaTimeCSharp.Commands.CommandClasses
{
    public static class CountCommand
    {
        public const CommandType Type = CommandType.Count;

        public static void Handle(TwitchBot twitchBot, ChatMessage chatMessage, string alias)
        {
            if (chatMessage.GetMessage().IsMatch(PatternCreator.CreateBoth(alias, @"\se(mote)?\s\S+")))
            {
#warning not finished
            }
            else if (chatMessage.GetMessage().IsMatch(PatternCreator.CreateBoth(alias, @"\s#\w+")))
            {
#warning not finished
            }
            else if (chatMessage.GetMessage().IsMatch(PatternCreator.CreateBoth(alias, @"\s\w+")))
            {
#warning not finished
            }
            else if (chatMessage.GetMessage().IsMatch(PatternCreator.CreateBoth(alias, @"\s-users")))
            {
#warning not finished
            }
            else if (chatMessage.GetMessage().IsMatch(PatternCreator.CreateBoth(alias, @"$")))
            {
#warning not finished
            }
        }
    }
}