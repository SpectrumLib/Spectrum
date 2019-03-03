# ![logo](Resources/Logo/Thumbnail.png) Spectrum [![Build Status](https://travis-ci.org/SpectrumLib/Spectrum.svg?branch=master)](https://travis-ci.org/SpectrumLib/Spectrum)

*Note: This library is still in active development, and is not currently in a useable state.*

Vulkan-powered framework for creating performant cross-platform games and simulations.
The design of this library is heavily influenced by the [MonoGame](http://www.monogame.net/) project, as well as some of its extensions, such as [MonoGame Extended](https://github.com/craftworkgames/MonoGame.Extended), and [Nez](https://github.com/prime31/Nez).

While Vulkan works on iOS and Android devices, this library is currently designed to run on Desktop (Windows, Linux, MacOS) platforms only. Mobile support is planned for the future. This library is 64-bit only. Because the 32-bit only Desktop market is so small, there are currently no plans to add 32-bit support in the future.

The library and associated tools are licensed under the GNU LGPL v3 license. In short, this license forces modified and derivative versions of this library to be released open source with the same license, but allows private and commercial applications to interface with this library without having to be open source themselves. Please see [LICENSE](LICENSE) for the full text. The libraries and projects used by Spectrum belong to their respecive authors, and are used (and rehosted, when applicable), under their original licenses.

## Prism
Prism is the content building tool used to pre-process and package content files for faster runtime loading and easier application distribution. It is very similar to the content pipeline for Monogame, in that it has a staged pipeline architecture that can be modified to include additional content processing, and can be used in either command line or through a UI. However, it packages the content into large cache files (similar to Unity) so applications can be distributed with a minimal number of files. Its project file format is also written in simple Json, making it easier to understand and manage the project files by hand, if needed/wanted.

## Building from Source
**TODO**

## Contributing
**TODO**

## Acknowledgements

Thanks to the following projects/groups for being sources of design ideas:
* [MonoGame](http://www.monogame.net/)
* [MonoGame Extended](https://github.com/craftworkgames/MonoGame.Extended)
* [Nez](https://github.com/prime31/Nez)

Thanks to the following projects/groups for their libraries and tools used in or adapted for this project:
* [VulkanCore](https://github.com/discosultan/VulkanCore) - C# wrapper for the Vulkan C API
* [GLFWDotNet](https://github.com/smack0007/GLFWDotNet) - Used as the basis of the custom GLFW3 bindings
* [OpenAL-CS](https://github.com/flibitijibibo/OpenAL-CS) - C# bindings for OpenAL, compiled directly into the project
* [ILRepack](https://github.com/gluck/il-repack) - Merges .NET assemblies for easier distribution

Thanks to the following native libraries used in this project, which can be found in the [Dependencies](https://github.com/SpectrumLib/Dependencies) project:

* [GLFW3](https://www.glfw.org/) - Windowing and Input API
* [OpenAL Soft](https://kcat.strangesoft.net/openal.html) - Software implementation of the OpenAL standard
* [stb_vorbis](https://github.com/nothings/stb) - Implementation of a simple OGG Vorbis file decoder
* [dr_libs](https://github.com/mackron/dr_libs) - Implementation of simple WAV and FLAC file decoders