# Avalonia.MessageBox
A MessageBox modal made to be used with Avalonia.

# Installation
From nuget package:

`dotnet add package StarDebris.Avalonia.MessageBox`
[NuGet page](https://www.nuget.org/packages/StarDebris.Avalonia.MessageBox/)

From source:

`dotnet add reference path/to/StarDebris.Avalonia.MessageBox.csproj`


# Usage

```csharp
new MessageBox("Hello World", delegate(DialogResult r, EventArgs e) {

    if (r.result == MessageBoxButtons.Ok)
        Console.WriteLine("Ok clicked");
    else if (r.result == MessageBoxButtons.Cancel)
        Console.WriteLine("Cancel clicked");
        
}, MessageBoxStyle.Info, MessageBoxButtons.Ok | MessageBoxButtons.Cancel).Show();

```

# License
```        DO WHAT THE FUCK YOU WANT TO PUBLIC LICENSE 
                    Version 2, December 2004 

 Copyright (C) 2004 Sam Hocevar <sam@hocevar.net> 

 Everyone is permitted to copy and distribute verbatim or modified 
 copies of this license document, and changing it is allowed as long 
 as the name is changed. 

            DO WHAT THE FUCK YOU WANT TO PUBLIC LICENSE 
   TERMS AND CONDITIONS FOR COPYING, DISTRIBUTION AND MODIFICATION 

  0. You just DO WHAT THE FUCK YOU WANT TO.
  ```
