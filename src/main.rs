use colored::*;
use indicatif::{ProgressBar, ProgressStyle};
use reqwest::blocking::get;
use std::env;
use std::fs;
use std::io::{self, Read, Write};
use std::path::Path;
use std::process::Command;
use zip::ZipArchive; // Add the zip crate to your Cargo.toml

const TDM_GCC_INSTALLER_URL: &str =
    "https://github.com/jmeubank/tdm-gcc/releases/download/v10.3.0-tdm64-2/tdm64-gcc-10.3.0-2.exe";
const NEIT_ZIP_URL_WINDOWS: &str =
    "https://github.com/OxumLabs/neit/releases/download/0.0.38/neit_win.zip";
const NEIT_ZIP_URL_LINUX: &str =
    "https://github.com/OxumLabs/neit/releases/download/0.0.38/neit_lin.zip";
const NEIT_FOLDER: &str = "neit"; // Folder to extract Neit
const VC_REDIST_URL: &str = "https://aka.ms/vs/17/release/vc_redist.x64.exe";

fn main() {
    let os_type = env::consts::OS;

    match os_type {
        "windows" => {
            install_tdm_gcc();
            check_and_install_vc_redist();
            download_and_extract_neit_windows(); // Call for Windows
        }
        "linux" => {
            install_clang();
            download_and_extract_neit_linux(); // Call for Linux
        }
        _ => {
            println!("{}", "Unsupported OS: ".red().bold());
        }
    }
}

// Function to install TDM-GCC for Windows
fn install_tdm_gcc() {
    println!("{}", "Installing TDM-GCC for Windows...".yellow());

    let installer_path = "tdm-gcc-installer.exe";

    // Check if TDM-GCC is already installed
    if which::which("gcc").is_ok() {
        println!("{}", "TDM-GCC is already installed.".green());
        return;
    }

    // Download the installer
    if let Err(e) = download_file(TDM_GCC_INSTALLER_URL, installer_path) {
        eprintln!("{}: {}", "Failed to download TDM-GCC".red(), e);
        return;
    }

    // Run the installer
    if let Err(e) = run_installer(installer_path) {
        eprintln!("{}: {}", "Failed to install TDM-GCC".red(), e);
        return;
    }

    // Set PATH (requires admin rights)
    if let Err(e) = add_to_path("C:\\TDM-GCC-64\\bin") {
        eprintln!("{}: {}", "Failed to set PATH".red(), e);
    }

    println!("{}", "TDM-GCC installed successfully!".green());
}

// Function to check and install VC Redistributable for Windows
fn check_and_install_vc_redist() {
    println!("{}", "Checking for VC Redistributable...".yellow());

    // Check if VC Redistributable is already installed
    if let Ok(_) = which::which("vcruntime140.dll") {
        println!("{}", "VC Redistributable is already installed.".green());
        return;
    }

    println!("{}", "Installing VC Redistributable...".yellow());

    // Download and install VC Redistributable
    let installer_path = "vc_redist.exe";
    if let Err(e) = download_file(VC_REDIST_URL, installer_path) {
        eprintln!("{}: {}", "Failed to download VC Redistributable".red(), e);
        return;
    }

    // Run the installer
    if let Err(e) = run_installer(installer_path) {
        eprintln!("{}: {}", "Failed to install VC Redistributable".red(), e);
        return;
    }

    println!("{}", "VC Redistributable installed successfully!".green());
}

// Function to install Clang for Linux
fn install_clang() {
    println!("{}", "Installing Clang for Linux...".yellow());

    // Check if clang is already installed
    if which::which("clang").is_ok() {
        println!("{}", "Clang is already installed.".green());
        if which::which("lld").is_ok() {
            println!("{}", "LLD is already installed.".green());
            return;
        } else {
            println!("Installing LLD...");
            let status = Command::new("sudo")
                .arg("apt-get")
                .arg("install")
                .arg("-y")
                .arg("lld")
                .status()
                .expect("Failed to execute command");

            if !status.success() {
                eprintln!("{}", "Failed to install LLD.".red());
                return;
            } else {
                println!("{}", "Finished Installing LLD and LLVM!");
            }
        }
        return;
    }

    // Install Clang using package manager
    let status = Command::new("sudo")
        .arg("apt-get")
        .arg("install")
        .arg("-y")
        .arg("clang")
        .status()
        .expect("Failed to execute command");

    if !status.success() {
        eprintln!("{}", "Failed to install Clang.".red());
        return;
    }

    println!("{}", "Clang and LLD installed successfully!".green());
}

// Helper function to download a file
fn download_file(url: &str, output: &str) -> Result<(), reqwest::Error> {
    let mut response = get(url)?;

    // Check the content length
    let total_size = response.content_length().unwrap_or(0) as u64;

    // Create a file to save the downloaded content
    let mut file = fs::File::create(output).unwrap();

    let pb = ProgressBar::new(total_size);
    pb.set_style(
        ProgressStyle::default_bar()
            .template("{msg} {bar:40.cyan/blue} {bytes:>7}/{total_bytes:7} ({eta})")
            .unwrap()
            .progress_chars("█ ▓"),
    );

    // Buffer for reading the response
    let mut buffer = vec![0; 32_768];
    let mut downloaded: u64 = 0;

    while let Ok(read_bytes) = response.read(&mut buffer) {
        if read_bytes == 0 {
            break;
        }
        file.write_all(&buffer[..read_bytes]).unwrap();
        downloaded += read_bytes as u64;
        pb.set_position(downloaded);
    }

    pb.finish_with_message("Download complete");
    Ok(())
}

// Run the installer (for Windows)
fn run_installer(installer_path: &str) -> io::Result<()> {
    let status = Command::new(installer_path)
        .arg("/S") // Silent mode, adjust arguments as necessary
        .status()?;

    if !status.success() {
        return Err(io::Error::new(io::ErrorKind::Other, "Installer failed"));
    }

    Ok(())
}

// Add a directory to the PATH environment variable
fn add_to_path(new_path: &str) -> io::Result<()> {
    let current_path = env::var("PATH").unwrap_or_else(|_| String::new());
    let new_path = format!("{};{}", current_path, new_path);
    env::set_var("PATH", new_path.clone());
    println!("{}: {}", "Added".green(), new_path);
    Ok(())
}

// Download and extract Neit for Windows
fn download_and_extract_neit_windows() {
    println!(
        "{}",
        "Downloading and extracting Neit for Windows...".yellow()
    );

    let installer_path = "neit_windows.zip"; // Use the correct name for the Windows ZIP file

    // Download the Neit zip file
    if let Err(e) = download_file(NEIT_ZIP_URL_WINDOWS, installer_path) {
        eprintln!("{}: {}", "Failed to download Neit".red(), e);
        return;
    }

    // Extract the Neit zip file
    let file = fs::File::open(installer_path).unwrap();
    let mut archive = ZipArchive::new(file).unwrap();

    for i in 0..archive.len() {
        let mut file = archive.by_index(i).unwrap();
        let outpath = format!(
            "C:/Users/{}/{}/{}",
            env::var("USERNAME").unwrap(),
            NEIT_FOLDER,
            file.name()
        );

        if file.name().ends_with('/') {
            fs::create_dir_all(&outpath).unwrap();
        } else {
            if let Some(parent) = Path::new(&outpath).parent() {
                fs::create_dir_all(parent).unwrap();
            }
            let mut output_file = fs::File::create(&outpath).unwrap();
            io::copy(&mut file, &mut output_file).unwrap();
        }
    }

    // Set up the path for Neit (if necessary)
    let neit_bin_path = format!(
        "C:/Users/{}/{}{}",
        env::var("USERNAME").unwrap(),
        NEIT_FOLDER,
        "/bin"
    );
    if env::var("PATH")
        .unwrap_or_else(|_| String::new())
        .contains(&neit_bin_path)
    {
        println!("{}: {}", "Neit already in PATH.".yellow(), neit_bin_path);
    } else {
        add_to_path(&neit_bin_path).unwrap();
        println!(
            "{}",
            "Neit installed and added to PATH successfully!".green()
        );
    }
}

// Download and extract Neit for Linux
fn download_and_extract_neit_linux() {
    println!(
        "{}",
        "Downloading and extracting Neit for Linux...".yellow()
    );

    let installer_path = "neit_linux.zip"; // Use the correct name for the Linux ZIP file

    // Download the Neit zip file
    if let Err(e) = download_file(NEIT_ZIP_URL_LINUX, installer_path) {
        eprintln!("{}: {}", "Failed to download Neit".red(), e);
        return;
    }

    // Extract the Neit zip file
    let file = fs::File::open(installer_path).unwrap();
    let mut archive = ZipArchive::new(file).unwrap();

    for i in 0..archive.len() {
        let mut file = archive.by_index(i).unwrap();
        let outpath = format!("/usr/local/{}/{}", NEIT_FOLDER, file.name()); // Change as per your Linux folder structure

        if file.name().ends_with('/') {
            fs::create_dir_all(&outpath).unwrap();
        } else {
            if let Some(parent) = Path::new(&outpath).parent() {
                fs::create_dir_all(parent).unwrap();
            }
            let mut output_file = fs::File::create(&outpath).unwrap();
            io::copy(&mut file, &mut output_file).unwrap();
        }
    }

    // Set up the path for Neit (if necessary)
    let neit_bin_path = format!("/usr/local/{}/bin", NEIT_FOLDER); // Change as per your Linux folder structure
    if env::var("PATH")
        .unwrap_or_else(|_| String::new())
        .contains(&neit_bin_path)
    {
        println!("{}: {}", "Neit already in PATH.".yellow(), neit_bin_path);
    } else {
        add_to_path(&neit_bin_path).unwrap();
        println!(
            "{}",
            "Neit installed and added to PATH successfully!".green()
        );
    }
}
