﻿using HLE.Time;
using OkayegTeaTime.Files;

#pragma warning disable CS0659

namespace OkayegTeaTime.Twitch.Models;

public sealed class AfkCooldown
{
    public long UserId { get; }

    public long Time { get; }

    public AfkCooldown(long userId)
    {
        UserId = userId;
        Time = TimeHelper.Now() + AppSettings.AfkCooldown;
    }

    public override bool Equals(object? obj)
    {
        return obj is AfkCooldown c && c.UserId == UserId;
    }
}
