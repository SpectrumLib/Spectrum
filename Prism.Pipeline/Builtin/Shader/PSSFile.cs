using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Prism.Builtin
{
	// Represents and parses Prism Shader Set (.pss) files
	internal class PSSFile
	{
		private static readonly char[] WS_SEP = { ' ', '\t' };
		private static readonly string[] VALID_STAGES = { "vert", "tesc", "tese", "geom", "frag" };

		public readonly List<PSSModule> Modules;
		public readonly List<PSSShader> Shaders;

		private PSSFile(List<PSSModule> modules, List<PSSShader> shaders)
		{
			Modules = modules;
			Shaders = shaders;
		}

		public static PSSFile Parse(string[] fileLines, PipelineLogger logger)
		{
			// Sanitize the input
			List<(string Line, int LNum)> lines = Strip(fileLines);

			// Make sure the modules are the first thing
			if (lines[0].Line.Replace(" ", "") != "modules{")
			{
				logger.Error("The modules block must be the first component in a shader set file.");
				return null;
			}

			// Parse the modules
			List<PSSModule> mods = new List<PSSModule>();
			int endLine = lines.FindIndex(1, line => line.Line == "}");
			if (endLine == -1)
			{
				logger.Error("The modules block is not closed in the shader set file.");
				return null;
			}
			if (endLine == 1) // Check for no modules, exit early
			{
				logger.Warn("The shader set file does not contain any modules, and will not produce any shaders.");
				return new PSSFile(mods, new List<PSSShader>());
			}
			for (int li = 1; li < endLine; ++li)
			{
				if (!ParseModule(lines[li].Line, lines[li].LNum, logger, out var mod))
					return null;
				if (mods.Any(m => m.Name == mod.Name))
				{
					logger.Error($"A module with the name '{mod.Name}' already exists in the shader set file.");
					return null;
				}
				mods.Add(mod);
			}
			logger.Info($"Found {mods.Count} modules in shader set: {String.Join(", ", mods.Select(m => m.Name))}.");

			// Parse the shaders
			List<PSSShader> shaders = new List<PSSShader>();
			int lineIndex = endLine + 1;
			while (lineIndex < lines.Count)
			{
				// Get the name
				if (!ParseShaderHeader(lines[lineIndex].Line, lines[lineIndex].LNum, logger, out var name))
					return null;
				var shader = new PSSShader { Name = name };

				// Check for validity
				var sei = lines.FindIndex(lineIndex + 1, line => line.Line == "}");
				if (sei == -1)
				{
					logger.Error($"[line {lineIndex}] - the shader '{name}' block does not close before the end of the file.");
					return null;
				}

				// Parse the stages
				while ((++lineIndex) < sei)
				{
					if (!ParseShaderStage(lines[lineIndex].Line, lines[lineIndex].LNum, logger, mods, ref shader))
						return null;
				}

				// Check we have the required stage
				if (shader.Vert == null)
				{
					logger.Error($"the shader '{name}' does not have a vertex shader stage.");
					return null;
				}

				// Move past the closing brace
				lineIndex = sei + 1;
				shaders.Add(shader);
			}

			// Warn for no shaders
			if (shaders.Count == 0)
				logger.Warn("The shader set file does not contain any shaders.");

			// Good to go
			return new PSSFile(mods, shaders);
		}

		private static bool ParseModule(string line, int lineNum, PipelineLogger logger, out PSSModule mod)
		{
			mod = default;

			// Get the name
			var nei = line.IndexOf(']');
			if ((line[0] != '[') || (nei == -1))
			{
				logger.Error($"[line {lineNum}] - could not find module name.");
				return false;
			}
			mod.Name = line.Substring(1, nei - 1).Trim();
			line = line.Substring(nei + 1).Trim();

			// Check for the equals sign
			if (line[0] != '=')
			{
				logger.Error($"[line {lineNum}] - unable to separate module name from value.");
				return false;
			}
			line = line.Substring(1).Trim();

			// Get the file path
			var pfe = line.IndexOf('"', 1);
			if ((line[0] != '"') || (pfe == -1) || (pfe == 0) || (pfe == 1))
			{
				logger.Error($"[line {lineNum}] - could not find shader file name.");
				return false;
			}
			mod.SourceFile = line.Substring(1, pfe - 1).Trim();
			mod.Type = Path.GetExtension(mod.SourceFile).Substring(1);
			if (!VALID_STAGES.Contains(mod.Type))
			{
				logger.Error($"[line {lineNum}] - the file extension '.{mod.Type}' does not appear to be a valid shader stage.");
				return false;
			}
			line = line.Substring(pfe + 1).Trim();
			if (String.IsNullOrWhiteSpace(mod.SourceFile))
			{
				logger.Error($"[line {lineNum}] - shader file path cannot be empty.");
				return false;
			}
			if (!Uri.IsWellFormedUriString(mod.SourceFile, UriKind.Relative))
			{
				logger.Error($"[line {lineNum}] - the file path '{mod.SourceFile}' is not a valid relative path.");
				return false;
			}

			// Get the entry point
			if (line[0] != '@')
			{
				logger.Error($"[line {lineNum}] - could not find shader entry point.");
				return false;
			}
			var epe = line.IndexOfAny(WS_SEP);
			if (epe == 1)
			{
				logger.Error($"[line {lineNum}] - shader entry point was not given, or was empty.");
				return false;
			}
			else if (epe == -1) // End of line
				epe = line.Length;
			mod.EntryPoint = line.Substring(1, epe - 1).Trim();
			line = (epe == line.Length) ? "" : line.Substring(epe + 1).Trim();

			// Get the list of macros
			if (line.Length > 0)
			{
				List<string> ms = new List<string>();

				while (line.Length > 0)
				{
					if (line[0] != '!')
					{
						logger.Error($"[line {lineNum}] - '!' expected for macro definition.");
						return false;
					}

					// Get the macro
					var wsi = line.IndexOfAny(WS_SEP);
					if (wsi == -1) // End of line
						wsi = line.Length;
					var macro = line.Substring(1, wsi - 1);

					// Find and validate any value
					var eqi = macro.IndexOf('=');
					if (eqi != -1)
					{
						var mval = macro.Substring(eqi + 1);
						bool isNum = Int32.TryParse(mval, out var ival) || Single.TryParse(mval, out var fval);
						if (!isNum)
						{
							logger.Error($"[line {lineNum}] - the value macro '{macro}' is not a valid numerical value.");
							return false;
						}
					}

					// Add and move to next
					ms.Add(macro);
					line = (wsi == line.Length) ? "" : line.Substring(wsi).Trim();
				}

				mod.Macros = ms.ToArray();
			}
			else
				mod.Macros = new string[0];

			// Good to go
			return true;
		}

		private static bool ParseShaderHeader(string line, int lineNum, PipelineLogger logger, out string name)
		{
			name = null;

			// Check format
			if (!line.StartsWith("shader"))
			{
				logger.Error($"[line {lineNum}] - expected shader block.");
				return false;
			}
			line = line.Substring(6).Trim(); // Skip past "shader"

			// Get name
			var nei = line.LastIndexOf(']');
			if ((line[0] != '[') || (nei == -1))
			{
				logger.Error($"[line {lineNum}] - unable to find shader name.");
				return false;
			}
			name = line.Substring(1, nei - 1);
			line = line.Substring(nei + 1).Trim();

			// Check rest of header
			if (line != "{")
			{
				logger.Error($"[line {lineNum}] - expected opening brace for shader block.");
				return false;
			}

			// Good to go
			return true;
		}

		private static bool ParseShaderStage(string line, int lineNum, PipelineLogger logger, List<PSSModule> modules, ref PSSShader shader)
		{
			// Validate
			var pos = line.IndexOf('=');
			if (pos == -1)
			{
				logger.Error($"[line {lineNum}] - unable to split shader stage into stage and module components.");
				return false;
			}

			// Extract and check
			var stage = line.Substring(0, pos).Trim();
			var mod = line.Substring(pos + 1).Trim();
			if (!VALID_STAGES.Contains(stage))
			{
				logger.Error($"[line {lineNum}] - the shader stage '{stage}' is not valid.");
				return false;
			}
			if (mod[0] != '[' || mod[mod.Length - 1] != ']')
			{
				logger.Error($"[line {lineNum}] - unable to find module name.");
				return false;
			}
			mod = mod.Substring(1, mod.Length - 2);

			// Validate the module name
			if (String.IsNullOrWhiteSpace(mod))
			{
				logger.Error($"[line {lineNum}] - the module name cannot be empty.");
				return false;
			}
			if (!modules.Any(m => m.Name == mod))
			{
				logger.Error($"[line {lineNum}] - the module '{mod}' does not exist in the shader set file.");
				return false;
			}

			// Assign the name
			if (stage == "vert")
			{
				if (shader.Vert != null)
				{
					logger.Error($"[line {lineNum}] - the shader already has a vertex stage.");
					return false;
				}
				shader.Vert = mod;
			}
			else if (stage == "tesc")
			{
				if (shader.Tesc != null)
				{
					logger.Error($"[line {lineNum}] - the shader already has a tessellation control stage.");
					return false;
				}
				shader.Tesc = mod;
			}
			else if (stage == "tese")
			{
				if (shader.Tese != null)
				{
					logger.Error($"[line {lineNum}] - the shader already has a tessellation eval stage.");
					return false;
				}
				shader.Tese = mod;
			}
			else if (stage == "geom")
			{
				if (shader.Geom != null)
				{
					logger.Error($"[line {lineNum}] - the shader already has a geometry stage.");
					return false;
				}
				shader.Geom = mod;
			}
			else
			{
				if (shader.Frag != null)
				{
					logger.Error($"[line {lineNum}] - the shader already has a fragment stage.");
					return false;
				}
				shader.Frag = mod;
			}

			// Good to go
			return true;
		}

		// Takes the raw file lines, and strips out empty lines and comments, and leading/trailing whitespace
		private static List<(string, int)> Strip(string[] lines)
		{
			int lnum = 1;
			return lines
				.Select(line => {
					if (String.IsNullOrWhiteSpace(line))
						return (null, lnum++);
					var ci = line.IndexOf("//");
					if (ci != -1)
						line = line.Substring(0, ci);
					return (line.Trim(), lnum++);
				})
				.Where(line => !String.IsNullOrWhiteSpace(line.Item1))
				.ToList();
		}
	}

	// Holds information about a module
	internal struct PSSModule
	{
		public string Name;
		public string Type;
		public string SourceFile;
		public string EntryPoint;
		public string[] Macros; // In the format "name" or "name=value"
	}

	// Holds information about a shader (name and modules)
	internal struct PSSShader
	{
		public string Name;
		public string Vert;
		public string Tesc;
		public string Tese;
		public string Geom;
		public string Frag;
	}
}
