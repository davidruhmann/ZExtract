using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZExtract;

namespace ZExtractTest
{
	[TestClass]
	public class ZExtractTests
	{
		[TestMethod]
		[TestCategory("Unit")]
		public void Unpack_NullSource()
		{
			Assert.ThrowsException<ArgumentNullException>(() => ZCompression.Unpack(null, null));
		}

		[TestMethod]
		[TestCategory("Unit")]
		public void Unpack_NullDestination()
		{
			var args = new string[] { "", null };
			Assert.ThrowsException<ArgumentNullException>(() => ZCompression.Unpack("", null));
		}

		[DataTestMethod]
		[DataRow("tiny.uasset.z", "tiny.uasset", "23f9f6150418afcc3d885b7778268efd92e5a99b1c35bf12abbe7bead1d81e97")]
		[DataRow("small.uasset.z", "small.uasset", "f03b309a7c409bde4c64ca33296ef8f9f9dffc5f2c29dc7fcde1a12973b78f3c")]
		[DataRow("normal.uasset.z", "normal.uasset", "43845dea09b53191d036cb51117c2a5a7bcb170125f59f39212597d0fe36836b")]
		[DataRow("large.umap.z", "large.umap", "1cf685386f666dc5e36a8fe0b5a4560b7c05145dc43d0b9821267d7a215f15b5")]
		//[DataRow("huge.umap.z", "huge.umap", "f5fad64cf15cf3b56be8e43b45d66557b60bfa9d78cd71dc21bb345f96db60a2")]
		[TestCategory("Unit")]
		public void Unpack_NewDirectoryDestination_Source(string source, string output, string expectedHash)
		{
			var temp = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
			var guid = Guid.NewGuid().ToString();
			var destination = $"{temp}{Path.DirectorySeparatorChar}{guid}{Path.DirectorySeparatorChar}";
			try
			{
				ZCompression.Unpack(source, destination);

				var result = $"{destination}{output}";
				Assert.IsTrue(File.Exists(result));
				using (var stream = new FileStream(result, FileMode.Open))
				using (var algo = SHA256.Create())
				{
					var hash = BitConverter.ToString(algo.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
					Assert.AreEqual(expectedHash, hash);
				}
			}
			finally
			{
				Directory.Delete(destination, true);
			}
		}
	}
}
