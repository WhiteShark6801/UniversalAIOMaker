using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

internal class HexEdit
{
    private static void HexEdit2(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return;
        }

        try
        {
            ProcessArguments(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            Environment.Exit(1);
        }
    }

    private static void ProcessArguments(string[] args)
    {
        bool createBackup = true;
        Encoding encoding = null;
        bool mixedMode = false;

        string searchText = null;
        string replaceText = null;
        string filename = null;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            switch (arg)
            {
                case "-n":
                    createBackup = false;
                    continue;

                case "-a":
                    EnsureSingleMode(encoding, mixedMode);
                    encoding = Encoding.ASCII;
                    break;

                case "-u":
                    EnsureSingleMode(encoding, mixedMode);
                    encoding = Encoding.Unicode;
                    break;

                case "-m":
                    EnsureSingleMode(encoding, mixedMode);
                    mixedMode = true;
                    break;

                default:
                    if (searchText == null)
                    {
                        searchText = arg;

                        if (i + 1 >= args.Length || args[i + 1].StartsWith("-"))
                            throw new ArgumentException("Missing replace pattern");

                        replaceText = args[++i];
                    }
                    else if (filename == null)
                    {
                        filename = arg;
                    }
                    break;
            }
        }

        if (searchText == null || replaceText == null || filename == null)
        {
            ShowHelp();
            return;
        }

        if (!File.Exists(filename))
        {
            Console.WriteLine($"Error: Input file '{filename}' not found");
            return;
        }

        byte[] searchBytes;
        byte[] replaceBytes;

        if (mixedMode)
        {
            searchBytes = ParseMixedString(searchText);
            replaceBytes = ParseMixedString(replaceText);

            Console.WriteLine($"Search mixed '{searchText}' → {BytesToHexString(searchBytes)}");
            Console.WriteLine($"Replace mixed '{replaceText}' → {BytesToHexString(replaceBytes)}");
        }
        else if (encoding != null)
        {
            searchBytes = encoding.GetBytes(searchText);
            replaceBytes = encoding.GetBytes(replaceText);

            Console.WriteLine($"Search text '{searchText}' converted to hex: {BytesToHexString(searchBytes)}");
            Console.WriteLine($"Replace text '{replaceText}' converted to hex: {BytesToHexString(replaceBytes)}");
        }
        else
        {
            searchBytes = ParseHexString(searchText);
            replaceBytes = ParseHexString(replaceText);
        }

        if (searchBytes.Length != replaceBytes.Length)
        {
            Console.WriteLine($"Warning: Length mismatch ({searchBytes.Length} vs {replaceBytes.Length}). Operation cancelled.");
            return;
        }

        ReplaceInFile(filename, searchBytes, replaceBytes, createBackup);
        Console.WriteLine("File successfully modified.");
    }

    private static void EnsureSingleMode(Encoding encoding, bool mixedMode)
    {
        if (encoding != null || mixedMode)
            throw new ArgumentException("Cannot specify multiple encoding modes (-a, -u, -m)");
    }

    private static byte[] ParseMixedString(string input)
    {
        var result = new List<byte>();
        string[] parts = input.Split(':');

        if (parts.Length == 0)
            throw new ArgumentException("Invalid mixed string");

        // First part = ASCII string
        result.AddRange(Encoding.ASCII.GetBytes(parts[0]));

        // Remaining parts must be xHH
        for (int i = 1; i < parts.Length; i++)
        {
            string part = parts[i];

            if (!part.StartsWith("x", StringComparison.OrdinalIgnoreCase) || part.Length != 3)
                throw new ArgumentException($"Invalid mixed hex token: {part}. Use format xHH");

            string hex = part.Substring(1);

            if (!byte.TryParse(hex, NumberStyles.HexNumber, null, out byte value))
                throw new ArgumentException($"Invalid hex byte: {hex}");

            result.Add(value);
        }

        return result.ToArray();
    }

    private static byte[] ParseHexString(string hexString)
    {
        string clean = hexString.Replace(" ", "")
                                .Replace("-", "")
                                .Replace(":", "");

        if (clean.Length % 2 != 0)
            throw new ArgumentException("Hex string must contain an even number of characters.");

        byte[] bytes = new byte[clean.Length / 2];

        for (int i = 0; i < bytes.Length; i++)
        {
            string byteStr = clean.Substring(i * 2, 2);

            if (!byte.TryParse(byteStr, NumberStyles.HexNumber, null, out bytes[i]))
                throw new ArgumentException($"Invalid hex byte: {byteStr}");
        }

        return bytes;
    }

    private static string BytesToHexString(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", " ");
    }

    private static void ReplaceInFile(string filename, byte[] searchBytes, byte[] replaceBytes, bool createBackup)
    {
        byte[] fileBytes = File.ReadAllBytes(filename);
        List<int> matches = new List<int>();

        for (int i = 0; i <= fileBytes.Length - searchBytes.Length; i++)
        {
            bool found = true;

            for (int j = 0; j < searchBytes.Length; j++)
            {
                if (fileBytes[i + j] != searchBytes[j])
                {
                    found = false;
                    break;
                }
            }

            if (found)
                matches.Add(i);
        }

        if (matches.Count == 0)
        {
            Console.WriteLine("No matches found.");
            return;
        }

        if (createBackup)
        {
            string backup = filename + ".bak";
            File.Copy(filename, backup, true);
            Console.WriteLine("Backup created: " + backup);
        }

        foreach (int pos in matches)
        {
            for (int i = 0; i < replaceBytes.Length; i++)
                fileBytes[pos + i] = replaceBytes[i];
        }

        Console.WriteLine($"Replaced {matches.Count} occurrence(s) at: {string.Join(", ", matches)}");

        File.WriteAllBytes(filename, fileBytes);
    }

    private static void ShowHelp()
    {
        Console.WriteLine(
@"HexEdit - File Hex Editing Utility

Usage:
  hexedit ""48 65 6C 6C 6F"" ""57 6F 72 6C 64"" file.ext [-n]
  hexedit -a ""Hello"" ""World"" file.ext [-n]
  hexedit -u ""Hello"" ""World"" file.ext [-n]
  hexedit -m ""Hello:x5C:x00"" ""World:x30:x00"" file.ext [-n]

Options:
  -a    ASCII text converted to hex
  -u    UTF-16 text converted to hex
  -m    Mixed mode (ASCII text + xHH hex suffix bytes)
  -n    Do not create backup

Notes:
  - Mixed format: First part is ASCII, additional parts must be xHH
  - Search and replace patterns must be same length
  - Backup created unless -n specified
");
    }
}
