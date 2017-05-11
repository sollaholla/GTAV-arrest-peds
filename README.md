---

# GTAV-arrest-peds
This is a sample project, for anyone looking to create some sort of arrest mechanic in their mod.

---

# Usage
1) Start by installing [addon props 1.1](https://www.gta5-mods.com/tools/addonprops), and follow the installation guide to move to the next step.
2) Once addon props is installed, use the _modernprisoncell_ prop that's provided in the Game Assets folder and add it to your addon-props repository as explained in the addon props readme.
3) Now let's set our build target. To do this we need to first have the project open in visual studio.
4) Navigate to Debug > ![properties_icon](http://i.imgur.com/kJcebm4.png) `GTA5_Arrest Properties`. You can also double click the properties icon ![properties_icon](http://i.imgur.com/kJcebm4.png) which is displayed in `Solution Explorer` underneath the project > ![project_icon](http://i.imgur.com/MMM4skT.png)
5) Use the following image to navigate to the build events text box:
![build_events_tutorial](http://i.imgur.com/kkmqRzE.png)
6) Change both paths (e.g. `C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V\scripts`) to point to your scripts folder.
7) Edit & Build the project, and (if you did this correctly) you'll notice the new files (.dll & .pdb) are placed into your scripts folder.

---

# Pre-requisites

- [ScriptHookV *Newest Version*](http://dev-c.com/gtav/scripthookv)
- [ScriptHookVDotNet 2.9 or Above](https://github.com/crosire/scripthookvdotnet/releases)
- [C++ Redist](https://www.microsoft.com/en-us/download/details.aspx?id=48145)
- [.NET Framework 4.5.2](https://www.microsoft.com/en-us/download/details.aspx?id=42642)

---
