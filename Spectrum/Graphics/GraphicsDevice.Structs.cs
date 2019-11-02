/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;
using Vk = SharpVk;
using static Spectrum.InternalLog;

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

			internal DeviceFeatures(in Vk.PhysicalDeviceFeatures feats)
			{
				GeometryShader = (feats.GeometryShader, false);
				TessellationShader = (feats.TessellationShader, false);
				FillModeNonSolid = (feats.FillModeNonSolid, false);
				WideLines = (feats.WideLines, false);
				LargePoints = (feats.LargePoints, false);
				SamplerAnisotropy = (feats.SamplerAnisotropy, false);
				ShaderFloat64 = (feats.ShaderFloat64, false);
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
				ShaderFloat64 = ShaderFloat64.Enabled
			};
		}

		// Contains information about the device queues
		internal struct DeviceQueues
		{
			public Vk.Queue Graphics;
			public Vk.Queue Transfer;
			public uint FamilyIndex;
			public readonly bool SeparateTransfer => Graphics.RawHandle.ToUInt64() != Transfer.RawHandle.ToUInt64();
		}
	}
}
