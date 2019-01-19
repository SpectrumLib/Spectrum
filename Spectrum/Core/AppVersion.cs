using System;

namespace Spectrum
{
	/// <summary>
	/// Object representation of a Major.Minor.Revision version structure, with an optional name for the version.
	/// </summary>
	public struct AppVersion : IComparable, IComparable<AppVersion>, IEquatable<AppVersion>
	{
		/// <summary>
		/// Default value for the app version, 0.0.0 with no name.
		/// </summary>
		public static readonly AppVersion Default = default(AppVersion);

		#region Fields
		/// <summary>
		/// Major version number.
		/// </summary>
		public readonly uint Major;
		/// <summary>
		/// Minor version number.
		/// </summary>
		public readonly uint Minor;
		/// <summary>
		/// Revision version number.
		/// </summary>
		public readonly uint Revision;
		/// <summary>
		/// Optional string name for this version. Does not factor into version comparisons.
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// Gets if the version has a name.
		/// </summary>
		public bool HasName => Name != null;
		/// <summary>
		/// Gets the version as a unique integer formatted as 0xMMmmRRRR.
		/// </summary>
		public uint Value => (Major << 24) & (Minor << 16) & Revision;
		#endregion // Fields

		/// <summary>
		/// Creates a new version object.
		/// </summary>
		/// <param name="major">The major version number.</param>
		/// <param name="minor">The minor version number.</param>
		/// <param name="revision">The optional revision version number, defaults to 0.</param>
		/// <param name="tag">The optional name of the version, defaults to no name.</param>
		public AppVersion(uint major, uint minor, uint revision = 0, string tag = null)
		{
			Major = major;
			Minor = minor;
			Revision = revision;
			Name = tag;
		}
		// From vulkan version
		internal AppVersion(VulkanCore.Version v)
		{
			Major = (uint)v.Major;
			Minor = (uint)v.Minor;
			Revision = (uint)v.Patch;
			Name = "";
		}

		public override string ToString()
		{
			return $"{{{Major}.{Minor}.{Revision}}}";
		}

		/// <summary>
		/// Gets a string representation of the version with the name, if it has one.
		/// </summary>
		/// <returns>A string formatted as <c>{Major.Minor.Revision (Name)}</c>.</returns>
		public string ToLongString()
		{
			return $"{{{Major}.{Minor}.{Revision}{(HasName ? $" ({Name})" : "")}}}";
		}

		public override bool Equals(object obj)
		{
			return (obj as IEquatable<AppVersion>)?.Equals(this) ?? false;
		}

		bool IEquatable<AppVersion>.Equals(AppVersion other)
		{
			return other.Value == Value;
		}

		public override int GetHashCode()
		{
			return (int)Value;
		}

		/// <summary>
		/// Attempts to parse a version string into an AppVersion object. Throws an exception if it cannot. The string
		/// can be one of the following formats, where whitespace is unimportant:
		/// <list type="bullet">
		///		<item><c>major.minor</c></item>
		///		<item><c>major.minor (name)</c></item>
		///		<item><c>major.minor.revision</c></item>
		///		<item><c>major.minor.revision (name)</c></item>
		/// </list>
		/// </summary>
		/// <param name="str">The string to parse.</param>
		/// <returns>The object representing the version string.</returns>
		public static AppVersion Parse(string str)
		{
			uint maj, min, rev = 0;
			string name = null;
			var split = str.Split('.', '(');
			if (split.Length < 2)
				throw new FormatException("The version string was malformed.");

			if (!UInt32.TryParse(split[0], out maj))
				throw new FormatException($"The version string major component was not a valid UInt32 ({split[0]}).");
			if (!UInt32.TryParse(split[1], out min))
				throw new FormatException($"The version string minor component was not a valid UInt32 ({split[1]}).");

			if (split.Length == 3)
			{
				if (split[2].EndsWith(")"))
					name = split[2].Substring(0, split[2].Length - 1);
				else if (!UInt32.TryParse(split[2], out rev))
					throw new FormatException($"The version string revision component was not a valid UInt32 ({split[2]}).");
			}
			else if (split.Length > 3)
			{
				if (!UInt32.TryParse(split[2], out rev))
					throw new FormatException($"The version string revision component was not a valid UInt32 ({split[2]}).");
				if (split[3].EndsWith(")"))
					name = split[3].Substring(0, split[3].Length - 1);
			}

			return new AppVersion(maj, min, rev, name);
		}

		/// <summary>
		/// Safe, no-exception version of <see cref="Parse"/>.
		/// </summary>
		/// <param name="str">The string to parse.</param>
		/// <param name="appv">The object to place the parsed version into, <see cref="Default"/> if invalid.</param>
		/// <returns>If the parse was successful.</returns>
		public static bool TryParse(string str, out AppVersion appv)
		{
			try
			{
				appv = Parse(str);
				return true;
			}
			catch
			{
				appv = AppVersion.Default;
				return false;
			}
		}

		/// <summary>
		/// Implictly attempts to cast the string to a version object using <see cref="Parse"/>.
		/// </summary>
		/// <param name="str">The string to cast.</param>
		public static implicit operator AppVersion (string str)
		{
			return Parse(str);
		}

		// Conversion to and from vulkan versions
		internal VulkanCore.Version ToVkVersion() => new VulkanCore.Version((int)Major, (int)Minor, (int)Revision);

		#region IComparable
		int IComparable<AppVersion>.CompareTo(AppVersion other)
		{
			return (this < other) ? -1 : (this > other) ? 1 : 0;
		}

		int IComparable.CompareTo(object obj)
		{
			return -((obj as IComparable<AppVersion>)?.CompareTo(this) ?? 1);
		}
		#endregion // IComparable

		#region Operators
		public static bool operator == (AppVersion l, AppVersion r)
		{
			return l.Value == r.Value;
		}

		public static bool operator != (AppVersion l, AppVersion r)
		{
			return l.Value != r.Value;
		}

		public static bool operator < (AppVersion l, AppVersion r)
		{
			return l.Value < r.Value;
		}

		public static bool operator > (AppVersion l, AppVersion r)
		{
			return l.Value > r.Value;
		}

		public static bool operator <= (AppVersion l, AppVersion r)
		{
			return l.Value <= r.Value;
		}

		public static bool operator >= (AppVersion l, AppVersion r)
		{
			return l.Value >= r.Value;
		}
		#endregion // Operators
	}
}
