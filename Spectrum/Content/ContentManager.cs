/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Spectrum.Content
{
	/// <summary>
	/// Manages the loading and lifetime of content items from the filesystem. Disposable items loaded with manager
	/// instances are tracked and cleaned up then the manager is disposed, but can manually be disposed as well.
	/// <para>
	/// Additionally, managers can perform caching of items, which allows fast reuse of items that have already been
	/// loaded once. Each content item can be cached as multiple types, if supported.
	/// </para>
	/// <para>
	/// Multiple manager instances can safely point to and use the same content pack at the same time. Instances of
	/// this type are *not* threadsafe.
	/// </para>
	/// </summary>
	public sealed class ContentManager : IDisposable
	{
		private static readonly List<WeakReference<ContentManager>> _Managers = 
			new List<WeakReference<ContentManager>>();
		private static readonly object _ManagersLock = new object();

		#region Fields
		// Pack and loaders
		private readonly ContentPack _pack;
		private readonly Dictionary<string, IContentLoader> _loaderCache = new Dictionary<string, IContentLoader>();

		// Cache objects
		private readonly List<IDisposable> _disposables = new List<IDisposable>();
		private readonly Dictionary<(string, Type), object> _cache = new Dictionary<(string, Type), object>();

		/// <summary>
		/// The load time of the most recent content item. Checking this value as equal to <see cref="TimeSpan.Zero"/>
		/// can be used to check if an item was loaded from the cache.
		/// </summary>
		public TimeSpan LastLoadTime { get; private set; } = TimeSpan.Zero;

		private bool _isDisposed = false;
		#endregion // Fields

		private ContentManager(ContentPack pack)
		{
			_pack = pack;

			AddManager(this);
		}
		~ContentManager()
		{
			dispose(false);
		}

		#region Load Functions
		/// <summary>
		/// Attempts to load the content item with the given name as <typeparamref name="T"/>. Will check if the item
		/// is cached before loading it from the filesystem.
		/// </summary>
		/// <typeparam name="T">The type of the content item to create.</typeparam>
		/// <param name="name">The name of the content item.</param>
		/// <param name="cache">If the loaded item should be stored in the cache.</param>
		/// <param name="manage">
		/// If the content manager should perform lifetime tracking for <typeparamref name="T"/> types that implement
		/// <see cref="IDisposable"/>. Content loaded with this option <b>must not</b> manually call 
		/// <see cref="IDisposable.Dispose"/>, or the content will be disposed twice.
		/// </param>
		/// <returns>The cached item, or newly loaded item.</returns>
		public T Load<T>(string name, bool cache = true, bool manage = true)
			where T : class
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Content item name cannot be null or whitespace", nameof(name));

			// Try the cache first
			if (_cache.TryGetValue((name, typeof(T)), out var cval))
			{
				LastLoadTime = TimeSpan.Zero;
				return cval as T;
			}

			// Find the item
			if (!_pack.TryGetItem(name, out var item))
				throw new ContentLoadException(name, $"The content item '{name}' does not exist");

			// Try loading the item
			Stopwatch timer = Stopwatch.StartNew();
			var content = readContentItem(item, typeof(T));
			if (cache)
				_cache.Add((item.Name, typeof(T)), content);
			if (manage && (content is IDisposable idc))
				_disposables.Add(idc);
			LastLoadTime = timer.Elapsed;
			return content as T;
		}

		/// <summary>
		/// Similar to <see cref="Load"/>, but will attempt to load localized content items first by appending locale
		/// codes to the end of the content name.
		/// <para>
		/// It will first try to load by region identifier (e.g. `en-US`), then by language (e.g. `en`), then will
		/// fallback to an unlocalized version.
		/// </para>
		/// </summary>
		/// <typeparam name="T">The type of the content item to create.</typeparam>
		/// <param name="name">The name of the content item.</param>
		/// <param name="culture">The culture to use as the localization, or <c>null</c> for the current culture.</param>
		/// <param name="cache">If the loaded item should be stored in the cache.</param>
		/// <param name="manage">
		/// If the content manager should perform lifetime tracking for <typeparamref name="T"/> types that implement
		/// <see cref="IDisposable"/>. Content loaded with this option <b>must not</b> manually call 
		/// <see cref="IDisposable.Dispose"/>, or the content will be disposed twice.
		/// </param>
		/// <returns>The cached item, or newly loaded item.</returns>
		public T LoadLocalized<T>(string name, CultureInfo culture = null, bool cache = true, bool manage = true)
			where T : class
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Content item name cannot be null or whitespace", nameof(name));
			culture ??= CultureInfo.CurrentCulture;

			// Region
			if (_pack.TryGetItem($"{name}.{culture.Name}", out var ritem))
				return Load<T>(ritem.Name, cache, manage);

			// Language
			if (_pack.TryGetItem($"{name}.{culture.TwoLetterISOLanguageName}", out var litem))
				return Load<T>(litem.Name, cache, manage);

			// Fallback
			return Load<T>(name, cache, manage);
		}

		/// <summary>
		/// Similar to <see cref="Load"/>, but does not check the item cache, instead loading directly from the
		/// filesystem. If <paramref name="cache"/> is <c>true</c>, then the item loaded by this function will
		/// replace the cached item.
		/// </summary>
		/// <typeparam name="T">The type of the content item to create.</typeparam>
		/// <param name="name">The name of the content item.</param>
		/// <param name="cache">If the loaded item should be stored in the cache.</param>
		/// <param name="manage">
		/// If the content manager should perform lifetime tracking for <typeparamref name="T"/> types that implement
		/// <see cref="IDisposable"/>. Content loaded with this option <b>must not</b> manually call 
		/// <see cref="IDisposable.Dispose"/>, or the content will be disposed twice.
		/// </param>
		/// <returns>The newly loaded item.</returns>
		public T Reload<T>(string name, bool cache = true, bool manage = true)
			where T : class
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Content item name cannot be null or whitespace", nameof(name));

			// Find the item
			if (!_pack.TryGetItem(name, out var item))
				throw new ContentLoadException(name, $"The content item '{name}' does not exist");

			// Try loading the item
			Stopwatch timer = Stopwatch.StartNew();
			var content = readContentItem(item, typeof(T));
			if (cache)
				_cache[(item.Name, typeof(T))] = content;
			if (manage && (content is IDisposable idc))
				_disposables.Add(idc);
			LastLoadTime = timer.Elapsed;
			return content as T;
		}

		/// <summary>
		/// Similar to <see cref="Reload"/>, but will attempt to load localized content items first by appending locale
		/// codes to the end of the content name.
		/// </summary>
		/// <typeparam name="T">The type of the content item to create.</typeparam>
		/// <param name="name">The name of the content item.</param>
		/// <param name="culture">The culture to use as the localization, or <c>null</c> for the current culture.</param>
		/// <param name="cache">If the loaded item should be stored in the cache.</param>
		/// <param name="manage">
		/// If the content manager should perform lifetime tracking for <typeparamref name="T"/> types that implement
		/// <see cref="IDisposable"/>. Content loaded with this option <b>must not</b> manually call 
		/// <see cref="IDisposable.Dispose"/>, or the content will be disposed twice.
		/// </param>
		/// <returns>The newly loaded item.</returns>
		public T ReloadLocalized<T>(string name, CultureInfo culture = null, bool cache = true, bool manage = true)
			where T : class
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Content item name cannot be null or whitespace", nameof(name));
			culture ??= CultureInfo.CurrentCulture;

			// Region
			if (_pack.TryGetItem($"{name}.{culture.Name}", out var ritem))
				return Reload<T>(ritem.Name, cache, manage);

			// Language
			if (_pack.TryGetItem($"{name}.{culture.TwoLetterISOLanguageName}", out var litem))
				return Reload<T>(litem.Name, cache, manage);

			// Fallback
			return Reload<T>(name, cache, manage);
		}

		/// <summary>
		/// Loads the raw binary data for the content item, performing decompression if needed.
		/// </summary>
		/// <param name="name">The name of the content item.</param>
		/// <param name="buffer">The buffer to read the data into, must be large enough to store the data.</param>
		/// <returns>The total number of bytes read.</returns>
		public uint LoadRaw(string name, Span<byte> buffer)
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Content item name cannot be null or whitespace", nameof(name));

			// Find the item
			if (!_pack.TryGetItem(name, out var item))
				throw new ContentLoadException(name, $"The content item '{name}' does not exist");
			if ((ulong)buffer.Length < item.DataSize)
				throw new ContentLoadException(item.Name, "Buffer too small for item data");

			// Open the content stream
			ContentStream stream;
			try
			{
				stream = new ContentStream(_pack.Release, _pack.Directory.FullName, item);
			}
			catch (ContentLoadException) { throw; }
			catch (Exception e)
			{
				throw new ContentLoadException(item.Name, "Failed to open content stream", e);
			}

			// Load in the bytes
			try
			{
				return (uint)stream.Read(buffer);
			}
			catch (ContentLoadException) { throw; }
			catch (Exception e)
			{
				throw new ContentLoadException(item.Name, "Failed to read content item data", e);
			}
		}

		/// <summary>
		/// Similar to <see cref="LoadRaw"/>, but will attempt to load localized content items first by appending locale
		/// codes to the end of the content name.
		/// </summary>
		/// <param name="name">The name of the content item.</param>
		/// <param name="buffer">The buffer to read the data into, must be large enough to store the data.</param>
		/// <param name="culture">The culture to use as the localization, or <c>null</c> for the current culture.</param>
		/// <returns>The total number of bytes read.</returns>
		public uint LoadLocalizedRaw(string name, Span<byte> buffer, CultureInfo culture = null)
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Content item name cannot be null or whitespace", nameof(name));
			culture ??= CultureInfo.CurrentCulture;

			// Find the item
			bool found =
				_pack.TryGetItem($"{name}.{culture.Name}", out var item) ||
				_pack.TryGetItem($"{name}.{culture.TwoLetterISOLanguageName}", out item) ||
				_pack.TryGetItem(name, out item);
			if (!found)
				throw new ContentLoadException(name, $"The content item '{name}' does not exist");
			if ((ulong)buffer.Length < item.DataSize)
				throw new ContentLoadException(item.Name, "Buffer too small for item data");

			// Open the content stream
			ContentStream stream;
			try
			{
				stream = new ContentStream(_pack.Release, _pack.Directory.FullName, item);
			}
			catch (ContentLoadException) { throw; }
			catch (Exception e)
			{
				throw new ContentLoadException(item.Name, "Failed to open content stream", e);
			}

			// Load in the bytes
			try
			{
				return (uint)stream.Read(buffer);
			}
			catch (ContentLoadException) { throw; }
			catch (Exception e)
			{
				throw new ContentLoadException(item.Name, "Failed to read content item data", e);
			}
		}

		/// <summary>
		/// Gets the length of the raw data for the content item. Designed for use with the <see cref="LoadRaw"/>
		/// function, to get the total size needed for the buffer.
		/// </summary>
		/// <param name="name">The content item name.</param>
		/// <returns>The content item data size, in bytes.</returns>
		public ulong GetItemDataSize(string name)
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Content item name cannot be null or whitespace", nameof(name));

			// Find the item
			if (!_pack.TryGetItem(name, out var item))
				throw new ContentLoadException(name, $"The content item '{name}' does not exist");

			return item.DataSize;
		}
		#endregion // Load Functions

		#region Load Impl
		private object readContentItem(ContentPack.Entry item, Type type)
		{
			// Get and reset the loader
			var loader = getOrCreateLoader(item);
			try
			{
				if (!loader.ContentType.IsAssignableFrom(type))
				{
					throw new ContentLoadException(item.Name,
						$"Loader/Runtime type mismatch ({loader.ContentType.Name} <=> {type.Name})");
				}
				loader.Reset();
			}
			catch (ContentLoadException) { throw; }
			catch (Exception e)
			{
				throw new ContentLoadException(item.Name, 
					$"Failed to reset loader '{loader.GetType().Name}'", e);
			}

			// Open the content stream
			ContentReader reader;
			try
			{
				reader = new ContentReader(new ContentStream(_pack.Release, _pack.Directory.FullName, item));
			}
			catch (Exception e)
			{
				throw new ContentLoadException(item.Name, "Failed to open content stream", e);
			}

			// Perform the object load and final type validation
			try
			{
				LoaderContext ctx = new LoaderContext(item, type);
				object result = loader.Load(reader, ctx);
				if (result is null)
					throw new ContentLoadException(item.Name, "Content loader produced a null value");
				if (!type.IsAssignableFrom(result.GetType()))
				{
					throw new ContentLoadException(item.Name,
						$"Generated/Requested type mismatch ({result.GetType().Name} <=> {type.Name})");
				}
				return result;
			}
			catch (ContentLoadException) { throw; }
			catch (Exception e)
			{
				throw new ContentLoadException(item.Name,
					$"Unhandled exception in content load ({e.GetType().Name})", e);
			}
			finally
			{
				reader.Dispose();
			}
		}

		private IContentLoader getOrCreateLoader(ContentPack.Entry item)
		{
			if (_loaderCache.TryGetValue(item.Type, out var cached))
				return cached;

			var ltype = LoaderRegistry.FindContentType(item.Type);
			if (ltype is null)
				throw new ContentLoadException(item.Name, $"No registered loader for type '{item.Type}'");

			try
			{
				var inst = ltype.Ctor.Invoke(null) as IContentLoader;
				_loaderCache.Add(item.Type, inst);
				return inst;
			}
			catch (Exception e)
			{
				throw new ContentLoadException(
					item.Name, $"Failed to initialize loader for type '{item.Type}'", e);
			}
		}
		#endregion // Load Impl

		#region Cache
		/// <summary>
		/// Clears the cache, and disposes all items being tracked by the instance. This will invalidate all
		/// <see cref="IDisposable"/> instances loaded through this instance.
		/// </summary>
		/// <returns>The number of items that were cleared from the cache, and were disposed.</returns>
		public (uint Cache, uint Dispose) UnloadAll()
		{
			var res = ((uint)_cache.Count, (uint)_disposables.Count);
			_cache.Clear();
			_disposables.ForEach(dis => dis.Dispose());
			_disposables.Clear();
			return res;
		}

		/// <summary>
		/// Gets if the item with the given name and type is currently cached by this manager.
		/// </summary>
		/// <typeparam name="T">The type of the cached item to check for.</typeparam>
		/// <param name="name">The item name to check for.</param>
		/// <returns>If the item is cached.</returns>
		public bool IsItemCached<T>(string name) where T : class => !String.IsNullOrWhiteSpace(name)
			? (_cache.TryGetValue((name, typeof(T)), out var item) && (item is T))
			: throw new ArgumentException("Item name cannot be null or whitespace", nameof(name));

		/// <summary>
		/// Gets if the item is cached by this manager.
		/// </summary>
		/// <typeparam name="T">The type of the item.</typeparam>
		/// <param name="item">The item to check for.</param>
		/// <returns>If the item is cached.</returns>
		public bool IsItemCached<T>(T item) where T : class => !(item is null)
			? _cache.Values.Any(obj => ReferenceEquals(obj, item))
			: throw new ArgumentNullException(nameof(item));

		/// <summary>
		/// Gets if the disposable item lifetime is controlled by this manager.
		/// </summary>
		/// <typeparam name="T">The item type.</typeparam>
		/// <param name="item">The item to check for.</param>
		/// <returns>If the item is disposed by this manager.</returns>
		public bool IsItemManaged<T>(T item) where T : class, IDisposable => !(item is null)
			? _disposables.Any(disp => ReferenceEquals(item, disp))
			: throw new ArgumentNullException(nameof(item));

		/// <summary>
		/// Removes the content item with the name and type from the cache.
		/// </summary>
		/// <typeparam name="T">The item type to remove.</typeparam>
		/// <param name="name">The item name to remove.</param>
		/// <returns>If an item with the given name and type was in the cache, and was removed.</returns>
		public bool RemoveFromCache<T>(string name) where T : class => !String.IsNullOrWhiteSpace(name)
			? _cache.Remove((name, typeof(T)))
			: throw new ArgumentException("Item name cannot be null or whitespace", nameof(name));

		/// <summary>
		/// Removes the content item from the cache.
		/// </summary>
		/// <typeparam name="T">The item type to remove.</typeparam>
		/// <param name="item">The item to remove.</param>
		/// <returns>If the item was in the cache, and was removed.</returns>
		public bool RemoveFromCache<T>(T item) where T : class => !(item is null)
			? (_cache.FirstOrDefault(pair => ReferenceEquals(pair.Value, item)) is var pair) && !(pair.Key.Item1 is null) 
				&& _cache.Remove(pair.Key)
			: throw new ArgumentNullException(nameof(item));

		/// <summary>
		/// Removes the disposable content item from this manager, optionally disposing it. This function can be used
		/// to take control of the lifetime of a disposable content item. This function will also remove the item
		/// from the cache, if present.
		/// </summary>
		/// <param name="item">The item to remove from the list of managed items.</param>
		/// <param name="dispose">If the item should be disposed when it is removed.</param>
		/// <returns>If the item was managed by this manager, and was removed.</returns>
		public bool StopManaging(IDisposable item, bool dispose = false)
		{
			if (item is null)
				throw new ArgumentNullException(nameof(item));

			RemoveFromCache(item);
			
			var didx = _disposables.IndexOf(ditem => ReferenceEquals(item, ditem));
			if (didx >= 0)
				_disposables.RemoveAt(didx);
			return (didx >= 0);
		}
		#endregion // Cache

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				// Remove the data
				_cache.Clear();
				if (disposing)
					_disposables.ForEach(dis => dis.Dispose());
				_disposables.Clear();

				// Remove the loaders
				if (disposing)
					_loaderCache.Values.ForEach(ldr => (ldr as IDisposable)?.Dispose());
				_loaderCache.Clear();

				RemoveManager(this);
			}
			_isDisposed = true;
		}
		#endregion // IDisposable

		#region ContentManager Lifetime
		/// <summary>
		/// Attempts to open a ContentManager instance which sources content from the given content pack file.
		/// </summary>
		/// <param name="path">The path to the content pack (.cpak) file.</param>
		/// <returns>The new content manager.</returns>
		public static ContentManager OpenContentPack(string path)
		{
			// Get the content pack
			if (!path.EndsWith(ContentPack.FILE_EXTENSION))
				path += $"Content{ContentPack.FILE_EXTENSION}";
			var pack = ContentPack.GetOrLoad(path);

			return new ContentManager(pack);
		}

		private static void AddManager(ContentManager cm)
		{
			lock (_ManagersLock)
			{
				_Managers.Add(new WeakReference<ContentManager>(cm));
				_Managers.RemoveAll(wref => !wref.TryGetTarget(out _));
			}
		}

		private static void RemoveManager(ContentManager cm)
		{
			lock (_ManagersLock)
			{
				_Managers.RemoveAll(wref => !wref.TryGetTarget(out var targ) || ReferenceEquals(targ, cm));
			}
		}
		#endregion // ContentManager Lifetime
	}
}
