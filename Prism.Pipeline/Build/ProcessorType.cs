using System;
using System.Linq;
using System.Reflection;

namespace Prism.Build
{
	// Holds a Type instance and associated information about a ContentProcessor type
	internal class ProcessorType
	{
		public static readonly string INTERFACE_NAME = typeof(IContentProcessor).FullName;
		public static readonly Type ATTRIBUTE_TYPE = typeof(ContentProcessorAttribute);
		public static readonly string WRITER_INTERFACE_NAME = typeof(IContentWriter).FullName;

		#region Fields
		public readonly Type Type;
		public string TypeName => Type.Name;

		public readonly Type InputType;
		public readonly Type OutputType;
		public readonly Type WriterType;

		public readonly ProcessorField[] Fields;

		public readonly ContentProcessorAttribute Attribute;
		#endregion // Fields

		private ProcessorType(Type type, ProcessorField[] fields, ContentProcessorAttribute attrib)
		{
			Type = type;
			var genArgs = type.BaseType.GetGenericArguments();
			InputType = genArgs[0];
			OutputType = genArgs[1];
			WriterType = genArgs[2];
			Fields = fields;
			Attribute = attrib;
		}

		public IContentProcessor CreateInstance() => (IContentProcessor)Activator.CreateInstance(Type);

		public IContentWriter CreateWriterInstance() => (IContentWriter)Activator.CreateInstance(WriterType);

		// Attempts to create an ProcessorType out of a passed type, will return null if it is invalid
		public static ProcessorType TryCreate(BuildEngine engine, Type type)
		{
			// Check if it is derived from IContentProcessor
			if (type.GetInterface(INTERFACE_NAME) == null)
				return null;

			// Must have the correct attribute
			ContentProcessorAttribute attrib =
				(ContentProcessorAttribute)type.GetCustomAttributes(ATTRIBUTE_TYPE, false).FirstOrDefault();
			if (attrib == null)
			{
				engine.Logger.EngineError($"The type '{type.FullName}' is a ContentProcessor but is missing the required attribute.");
				return null;
			}

			// Validate the attribute information
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

			// Validate the specified content writer
			Type writerType = type.GetGenericArguments()[2];
			if (writerType.IsAbstract)
			{
				WriterError(engine, writerType, "is abstract and cannot be instantiated.");
				return null;
			}
			if (writerType.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis, new Type[0], null) == null)
			{
				WriterError(engine, writerType, "does not have a public no-args constructor, and cannot be instantiated.");
				return null;
			}

			// Get the fields
			var fields = ProcessorField.LoadFromType(engine, type);
			if (fields == null)
				return null;

			// Good to go
			return new ProcessorType(type, fields, attrib);
		}

		private static void TypeError(BuildEngine engine, Type type, string error) =>
			engine.Logger.EngineError($"The ContentProcessor type '{type.FullName}' {error}.");

		private static void WriterError(BuildEngine engine, Type type, string error) =>
			engine.Logger.EngineError($"The ContentWriter type '{type.FullName}' {error}.");
	}

	// Holds information about a field in a ContentProcessor instance that is decorated with a PipelineParameterAttribute
	internal class ProcessorField
	{
		public static readonly Type ATTRIBUTE_TYPE = typeof(PipelineParameterAttribute);
		public static readonly Type STRING_TYPE = typeof(String);

		#region Fields
		public readonly FieldInfo Info;
		public string Name => Info.Name;
		public Type FieldType => Info.FieldType;

		public readonly PipelineParameterAttribute Attribute;
		public string ParamName => Attribute.Name ?? Info.Name; // The name as it appears in the content project file
		public readonly object DefaultValue;
		#endregion // Fields

		private ProcessorField(FieldInfo info, PipelineParameterAttribute attrib)
		{
			Info = info;
			Attribute = attrib;
			DefaultValue = Attribute.DefaultValue ?? (FieldType.IsValueType ? Activator.CreateInstance(FieldType) : "");
		}

		// This is guarenteed to already by a valid ContentProcessor type
		public static ProcessorField[] LoadFromType(BuildEngine engine, Type type)
		{
			return type
				.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
				.Select(f => (field: f, attrib: f.GetCustomAttribute(ATTRIBUTE_TYPE, false)))
				.Where(f => f.attrib != null && !f.field.IsInitOnly && (f.field.FieldType.IsValueType || (f.field.FieldType == STRING_TYPE)))
				.Select(f => new ProcessorField(f.field, (PipelineParameterAttribute)f.attrib))
				.ToArray();
		}
	}
}
