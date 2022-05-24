# renderware-preview

This is a dotnet 6 WPF application for previewing GTA:SA skin models in 3D, with support for live-reloading texture changes.  

## Download
- Download the .exe from the [most recent release](https://github.com/NanoBob/renderware-preview/releases)

## Usage
- Start the application
- Select your GTA:SA install directory (If you have MTA:SA installed it should do this automatically)
- Select the skin you want to preview
- Select the correct texture (some skins have multiple textures)
- (Optional) export the default texture using the "export" button
- Select the image you wish to live-reload
- Modify your image and save, this should instantly be represented in the preview.

### Controls
In order to move the camera hold one of the mouse buttons on the 3D preview and:
- Use WASD for lateral movement
- Use shift to move the camera up
- Use control to move the camera down
- Use space to move the camera faster
- Use the mouse to rotate the camera

## Tech stack
The project is built using dotnet 6.0, WPF. Using https://github.com/nanobob/renderwareio for reading renderware files (img, dff, txd, ide). And uses WPF's 3D viewport for the preview rendering.

## Contributing
Contributions are welcome, if you have any questions contact me on Discord (NanoBob#7142)