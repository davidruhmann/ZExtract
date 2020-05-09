using System;

namespace ZExtract
{
	[System.Serializable]
	public class SizeMismatchException : ZExtractException
	{
		public SizeMismatchException() { }
		public SizeMismatchException(string message) : base(message) { }
		public SizeMismatchException(string message, Exception inner) : base(message, inner) { }
	}
}
