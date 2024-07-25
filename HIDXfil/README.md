# HIDXfil
**HIDXfil** is a tool designed to exfiltrate data through a Human Interface Device (HID). This can be useful for scenarios where traditional network-based exfiltration methods are restricted or monitored. This tool can send messages, clipboard content, or whole files to a specified USB device using its Vendor ID (VID) and Product ID (PID). **HIDXfil** is designed to work with HIDX StealthLink, a feature for [O.MG Elite devices](https://shop.hak5.org/collections/mischief-gadgets).

## Usage
The Usage of the provided project is pretty much self explanatory.
After successfully compiling the project, the binary can simply be used on a Windows Target system. 
```
 _   _ _____________   __  _____ _             _ _   _       _     _       _
| | | |_   _|  _  \ \ / / /  ___| |           | | | | |     | |   (_)     | |
| |_| | | | | | | |\ V /  \ `--.| |_ ___  __ _| | |_| |__   | |    _ _ __ | | __
|  _  | | | | | | |/   \   `--. \ __/ _ \/ _` | | __| '_ \  | |   | | '_ \| |/ /
| | | |_| |_| |/ // /^\ \ /\__/ / ||  __/ (_| | | |_| | | | | |___| | | | |   <
\_| |_/\___/|___/ \/   \/ \____/ \__\___|\__,_|_|\__|_| |_| \_____/_|_| |_|_|\_\

HID-Based Exfiltration Tool by Ø1phor1³

Usage: HIDXfil.exe /message <Message> /clipboard /vid <VendorID> /pid <ProductID> /chunksize <chunksize> /file <filepath>

Parameters:
  /message:   Specify the message to exfiltrate
  /clipboard: Exfiltrate the Users clipboard content
  /file:      Specify the file path to exfiltrate
  /vid:       Specify the Vendor ID of the target device (Optional - Default:D3C0)
  /pid:       Specify the Product ID of the target device (Optional - Default:D34D)
  /chunksize: Specify the chunk size for data transfer (Optional - Default:8)

Example: HIDXfil.exe /message "Hello World" /vid D3C0 /pid D34D /chunksize 8
```
A chunksize of 8 was set to ensure the best results. Changing this value may result in missing data and corrupted files.
Generally, any TCP Listener can be used to retreive the data sent by the O.MG device. 

Netcat may be your first choice: `nc -lvnp <port> / nc -lvnp <port> > example.file`

## Disclaimer
By using the provided code, you agree to the following terms:

1.  **No Warranty**: This code is provided "as is" without warranty of any kind, express or implied. The creator does not warrant that the provided code will be error-free or will meet your requirements or expectations.
    
2.  **Use at Your Own Risk**: You are solely responsible for the use of this code. The creator is not responsible for any damage, loss, or issues that may arise from using this script, including but not limited to loss of data, system malfunctions, or security vulnerabilities.
    
3.  **Compliance with Laws**: You agree to use this script in compliance with all applicable local, state, national, and international laws and regulations. The creator is not responsible for any illegal or unauthorized use of this code.
    
4.  **No Liability**: The creator shall not be liable for any direct, indirect, incidental, special, exemplary, or consequential damages.

By using this code, you acknowledge that you have read and understood this disclaimer and agree to be bound by its terms.
