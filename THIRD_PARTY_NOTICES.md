# Third-party notices

`PolarisInstaller.exe` embeds and installs the following unmodified binaries (under `Installer/Payload/game/`), taken from the BepInEx 6.0.0-be.788 Unity Mono build:

| Component | License | Source |
|---|---|---|
| BepInEx (Core, Preloader, Unity.Mono, Unity.Common) | LGPL-2.1 | https://github.com/BepInEx/BepInEx |
| HarmonyX (`0Harmony.dll`) | MIT | https://github.com/BepInEx/HarmonyX |
| MonoMod (RuntimeDetour, Utils) | MIT | https://github.com/MonoMod/MonoMod |
| Mono.Cecil | MIT | https://github.com/jbevain/cecil |
| SemanticVersioning | MIT | https://github.com/adamreeve/semver.net |
| AssetRipper.Primitives | MIT | https://github.com/AssetRipper/AssetRipper.Primitives |
| Unity Doorstop (`winhttp.dll`, `doorstop_config.ini`) | CC0 | https://github.com/NeighTools/UnityDoorstop |

BepInEx is licensed under the GNU Lesser General Public License v2.1; the unmodified binaries are redistributed here and their source is available at the link above.

The installer itself is a self-contained .NET 8 / WPF application (MIT-licensed runtime, Microsoft).

The Core resource loader also redistributes these NuGet dependencies under `BepInEx/plugins/Polaris/`:

| Component | License | Source |
|---|---|---|
| NVorbis (OGG decoding, retained from PolarisRes) | MIT | https://github.com/NVorbis/NVorbis |
| System.Buffers, System.Memory, System.Numerics.Vectors, System.Runtime.CompilerServices.Unsafe | MIT | https://github.com/dotnet/corefx |

Their license texts and notices are in [doc/licenses](doc/licenses/) and are included in the installer and manual package alongside the dependencies.
