using System;
using System.IO;

public static class GameLaunchVfs
{
    /// <summary>
    /// Resolves a directory URI from <c>bridge.play.start</c> assets into a local path suitable for
    /// <see cref="Level.Path"/> (absolute, trailing directory separator).
    /// </summary>
    public static string ResolveDirectoryPath(string vfsUri)
    {
        if (string.IsNullOrWhiteSpace(vfsUri))
        {
            return string.Empty;
        }

        string path;
        if (vfsUri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            path = Uri.UnescapeDataString(new Uri(vfsUri).LocalPath);
        }
        else
        {
            path = vfsUri;
        }

        if (!path.EndsWith(Path.DirectorySeparatorChar.ToString())
            && !path.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
        {
            path += Path.DirectorySeparatorChar;
        }

        return path;
    }

    public static string ResolveFileUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return null;
        }

        if (uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            return uri;
        }

        return new Uri(Path.GetFullPath(uri)).AbsoluteUri;
    }

    public static string ResolveFileUri(string vfsUri, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(vfsUri) || string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var directory = ResolveDirectoryPath(vfsUri);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        return ResolveFileUri(Path.Combine(directory, relativePath));
    }

    public static string ResolveFilePath(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return null;
        }

        if (uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            return Uri.UnescapeDataString(new Uri(uri).LocalPath);
        }

        return uri;
    }
}
