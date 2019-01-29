using System;
using System.Collections.Generic;
using System.Linq;
using Vk = VulkanCore;
using static Spectrum.Utilities.CollectionUtils;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Holds a collection of textures that can be used as render targets and input attachments in render passes.
	/// </summary>
	public sealed class Framebuffer : IDisposable
	{
		#region Fields
		/// <summary>
		/// The graphics device holding this Framebuffer's resources.
		/// </summary>
		public readonly GraphicsDevice Device;

		/// <summary>
		/// The width of the images in this framebuffer.
		/// </summary>
		public uint Width { get; private set; } = 0;
		/// <summary>
		/// The height of the images in this framebuffer.
		/// </summary>
		public uint Height { get; private set; } = 0;

		// Attachment descriptor objects
		private readonly List<ResourceInfo> _resources = new List<ResourceInfo>();
		private readonly List<FBImage> _images = new List<FBImage>();

		/// <summary>
		/// The number of texture resources within the instance.
		/// </summary>
		public uint AttachmentCount => (uint)_images.Count;

		/// <summary>
		/// Gets the attachment reference with the given name, or throws an exception if the attachment does not exist.
		/// </summary>
		/// <param name="name">The name of the attachment to get.</param>
		/// <returns>An opaque reference to the attachment with the given name.</returns>
		public FramebufferAttachment this [string name] => GetAttachment(name);

		private bool _isDisposed = false;
		#endregion // Fields

		/// <summary>
		/// Creates a new set of texture resources with the given size.
		/// </summary>
		/// <param name="width">The initial width of the texture resources. Cannot be zero.</param>
		/// <param name="height">The initial height of the texture resources. Cannot be zero.</param>
		public Framebuffer(uint width, uint height)
		{
			if (width == 0 || height == 0)
				throw new ArgumentException($"Framebuffers cannot have a zero dimension ({width}x{height})");

			Device = SpectrumApp.Instance.GraphicsDevice;
			Width = width;
			Height = height;
		}
		~Framebuffer()
		{
			dispose(false);
		}

		/// <summary>
		/// Recreates the texture resources in this framebuffer with a new size. If the framebuffer is already equal to
		/// the new size, then nothing is changed or regenerated.
		/// </summary>
		/// <param name="width">The width of the new texture resources. Cannot be zero.</param>
		/// <param name="height">The height of the new texture resources. Cannot be zero.</param>
		public void Rebuild(uint width, uint height)
		{
			if (width == 0 || height == 0)
				throw new ArgumentException($"Framebuffers cannot have a zero dimension ({width}x{height})");
			if (Width == width && Height == height)
				return;

			Width = width;
			Height = height;

			// Dispose and clear the old images
			_images.ForEach(im => {
				im.VkView.Dispose();
				im.VkImage.Dispose();
				im.VkMemory.Dispose();
			});
			_images.Clear();

			// Create images with the new size
			_resources.ForEach(res => _images.Add(createImage(res)));
		}

		/// <summary>
		/// Creates a new texture resource with the given name, format, and usage hints.
		/// </summary>
		/// <param name="name">The name of the texture resource.</param>
		/// <param name="format">The format of the texture resource.</param>
		/// <param name="allowRead">If the attachment can be explicitly read from in a shader as an input attachment.</param>
		public void AddAttachment(string name, TexelFormat format, bool allowRead = true)
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException($"The attachment name cannot be null or empty", nameof(name));
			if (_resources.Any(res => res.Name == name))
				throw new ArgumentException($"The framebuffer already has a resource with the name '{name}'", nameof(name));
			if (allowRead && !format.IsValidForInput())
				throw new ArgumentException($"The attachment format ({format}) cannot be used in an attachment that allows shader reads", nameof(format));

			var info = new ResourceInfo(name, format, allowRead);
			_resources.Add(info);
			_images.Add(createImage(info));
		}

		/// <summary>
		/// Gets the attachment reference with the given name, or throws an exception if the attachment does not exist.
		/// </summary>
		/// <param name="name">The name of the attachment to get.</param>
		/// <returns>An opaque reference to the attachment with the given name.</returns>
		public FramebufferAttachment GetAttachment(string name)
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("The provided name is not valid for a framebuffer attachment", nameof(name));

			int idx = _resources.IndexOf(res => res.Name == name);
			if (idx == -1)
				throw new ArgumentException($"The frameubffer does not contain an attachment with the name '{name}'", nameof(name));
			var info = _resources[idx];
			var img = _images[idx];
			return new FramebufferAttachment(name, this, info.Format, info.AllowRead, img.VkView);
		}

		/// <summary>
		/// Gets if the instance has a resource with the given name.
		/// </summary>
		/// <param name="name">The resource name to check for.</param>
		/// <returns>If the named resource already exists.</returns>
		public bool HasAttachment(string name)
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException($"The attachment name cannot be null or empty", nameof(name));
			return _resources.Any(res => res.Name == name);
		}

		// Creates a new image from the info
		private FBImage createImage(in ResourceInfo info)
		{
			// Create the image
			var usage = info.Format.IsDepthFormat() ? Vk.ImageUsages.DepthStencilAttachment : Vk.ImageUsages.ColorAttachment;
			if (info.AllowRead)
				usage |= Vk.ImageUsages.InputAttachment;
			var ici = new Vk.ImageCreateInfo {
				ImageType = Vk.ImageType.Image2D,
				Extent = new Vk.Extent3D((int)Width, (int)Height, 1),
				MipLevels = 1,
				ArrayLayers = 1,
				Format = (Vk.Format)info.Format,
				Tiling = Vk.ImageTiling.Optimal,
				InitialLayout = info.Format.IsDepthFormat() ? Vk.ImageLayout.DepthStencilAttachmentOptimal : Vk.ImageLayout.ColorAttachmentOptimal,
				Usage = Vk.ImageUsages.TransientAttachment | Vk.ImageUsages.TransferSrc | usage,
				SharingMode = Vk.SharingMode.Exclusive,
				Samples = Vk.SampleCounts.Count1,
				Flags = Vk.ImageCreateFlags.None
			};
			var image = Device.VkDevice.CreateImage(ici);

			// Create the backing memory
			var memReq = image.GetMemoryRequirements();
			var memIdx = Device.FindMemoryTypeIndex(memReq.MemoryTypeBits, Vk.MemoryProperties.DeviceLocal | Vk.MemoryProperties.LazilyAllocated);
			if (memIdx == -1)
				throw new InvalidOperationException("Cannot find a memory type that supports framebuffer textures");
			var mai = new Vk.MemoryAllocateInfo(memReq.Size, memIdx);
			var memory = Device.VkDevice.AllocateMemory(mai);

			// Create the image view
			var aspect = info.Format.IsDepthFormat() ? Vk.ImageAspects.Depth : Vk.ImageAspects.Color;
			if (info.Format.HasStencilComponent())
				aspect |= Vk.ImageAspects.Stencil;
			var vci = new Vk.ImageViewCreateInfo(
				(Vk.Format)info.Format,
				new Vk.ImageSubresourceRange(aspect, 0, 1, 0, 1),
				viewType: Vk.ImageViewType.Image2D
			);
			var view = image.CreateView(vci);

			// Collect and return
			return new FBImage(image, memory, view);
		}
		
		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!_isDisposed && disposing)
			{
				// Dispose and clear the images
				_images.ForEach(im => {
					im.VkView.Dispose();
					im.VkImage.Dispose();
					im.VkMemory.Dispose();
				});
			}
			_isDisposed = true;
		}
		#endregion // IDisposable

		// Simple container type for image objects
		private struct FBImage
		{
			public readonly Vk.Image VkImage;
			public readonly Vk.ImageView VkView;
			public readonly Vk.DeviceMemory VkMemory;

			public FBImage(Vk.Image i, Vk.DeviceMemory m, Vk.ImageView v)
			{
				VkImage = i;
				VkView = v;
				VkMemory = m;
			}
		}

		// Simple container type for holding resource information persisted across rebuilds
		private struct ResourceInfo
		{
			public readonly string Name;
			public readonly TexelFormat Format;
			public readonly bool AllowRead;

			public ResourceInfo(string n, TexelFormat f, bool ar)
			{
				Name = n;
				Format = f;
				AllowRead = ar;
			}
		}
	}

	/// <summary>
	/// An opaque descriptive handle to a texture resource within a <see cref="Framebuffer"/> instance.
	/// </summary>
	public struct FramebufferAttachment
	{
		#region Fields
		/// <summary>
		/// The name of the attachment.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The framebuffer the attachment belongs to.
		/// </summary>
		public readonly Framebuffer Framebuffer;
		/// <summary>
		/// The format of the attachment texels.
		/// </summary>
		public readonly TexelFormat Format;
		/// <summary>
		/// If the attachment can be used as a read-enabled input attachment in a shader.
		/// </summary>
		public readonly bool AllowRead;
		// The actual image view used as a reference to the image
		internal readonly Vk.ImageView View;
		#endregion // Fields

		internal FramebufferAttachment(string name, Framebuffer fb, TexelFormat format, bool ar, Vk.ImageView view)
		{
			Name = name;
			Framebuffer = fb;
			Format = format;
			AllowRead = ar;
			View = view;
		}
	}
}
