using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ZExtractCLITest
{
	[TestClass]
	public class ProgramTests
	{
		[TestMethod]
		[TestCategory("Unit")]
		public void Main_DisplayHelp()
		{
			using (var writer = new StringWriter())
			{
				Console.SetOut(writer);

				var args = new string[] { "help", null };
				ZExtractCLI.Program.Main(args);

				Assert.AreEqual($"Usage: zextract [source] [destination]{Environment.NewLine}", writer.ToString());
			}
		}
	}
}
