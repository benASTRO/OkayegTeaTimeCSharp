﻿using System.IO;
using HLE.Collections;

namespace OkayegTeaTime.Files;

public static class FileLocator
{
    private static List<string>? _files;

    /// <summary>
    /// Searches for a file in the current and all sub directories and returns the path of it.
    /// </summary>
    /// <exception cref="FileNotFoundException">
    /// Thrown if a file couldn't be found.
    /// </exception>
    public static string Find(string fileName)
    {
        if (_files is null)
        {
            BuildFileCache();
        }

        Regex filePattern = new($@"[\/\\]{fileName}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));
        return _files!.FirstOrDefault(f => filePattern.IsMatch(f)) ?? throw new FileNotFoundException($"The file {fileName} could not be found in the current or any sub directory.");
    }

    private static void BuildFileCache()
    {
        _files = new();
        GetFiles(Directory.GetCurrentDirectory());
    }

    private static void GetFiles(string directory)
    {
        string[] dirs = Directory.GetDirectories(directory);
        dirs.ForEach(GetFiles);

        string[] files = Directory.GetFiles(directory);
        _files?.AddRange(files);
    }
}
