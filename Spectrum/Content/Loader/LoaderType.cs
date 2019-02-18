using System;
using System.Linq;
using System.Reflection;

namespace Spectrum.Content
{
	// Holds information about a ContentLoader type
	internal class LoaderType
	{
		public static readonly string INTERFACE_NAME = typeof(IContentLoader).FullName;
		public static readonly Type ATTRIBUTE_TYPE = typeof(ContentLoaderAttribute);

		#region Fields
		public readonly Type Type;
		public readonly Type ContentType;
		public readonly string Name; // The [Assembly:Name] formatted name used by ContentWriters
		#endregion // Fields

		internal LoaderType(Type type, string assembly, string name)
		{
			Type = type;
			ContentType = type.BaseType.GetGenericArguments()[0];
			Name = $"{assembly}:{name}";
		}

		public IContentLoader CreateInstance() => (IContentLoader)Activator.CreateInstance(Type);

		// Returns null means that it could not load, but error != null means that there was an error
		public static LoaderType TryCreate(Type type, string assembly, string name, out string error)
		{
			error = null;

			// Check if it derives from ContentLoader
			if (type.GetInterface(INTERFACE_NAME) == null)
				return null;

			// Check for the attribute
			ContentLoaderAttribute attrib =
				(ContentLoaderAttribute)type.GetCustomAttributes(ATTRIBUTE_TYPE, false)?.FirstOrDefault();
			if (attrib == null)
			{
				error = "the type does not have a ContentLoader attribute.";
				return null;
			}
			if (attrib.Name != name)
				return null; // Not an error, just not the correct type
			if (!attrib.Enabled)
			{
				error = "the type is not currently enabled.";
				return null;
			}

			// Ensure some required type info
			if (type.IsAbstract)
			{
				error = "the type is abstract and cannot be instantiated.";
				return null;
			}
			if (type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis, new Type[0], null) == null)
			{
				error = "the type does not have a public no-args constructor, and cannot be instantiated.";
				return null;
			}

			// Good to go
			return new LoaderType(type, assembly, name);
		}
	}
}
