/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Spectrum.InternalLog;

namespace Spectrum.Content
{
	/// <summary>
	/// Contains the collection of registered <see cref="ContentLoader{T}"/> types that can be instantiated and used
	/// to load content items at runtime.
	/// </summary>
	public static class LoaderRegistry
	{
		private static readonly List<LoaderType> _Loaders = new List<LoaderType>();

		internal static LoaderType FindDisplayName(string name) =>
			_Loaders.FirstOrDefault(lt => lt.Attr.DisplayName == name);

		internal static LoaderType FindContentType(string type) =>
			_Loaders.FirstOrDefault(lt => lt.Attr.ContentType == type);

		/// <summary>
		/// Registers all valid <see cref="ContentLoader{T}"/> types from the passed assembly.
		/// </summary>
		/// <param name="asm">The assembly to load types from.</param>
		/// <returns>
		/// Pairs of types and strings, for types that were attempted to be registered. If the string is not null,
		/// then the type could not be registered, and the string gives the error message.
		/// </returns>
		public static (Type, string)[] RegisterAssembly(Assembly asm)
		{
			if (asm is null)
				throw new ArgumentNullException(nameof(asm));

			// Pull types
			var types = asm .GetExportedTypes()
							.Where(typ => typ.GetCustomAttribute(ContentLoaderAttribute.TYPE) != null)
							.Where(typ => typ.IsSubclassOf(IContentLoader.TYPE))
							.Where(typ => !typ.IsAbstract && !typ.IsGenericType);

			// Iterate and extract the filtered types
			List<(Type, string)> ret = new List<(Type, string)>();
			foreach (var loadtype in types)
			{
				if (!RegisterType(loadtype, out var err))
					IWARN($"Ignoring ContentLoader type '{loadtype.Name}' - {err}.");
				ret.Add((loadtype, err));
			}
			return ret.ToArray();
		}

		/// <summary>
		/// Registers the type, which must be a valid <see cref="ContentLoader{T}"/> type.
		/// </summary>
		/// <param name="type">The type to register.</param>
		/// <param name="err">A description of the error that occured while trying to register the type.</param>
		/// <returns>If the type was registered.</returns>
		public static bool RegisterType(Type type, out string err)
		{
			try
			{
				// Validate the type and attrib
				if (!type.IsSubclassOf(IContentLoader.TYPE))
					throw new ContentException("Type is not subclass of ContentLoader<T>");
				if (type.IsAbstract)
					throw new ContentException("Is abstract, and cannot be instantiated");
				if (type.IsGenericType)
					throw new ContentException("Is generic, and cannot be instantiated.");

				// Get ctor info
				var ctor = type.GetConstructor(Type.EmptyTypes);
				if (ctor is null)
					throw new ContentException("No public, no-args constructor");

				// Check attribute values
				var attr = type.GetCustomAttribute(ContentLoaderAttribute.TYPE) as ContentLoaderAttribute;
				if (attr is null)
					throw new ContentException("Is not decorated with ContentLoaderAttribute");
				foreach (var ltype in _Loaders)
				{
					if (ltype.Attr.DisplayName == attr.DisplayName)
						throw new ContentException($"Duplicate display name {attr.DisplayName}");
					if (ltype.Attr.ContentType == attr.ContentType)
						throw new ContentException($"Duplicate content type {attr.ContentType}");
				}

				// Add the type
				_Loaders.Add(new LoaderType(type, attr, ctor));
				err = null;
				return true;
			}
			catch (ContentException e)
			{
				err = e.Message;
				return false;
			}
		}

		static LoaderRegistry()
		{
			_Loaders.Add(new LoaderType(PassthroughLoader.TYPE, PassthroughLoader.ATTR, PassthroughLoader.CTOR));
			_Loaders.Add(new LoaderType(AudioLoader.TYPE, AudioLoader.ATTR, AudioLoader.CTOR));
		}
	}

	internal class LoaderType
	{
		public readonly Type Type;
		public readonly ContentLoaderAttribute Attr;
		public readonly ConstructorInfo Ctor;

		public LoaderType(Type t, ContentLoaderAttribute a, ConstructorInfo c)
		{
			Type = t;
			Attr = a;
			Ctor = c;
		}
	}
}
