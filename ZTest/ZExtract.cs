using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace ZTest
{
	public enum CompressionMethod
	{
		Deflate = 8,
		Reserved = 15
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
		Reserved = 128
	}

	public enum CompressionLevel
	{
		Fastest, // No Compression
		Fast,
		Default,
		Maximum // Slowest
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

		public const long DefaultSignature = 0x9EC1832A;
		public const long SwappedSignature = 0x9E2A83C1;
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

		//private static long Swap(long value)
		//{
		//	return BitConverter.ToInt64(BitConverter.GetBytes(value).Reverse().ToArray(), 0);
		//}

		public static void Unpack(string source, string destination)
		{
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
							UnpackedSize = reader.ReadInt64()
						}
					};

					if (header.Signature != DefaultSignature
						&& header.Signature != SwappedSignature)
					{
						// TODO WARNING file signature does not match what is known
						Console.WriteLine("Invalid");
					}

					var swapped = header.Signature != DefaultSignature;
					if (swapped)
					{
						//  C# Stream Reader auto swaps
						//	// Assume any non default signature is swapped
						//	header.UnpackedChunkSize = Swap(header.UnpackedChunkSize);
						//	header.Summary.PackedSize = Swap(header.Summary.PackedSize);
						//	header.Summary.UnpackedSize = Swap(header.Summary.UnpackedSize);
					}

					var size = header.UnpackedChunkSize;
					if (size == DefaultSignature || size == 0)
					{
						size = DefaultChunkSize;
						// TODO WARNING no header chunk size found
						Console.WriteLine("Invalid");
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
							UnpackedSize = reader.ReadInt64()
						};
						//if (swapped)
						//{
						//	index.PackedSize = Swap(index.PackedSize);
						//	index.UnpackedSize = Swap(index.UnpackedSize);
						//}
						catalog.Add(index);
						total += index.UnpackedSize;
						largest = Math.Max(largest, index.UnpackedSize);
					}

					if (total > header.Summary.UnpackedSize)
					{
						// TODO WARNING total bytes expected does not match indices total
						// TODO Try Method 2 reference count
						Console.WriteLine("Invalid");
					}

					if (catalog.Count != count)
					{
						// TODO WARNING total number of indices found is not as expected
						Console.WriteLine("Invalid");
					}

					//using (var deflate = new DeflateStream(File.Open(destination, FileMode.Create), CompressionMode.Decompress))
					//{
					//	reader.BaseStream.CopyTo(deflate);
					//}

					var x = 0;
					foreach (var index in catalog)
					{
						var zlib = new ZLibHeader
						{
							CMF = reader.ReadByte(),
							FLG = reader.ReadByte()
						};

						if (!zlib.IsValid)
						{
							// TODO WARNING that the zlib header failed validation
							Console.WriteLine("Invalid");
						}

						var data = new byte[largest];
						Console.WriteLine(reader.BaseStream.Position);
						var input = reader.ReadBytes((int)index.PackedSize - 2);
						Console.WriteLine(reader.BaseStream.Position);
						using (var deflate = new DeflateStream(new MemoryStream(input), CompressionMode.Decompress))
						{
							var read = deflate.Read(data, 0, (int)index.UnpackedSize);
							writer.Write(data, 0, read);
							x++;
							// TODO Validate Adler32 of zlib chunk
						}
						x++;
					}
					// TODO Verify output file with *.uncompressed_size
				}
			}
		}
	}
}
