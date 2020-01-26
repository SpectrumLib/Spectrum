/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.IO;

namespace Prism.Pipeline
{
	// Default builtin content processor for textures.
	[ContentProcessor("TextureProcessor", "Texture", "png,jpg,jpeg,bmp")]
	internal sealed class TextureProcessor : ContentProcessor
	{
		#region Fields
		public override string LoaderName => "TextureLoader";

		private string _name;
		private long _size;
		#endregion // Fields

		public TextureProcessor()
		{

		}

		public override void Reset()
		{
			
		}

		public override void Begin(PipelineContext ctx, BinaryReader stream)
		{
			_name = ctx.ItemName;
			_size = stream.BaseStream.Length;
		}

		public override bool Read(PipelineContext ctx, BinaryReader stream)
		{
			return ctx.IsFirstLoop;
		}

		public override void Process(PipelineContext ctx)
		{
			
		}

		public override void Write(PipelineContext ctx, BinaryWriter stream)
		{
			
		}

		public override void End(PipelineContext ctx, BinaryWriter stream)
		{
			stream.Write(_name.AsSpan()); stream.Write('\n');
			stream.Write($"Size = {_size}".AsSpan());
		}

		protected override void onDispose(bool disposing)
		{
			
		}
	}
}
