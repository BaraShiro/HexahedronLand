using System.IO;

public static class StringExtensions
{
    /// <summary>Indicates whether the specified string is <see langword="null" /> or empty.</summary>
    /// <returns>
    /// <see langword="true" /> if this string is <see langword="null" /> or <see cref="F:System.String.Empty" />,
    /// otherwise <see langword="false" />.
    /// </returns>
    public static bool IsNullOrEmpty(this string s)
    {
        return string.IsNullOrEmpty(s);
    }

    /// <summary>Indicates whether the specified string is <see langword="null" /> or empty.</summary>
    /// <returns>
    /// <see langword="false" /> if this string is <see langword="null" /> or <see cref="F:System.String.Empty" />,
    /// otherwise <see langword="true" />.
    /// </returns>
    public static bool IsNotNullOrEmpty(this string s)
    {
        return !string.IsNullOrEmpty(s);
    }

    /// <summary>
    /// Indicates whether a specified string is <see langword="null" />,
    /// empty, or consists only of white-space characters.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if this string is <see langword="null" /> or <see cref="F:System.String.Empty" />,
    /// or consists exclusively of white-space characters, otherwise <see langword="false" />.
    /// </returns>
    public static bool IsNullOrWhiteSpace(this string s)
    {
        return string.IsNullOrWhiteSpace(s);
    }

    /// <summary>
    /// Indicates whether a specified string is <see langword="null" />,
    /// empty, or consists only of white-space characters.
    /// </summary>
    /// <returns>
    /// <see langword="false" /> if this string is <see langword="null" /> or <see cref="F:System.String.Empty" />,
    /// or consists exclusively of white-space characters, otherwise <see langword="true" />.
    /// </returns>
    public static bool IsNotNullOrWhiteSpace(this string s)
    {
        return !string.IsNullOrWhiteSpace(s);
    }

    /// <summary>
    /// Returns a copy of this <see cref="T:System.String" /> object with all characters that are not allowed in
    /// path names removed.
    /// </summary>
    /// <param name="pathname">The string to remove disallowed characters from.</param>
    /// <returns>
    /// A string that consists of <paramref name="pathname"/> without characters that are not allowed in path names.
    /// </returns>
    ///
    public static string RemoveInvalidPathChars(this string pathname)
    {
        return string.Concat(pathname.Split(Path.GetInvalidPathChars()));
    }

    /// <summary>
    /// Returns a copy of this <see cref="T:System.String" /> object with all characters that are not allowed in
    /// path names replaced with a new value.
    /// </summary>
    /// <param name="pathname">The string to replace disallowed characters in.</param>
    /// <param name="replaceWith">A string that will replace all disallowed characters.</param>
    /// <returns>
    /// A string that consists of <paramref name="pathname"/> with characters that are not allowed in
    /// path names replaced with <paramref name="replaceWith"/>.
    /// </returns>
    public static string ReplaceInvalidPathChars(this string pathname, string replaceWith)
    {

        return string.Join(replaceWith, pathname.Split(Path.GetInvalidPathChars()));
    }

    /// <summary>
    /// Returns a copy of this <see cref="T:System.String" /> object with all characters that are not allowed in
    /// file names removed.
    /// </summary>
    /// <param name="filename">The string to remove disallowed characters from.</param>
    /// <returns>
    /// A string that consists of <paramref name="filename"/> without characters that are not allowed in file names.
    /// </returns>
    public static string RemoveInvalidFileNameChars(this string filename)
    {
        return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
    }

    /// <summary>
    /// Returns a copy of this <see cref="T:System.String" /> object with all characters that are not allowed in
    /// file names replaced with a new value.
    /// </summary>
    /// <param name="filename">The string to replace disallowed characters in.</param>
    /// <param name="replaceWith">A string that will replace all disallowed characters.</param>
    /// <returns>
    /// A string that consists of <paramref name="filename"/> with characters that are not allowed in
    /// file names replaced with <paramref name="replaceWith"/>.
    /// </returns>
    public static string ReplaceInvalidFileNameChars(this string filename, string replaceWith)
    {

        return string.Join(replaceWith, filename.Split(Path.GetInvalidFileNameChars()));
    }
}