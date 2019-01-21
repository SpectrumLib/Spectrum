using System;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Controls the depth and stencil buffer functionality of a pipeline.
	/// </summary>
	public struct DepthStencilState
	{
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
	/// Operations to perform on the stencil buffer when stencil tests pass.
	/// </summary>
	public enum StencilOp
	{
		Keep = Vk.StencilOp.Keep,
		Zero = Vk.StencilOp.Zero,
		Replace = Vk.StencilOp.Replace,
		IncrementAndClamp = Vk.StencilOp.IncrementAndClamp,
		DecrementAndClamp = Vk.StencilOp.DecrementAndClamp,
		Invert = Vk.StencilOp.Invert,
		IncrementAndWrap = Vk.StencilOp.IncrementAndWrap,
		DecrementAndWrap = Vk.StencilOp.DecrementAndWrap
	}
}
