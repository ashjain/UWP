# App Extensibility Sample for Desktop app bridge UWP Apps

Win32 apps allow developers to extend the app by creating a plugin.  This plugin is typically a DLL or similar which is installed in some app folder and the Win32 apps does a LoadLibrary or LoadAssembly at runtime to load code from these DLLs/plugins.

This model directly does not work in case of Win32 desktop app bridge UWP apps as plugin developers may not be able to directly put a DLL into an app folder for native loading at runtime.

This sample demonstrates how to use UWP app extensions to support plugin developers to create UWP app extensions to bundle their DLL as UWP App Extension which can then be consumed by Desktop App Bridge UWP apps by being a UWP App Extension Host.  The UWP App Extensions are submitted and downloaded from the Store.