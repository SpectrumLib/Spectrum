/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;
using Vk = SharpVk;
using static Spectrum.InternalLog;
using System.Collections.Generic;

namespace Spectrum.Graphics
{
	// Contains extra types related to the graphics device
	public sealed partial class GraphicsDevice : IDisposable
	{
		/// <summary>
		/// Contains overall information about a physical device.
		/// </summary>
		public struct DeviceInfo
		{
			/// <summary>
			/// Human-readable name for the device.
			/// </summary>
			public string Name;
			/// <summary>
			/// Version of the Vulkan API initialized on the device.
			/// </summary>
			public Version ApiVersion;
			/// <summary>
			/// Device driver version.
			/// </summary>
			public Version DriverVersion;
			/// <summary>
			/// If the device is discrete.
			/// </summary>
			public bool IsDiscrete;
			/// <summary>
			/// Vendor-specific identification value.
			/// </summary>
			public uint VendorId;
			/// <summary>
			/// Device-specific identification value.
			/// </summary>
			public uint DeviceId;
			/// <summary>
			/// Universally unique identifier for the device.
			/// </summary>
			public Guid Uuid;

			internal DeviceInfo(in Vk.PhysicalDeviceProperties props)
			{
				Name = props.DeviceName;
				ApiVersion = new Version(props.ApiVersion.Major, props.ApiVersion.Minor, props.ApiVersion.Patch);
				DriverVersion = new Version(props.DriverVersion.Major, props.DriverVersion.Minor, props.DriverVersion.Patch);
				IsDiscrete = props.DeviceType == Vk.PhysicalDeviceType.DiscreteGpu;
				VendorId = props.VendorID;
				DeviceId = props.DeviceID;
				Uuid = props.PipelineCacheUUID;
			}
		}

		/// <summary>
		/// Optional features on the graphics device that can be enabled, if they are supported.
		/// </summary>
		public struct DeviceFeatures
		{
			/// <summary>
			/// Geometry shader stage.
			/// </summary>
			public (bool Enabled, bool Strict) GeometryShader;
			/// <summary>
			/// Tessellation shader stages (control and evaluation).
			/// </summary>
			public (bool Enabled, bool Strict) TessellationShader;
			/// <summary>
			/// Non-solid polygon fill mode (wireframe mode).
			/// </summary>
			public (bool Enabled, bool Strict) FillModeNonSolid;
			/// <summary>
			/// Lines can be drawn with widths other than one.
			/// </summary>
			public (bool Enabled, bool Strict) WideLines;
			/// <summary>
			/// Points can be drawn with sizes other than one.
			/// </summary>
			public (bool Enabled, bool Strict) LargePoints;
			/// <summary>
			/// Anisotropic filtering for textures.
			/// </summary>
			public (bool Enabled, bool Strict) SamplerAnisotropy;
			/// <summary>
			/// Support for double-precision floating point numbers in shaders.
			/// </summary>
			public (bool Enabled, bool Strict) ShaderFloat64;
			/// <summary>
			/// Support for depth bounds testing in pipelines.
			/// </summary>
			public (bool Enabled, bool Strict) DepthBoundsTesting;
			/// <summary>
			/// Support for depth clamping in pipeline rasterization.
			/// </summary>
			public (bool Enabled, bool Strict) DepthClamp;

			internal DeviceFeatures(in Vk.PhysicalDeviceFeatures feats)
			{
				GeometryShader = (feats.GeometryShader, false);
				TessellationShader = (feats.TessellationShader, false);
				FillModeNonSolid = (feats.FillModeNonSolid, false);
				WideLines = (feats.WideLines, false);
				LargePoints = (feats.LargePoints, false);
				SamplerAnisotropy = (feats.SamplerAnisotropy, false);
				ShaderFloat64 = (feats.ShaderFloat64, false);
				DepthBoundsTesting = (feats.DepthBounds, false);
				DepthClamp = (feats.DepthClamp, false);
			}

			// Ensures that the requested features are available
			// Modifies the struct for non-strict features, throws an exception for strict features
			internal void Check(in Vk.PhysicalDeviceFeatures feats)
			{
				GeometryShader.Enabled = 
					CheckFeature(GeometryShader.Enabled, feats.GeometryShader, GeometryShader.Strict, "GeometryShader");
				TessellationShader.Enabled =
					CheckFeature(TessellationShader.Enabled, feats.TessellationShader, TessellationShader.Strict, "TessellationShader");
				FillModeNonSolid.Enabled =
					CheckFeature(FillModeNonSolid.Enabled, feats.FillModeNonSolid, FillModeNonSolid.Strict, "FillModeNonSolid");
				WideLines.Enabled =
					CheckFeature(WideLines.Enabled, feats.WideLines, WideLines.Strict, "WideLines");
				LargePoints.Enabled =
					CheckFeature(LargePoints.Enabled, feats.LargePoints, LargePoints.Strict, "LargePoints");
				SamplerAnisotropy.Enabled =
					CheckFeature(SamplerAnisotropy.Enabled, feats.SamplerAnisotropy, SamplerAnisotropy.Strict, "SamplerAnisotropy");
				ShaderFloat64.Enabled =
					CheckFeature(ShaderFloat64.Enabled, feats.ShaderFloat64, ShaderFloat64.Strict, "ShaderFloat64");
				DepthBoundsTesting.Enabled =
					CheckFeature(DepthBoundsTesting.Enabled, feats.DepthBounds, DepthBoundsTesting.Strict, "DepthBoundsTesting");
				DepthClamp.Enabled =
					CheckFeature(DepthClamp.Enabled, feats.DepthClamp, DepthClamp.Strict, "DepthClamp");
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static bool CheckFeature(bool r, bool a, bool strict, string fname)
			{
				if (r && !a)
				{
					if (strict) throw new PlatformNotSupportedException($"Required device feature '{fname}' not available on system.");
					IWARN($"Requested device feature '{fname}' not available on system, disabling.");
					return false;
				}
				return r;
			}

			internal Vk.PhysicalDeviceFeatures ToVulkanType() => new Vk.PhysicalDeviceFeatures { 
				GeometryShader = GeometryShader.Enabled,
				TessellationShader = TessellationShader.Enabled,
				FillModeNonSolid = FillModeNonSolid.Enabled,
				WideLines = WideLines.Enabled,
				LargePoints = LargePoints.Enabled,
				SamplerAnisotropy = SamplerAnisotropy.Enabled,
				ShaderFloat64 = ShaderFloat64.Enabled,
				DepthBounds = DepthBoundsTesting.Enabled,
				DepthClamp = DepthClamp.Enabled
			};
		}

		/// <summary>
		/// Numeric limits for device features and objects.
		/// </summary>
		public struct DeviceLimits
		{
			#region Fields
			/// <summary>
			/// The maximum width of a <see cref="RenderTarget"/>.
			/// </summary>
			public uint RenderTargetWidth;
			/// <summary>
			/// The maximum height of a <see cref="RenderTarget"/>.
			/// </summary>
			public uint RenderTargetHeight;
			/// <summary>
			/// The maximum number of color attachments allowed in a single <see cref="RenderPass"/>.
			/// </summary>
			public uint ColorAttachments;
			/// <summary>
			/// The maximum number of input attachments allowed in a single <see cref="RenderPass"/>.
			/// </summary>
			public uint InputAttachments;
			/// <summary>
			/// The minimum and maximum values for <see cref="RasterizerState.LineWidth"/>.
			/// </summary>
			public (float Min, float Max) LineWidth;
			/// <summary>
			/// The maximum texel count for textures of type <see cref="TextureType.Tex1D"/> and 
			/// <see cref="TextureType.Tex1DArray"/>.
			/// </summary>
			public uint TextureSize1D;
			/// <summary>
			/// The maximum side length (in texels) for textures of type <see cref="TextureType.Tex2D"/> and
			/// <see cref="TextureType.Tex2DArray"/>.
			/// </summary>
			public uint TextureSize2D;
			/// <summary>
			/// The maximum side length (in texels) for textures of type <see cref="TextureType.Tex3D"/>.
			/// </summary>
			public uint TextureSize3D;
			/// <summary>
			/// The maximum number of layers for textures of type <see cref="TextureType.Tex1DArray"/> and 
			/// <see cref="TextureType.Tex2DArray"/>.
			/// </summary>
			public uint TextureLayers;
			#endregion // Fields

			internal DeviceLimits(in Vk.PhysicalDeviceLimits lims)
			{
				RenderTargetWidth = lims.MaxFramebufferWidth;
				RenderTargetHeight = lims.MaxFramebufferHeight;
				ColorAttachments = lims.MaxColorAttachments;
				InputAttachments = lims.MaxDescriptorSetInputAttachments;
				LineWidth = lims.LineWidthRange;
				TextureSize1D = lims.MaxImageDimension1D;
				TextureSize2D = lims.MaxImageDimension2D;
				TextureSize3D = lims.MaxImageDimension3D;
				TextureLayers = lims.MaxImageArrayLayers;
			}
		}

		// Contains information about the device queues
		internal struct DeviceQueues
		{
			public Vk.Queue Graphics;
			public Vk.Queue Transfer;
			public uint FamilyIndex;
			public readonly bool SeparateTransfer => Graphics.RawHandle.ToUInt64() != Transfer.RawHandle.ToUInt64();
		}

		// Contains information and operations for device memory types
		internal class DeviceMemory
		{
			public Vk.PhysicalDeviceMemoryProperties Properties;
			private readonly Dictionary<ulong, uint> _memoryCache;

			public DeviceMemory(in Vk.PhysicalDeviceMemoryProperties mp)
			{
				Properties = mp;
				_memoryCache = new Dictionary<ulong, uint>();
			}

			// Tries to find the memory heap index that supports the given requirements
			public uint? Find(uint bits, Vk.MemoryPropertyFlags flags)
			{
				ulong idx = ((ulong)bits << 32) | (ulong)flags;
				if (_memoryCache.TryGetValue(idx, out var mem))
					return mem;

				for (int mi = 0; mi < Properties.MemoryTypes.Length; ++mi)
				{
					uint mask = 1u << mi;
					if ((bits & mask) > 0 && (Properties.MemoryTypes[mi].PropertyFlags & flags) == flags)
					{
						_memoryCache.Add(idx, (uint)mi);
						return (uint)mi;
					}
				}

				return null;
			}
		}
	}
}
