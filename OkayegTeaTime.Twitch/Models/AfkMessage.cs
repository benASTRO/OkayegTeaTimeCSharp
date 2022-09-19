﻿using HLE;
using HLE.Time;
using OkayegTeaTime.Database.Models;
using OkayegTeaTime.Files.Models;

namespace OkayegTeaTime.Twitch.Models;

public sealed class AfkMessage
{
    public string ComingBack
    {
        get
        {
            if (_comingBack is not null)
            {
                return _comingBack;
            }

            _comingBack = _afkCommand.ComingBack.Replace("{username}", _user.Username).Replace("{time}", $"{TimeHelper.GetUnixDifference(_user.AfkTime)} ago").Replace("{message}", _user.AfkMessage);
            _comingBack = string.IsNullOrEmpty(_user.AfkMessage) ? _comingBack.Remove(":").TrimAll() : _comingBack;
            return _comingBack;
        }
    }

    public string GoingAway => _goingAway ??= _afkCommand.GoingAway.Replace("{username}", _user.Username);

    public string Resuming => _resuming ??= _afkCommand.Resuming.Replace("{username}", _user.Username);

    private readonly User _user;
    private readonly AfkCommand _afkCommand;
    private string? _comingBack;
    private string? _goingAway;
    private string? _resuming;

    public AfkMessage(User user, AfkCommand cmd)
    {
        _user = user;
        _afkCommand = cmd;
    }
}
