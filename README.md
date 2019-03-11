# ![logo](Resources/Logo/Thumbnail.png) Spectrum [![Build Status](https://travis-ci.org/SpectrumLib/Spectrum.svg?branch=master)](https://travis-ci.org/SpectrumLib/Spectrum)

*Note: This library is still in active development, and is not currently in a useable state.*

Vulkan-powered framework for creating performant cross-platform games and simulations.
The design of this library is heavily influenced by the [MonoGame](http://www.monogame.net/) project, as well as some of its extensions, such as [MonoGame Extended](https://github.com/craftworkgames/MonoGame.Extended), and [Nez](https://github.com/prime31/Nez).

While Vulkan works on iOS and Android devices, this library is currently designed to run on Desktop (Windows, Linux, MacOS) platforms only. Mobile support is planned for the future. This library is 64-bit only. Because the 32-bit only Desktop market is so small, there are currently no plans to add 32-bit support in the future.

The library and associated tools are licensed under the GNU LGPL v3 license. In short, this license forces modified and derivative versions of this library to be released open source with the same license, but allows private and commercial applications to interface with this library without having to be open source themselves. Please see [LICENSE](LICENSE) for the full text. The libraries and projects used by Spectrum belong to their respecive authors, and are used (and rehosted, when applicable), under their original licenses.

## Prism
Prism is the content building tool used to pre-process and package content files for faster runtime loading and easier application distribution. It is very similar to the content pipeline for Monogame, in that it has a staged pipeline architecture that can be modified to include additional content processing, and can be used in either command line or through a UI. However, it packages the content into large cache files (similar to Unity) so applications can be distributed with a minimal number of files. Its project file format is also written in simple Json, making it easier to understand and manage the project files by hand, if needed/wanted.

## Dependencies

One of the design goals of the Spectrum project is to package all of the required dependencies in the library binary, which greatly reduces the amount of setup required of both developers and end-users. This includes both the native (C/C++) and managed (.NET) binaries. Because of this, the library binary is somewhat larger than it would otherwise be, but it provides a drop-and-play product that just works out of the box.

This is accomplished differently for the binary types. Managed binaries are compiled directly into the assembly using the ILRepack tool (see below). Native binaries are built into the assembly as embedded resources, and are extracted at runtime and loaded dynamically using the host system's dynamic library loading interface (`LoadLibrary` on Windows, and `dlopen` on *nix).

#### Vulkan

Every single dependency used by both Spectrum and Prism are built into the library and tool assemblies, save one: Vulkan. Because the runtime and sdk for Vulkan are both massive, they are not included in the assemblies. Instead, the Vulkan runtime is required on all systems that using Prism and Spectrum (end user and developer), which is simple as the host graphics drivers will provide this. Additionally, developers writing Spectrum applications and/or using Prism will also **require the Vulkan SDK to be installed and in the PATH**.

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