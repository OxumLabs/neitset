using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NeitInstaller
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Neit Installer";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Welcome to the Neit Installer");
            Console.WriteLine("====================================");
            Console.ResetColor();
            Console.WriteLine();

            // Detect OS
            //Console.WriteLine("OS : ", RuntimeInformation.OSDescription);
            if (IsRunningOnWsl())
            {
                HandleLinuxInstallation();

            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await HandleWindowsInstallation();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                HandleLinuxInstallation();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Unsupported operating system.");
                Console.ResetColor();
            }
        }
        static bool IsRunningOnWsl()
        {
            // Check if OS is Linux and contains "Microsoft" or "WSL" in the OS description
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string osDescription = RuntimeInformation.OSDescription;
                return osDescription.Contains("Microsoft") || osDescription.Contains("WSL");
            }
            return false;
        }

        // Handles installation for Windows
        static async Task HandleWindowsInstallation()
        {
            if (!IsRunAsAdministrator())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: This installer must be run as an administrator.");
                Console.ResetColor();
                return;
            }

            try
            {
                // Step 1: Install LLVM for Windows
                if (ConfirmAction("Do you want to install LLVM? (y/n)"))
                {
                    await InstallLLVMWindows("https://github.com/llvm/llvm-project/releases/download/llvmorg-19.1.0/LLVM-19.1.0-win64.exe");
                }

                // Step 2: Download and install Neit
                string neitInstallPath = @"C:\Program Files\Neit";
                if (ConfirmAction("Do you want to download and install Neit? (y/n)"))
                {
                    string neitZipUrl = "https://github.com/OxumLabs/neit/releases/download/0.0.34/neit_win.zip";
                    await DownloadAndExtractNeitWindows(neitZipUrl, neitInstallPath);
                }

                // Step 3: Install Visual C++ Redistributable
                if (ConfirmAction("Do you want to install Visual C++ Redistributable? (y/n)"))
                {
                    await InstallVCRedist();
                }

                // Step 4: Add Neit to system PATH
                if (ConfirmAction("Do you want to add Neit to your system PATH? (y/n)"))
                {
                    AddToEnvironmentPathWindows(neitInstallPath + "/windows");
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Windows installation completed successfully!");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.ResetColor();
            }
        }

        // Handles installation for Linux
        static void HandleLinuxInstallation()
        {
            if (!IsSudo())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: This installer must be run with sudo privileges.");
                Console.ResetColor();
                return;
            }

            try
            {
                // Step 1: Install LLVM for Linux
                if (ConfirmAction("Do you want to install LLVM? (y/n)"))
                {
                    InstallLLVMLinux();
                }

                // Step 2: Download and install Neit using wget
                string neitInstallPath = "/usr/local/bin/Neit/";
                if (ConfirmAction("Do you want to download and install Neit? (y/n)"))
                {
                    string neitZipUrl = "https://github.com/OxumLabs/neit/releases/download/0.0.34/neit_linux.zip";
                    DownloadAndExtractNeitLinux(neitZipUrl, neitInstallPath);
                }

                // Step 3: Add Neit to system PATH
                if (ConfirmAction("Do you want to add Neit to your system PATH? (y/n)"))
                {
                    AddToEnvironmentPathLinux(neitInstallPath);
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Linux installation completed successfully!");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.ResetColor();
            }
        }

        // Confirm action with the user
        static bool ConfirmAction(string message)
        {
            Console.Write($"{message} ");
            string response = Console.ReadLine().Trim().ToLower();
            return response == "y" || response == "yes";
        }

        // ===================== Linux-Specific Functions =====================

        // Check if running with sudo privileges (Linux)
        static bool IsSudo()
        {
            return geteuid() == 0;
        }

        [DllImport("libc")]
        private static extern uint geteuid();

        // Install LLVM for Linux
        static void InstallLLVMLinux()
        {
            string[] packageManagers = {
                "sudo apt-get update && sudo apt-get install llvm lld -y",   // Debian/Ubuntu
"sudo dnf install llvm lld -y",                             // Fedora
"sudo pacman -S llvm lld --noconfirm",                      // Arch Linux
"sudo yum install llvm lld -y",                             // CentOS
"sudo zypper install llvm lld",                             // openSUSE
"sudo apk add llvm lld",                                    // Alpine
"sudo eopkg install llvm lld -y",                           // Solus
"sudo xbps-install -S llvm lld",                            // Void Linux
"sudo emerge --ask dev-lang/llvm dev-lang/lld"             // Gentoo

            };

            foreach (var command in packageManagers)
            {
                try
                {
                    Console.WriteLine($"Trying to install LLVM using: {command}");
                    RunShellCommand(command);
                    Console.WriteLine("LLVM installation succeeded.");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed with {command}: {ex.Message}");
                }
            }

            throw new Exception("Failed to install LLVM using available package managers.");
        }

        // Download and extract Neit using wget (Linux)
        static void DownloadAndExtractNeitLinux(string url, string destinationFolder)
        {
            string neitZipPath = Path.Combine(Path.GetTempPath(), "neit.zip");

            Console.WriteLine($"Downloading Neit using wget ({url})...");
            string wgetCommand = $"wget -O {neitZipPath} {url}";

            RunShellCommand(wgetCommand);
            Console.WriteLine("\nDownload completed.");

            Console.WriteLine("Extracting Neit...");
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }
            ZipFile.ExtractToDirectory(neitZipPath, destinationFolder);
            Console.WriteLine("Neit extracted successfully.");
        }

        // Add Neit to environment PATH for Linux
        static void AddToEnvironmentPathLinux(string path)
        {
            string bashProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".bashrc");
            if (!File.ReadAllText(bashProfilePath).Contains(path))
            {
                File.AppendAllText(bashProfilePath, $"\nexport PATH=\"$PATH:{path}\"");
                Console.WriteLine("Updated system PATH to include Neit.");
            }
            else
            {
                Console.WriteLine("Neit path is already included in the system PATH.");
            }
        }

        // ===================== Windows-Specific Functions =====================

        // Check if running as administrator (Windows)
        static bool IsRunAsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        // Download and extract Neit for Windows
        static async Task DownloadAndExtractNeitWindows(string url, string destinationFolder)
        {
            string neitZipPath = Path.Combine(Path.GetTempPath(), "neit_win.zip");
            long fileSize = await GetFileSize(url);

            Console.WriteLine($"Downloading Neit ({FormatBytes(fileSize)})...");
            using (WebClient webClient = new WebClient())
            {
                webClient.DownloadProgressChanged += (s, e) =>
                {
                    Console.Write($"\rDownload progress: {e.ProgressPercentage}%");
                };

                await webClient.DownloadFileTaskAsync(url, neitZipPath);
            }
            Console.WriteLine("\nDownload completed.");

            Console.WriteLine("Extracting Neit...");
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }
            ZipFile.ExtractToDirectory(neitZipPath, destinationFolder);
            Console.WriteLine("Neit extracted successfully.");
        }

        // Install LLVM for Windows
        static async Task InstallLLVMWindows(string downloadUrl)
        {
            Console.WriteLine("Downloading LLVM installer...");
            string tempFilePath = Path.GetTempFileName() + ".exe";

            using (WebClient webClient = new WebClient())
            {
                await webClient.DownloadFileTaskAsync(new Uri(downloadUrl), tempFilePath);
            }

            Console.WriteLine("Running LLVM installer...");
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = tempFilePath,
                    Arguments = "/S",  // Silent install
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit();

            Console.WriteLine("LLVM installation completed.");
        }

        // Install Visual C++ Redistributable for Windows
        static async Task InstallVCRedist()
        {
            Console.WriteLine("Downloading Visual C++ Redistributable installer...");
            string vcredistUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe";
            string tempFilePath = Path.GetTempFileName() + ".exe";

            using (WebClient webClient = new WebClient())
            {
                await webClient.DownloadFileTaskAsync(new Uri(vcredistUrl), tempFilePath);
            }

            Console.WriteLine("Running Visual C++ Redistributable installer...");
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = tempFilePath,
                    Arguments = "/quiet /norestart",  // Silent install with no restart
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit();

            Console.WriteLine("Visual C++ Redistributable installation completed.");
        }

        // Add Neit to environment PATH for Windows
        static void AddToEnvironmentPathWindows(string path)
        {
            string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
            if (!currentPath.Contains(path))
            {
                Environment.SetEnvironmentVariable("PATH", currentPath + ";" + path, EnvironmentVariableTarget.Machine);
                Console.WriteLine("Updated system PATH to include Neit.");
            }
            else
            {
                Console.WriteLine("Neit path is already included in the system PATH.");
            }
        }

        // ===================== Helper Functions =====================

        // Runs a shell command (for Linux)
        static void RunShellCommand(string command)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process process = Process.Start(psi);
            process.WaitForExit();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception(error);
            }

            Console.WriteLine(output);
        }

        // Gets file size from a URL
        static async Task<long> GetFileSize(string url)
        {
            WebRequest req = WebRequest.Create(url);
            req.Method = "HEAD";
            using (WebResponse resp = await req.GetResponseAsync())
            {
                return resp.ContentLength;
            }
        }

        // Formats bytes to a readable format
        static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes = bytes / 1024;
            }
            return $"{bytes:0.##} {sizes[order]}";
        }
    }
}
