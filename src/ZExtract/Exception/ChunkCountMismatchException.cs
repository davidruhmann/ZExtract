using System;

namespace ZExtract
{
	[System.Serializable]
	public class ChunkCountMismatchException : ZExtractException
	{
		public ChunkCountMismatchException() { }
		public ChunkCountMismatchException(string message) : base(message) { }
		public ChunkCountMismatchException(string message, Exception inner) : base(message, inner) { }
	}
}
