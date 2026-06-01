using System;
using System.Collections.Generic;
using System.IO;

public static class GameLaunchVfs
{
    public static string ResolveRootDirectoryPath(string vfsUri)
    {
        if (string.IsNullOrWhiteSpace(vfsUri))
        {
            throw new ArgumentException("assets.vfsUri is required.");
        }

        Uri uri;
        try
        {
            uri = new Uri(vfsUri);
        }
        catch (Exception e)
        {
            throw new ArgumentException($"assets.vfsUri is not a valid URI: {e.Message}");
        }

        if (!uri.IsAbsoluteUri || !uri.IsFile || uri.IsUnc || !string.IsNullOrEmpty(uri.Host))
        {
            throw new ArgumentException("assets.vfsUri must be a local file:// directory URI.");
        }

        var path = Uri.UnescapeDataString(uri.LocalPath);
        var fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath))
        {
            throw new ArgumentException("assets.vfsUri root must be a directory, not a file.");
        }

        return EnsureTrailingSeparator(fullPath);
    }

    public static string ResolveRequiredFilePath(string rootDirectory, string assetPath, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            throw new ArgumentException($"{fieldName} is required.");
        }

        return ResolveFilePath(rootDirectory, assetPath, fieldName);
    }

    public static string ResolveOptionalFilePath(string rootDirectory, string assetPath, string fieldName)
    {
        return string.IsNullOrWhiteSpace(assetPath) ? null : ResolveFilePath(rootDirectory, assetPath, fieldName);
    }

    public static string ResolveRequiredFileUri(string rootDirectory, string assetPath, string fieldName)
    {
        return ToFileUri(ResolveRequiredFilePath(rootDirectory, assetPath, fieldName));
    }

    public static string ResolveOptionalFileUri(string rootDirectory, string assetPath, string fieldName)
    {
        var path = ResolveOptionalFilePath(rootDirectory, assetPath, fieldName);
        return path == null ? null : ToFileUri(path);
    }

    public static string ToFileUri(string path)
    {
        return new Uri(Path.GetFullPath(path)).AbsoluteUri;
    }

    private static string ResolveFilePath(string rootDirectory, string assetPath, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("VFS root directory is required.");
        }

        var root = EnsureTrailingSeparator(Path.GetFullPath(rootDirectory));
        var relative = NormalizeRelativePath(assetPath, fieldName);
        var target = Path.GetFullPath(Path.Combine(root, relative));

        if (!IsInsideRoot(root, target))
        {
            throw new ArgumentException($"{fieldName} escapes the VFS root.");
        }

        return target;
    }

    private static string NormalizeRelativePath(string assetPath, string fieldName)
    {
        if (assetPath.IndexOf('\0') >= 0)
        {
            throw new ArgumentException($"{fieldName} contains a NUL character.");
        }

        if (assetPath.StartsWith(@"\\", StringComparison.Ordinal) ||
            assetPath.StartsWith("//", StringComparison.Ordinal))
        {
            throw new ArgumentException($"{fieldName} must not be a UNC path.");
        }

        var normalized = assetPath.Replace('\\', '/');
        while (normalized.StartsWith("/", StringComparison.Ordinal))
        {
            normalized = normalized.Substring(1);
        }

        if (HasUriScheme(normalized))
        {
            throw new ArgumentException($"{fieldName} must be a VFS-relative path, not a URI or absolute path.");
        }

        var parts = new List<string>();
        foreach (var part in normalized.Split('/'))
        {
            if (part.Length == 0 || part == ".")
            {
                continue;
            }

            if (part == "..")
            {
                throw new ArgumentException($"{fieldName} must not contain '..' path segments.");
            }

            parts.Add(part);
        }

        if (parts.Count == 0)
        {
            throw new ArgumentException($"{fieldName} must point to a file inside the VFS root.");
        }

        return Path.Combine(parts.ToArray());
    }

    private static bool HasUriScheme(string path)
    {
        var colonIndex = path.IndexOf(':');
        if (colonIndex <= 0)
        {
            return false;
        }

        for (var i = 0; i < colonIndex; i++)
        {
            var c = path[i];
            var valid = i == 0
                ? char.IsLetter(c)
                : char.IsLetterOrDigit(c) || c == '+' || c == '-' || c == '.';
            if (!valid)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsInsideRoot(string root, string target)
    {
        var comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return target.StartsWith(root, comparison);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
            path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            return path;
        }

        return path + Path.DirectorySeparatorChar;
    }
}
