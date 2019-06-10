using System;
using System.Runtime.CompilerServices;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// A special type of texture that can be used as the target for pipeline draw commands. It can then be used
	/// as a source of data in future shader invocations. 
	/// <para>
	/// Note that this type is reference counted by the
	/// <see cref="Pipeline"/> instances that use it, and attempting to dispose a render target that is still in
	/// use will result in an error.
	/// </para>
	/// </summary>
	public class RenderTarget : IDisposable
	{
		#region Fields
		/// <summary>
		/// The width of the render target texture.
		/// </summary>
		public uint Width { get; private set; }
		/// <summary>
		/// The height of the render target texture.
		/// </summary>
		public uint Height { get; private set; }
		/// <summary>
		/// The format of the render target texels.
		/// </summary>
		public readonly TexelFormat Format;
		/// <summary>
		/// The optional name for this render target, useful for identification and debugging.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The size of the backing memory for the render target, in bytes.
		/// </summary>
		public uint DataSize { get; private set; }

		/// <summary>
		/// Gets the size of the render target as a <see cref="Point"/>.
		/// </summary>
		public Point Size => new Point((int)Width, (int)Height);
		/// <summary>
		/// Gets if the format of this render target allows it to store depth information.
		/// </summary>
		public bool HasDepth => Format.IsDepthFormat();
		/// <summary>
		/// Gets if the format of this render target allows it to store stencil information.
		/// </summary>
		public bool HasStencil => Format.HasStencilComponent();

		/// <summary>
		/// A reference to the graphics device managing this render target.
		/// </summary>
		protected GraphicsDevice Device => SpectrumApp.Instance.GraphicsDevice;

		/// <summary>
		/// Gets the default viewport to access the entire render target.
		/// </summary>
		public Viewport DefaultViewport => new Viewport(0, 0, Width, Height);
		/// <summary>
		/// Gets the default scissor to access the entire render target.
		/// </summary>
		public Scissor DefaultScissor => new Scissor(0, 0, Width, Height);

		// Vulkan objects
		internal Vk.Image VkImage { get; private set; } = null;
		internal Vk.DeviceMemory VkMemory { get; private set; } = null;
		internal Vk.ImageView VkView { get; private set; } = null;
		internal readonly Vk.ImageAspects VkAspects;
		internal readonly Vk.ImageLayout DefaultImageLayout;

		// Reference counting
		private uint _refCount = 0;
		private readonly object _refLock = new object();
		/// <summary>
		/// Gets the number of <see cref="Pipeline"/> instances currently using the render target. This property is
		/// thread-safe.
		/// </summary>
		public uint ReferenceCount
		{
			get { lock (_refLock) { return _refCount; } }
		}

		private bool _isDisposed = false;
		#endregion // Fields

		/// <summary>
		/// Creates a new render target texture for immediate use.
		/// </summary>
		/// <param name="width">The width of the render target.</param>
		/// <param name="height">The height of the render target.</param>
		/// <param name="format">The render target format.</param>
		/// <param name="name">An optional name for the render target for identification and debugging.</param>
		public RenderTarget(uint width, uint height, TexelFormat format, string name = null)
		{
			if ((width == 0) || (height == 0))
				throw new ArgumentException($"Cannot create a render target with zero dimension ({width}x{height})");
			if ((width > Device.Limits.MaxTextureSize2D) || (height > Device.Limits.MaxTextureSize2D))
				throw new ArgumentException($"Cannot create a render target larger than the image size limits ({width}x{height} > {Device.Limits.MaxTextureSize2D})");
			Format = format;
			VkAspects = HasDepth ? (Vk.ImageAspects.Depth | (HasStencil ? Vk.ImageAspects.Stencil : 0)) : Vk.ImageAspects.Color;
			DefaultImageLayout = HasDepth ? Vk.ImageLayout.DepthStencilAttachmentOptimal : Vk.ImageLayout.ColorAttachmentOptimal;
			Name = name;

			Rebuild(width, height);
		}
		~RenderTarget()
		{
			dispose(false);
		}

		/// <summary>
		/// Rebuilds the render target to use a new size.
		/// </summary>
		/// <param name="width">The new width of the render target.</param>
		/// <param name="height">The new height of the render target.</param>
		/// <param name="force">If <c>true</c>, the render target will be rebuilt even if the size doesnt change.</param>
		public void Rebuild(uint width, uint height, bool force = false)
		{
			if (!force && width == Width && height == Height)
				return;
			Width = width;
			Height = height;

			// Destroy the existing objects, if needed
			VkView?.Dispose();
			VkImage?.Dispose();
			VkMemory?.Dispose();

			// Create the image
			var ici = new Vk.ImageCreateInfo
			{
				ImageType = Vk.ImageType.Image2D,
				Extent = new Vk.Extent3D((int)width, (int)height, 1),
				MipLevels = 1,
				ArrayLayers = 1,
				Format = (Vk.Format)Format,
				Tiling = Vk.ImageTiling.Optimal,
				InitialLayout = Vk.ImageLayout.Undefined,
				Usage = Vk.ImageUsages.Sampled | Vk.ImageUsages.TransferSrc | Vk.ImageUsages.TransferDst | 
					(HasDepth ? Vk.ImageUsages.DepthStencilAttachment : Vk.ImageUsages.ColorAttachment), // No subpassInput support yet, but add it here
				SharingMode = Vk.SharingMode.Exclusive,
				Samples = Vk.SampleCounts.Count1, // TODO: Change when we support multisampling
				Flags = Vk.ImageCreateFlags.None
			};
			VkImage = Device.VkDevice.CreateImage(ici);

			// Create the backing memory for the image
			var memReq = VkImage.GetMemoryRequirements();
			var memIdx = Device.FindMemoryTypeIndex(memReq.MemoryTypeBits, Vk.MemoryProperties.DeviceLocal);
			if (memIdx == -1)
				throw new InvalidOperationException("Cannot find a memory type that supports render targets (this means bad or out-of-date hardware)");
			var mai = new Vk.MemoryAllocateInfo(memReq.Size, memIdx);
			VkMemory = Device.VkDevice.AllocateMemory(mai);
			DataSize = (uint)memReq.Size;
			VkImage.BindMemory(VkMemory);

			// Create the view
			var vci = new Vk.ImageViewCreateInfo(
				(Vk.Format)Format,
				new Vk.ImageSubresourceRange(VkAspects, 0, 1, 0, 1),
				viewType: Vk.ImageViewType.Image2D
			);
			VkView = VkImage.CreateView(vci);

			// Make the initial layout transition
			Device.SubmitScratchCommand(buf => {
				buf.CmdPipelineBarrier(Vk.PipelineStages.AllGraphics, Vk.PipelineStages.AllGraphics, imageMemoryBarriers: new[] { new Vk.ImageMemoryBarrier(
					VkImage, new Vk.ImageSubresourceRange(VkAspects, 0, 1, 0, 1), Vk.Accesses.None, Vk.Accesses.None,
					Vk.ImageLayout.Undefined, DefaultImageLayout
				)});
			});
		}

		// Increases the number of refernces to this render target
		internal void IncRefCount()
		{
			lock (_refLock) { _refCount += 1; }
		}
		// Decreases the number of references to this render target
		internal void DecRefCount()
		{
			lock (_refLock) { if (_refCount > 0) _refCount -= 1; }
		}

		// Gets the default attachment description for this render target
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Vk.AttachmentDescription GetDescription()
		{
			return new Vk.AttachmentDescription(
				Vk.AttachmentDescriptions.MayAlias,
				(Vk.Format)Format,
				Vk.SampleCounts.Count1, // TODO: change when we support multisampling
				// All pipelines will preserve attachments when loading and storing
				// This is not the most efficient, but its too complex to support otherwise (for now)
				Vk.AttachmentLoadOp.Load,
				Vk.AttachmentStoreOp.Store,
				Vk.AttachmentLoadOp.Load,
				Vk.AttachmentStoreOp.Store,
				DefaultImageLayout,
				DefaultImageLayout
			);
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (disposing && !_isDisposed)
			{
				if (ReferenceCount > 0)
					throw new InvalidOperationException($"Cannot dispose render target \"{Name ?? "none"}\" still in use (uses = {_refCount})");

				VkView?.Dispose();
				VkImage.Dispose();
				VkMemory?.Dispose();
			}
			_isDisposed = true;
		}
		#endregion // IDisposable

		/// <summary>
		/// Creates a new default render target for holding color data (R8G8B8A8UNorm format).
		/// </summary>
		/// <param name="width">The width of the render target.</param>
		/// <param name="height">The height of the render target.</param>
		/// <param name="name">The optional render target name.</param>
		/// <returns>A new render target designed to hold color data.</returns>
		public static RenderTarget CreateColor(uint width, uint height, string name = null) =>
			new RenderTarget(width, height, TexelFormat.Color, name);

		/// <summary>
		/// Creates a new default render target for holding depth (and optionally stencil) data.
		/// </summary>
		/// <param name="width">The width of the render target.</param>
		/// <param name="height">The height of the render target.</param>
		/// <param name="stencil">
		/// If the render target should also support stencil data. If <c>true</c>, the render target will use 
		/// <see cref="TexelFormat.Depth24Stencil8"/>, otherwise it will use <see cref="TexelFormat.Depth32"/>.
		/// </param>
		/// <param name="name">The optional render target name.</param>
		/// <returns>A new render target designed to hold depth (and optionally stencil) data.</returns>
		public static RenderTarget CreateDepth(uint width, uint height, bool stencil = true, string name = null) =>
			new RenderTarget(width, height, stencil ? TexelFormat.Depth24Stencil8 : TexelFormat.Depth32, name);
	}
}
