using System;
using System.Linq;
using System.Reflection;

namespace Prism.Build
{
	// Holds a Type instance and associated information about a ContentImporter type
	internal class ImporterType
	{
		public static readonly string INTERFACE_NAME = typeof(IContentImporter).FullName;
		public static readonly Type ATTRIBUTE_TYPE = typeof(ContentImporterAttribute);

		#region Fields
		public readonly Type Type;
		public string TypeName => Type.Name;

		public readonly Type OutputType;

		public readonly ContentImporterAttribute Attribute;
		#endregion // Fields

		private ImporterType(Type type, ContentImporterAttribute attrib)
		{
			Type = type;
			OutputType = type.BaseType.GetGenericArguments()[0];
			Attribute = attrib;
		}

		public IContentImporter CreateInstance() => (IContentImporter)Activator.CreateInstance(Type);

		// Attempts to create an ImporterType out of a passed type, will return null if it is invalid
		public static ImporterType TryCreate(BuildEngine engine, Type type)
		{
			// Check if it is derived from IContentImporter
			if (type.GetInterface(INTERFACE_NAME) == null)
				return null;

			// Must have the correct attribute
			ContentImporterAttribute attrib = 
				(ContentImporterAttribute)type.GetCustomAttributes(ATTRIBUTE_TYPE, false)?.FirstOrDefault();
			if (attrib == null)
			{
				engine.Logger.EngineError($"The type '{type.FullName}' is a ContentImporter but is missing the required attribute.");
				return null;
			}
			if (!attrib.Enabled)
			{
				engine.Logger.EngineInfo($"Skipping ContentImporter type '{type.FullName}' - it is marked as disabled.");
				return null;
			}

			// Validate the attribute information
			if (attrib.DefaultProcessor?.GetInterface(ProcessorType.INTERFACE_NAME) == null)
			{
				TypeError(engine, type, $"has an invalid default ContentProcessor type '{attrib.DefaultProcessor?.FullName}'");
				return null;
			}
			if (attrib.DisplayName == null)
			{
				TypeError(engine, type, "cannot have a null display name.");
				return null;
			}

			// Ensure some required type info
			if (type.IsAbstract)
			{
				TypeError(engine, type, "is abstract and cannot be instantiated.");
				return null;
			}
			if (type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis, new Type[0], null) == null)
			{
				TypeError(engine, type, "does not have a public no-args constructor, and cannot be instantiated.");
				return null;
			}

			// Good to go
			return new ImporterType(type, attrib);
		}

		private static void TypeError(BuildEngine engine, Type type, string error) => 
			engine.Logger.EngineError($"The ContentImporter type '{type.FullName}' {error}.");
	}
}
