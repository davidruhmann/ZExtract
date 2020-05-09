using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;

namespace ZExtract
{
	public enum CompressionMethod
	{
		Deflate = 8,
		Reserved = 15,
	}

	[Flags]
	public enum CompressionInfo
	{
		// LZ77 Window Size
		Size256 = 0,
		Size512 = 16,
		Size1K = 32,
		Size2K = 48,
		Size4K = 64,
		Size8K = 80,
		Size16K = 96,
		Size32K = 112,
		Reserved = 128,
	}

	public enum CompressionLevel
	{
		Fastest, // No Compression
		Fast,
		Default,
		Maximum, // Slowest
	}

	public struct ZLibHeader
	{
		public byte CMF;
		public byte FLG;

		public CompressionMethod CompressionMethod => (CompressionMethod)(CMF & 0x0F); // CMF (bits 0-3)
		public CompressionInfo CompressionInfo => (CompressionInfo)(CMF & 0xF0); // CMF (bits 4-7)
		public byte CheckFlag => (byte)(FLG & 0x1F); // FLG (bits 0-4)
		public bool CheckPassed => 0 == ((CMF * 256) + FLG) % 31;
		public bool HasDictionary => (FLG & 0x20) == 1; // FLG (bit 5)
		public CompressionLevel CompressionLevel => (CompressionLevel)((FLG & 0xC0) >> 6); // FLG (bit 6-7)

		public bool IsValid =>
			CompressionMethod == CompressionMethod.Deflate
			&& (CompressionInfo & CompressionInfo.Reserved) == 0
			&& CheckPassed;
	}

	public static class ZCompression
	{
		// .z archives are
		// little endian encoded,
		// 8 byte aligned fields,
		// zlib compressed chunks

		// Header
		// Reference[]
		// Data[]

		public const long SwappedSignature = 0x9EC1832A;
		public const long DefaultSignature = 0x9E2A83C1;
		public const long DefaultChunkSize = 0x020000;

		public struct Header
		{
			public long Signature;
			public long UnpackedChunkSize;
			public Reference Summary;
		}

		public struct Reference
		{
			public long PackedSize;
			public long UnpackedSize;
		}

		private static long Swap(long value)
		{
			return BitConverter.ToInt64(BitConverter.GetBytes(value).Reverse().ToArray(), 0);
		}

		public static void Unpack(string source, string destination, bool strict = true)
		{
			if (string.IsNullOrWhiteSpace(source))
			{
				throw new ArgumentNullException("source");
			}
			if (string.IsNullOrWhiteSpace(destination))
			{
				throw new ArgumentNullException("destination");
			}

			// Destination Handling
			var overwriteFile = File.Exists(destination);
			var directoryExists = Directory.Exists(destination);
			var endsInSeparator = destination.EndsWith(Path.DirectorySeparatorChar.ToString());
			if (!overwriteFile && (directoryExists || endsInSeparator))
			{
				if (endsInSeparator && !directoryExists)
				{
					Directory.CreateDirectory(destination);
				}
				var sourceFilename = Path.GetFileNameWithoutExtension(source);
				destination = $"{destination.TrimEnd(Path.DirectorySeparatorChar)}{Path.DirectorySeparatorChar}{sourceFilename}";
			}

			using (var reader = new BinaryReader(File.Open(source, FileMode.Open)))
			{
				using (var writer = new BinaryWriter(File.Open(destination, FileMode.Create)))
				{
					var header = new Header
					{
						Signature = reader.ReadInt64(),
						UnpackedChunkSize = reader.ReadInt64(),
						Summary = new Reference
						{
							PackedSize = reader.ReadInt64(),
							UnpackedSize = reader.ReadInt64(),
						}
					};

					if (header.Signature != DefaultSignature
						&& header.Signature != SwappedSignature)
					{
						throw new InvalidSignatureException($"ZExtract does not know this signature={header.Signature}.");
					}

					var swapped = header.Signature != DefaultSignature;
					if (swapped)
					{
						// Assume any non default signature is swapped
						header.UnpackedChunkSize = Swap(header.UnpackedChunkSize);
						header.Summary.PackedSize = Swap(header.Summary.PackedSize);
						header.Summary.UnpackedSize = Swap(header.Summary.UnpackedSize);
					}

					var size = header.UnpackedChunkSize;
					if (size == DefaultSignature || size == 0)
					{
						if (strict)
						{
							throw new MissingHeaderChunkSizeException("Z file header is missing chunk size.");
						}
						// Attempt with the default.
						size = DefaultChunkSize;
					}

					var ceiling = size - 1; // Allows for partial chunks
					var count = (header.Summary.UnpackedSize + ceiling) / size;

					long total = 0;
					long largest = 0;
					var catalog = new List<Reference>();
					while (total < header.Summary.UnpackedSize)
					{
						var index = new Reference
						{
							PackedSize = reader.ReadInt64(),
							UnpackedSize = reader.ReadInt64(),
						};
						if (swapped)
						{
							index.PackedSize = Swap(index.PackedSize);
							index.UnpackedSize = Swap(index.UnpackedSize);
						}
						catalog.Add(index);
						total += index.UnpackedSize;
						largest = Math.Max(largest, index.UnpackedSize);
					}

					if (total > header.Summary.UnpackedSize)
					{
						// WARNING total bytes expected does not match indices total
						if (strict)
						{
							throw new SizeMismatchException($"Chunk total size ({total}) does not match specified header size ({header.Summary.UnpackedSize}).");
						}
						// TODO option: use reference count as the truth option.
					}

					if (catalog.Count != count)
					{
						// WARNING total number of indices found is not as expected
						if (strict)
						{
							throw new ChunkCountMismatchException($"Actual chunk count ({catalog.Count}) does not match expected count ({count}). Recommend trying again with strict mode off.");
						}
					}

					var entryCount = -1;
					foreach (var entry in catalog)
					{
						entryCount++;
						var zlib = new ZLibHeader
						{
							CMF = reader.ReadByte(),
							FLG = reader.ReadByte(),
						};

						if (!zlib.IsValid)
						{
							if (strict)
							{
								throw new InvalidZlibHeaderException($"Chunk ({entryCount}) has an invalid zlib header.");
							}
						}

						var data = new byte[largest];
						var input = reader.ReadBytes((int)entry.PackedSize - Marshal.SizeOf(typeof(ZLibHeader)));
						using (var deflate = new DeflateStream(new MemoryStream(input), CompressionMode.Decompress))
						{
							var read = deflate.Read(data, 0, (int)entry.UnpackedSize);
							writer.Write(data, 0, read);
							if (strict)
							{
								// TODO Validate Adler32 of zlib chunk
							}
						}
					}
					if (strict)
					{
						// TODO Verify output file with *.uncompressed_size
					}
				}
			}
		}
	}
}
