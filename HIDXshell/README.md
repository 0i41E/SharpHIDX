
# HIDXShell
**HIDXShell** is a tool that operates through HID communication, enabling remote command execution on a target system using USB devices. This project is designed to be used with HIDX StealthLink, a feature for [O.MG Elite devices](https://shop.hak5.org/collections/mischief-gadgets).
By identifying the device via its VID and PID, it opens a communication channel to send commands, executes them on the target system, and handles the output or feedback. 
**HIDXShell** is particularly useful in environments where network-based access is restricted or monitored.

![hidxshell-diagram](https://github.com/user-attachments/assets/58ee0179-7280-4fdf-b770-e82f7c9fa08f)

## Usage
The Usage of the provided project is pretty much self explanatory.
After successfully compiling the project, the binary can simply be used on a Windows Target system. 
As a listener, Mischief Gadgets [Universal Python Listener](https://github.com/O-MG/O.MG-Firmware/blob/stable/tools/HIDX/python/stealthlink-client-universal.py) is highly recommended, as it handles certain quirks required for a smooth experience. 
```
 _   _ _____________   __  _____ _             _ _   _       _     _       _
| | | |_   _|  _  \ \ / / /  ___| |           | | | | |     | |   (_)     | |
| |_| | | | | | | |\ V /  \ `--.| |_ ___  __ _| | |_| |__   | |    _ _ __ | | __
|  _  | | | | | | |/   \   `--. \ __/ _ \/ _` | | __| '_ \  | |   | | '_ \| |/ /
| | | |_| |_| |/ // /^\ \ /\__/ / ||  __/ (_| | | |_| | | | | |___| | | | |   <
\_| |_/\___/|___/ \/   \/ \____/ \__\___|\__,_|_|\__|_| |_| \_____/_|_| |_|_|\_\

HID-Based Remote Access Tool by Ø1phor1³

Usage: HIDXShell.exe /vid <VendorID> /pid <ProductID> /powershell /verbose

Parameters:
  /vid:        Specify the Vendor ID of the target device (default: D3C0)
  /pid:        Specify the Product ID of the target device (default: D34D)
  /powershell: Use PowerShell.exe as the command line interface (optional, slower)
  /verbose:    Enable verbose output (optional)

Example: HIDXShell.exe /vid D3C0 /pid D34D /powershell
```
**HIDXShell** gives you the freedom to choose between different CLI options. While the default `cmd` instance is the the faster one and may be sufficient for most users, `/powershell` opens up more functionalities and a full blown PowerShell. The drawback comes with the way PowerShell is executed. Users may be impacted by reduced speed. 

## Running HIDXShell Through PowerShell
While there are PowerShell scripts providing remote-access via HIDX StealthLink, **HIDXShell.exe** may introduce more functionalities. If you do want to run it through PowerShell though, considere the following steps:

If you want to run **HIDXShell** in-memory through a PowerShell wrapper, first compile **HIDXShell** and encode the resulting binary into base64:

`$base64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("<Path to .exe>"))`

**HIDXShell** can then be loaded in a PowerShell script with the following:

`$AssemblyLoad = [System.Reflection.Assembly]::Load([Convert]::FromBase64String($base64))`

Afterwards, identify the entry point:

`$EntryPoint = $AssemblyLoad.GetTypes().Where({ $_.Name -eq 'Program' }, 'First').GetMethod('Main', [Reflection.BindingFlags] 'Static, Public, NonPublic')`

Executing the loaded assembly then can be achieved like this (If you do need to define multiple arguments, seperate them via `,`):

`$EntryPoint.Invoke($null, (, [string[]] ('<Argument(s)>')))`

## Disclaimer
By using the provided code, you agree to the following terms:

1.  **No Warranty**: This code is provided "as is" without warranty of any kind, express or implied. The creator does not warrant that the provided code will be error-free or will meet your requirements or expectations.
    
2.  **Use at Your Own Risk**: You are solely responsible for the use of this code. The creator is not responsible for any damage, loss, or issues that may arise from using this script, including but not limited to loss of data, system malfunctions, or security vulnerabilities.
    
3.  **Compliance with Laws**: You agree to use this script in compliance with all applicable local, state, national, and international laws and regulations. The creator is not responsible for any illegal or unauthorized use of this code.
    
4.  **No Liability**: The creator shall not be liable for any direct, indirect, incidental, special, exemplary, or consequential damages.

By using this code, you acknowledge that you have read and understood this disclaimer and agree to be bound by its terms.
