using System;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Controls the depth and stencil buffer functionality of a pipeline.
	/// </summary>
	public struct DepthStencilState
	{
		#region Predefined States
		/// <summary>
		/// No depth or stencil information is written or tested against.
		/// </summary>
		public static readonly DepthStencilState None = new DepthStencilState {
			DepthTestEnable = false, DepthWriteEnable = false, DepthOp = CompareOp.Always,
			DepthBoundsEnable = false, MinDepthBounds = 0, MaxDepthBounds = 1,
			StencilTestEnable = false, StencilOp = CompareOp.Never, FailOp = Graphics.StencilOp.Keep, PassOp = Graphics.StencilOp.Keep,
			DepthFailOp = Graphics.StencilOp.Keep, CompareMask = null, WriteMask = null, StencilReference = 0
		};
		/// <summary>
		/// Standard depth testing with closer fragments overriding further ones, with no stencil testing.
		/// </summary>
		public static readonly DepthStencilState Default = new DepthStencilState {
			DepthTestEnable = true, DepthWriteEnable = true, DepthOp = CompareOp.Less,
			DepthBoundsEnable = false, MinDepthBounds = 0, MaxDepthBounds = 1,
			StencilTestEnable = false, StencilOp = CompareOp.Never, FailOp = Graphics.StencilOp.Keep, PassOp = Graphics.StencilOp.Keep,
			DepthFailOp = Graphics.StencilOp.Keep, CompareMask = null, WriteMask = null, StencilReference = 0
		};
		/// <summary>
		/// The standard depth test is performed, but new depth information is never written to the depth buffer. Does
		/// not have any stencil testing.
		/// </summary>
		public static readonly DepthStencilState DepthRead = new DepthStencilState {
			DepthTestEnable = true, DepthWriteEnable = false, DepthOp = CompareOp.Less,
			DepthBoundsEnable = false, MinDepthBounds = 0, MaxDepthBounds = 1,
			StencilTestEnable = false, StencilOp = CompareOp.Never, FailOp = Graphics.StencilOp.Keep, PassOp = Graphics.StencilOp.Keep,
			DepthFailOp = Graphics.StencilOp.Keep, CompareMask = null, WriteMask = null, StencilReference = 0
		};
		/// <summary>
		/// Standard depth testing, with passing depth samples also writing a value of 1 to the stencil buffer.
		/// </summary>
		/// <remarks>Designed to be used in conjunction with <see cref="StencilRead"/> to implement masked rendering.</remarks>
		public static readonly DepthStencilState StencilWrite = new DepthStencilState {
			DepthTestEnable = true, DepthWriteEnable = true, DepthOp = CompareOp.Less,
			DepthBoundsEnable = false, MinDepthBounds = 0, MaxDepthBounds = 1,
			StencilTestEnable = true, StencilOp = CompareOp.Always, FailOp = Graphics.StencilOp.Replace, PassOp = Graphics.StencilOp.Replace,
			DepthFailOp = Graphics.StencilOp.Zero, CompareMask = null, WriteMask = null, StencilReference = 1
		};
		/// <summary>
		/// Standard read-only depth testing, also checking against the stencil buffer having a value of 1.
		/// </summary>
		/// <remarks>Designed to be used in conjunction with <see cref="StencilWrite"/> to implement masked rendering.</remarks>
		public static readonly DepthStencilState StencilRead = new DepthStencilState {
			DepthTestEnable = true, DepthWriteEnable = false, DepthOp = CompareOp.Less,
			DepthBoundsEnable = false, MinDepthBounds = 0, MaxDepthBounds = 1,
			StencilTestEnable = true, StencilOp = CompareOp.Equal, FailOp = Graphics.StencilOp.Keep, PassOp = Graphics.StencilOp.Keep,
			DepthFailOp = Graphics.StencilOp.Keep, CompareMask = null, WriteMask = null, StencilReference = 1
		};
		#endregion // Predefined States

		#region Fields
		/// <summary>
		/// If testing against the depth buffer is enabled.
		/// </summary>
		public bool DepthTestEnable;
		/// <summary>
		/// If writing to the depth buffer is enabled.
		/// </summary>
		public bool DepthWriteEnable;
		/// <summary>
		/// The operation to use when testing the depth buffer.
		/// </summary>
		public CompareOp DepthOp;
		/// <summary>
		/// If depths bounds testing is enabled.
		/// </summary>
		public bool DepthBoundsEnable;
		/// <summary>
		/// If depth bounds testing is enabled, the minimum value of passing depth samples.
		/// </summary>
		public float MinDepthBounds;
		/// <summary>
		/// If depth bounds testing is enabled, the maximum value of passing depth samples.
		/// </summary>
		public float MaxDepthBounds;
		/// <summary>
		/// If testing against the stencil buffer is enabled.
		/// </summary>
		public bool StencilTestEnable;
		/// <summary>
		/// The operation to use when testing the stencil buffer.
		/// </summary>
		public CompareOp StencilOp;
		/// <summary>
		/// The operation to perform on the stencil buffer when the stencil test fails.
		/// </summary>
		public StencilOp FailOp;
		/// <summary>
		/// The operation to perform on the stencil buffer when the stencil test passes.
		/// </summary>
		public StencilOp PassOp;
		/// <summary>
		/// The operation to perform on the stencil buffer when the stencil test passes, but the depth test fails.
		/// </summary>
		public StencilOp DepthFailOp;
		/// <summary>
		/// The mask for bits to use in the stencil buffer during comparison operations. Null defaults to all bits.
		/// </summary>
		public uint? CompareMask;
		/// <summary>
		/// The mask for bits to use in the stencil buffer when writing. Null defaults to all bits.
		/// </summary>
		public uint? WriteMask;
		/// <summary>
		/// The value used in stencil operations that require a reference value.
		/// </summary>
		public int StencilReference;
		#endregion // Fields

		internal Vk.PipelineDepthStencilStateCreateInfo ToCreateInfo()
		{
			var sop = new Vk.StencilOpState {
				FailOp = (Vk.StencilOp)FailOp,
				PassOp = (Vk.StencilOp)PassOp,
				DepthFailOp = (Vk.StencilOp)DepthFailOp,
				CompareOp = (Vk.CompareOp)StencilOp,
				CompareMask = CompareMask.HasValue ? (int)CompareMask.Value : 0x7FFFFFFF,
				WriteMask = WriteMask.HasValue ? (int)WriteMask.Value : 0x7FFFFFFF,
				Reference = StencilReference
			};
			return new Vk.PipelineDepthStencilStateCreateInfo {
				DepthTestEnable = DepthTestEnable,
				DepthWriteEnable = DepthWriteEnable,
				DepthCompareOp = (Vk.CompareOp)DepthOp,
				DepthBoundsTestEnable = DepthBoundsEnable,
				MinDepthBounds = Mathf.UnitClamp(MinDepthBounds),
				MaxDepthBounds = Mathf.UnitClamp(MaxDepthBounds),
				StencilTestEnable = StencilTestEnable,
				Front = sop,
				Back = sop
			};
		}
	}

	/// <summary>
	/// Operations to use when making comparisons in depth and stencil buffers.
	/// </summary>
	public enum CompareOp
	{
		/// <summary>
		/// The test never passes.
		/// </summary>
		Never = Vk.CompareOp.Never,
		/// <summary>
		/// Test passes if the new value is less than the old value.
		/// </summary>
		Less = Vk.CompareOp.Less,
		/// <summary>
		/// Test passes if the new value is equal to the old value.
		/// </summary>
		Equal = Vk.CompareOp.Equal,
		/// <summary>
		/// Test passes if the new value is less than or equal to the old value.
		/// </summary>
		LessOrEqual = Vk.CompareOp.LessOrEqual,
		/// <summary>
		/// Test passes if the new value is greater than the old value.
		/// </summary>
		Greater = Vk.CompareOp.Greater,
		/// <summary>
		/// Test passes if the new value is not equal to the old value.
		/// </summary>
		NotEqual = Vk.CompareOp.NotEqual,
		/// <summary>
		/// Test passes if the new value is greater than or equal to the old value.
		/// </summary>
		GreaterOrEqual = Vk.CompareOp.GreaterOrEqual,
		/// <summary>
		/// The test always passes.
		/// </summary>
		Always = Vk.CompareOp.Always
	}

	/// <summary>
	/// Operations to perform on a stencil buffer fragment when it passes the stencil test.
	/// </summary>
	public enum StencilOp
	{
		/// <summary>
		/// Keeps the existing fragment value.
		/// </summary>
		Keep = Vk.StencilOp.Keep,
		/// <summary>
		/// Zeros out the fragment.
		/// </summary>
		Zero = Vk.StencilOp.Zero,
		/// <summary>
		/// Replaces the fragment with the reference value.
		/// </summary>
		Replace = Vk.StencilOp.Replace,
		/// <summary>
		/// Increments the fragment value by one, and clamps it to a max value.
		/// </summary>
		IncrementAndClamp = Vk.StencilOp.IncrementAndClamp,
		/// <summary>
		/// Decrements the fragment value by one, and clamps it to zero.
		/// </summary>
		DecrementAndClamp = Vk.StencilOp.DecrementAndClamp,
		/// <summary>
		/// Bit-wise inverts the fragment value.
		/// </summary>
		Invert = Vk.StencilOp.Invert,
		/// <summary>
		/// Increments the fragment value by one, and wraps around to zero on overflow.
		/// </summary>
		IncrementAndWrap = Vk.StencilOp.IncrementAndWrap,
		/// <summary>
		/// Decrements the fragment value by one, and wraps around to max value on underflow.
		/// </summary>
		DecrementAndWrap = Vk.StencilOp.DecrementAndWrap
	}
}
