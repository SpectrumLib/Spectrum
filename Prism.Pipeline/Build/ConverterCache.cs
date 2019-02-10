using System;
using System.Collections.Generic;

namespace Prism.Build
{
	// Function for attempting to convert a string to a pipeline parameter value
	//   Should put the converted value in the out argument, and return if the conversion was successful
	internal delegate bool TryConvertFunction(string str, out object value);

	// Function for converting a pipeline parameter to a string
	internal delegate string ToStringFunction(object value);

	// Contains the functions for converting to/from strings found in content projects and their typed values
	internal static class ConverterCache
	{
		#region Fields
		private static readonly Dictionary<Type, TryConvertFunction> s_fromFunctions;
		private static readonly Dictionary<Type, ToStringFunction> s_toFunctions;
		#endregion // Fields

		// Gets if there is a converter registered for the type
		public static bool CanConvert(Type t) => s_fromFunctions.ContainsKey(t);

		// Converts a string to the type
		public static bool Convert(Type t, string str, out object value)
		{
			if (!CanConvert(t))
				throw new ArgumentException($"Cannot convert a string into the type {t.Name}", nameof(t));
			return s_fromFunctions[t](str, out value);
		}

		// Converts a type to a string
		public static string MakeString(Type t, object value)
		{
			if (!CanConvert(t))
				throw new ArgumentException($"Cannot convert to the type {t.Name} to a string", nameof(t));
			return s_toFunctions[t](value);
		}

		// "Meta" function for converting the standard signed integer types
		//   ENSURE THAT THE TYPEPARAM IS ALWAYS AN INTEGER TYPE
		private unsafe static bool ConvertInteger(string str, out object value, bool signed, uint size)
		{
			long _clampS(long v, long min, long max) => (v < min) ? min : (v > max) ? max : v;
			ulong _clampU(ulong v, ulong max) => (v > max) ? max : v;
			
			value = null;
			if (signed)
			{
				if (!Int64.TryParse(str, out long parsed))
					return false;
				switch (size)
				{
					case 0: value = (sbyte)_clampS(parsed, SByte.MinValue, SByte.MaxValue); break;
					case 1: value = (short)_clampS(parsed, Int16.MinValue, Int16.MaxValue); break;
					case 2: value = (int)_clampS(parsed, Int32.MinValue, Int32.MaxValue); break;
					case 3: value = _clampS(parsed, Int64.MinValue, Int64.MaxValue); break;
					default: return false;
				}
				return true;
			}
			else
			{
				if (!UInt64.TryParse(str, out ulong parsed))
					return false;
				switch (size)
				{
					case 0: value = (byte)_clampU(parsed, Byte.MaxValue); break;
					case 1: value = (ushort)_clampU(parsed, UInt16.MaxValue); break;
					case 2: value = (uint)_clampU(parsed, UInt32.MaxValue); break;
					case 3: value = _clampU(parsed, UInt64.MaxValue); break;
					default: return false;
				}
				return true;
			}
		}

		// "Meta" function for converting the standard floating point types
		//   'type = 0 for float, 1 for double, 2 for decimal
		private static bool ConvertFloatingPoint(string str, out object value, int type)
		{
			value = null;
			if (type != 2)
			{
				if (!Double.TryParse(str, out double parsed))
					return false;
				if (type == 1) value = parsed;
				else value = (float)parsed;
				return true; 
			}
			else
			{
				if (!Decimal.TryParse(str, out decimal parsed))
					return false;
				value = parsed;
				return true;
			}
		}

		// Adds the built-in converters
		static ConverterCache()
		{
			s_fromFunctions = new Dictionary<Type, TryConvertFunction>();
			s_toFunctions = new Dictionary<Type, ToStringFunction>();

			// From string functions
			s_fromFunctions.Add(typeof(sbyte), (string str, out object value) =>  ConvertInteger(str, out value, true, 0));
			s_fromFunctions.Add(typeof(byte), (string str, out object value) =>   ConvertInteger(str, out value, false, 0));
			s_fromFunctions.Add(typeof(short), (string str, out object value) =>  ConvertInteger(str, out value, true, 1));
			s_fromFunctions.Add(typeof(ushort), (string str, out object value) => ConvertInteger(str, out value, false, 1));
			s_fromFunctions.Add(typeof(int), (string str, out object value) =>    ConvertInteger(str, out value, true, 2));
			s_fromFunctions.Add(typeof(uint), (string str, out object value) =>   ConvertInteger(str, out value, false, 2));
			s_fromFunctions.Add(typeof(long), (string str, out object value) =>   ConvertInteger(str, out value, true, 3));
			s_fromFunctions.Add(typeof(ulong), (string str, out object value) =>  ConvertInteger(str, out value, false, 3));
			s_fromFunctions.Add(typeof(float), (string str, out object value) =>  ConvertFloatingPoint(str, out value, 0));
			s_fromFunctions.Add(typeof(double), (string str, out object value) => ConvertFloatingPoint(str, out value, 1));
			s_fromFunctions.Add(typeof(decimal), (string str, out object value) => ConvertFloatingPoint(str, out value, 2));
			s_fromFunctions.Add(typeof(string), (string str, out object value) => { value = str; return true; });
			s_fromFunctions.Add(typeof(bool), (string str, out object value) => {
				str = str.ToLower().Trim();
				value = null;
				if (str == "true") value = true;
				else if (str == "false") value = false;
				return (value != null);
			});

			// To string functions
			s_toFunctions.Add(typeof(sbyte), val => ((sbyte)val).ToString());
			s_toFunctions.Add(typeof(byte), val => ((byte)val).ToString());
			s_toFunctions.Add(typeof(short), val => ((short)val).ToString());
			s_toFunctions.Add(typeof(ushort), val => ((ushort)val).ToString());
			s_toFunctions.Add(typeof(int), val => ((int)val).ToString());
			s_toFunctions.Add(typeof(uint), val => ((uint)val).ToString());
			s_toFunctions.Add(typeof(long), val => ((long)val).ToString());
			s_toFunctions.Add(typeof(ulong), val => ((ulong)val).ToString());
			s_toFunctions.Add(typeof(float), val => ((float)val).ToString());
			s_toFunctions.Add(typeof(double), val => ((double)val).ToString());
			s_toFunctions.Add(typeof(decimal), val => ((decimal)val).ToString());
			s_toFunctions.Add(typeof(string), val => (string)val);
			s_toFunctions.Add(typeof(bool), val => ((bool)val).ToString());
		}
	}
}
