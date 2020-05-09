using System;

namespace ZExtract
{
	[System.Serializable]
	public class InvalidSignatureException : ZExtractException
	{
		public InvalidSignatureException() { }
		public InvalidSignatureException(string message) : base(message) { }
		public InvalidSignatureException(string message, Exception inner) : base(message, inner) { }
	}
}
