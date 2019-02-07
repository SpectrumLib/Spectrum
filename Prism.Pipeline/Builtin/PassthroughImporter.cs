using System;
using System.IO;

namespace Prism.Builtin
{
	// Loads the file's contents into memory unaltered
	[ContentImporter("Passthrough Importer", null, ".txt")]
	internal sealed class PassthroughImporter : ContentImporter<byte[]>
	{
		public override byte[] Import(FileStream stream, ImporterContext ctx)
		{
			using (BinaryReader reader = new BinaryReader(stream))
			{
				byte[] data = new byte[ctx.FileLength];
				reader.Read(data, 0, (int)ctx.FileLength);
				return data;
			}
		}
	}
}
