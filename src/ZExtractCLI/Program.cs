using System;
using ZExtract;

namespace ZExtractCLI
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var source = args[0];
			if (source == "help")
			{
				DisplayHelp();
				return;
			}
			var destination = args[1];
			ZCompression.Unpack(source, destination);
		}

		static void DisplayHelp()
		{
			Console.WriteLine("Usage: zextract [source] [destination]");
		}
	}
}
