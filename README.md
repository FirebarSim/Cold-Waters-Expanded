# Cold-Waters-Expanded
## Install Guide
* Download the BepInEx Framework (32-Bit) from BepInEx https://github.com/BepInEx/BepInEx/releases and install it by following their install guide: https://bepinex.github.io/bepinex_docs/master/articles/user_guide/installation/index.html?tabs=tabid-win
* Run Cold Waters to generate the BepInEx config file (the game might not start properly here).
* Edit the `<[Preloader.Entrypoint]>` section in `Cold Waters\BepInEx\config\BepInEx.cfg` as follows:
```
[Preloader.Entrypoint]
    
## The local filename of the assembly to target.
# Setting type: String
# Default value: UnityEngine.dll
Assembly = UnityEngine.dll

## The name of the type in the entrypoint assembly to search for the entrypoint method.
# Setting type: String
# Default value: Application
Type = MonoBehaviour

## The name of the method in the specified entrypoint assembly and type to hook and load Chainloader from.
# Setting type: String
# Default value: .cctor
Method = .cctor
```
* Download the latest CWE release .zip file from https://github.com/FirebarSim/Cold-Waters-Expanded/releases
* Extract the CWE .zip folder to `Cold Waters\BepInEx\plugins`
* Verify filestructure `Cold Waters\BepInEx\plugins\Cold Waters Expanded\` contains the Mod files.
* Run Cold Waters - refer to the Output Log for errors.

## Sample Data
The sample data folder contains several sets of data:
* Sample .blend files for use in Blender
* Sample .xcf file for use in Gimp 2
* The asset bundle exporter that I use (to use this Unity v5 is required and the export path must be changed to your Cold Waters install location)
* My Cold Waters override folder, this should contain everything needed to import new files, but care should be taken in its use
