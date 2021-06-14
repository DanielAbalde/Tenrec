# Tenrec
###### Run Grasshopper files as Unit Tests in Visual Studio

This WIP plugin and workflow allows to replace unit tests written from code (as usual), by Grasshopper files, and to continue using the Test Explorer in Visual Studio.
The plugin contains two special objects, a group that allows to gather components into a single unit test, and a component that throws an error when the test fails.

![Capture Tenrec Group](.git/captureTenrecGroup.png)

### 🗺️ Motivation
The motivation is to automate the test code and allow to create Grasshopper definitions that can be both component unit tests and integration tests between various components. It also allows to have a better control over the data, mainly geometrical, that parameterise the tests. And above all, it avoids having to open Rhino + Grasshopper + Document to debug a plugin.

### 😎 Overview
The idea is to create test definitions in Grasshopper and use the Tenrec Source Code Generator to create a code file with all the classes and methods contained in the definition, and add this to your test project in Visual Studio. Then you run the tests and don't need to debug your plugin by opening Rhino and Grasshopper.

### 🔒 Limitations
It only works for Rhino 7 or higher (it uses RhinoInside), with .NET Framework 4.8 or higher and the project must be build to x64.
For the moment the Unit Test Source Code Generator is just for Windows and just generate C# code using the MSTest framework. 

### 📖 How to use it

##### Install Tenrec.gha

1. Download from...
2. Paste Tenrec.gha in _C:\Users\<YOUR USERNAME>\AppData\Roaming\Grasshopper\Libraries_.
3. Restart Rhinoceros and Grasshopper.
4. Find _Tenrec_ group and _Assert_ component in _Params > Util_.

##### Create a Grasshopper Unit Test file

1. Create a Grasshopper definition to make a Unit Test.
2. Add the _Assert_ component, set input _Assert (A)_ to false when your Unit Test fail (and true otherwise) and include a message for when that happens.
3. Select all components that are part of the Unit Test.
4. Add a _Tenrec_ group to the canvas, it will group all selected components.
5. Change the _Tenrec_ group name by double click on it.
6. Repeat 1 to 5 if you want to include more Unit Test in the same file.
7. Save the Grasshopper file in your test folder.
8. In the top right corner, click on _settings > Source Code Generator_.
9. The _Unit Test Source Code Generator_ window should appear, set your test folder path in the _Grashopper file folder_ section, set the folder to save the code file in _Output folder_ section and its name in _Output name_.
10. Press _Generate_ and close the window. The code file will be located at _Output folder_/_Output name_.cs

##### Create a Unit Test Project

1. In Visual Studio 2019, create a new Unit Test Project (.NET Framework). with .NET Framework 4.8.
2. From project _Properties > Build_, set _Platform target: x64_ and make sure _Prefer 32 bit_ is disabled/unchecked.
3. Browse in _Nuget Package Manager_ to install the last realease of _Rhino.Inside_. 
4. If in the project references you see several Rhino libraries, set copy local to false to all of them except Rhino.Inside (Eto, Eto.Wpf, GH_IO, Grasshopper, Rhino.UI, RhinoCommon, RhinoWindows). If you see a single Rhino.Inside nuget package, ignore this step.
5. Add reference to _Tenrec.dll_ provided in the download.
6. In your project from _Solution Explorer_, _Add > Existing item..._ and include the code file generated previosly.
7. Build the project.

##### Run the Unit Tests

1. In Visual Studio 2019 _toolbar > Test > Test Explorer_. 
2. Press Run All Test In View.
3. You should see the results in all your Grasshopper tests.


### 🌈 License

This project is free software: you can redistribute it and/or modify it under the terms of the [GNU General Public License](https://www.gnu.org/licenses/gpl-3.0.en.html) as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.


### ❓ Questions & Feedback

Join to my Discord for [assitance](https://discord.gg/Mb4hqdKEYy) or [feedback](https://discord.gg/9PssC4nVfe).


### ☕ Support me!

Contribute to thank or further develop  using [Ko-fi](https://ko-fi.com/daniga) or [Paypal](https://www.paypal.com/paypalme/danielabalde). Thank you!