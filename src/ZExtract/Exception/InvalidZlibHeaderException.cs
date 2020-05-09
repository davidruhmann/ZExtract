using System;

namespace ZExtract
{
	[System.Serializable]
	public class InvalidZlibHeaderException : ZExtractException
	{
		public InvalidZlibHeaderException() { }
		public InvalidZlibHeaderException(string message) : base(message) { }
		public InvalidZlibHeaderException(string message, Exception inner) : base(message, inner) { }
	}
}
