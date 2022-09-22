﻿using System;
using System.Text;
using System.Text.RegularExpressions;
using HLE;

namespace OkayegTeaTime.Utils;

public static class StringHelper
{
    private static readonly Regex _onlyWhitespacesPattern = new(@"^\s+$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));

    public static string Antiping(this string value)
    {
        return value.Insert(value.Length >> 1, "󠀀");
    }

    public static string NewLinesToSpaces(this string value)
    {
        return value.Remove("\r").Replace('\n', ' ');
    }

    public static bool IsNullOrEmptyOrWhitespace(this string? value)
    {
        return value is null || value.Length == 0 || _onlyWhitespacesPattern.IsMatch(value);
    }

    public static string Format(this TimeSpan span)
    {
        StringBuilder builder = new();
        if (span.Days > 0)
        {
            builder.Append($"{span.Days}d");
        }

        if (span.Hours > 0)
        {
            builder.Append(builder.Length > 0 ? $", {span.Hours}h" : $"{span.Hours}h");
        }

        if (span.Minutes > 0)
        {
            builder.Append(builder.Length > 0 ? $", {span.Minutes}min" : $"{span.Minutes}min");
        }

        if (span.Seconds > 0)
        {
            builder.Append(builder.Length > 0 ? $", {span.Seconds}s" : $"{span.Seconds}s");
        }

        if (builder.Length == 0)
        {
            builder.Append("<1s");
        }

        return builder.ToString();
    }
}
