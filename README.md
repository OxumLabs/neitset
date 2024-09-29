# Neitset

Welcome to the **Neitset**, a cross-platform installer that simplifies the process of setting up the [_*Neit programming language*_](https://github.com/oxumlabs/neit) on both Windows and Linux. The installer automatically detects your operating system and guides you through the necessary steps to install dependencies and configure Neit.

## Features

- **OS Detection**: Automatically detects if you're running Windows or Linux and executes the appropriate installation process.
- **Dependency Management**: Installs essential tools like LLVM, Visual C++ Redistributable (for Windows), and sets up the environment.
- **System PATH Configuration**: Automatically adds Neit to your system's PATH for easy command-line access.
- **Simple User Prompts**: Offers an easy-to-follow installation process with yes/no prompts for each installation step.

## Prerequisites

### Windows

- Administrator privileges
- Internet connection to download dependencies like LLVM and Visual C++ Redistributable.

### Linux

- Internet connection
- `sudo` privileges to install packages (LLVM and Neit).

## Installation Steps

### Windows

1. Run the installer as Administrator.
2. Follow the prompts to:
   - Install LLVM
   - Install Neit (if you want to upgrade and this is the latest installer then type 'y' to update)
   - Install Visual C++ Redistributable (if needed)
   - Add Neit to your system's PATH
3. The installation will complete successfully, and Neit will be ready to use!

### Linux

1. Run the installer with `sudo` privileges.
2. Follow the prompts to:
   - Install LLVM (for your specific Linux distribution)
   - Install Neit (if you want to upgrade and this is the latest installer then type 'y' to update)
   - Add Neit to your system's PATH
3. The installation will complete successfully, and Neit will be ready to use!

## License

This installer is provided under the [MIT License](LICENSE).
