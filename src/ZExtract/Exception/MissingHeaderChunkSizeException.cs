using System;

namespace ZExtract
{
	[System.Serializable]
	public class MissingHeaderChunkSizeException : ZExtractException
	{
		public MissingHeaderChunkSizeException() { }
		public MissingHeaderChunkSizeException(string message) : base(message) { }
		public MissingHeaderChunkSizeException(string message, Exception inner) : base(message, inner) { }
	}
}
