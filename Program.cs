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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await HandleWindowsInstallation();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                await HandleLinuxInstallation();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Unsupported operating system.");
                Console.ResetColor();
            }
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
                if (await ConfirmAction("Do you want to install LLVM? (y/n)"))
                {
                    await InstallLLVMWindows("https://github.com/llvm/llvm-project/releases/download/llvmorg-19.1.0/LLVM-19.1.0-win64.exe");
                }

                // Step 2: Download and install Neit
                string neitInstallPath = @"C:\Program Files\Neit";
                if (await ConfirmAction("Do you want to download and install Neit? (y/n)"))
                {
                    string neitZipUrl = "https://github.com/OxumLabs/neit/releases/download/0.0.34/neit_win.zip";
                    await DownloadAndExtractNeitWindows(neitZipUrl, neitInstallPath);
                }

                // Step 3: Install Visual C++ Redistributable
                if (await ConfirmAction("Do you want to install Visual C++ Redistributable? (y/n)"))
                {
                    await InstallVCRedist();
                }

                // Step 4: Add Neit to system PATH
                if (await ConfirmAction("Do you want to add Neit to your system PATH? (y/n)"))
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
        static async Task HandleLinuxInstallation()
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
                if (await ConfirmAction("Do you want to install LLVM? (y/n)"))
                {
                    await InstallLLVMLinux();
                }

                // Step 2: Download and install Neit using wget
                string neitInstallPath = "/usr/local/bin/Neit/linux";
                if (await ConfirmAction("Do you want to download and install Neit? (y/n)"))
                {
                    string neitZipUrl = "https://github.com/OxumLabs/neit/releases/download/0.0.34/neit_lin.zip";
                    await DownloadAndExtractNeitLinux(neitZipUrl, neitInstallPath);
                }

                // Step 3: Add Neit to system PATH
                if (await ConfirmAction("Do you want to add Neit to your system PATH? (y/n)"))
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
        static async Task<bool> ConfirmAction(string message)
        {
            Console.Write($"{message} ");
            string response = Console.ReadLine().Trim().ToLower();
            return response == "y" || response == "yes";
        }

        // ===================== Linux-Specific Functions =====================

        // Check if running with sudo privileges (Linux)
        static bool IsSudo()
        {
            return Environment.UserName == "root";
        }

        // Install LLVM for Linux
        static async Task InstallLLVMLinux()
        {
            string[] packageManagers = {
                "sudo apt-get update && sudo apt-get install llvm -y",  // Debian/Ubuntu
                "sudo dnf install llvm -y",                             // Fedora
                "sudo pacman -S llvm --noconfirm",                      // Arch Linux
                "sudo yum install llvm -y",                             // CentOS
                "sudo zypper install llvm",                             // openSUSE
                "sudo apk add llvm",                                    // Alpine
                "sudo eopkg install llvm -y",                           // Solus
                "sudo xbps-install -S llvm",                            // Void Linux
                "sudo emerge --ask dev-lang/llvm"                       // Gentoo
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
        static async Task DownloadAndExtractNeitLinux(string url, string destinationFolder)
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

        // Add Neit to environment PATH for Windows
        static void AddToEnvironmentPathWindows(string path)
        {
            string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
            if (!currentPath.Contains(path))
            {
                string newPath = $"{currentPath};{path}";
                Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Machine);
                Console.WriteLine("Updated system PATH to include Neit.");
            }
            else
            {
                Console.WriteLine("Neit path is already included in the system PATH.");
            }
        }

        // Download and install LLVM for Windows
        static async Task InstallLLVMWindows(string url)
        {
            string llvmInstallerPath = Path.Combine(Path.GetTempPath(), "LLVMInstaller.exe");
            Console.WriteLine("Downloading LLVM...");
            using (WebClient webClient = new WebClient())
            {
                await webClient.DownloadFileTaskAsync(url, llvmInstallerPath);
            }

            Console.WriteLine("Installing LLVM...");
            Process.Start(llvmInstallerPath)?.WaitForExit();
            Console.WriteLine("LLVM installed successfully.");
        }

        // Install Visual C++ Redistributable for Windows
        static async Task InstallVCRedist()
        {
            string url = "https://aka.ms/vs/17/release/vc_redist.x64.exe";
            string installerPath = Path.Combine(Path.GetTempPath(), "vcredist.exe");

            Console.WriteLine("Downloading Visual C++ Redistributable...");
            using (WebClient webClient = new WebClient())
            {
                await webClient.DownloadFileTaskAsync(url, installerPath);
            }

            Console.WriteLine("Installing Visual C++ Redistributable...");
            Process.Start(installerPath, "/install /quiet /norestart")?.WaitForExit();
            Console.WriteLine("Visual C++ Redistributable installed successfully.");
        }

        // ===================== Helper Functions =====================

        // Run a shell command (for Linux)
        static void RunShellCommand(string command)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Command '{command}' failed with exit code {process.ExitCode}");
            }
        }

        // Get file size (for download progress)
        static async Task<long> GetFileSize(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "HEAD";
            using (WebResponse resp = await req.GetResponseAsync())
            {
                return resp.ContentLength;
            }
        }

        // Format bytes to human-readable size
        static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes /= 1024;
            }
            return $"{bytes:0.##} {sizes[order]}";
        }
    }
}
