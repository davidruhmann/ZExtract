using System;

namespace ZExtract
{
	[System.Serializable]
	public class ZExtractException : Exception
	{
		public ZExtractException() { }
		public ZExtractException(string message) : base(message) { }
		public ZExtractException(string message, Exception inner) : base(message, inner) { }
	}
}
