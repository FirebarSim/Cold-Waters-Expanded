# Cold-Waters-Expanded
## What this Does
Cold Waters Expanded is my attempt to provide a wider range of modding functionality for Cold Waters. It also aims to increase the modularity of the game to reduce the conflict points that mods can cause. The mod provides, as of version 1.0.1.0:
* Ability to load gLTF model files at run time, providing easy modding of new models
* Ability to load Unity AssetBundles, providing the full power of the Unity materials system etc, some particles are supported at the moment and more will be as I continue work.
* X Plane functionality, to support vessels such as the Suffren
* Mesh visibility functionality, to show and hide mesh based on some conditions
* Mesh translate function, to translate mesh when a condition is met or not
* Nation Flags, extra flags in the Unit Reference viewer
* A fix to allow an odd number of Torpedo tubes
* Removed the necessity to add all new vessels to the `_vessel_list.txt` file, the game will load and sort new vessels on load
* Removed the necessity to add all new aircraft to the `aircraft.txt` file, the game will append individual aircraft definition files in a search for `override\\aircraft\\aircraft_*.txt` to this file.
* Removed the necessity to add all new weapons to the `weapons.txt` file, the game will append individual weapon definition files in a search for `override\\weapons\\weapon_*.txt` for Torpedoes and Missiles, `override\\weapons\\mortar_*.txt` and `override\\weapons\\gun_*.txt` for "Depth Weapons", and `override\\weapons\\countermeasure_*.txt` for Countermeasures to this file.
* Removed the necessity to add Depth Weapon Descriptions to the `depth_weapon_display_names.txt` file in whatever language is chosen. The game will look in the individual weapon files as described above for `language_xx=` and add that to the sensor descriptions.
* Removed the necessity to add all new sensors to the `sensors.txt` file, the game will append individual weapon definition files in a search for `override\\sensors\\radar_*.txt` and `override\\sensors\\sonar_*.txt` to this file.
* Removed the necessity to add Sensor Descriptions to the `sensor_display_names.txt` file in whatever language is chosen. The game will look in the individual sensor files as described above for `language_xx=` and add that to the sensor descriptions.
Removed the need to add single mission names to the list in a language. The game now looks for `language_xx` in the mission files (of the form `\\override\\single*.txt` and adds that to the single missions list.
* Some game fixes

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
