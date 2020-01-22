# Screenshot Companion

With this editor extension you can take screenshots inside the Unity Editor and projects built with Unity. If you are a developer, you should help make it even more useful by forking the project on [Github](https://github.com/Pfannkuchen/ScreenshotCompanion) so everyone can take great screenshots with ease.


## Installation
This folder simple needs to be placed inside your Unity project like this:
*YourUnityProject __/Assets/TheTopicbirdTools/ScreenshotCreator/__ ThisTool*.


## Instructions
1. Taking screenshots is easy, just drop the Prefab *ScreenshotCreatorPrefab* into your Scene, add the Script *ScreenshotCreator* to a GameObject in your Scene or open a new Editor Window by clicking on the Menu Item "Window" and then *Screenshot Creator*.

2. The Settings tab can be expanded by pressing *TOGGLE SETTINGS*. This lets you set all the important options of your screenshots:

    2.1. __Capture settings__
    - "Capture Screenshot" uses the built-in algorithm Application.CaptureScreenshot, which can be supersized (int multiples of the Game View) and supports multiple Cameras, but creates blurry object outlines for bigger resolutions. This is the easiest option, so we made it the default.
    - "Render Texture" only supports a single Camera, but can also be supersized (float multiples of the Game View) and will always keep the object outlines sharp. If you only use one Camera, pick this one.
    - "Cutout" will save a custom rectangle from the Game View pixel by pixel, no upscaling. It supports all amounts of Cameras, because it simply saves what you see in the Game View. The other capture methods may even work in the Edit Mode of Unity, but this one will not.

    2.2. __Directory settings__
    - The "Custom Name" field lets you pick your own directory name instead of "Screenshots".
    - You can select if you want to save into the main folder of your Unity project or to the presistent path of your OS. The final directory link will be displayed underneath.

    2.3. __File settings__
    - The *Custom Name* field lets you pick your own file name instead of your Unity project name.
    - Select what information you want to add to the file name (Camera name, date, resolution). The final file name will be displayed underneath.
    - Select the file format (PNG is lossless, JPG is compressed).

3. To add more Camera slots, press the *ADD CAMERA* button at the bottom of the Inspector / Editor Window. Then drag any Camera from your Scene into the empty new slot that says *None (Game Object)*. You can also select a parent GameObject with other Cameras as children, if the capture methods supports that.

4. You can delete a Camera slot by pressing the respective red *X* button. The delete button will ask you if you are sure and you have to click the button a second time consecutive.


## Credits
We are working on this in our free time, so if you want to support us you can get one of our assets. ♥

The Topicbird at the [Assetstore](http://u3d.as/gBa).