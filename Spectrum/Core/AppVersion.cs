using System;

namespace Spectrum
{
	/// <summary>
	/// Object representation of a Major.Minor.Patch version structure, with an optional name for the version.
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
		/// Patch version number.
		/// </summary>
		public readonly uint Patch;
		/// <summary>
		/// Optional string name for this version. Does not factor into version comparisons.
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// Gets if the version has a name.
		/// </summary>
		public bool HasName => Name != null;
		/// <summary>
		/// Gets the version as a unique integer formatted as 0xVVRRPPPP.
		/// </summary>
		public uint Value => (Major << 24) & (Minor << 16) & Patch;
		#endregion // Fields

		/// <summary>
		/// Creates a new version object.
		/// </summary>
		/// <param name="major">The major version number.</param>
		/// <param name="minor">The minor version number.</param>
		/// <param name="patch">The optional patch version number, defaults to 0.</param>
		/// <param name="tag">The optional name of the version, defaults to no name.</param>
		public AppVersion(uint major, uint minor, uint patch = 0, string tag = null)
		{
			Major = major;
			Minor = minor;
			Patch = patch;
			Name = tag;
		}

		public override string ToString()
		{
			return $"{{{Major}.{Minor}.{Patch}}}";
		}

		/// <summary>
		/// Gets a string representation of the version with the name, if it has one.
		/// </summary>
		/// <returns>A string formatted as <c>{Major.Minor.Patch (Name)}</c>.</returns>
		public string ToLongString()
		{
			return $"{{{Major}.{Minor}.{Patch}{(HasName ? $" ({Name})" : "")}}}";
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
