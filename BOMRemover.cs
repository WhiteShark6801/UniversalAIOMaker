// BOMRemover, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BOMRemover
using System;
using System.IO;
using System.Linq;
using System.Text;

internal class BOMRemover
{
	private static bool verbose;

	private static bool createBackup;

	private static void BOMRemove(string[] args)
	{
		string text = null;
		for (int i = 0; i < args.Length; i++)
		{
			switch (args[i])
			{
			case "-i":
				if (i + 1 < args.Length)
				{
					text = args[i + 1];
					i++;
				}
				break;
			case "-v":
				verbose = true;
				break;
			case "-b":
				createBackup = true;
				break;
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			Console.WriteLine("Usage: BOMRemover -i <input file> [-v] [-b]");
			Console.WriteLine("Options:");
			Console.WriteLine("  -i <input file>  Specify input file");
			Console.WriteLine("  -v              Verbose output");
			Console.WriteLine("  -b              Create backup file");
			return;
		}
		if (!File.Exists(text))
		{
			Console.WriteLine("Error: File '" + text + "' not found.");
			return;
		}
		try
		{
			ProcessFile(text);
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error: " + ex.Message);
		}
	}

	private static void ProcessFile(string filePath)
	{
		if (verbose)
		{
			Console.WriteLine("Processing file: " + filePath);
		}
		byte[] array = File.ReadAllBytes(filePath);
		Encoding encoding = DetectEncoding(array);
		if (verbose)
		{
			Console.WriteLine("Original encoding: " + encoding.EncodingName);
			DetectSpecialCharacters(array, encoding);
		}
		if (array.Length < 3 || array[0] != 239 || array[1] != 187 || array[2] != 191)
		{
			if (verbose)
			{
				Console.WriteLine("File does not have UTF-8 BOM. No changes made.");
			}
			else
			{
				Console.WriteLine("No BOM found. File unchanged.");
			}
			return;
		}
		if (createBackup)
		{
			string text = filePath + ".bak";
			File.Copy(filePath, text, overwrite: true);
			if (verbose)
			{
				Console.WriteLine("Created backup file: " + text);
			}
		}
		byte[] array2 = new byte[array.Length - 3];
		Array.Copy(array, 3, array2, 0, array2.Length);
		File.WriteAllBytes(filePath, array2);
		if (verbose)
		{
			Console.WriteLine("UTF-8 BOM removed successfully.");
		}
		else
		{
			Console.WriteLine("BOM removed successfully.");
		}
	}

	private static Encoding DetectEncoding(byte[] bytes)
	{
		if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
		{
			return Encoding.UTF8;
		}
		if (bytes.Length >= 2)
		{
			if (bytes[0] == 254 && bytes[1] == byte.MaxValue)
			{
				return Encoding.BigEndianUnicode;
			}
			if (bytes[0] == byte.MaxValue && bytes[1] == 254)
			{
				return Encoding.Unicode;
			}
		}
		return Encoding.UTF8;
	}

	private static void DetectSpecialCharacters(byte[] bytes, Encoding encoding)
	{
		string source = encoding.GetString(bytes);
		bool flag = source.Any((char c) => c >= 'Ѐ' && c <= 'ӿ');
		bool flag2 = source.Any((char c) => (c >= '一' && c <= '鿿') || (c >= '\u3040' && c <= 'ヿ') || (c >= '가' && c <= '\ud7af'));
		bool flag3 = source.Any((char c) => c >= 'Ͱ' && c <= 'Ͽ');
		bool flag4 = source.Any((char c) => c >= '\u0600' && c <= 'ۿ');
		bool flag5 = source.Any((char c) => c >= '\u0590' && c <= '\u05ff');
		Console.WriteLine("Detected character sets:");
		Console.WriteLine("  Cyrillic: " + (flag ? "Yes" : "No"));
		Console.WriteLine("  East Asian: " + (flag2 ? "Yes" : "No"));
		Console.WriteLine("  Greek: " + (flag3 ? "Yes" : "No"));
		Console.WriteLine("  Arabic: " + (flag4 ? "Yes" : "No"));
		Console.WriteLine("  Hebrew: " + (flag5 ? "Yes" : "No"));
	}
}
