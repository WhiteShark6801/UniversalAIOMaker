// InfFileEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// InfFileEditor
using System;
using System.IO;
using System.Text;

public class InfFileEditor
{
	private static int verbosity = 1;

	private static int InfEdit(string[] args)
	{
		string text = null;
		string text2 = null;
		string text3 = null;
		string valueToSet = null;
		string outputEncoding = null;
		bool flag = false;
		try
		{
			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
				case "-i":
					if (i + 1 < args.Length)
					{
						text = args[++i];
					}
					break;
				case "-m":
					if (i + 1 < args.Length)
					{
						text2 = args[++i];
					}
					break;
				case "-n":
					if (i + 1 < args.Length)
					{
						text3 = args[++i];
					}
					break;
				case "-d":
					if (i + 1 < args.Length)
					{
						valueToSet = args[++i];
					}
					break;
				case "-c":
					if (i + 1 < args.Length)
					{
						outputEncoding = args[++i];
					}
					break;
				case "-h":
					flag = true;
					break;
				case "-v":
					if (i + 1 < args.Length)
					{
						verbosity = int.Parse(args[++i]);
					}
					break;
				}
			}
			if (flag)
			{
				ShowHelp();
				return 0;
			}
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text2) || string.IsNullOrEmpty(text3))
			{
				Log("Error: -i, -m, and -n parameters are required.", 0);
				ShowHelp();
				return 1;
			}
			if (text2 != "readonly" && text2 != "readwrite")
			{
				Log("Error: Invalid mode '" + text2 + "'. Valid options are 'readonly' or 'readwrite'.", 0);
				return 1;
			}
			return (!ProcessInfFile(text, text2, text3, valueToSet, outputEncoding)) ? 1 : 0;
		}
		catch (Exception ex)
		{
			Log("Error: " + ex.Message, 0);
			if (verbosity >= 2)
			{
				Log("Stack trace: " + ex.StackTrace, 2);
			}
			return 1;
		}
	}

	private static bool ProcessInfFile(string filePath, string mode, string valueName, string valueToSet, string outputEncoding)
	{
		if (!File.Exists(filePath))
		{
			Log("Error: File not found: " + filePath, 0);
			return false;
		}
		try
		{
			DateTime lastWriteTime = File.GetLastWriteTime(filePath);
			string text;
			Encoding encoding;
			using (StreamReader streamReader = new StreamReader(filePath, detectEncodingFromByteOrderMarks: true))
			{
				text = streamReader.ReadToEnd();
				encoding = streamReader.CurrentEncoding;
			}
			if (!string.IsNullOrEmpty(outputEncoding))
			{
				try
				{
					encoding = Encoding.GetEncoding(outputEncoding);
					Log("Using specified output encoding: " + encoding.EncodingName, 1);
				}
				catch (ArgumentException)
				{
					Log("Warning: Unsupported encoding '" + outputEncoding + "'. Keeping original encoding.", 0);
				}
			}
			Log("Processing file: " + filePath, 1);
			Log("Using encoding: " + encoding.EncodingName, 2);
			string text2 = FindActualLine(text, valueName.Trim());
			if (text2 == null)
			{
				Log("Value '" + valueName.Trim() + "' not found in the file.", 0);
				return false;
			}
			string text3 = text2.Substring(0, text2.IndexOf('=')).TrimEnd();
			string text4 = text2.Substring(text2.IndexOf('=') + 1).Trim();
			Log("Found value '" + text3 + "' with current value: " + text4, 1);
			if (mode == "readonly")
			{
				Console.WriteLine(text4);
				return true;
			}
			if (mode == "readwrite")
			{
				if (string.IsNullOrEmpty(valueToSet))
				{
					Log("Error: -d parameter is required in readwrite mode.", 0);
					return false;
				}
				string newValue = text3 + " = " + valueToSet;
				string contents = text.Replace(text2, newValue);
				File.WriteAllText(filePath, contents, encoding);
				File.SetLastWriteTime(filePath, lastWriteTime);
				Log("Value '" + text3 + "' updated from '" + text4 + "' to '" + valueToSet + "'", 1);
				return true;
			}
			return false;
		}
		catch (Exception ex2)
		{
			Log("Error processing file: " + ex2.Message, 0);
			return false;
		}
	}

	private static string FindActualLine(string content, string valueName)
	{
		string[] array = content.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (text.TrimStart().StartsWith(valueName + "=", StringComparison.OrdinalIgnoreCase) || text.TrimStart().StartsWith(valueName + " =", StringComparison.OrdinalIgnoreCase))
			{
				return text;
			}
		}
		return null;
	}

	private static void Log(string message, int requiredVerbosity)
	{
		if (verbosity >= requiredVerbosity)
		{
			Console.WriteLine(message);
		}
	}

	private static void ShowHelp()
	{
		Console.WriteLine("\nInf File Editor - Detect and set values in .inf files\n\nUsage:\n  -i <input inf file>    : Path to the input .inf file (required)\n  -m <mode>              : Operation mode (required) - 'readonly' or 'readwrite'\n  -n <valuename>         : Name of the value to read/modify (required)\n  -d <value to set>      : New value to set (required for readwrite mode)\n  -c <encoding>          : Output file encoding (optional) - e.g. 'us-ascii', 'utf-8'\n  -v <verbosity level>   : 0=silent, 1=basic (default), 2=debugging\n  -h                     : Show this help message\n\nExamples:\n  1. Read a value:\n     InfFileEditor.exe -i input.inf -m readonly -n ProductType\n\n  2. Read a value (with space in name):\n     InfFileEditor.exe -i input.inf -m readonly -n \" ProductType\"\n\n  3. Set a value:\n     InfFileEditor.exe -i input.inf -m readwrite -n ProductType -d 7\n\n  4. Set a value with specific encoding:\n     InfFileEditor.exe -i input.inf -m readwrite -n ProductType -d 7 -c us-ascii\n\nNote: When reading, spaces before the value name are ignored. When writing, the original spacing is preserved.\n");
	}
}
