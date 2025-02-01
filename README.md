# ReLog

ReLog is a VRChat Debugging Solution for World Developers. It allows developers to quickly and easily redirect normal UnityEngine.Debug Log functions to the ReLog system or integrate their existing system with ReLog for out-of-the-box support.

## Installing

1. Download the latest [ReLog.unitypackage](https://github.com/200Tigersbloxed/ReLog/releases/latest/download/ReLog.unitypackage)
2. Import the ReLog package into your VRChat World Project
3. Open the path `Assets/ReLog/Examples` for some example setups! (see [Examples](https://github.com/200Tigersbloxed/ReLog/edit/main/README.md#examples) below)

## Examples

ReLog comes with two examples that make setup quick and painless.

### Base

![image](https://github.com/user-attachments/assets/b3269b68-eec5-461f-9765-7d390976f612)

The base example is only the CoreLogger object itself. This will be the object than handles everything logging related: scripting logs, player networking, etc. Simply drag this into your scene and unpack the prefab to start using it.

> [!WARNING]
>
> Only one instance of CoreLogger is supported! Multiple instances of CoreLogger may cause significant issues.

### ConsoleWindow

![image](https://github.com/user-attachments/assets/b23218da-1c23-478d-b04f-2f359eba7e2d)

Likely what most people will want to use. The ConsoleWindow example is a console window inside of Unity that will display all user logs including a filter for users and clearing for specific users.

> [!TIP]
>
> The Clear button on the console window only appears whenever you filter a specific user. You cannot clear All!

## Terminology

ReLog uses a lot of special words to describe what it does and how it functions in a sensible way. Below is a list of words/phrases we like to use:

+ Text Objects
  + The objects which have a TextMeshPro Text component attached to them where text from logs will be written to
+ Scrolls
  + Any scroll containers that Text Objects may be contained under
+ Dropdowns
  + Any dropdowns for filtering users
+ Clear Buttons
  + Any buttons which are used for clearing user logs locally
+ Persistence
  + Describes the state of an object being similar at runtime
  + This does **NOT** mean saving PlayerData
+ Networker
  + A networker is an object which syncs player data across the server
+ "Generate Code" (etc.)
  + To generate code means to rewrite scripts to integrate functionality with ReLog

## Generating/Converting Code

What makes ReLog work with a lot of already existing VRChat UdonSharp assets is how we *force* integration. Code generation will find all references of UnityEngine.Debug's `Log`, `LogWarning`, and `LogError` functions and rewrite them to redirect them to a CoreLogger's functions. This process will also force reference the specified CoreLogger behaviour in the object instances of scripts that were rewritten. This sounds really confusing, but fortunately for you (the user) this is dead simple. However, before continuing...

> [!CAUTION]
> 
> # CREATE A BACKUP!
> 
> This process will rewrite **ANY** target script and may cause irreparable damage! You have been warned.

To get started,

1. Import a CoreLogger object
    + You can find some examples under `Assets/ReLog/Examples`
    + Or simply create a GameObject and attach a `CoreLogger` behaviour
2. Click the `Auto-Generate Logging` button
3. Click `Check`
    + This will scan all files in your project for any references to UnityEngine.Debug's target functions (listed above).
    + This may take a minute or two. Please do not click off of the inspector while this process is running.
4. Verify all files listed are files you are okay with overwriting.
5. Click the Convert button and wait.
6. Save!

You should also see all objects in the scene that referenced a target script have a new `Logger` field. It should automatically be assigned by the object you did the conversion from, but if it isn't, simply set it to your CoreLogger object.

> [!NOTE]
>
> ReLog currently only supports the `Log(object message)`, `LogWarning(object message)`, and `LogError(object message)` overloads. Attempting to convert a script with the `Object context` overload will fail.

### Disabling Code Generation

Code Generation is fairly unstable and may cause many issues with your projects, especially due to the nature of its build assets. Fortunately, for people who have special assemblies (or those who don't want to risk any sort of damage), there is a way to disable this feature entirely.

1. Open your Project Settings under `Edit` > `Project Settings...`
2. Navigate to the `Player` section of your Project
3. Select the correct build target and expand the `Other Settings` sub-section
4. Navigate to the `Scripting Define Symbols` section and add the symbol `DISABLE_RELOG_ANALYSIS`
5. Click `Apply`
6. *(Optional)* Move/Rename the directory at `Assets/ReLog/Scripts/Analyzer` to `Assets/ReLog/Scripts/.Analyzer` and delete the file at `Assets/ReLog/Scripts/Analyzer.meta`
    + This will prevent the Code Generation assemblies from being loaded into Unity. Nothing will ever convert without these.

![image](https://github.com/user-attachments/assets/2be89b73-6ba0-4fed-a550-5dd3a4c8223c)

## Developer API

The `CoreLogger` behaviour provides methods for developers to natively integrate ReLog support so users don't have to convert any code. Below is an example script.

```cs
using UdonSharp;
using UnityEngine;
using ReLog;

public class MyLoggingObject : UdonSharpBehaviour
{
    public CoreLogger Logger;

    public void Start()
    {
        // Send a simple log message
        Logger.Log("Hello World!");
        // Send a log message with a warning level
        Logger.LogWarning("This could be bad...");
        // Send a log message with an error level
        Logger.LogError("Oh No!");
    }
}
```

> [!NOTE]
>
> Feel free to log whenever you want! If the CoreLogger isn't ready for logging, that's okay! It will put your log in a queue with a timestamp and will send it when it is ready again.

## Acknowledgements

+ ReLog is heavily inspired by [CHAMCHI_Console](https://github.com/kibalab/CHAMCHI_Console)
+ ReLog is dedicated to [GAMBLE <><](https://vrchat.com/home/group/grp_98781622-e47c-4b2f-bd6b-3edd52e4522f)
