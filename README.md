# UniversalAIOMaker

UniversalAIOMaker is a Windows Forms application for building a bootable, multi-version Windows installation ISO from ISO files that you provide. It groups older Windows releases into GRUB4DOS menus and combines supported Vista-and-later setup media using Windows Imaging tools.

This repository contains the C# GUI edition. It does not include the Windows installation ISOs that you process. The app requires separate boot files and command-line utilities at runtime; see [External tools and boot files](#external-tools-and-boot-files).

## What the application does

At a high level, the program scans folders you choose, lets you review the detected ISO list and menu labels, extracts and adjusts boot files into a temporary working tree, then invokes external tools to create a bootable output ISO. The main code areas are:

- `Program.cs` starts the Windows Forms application.
- `MainForm.cs` implements the three-tab interface, path settings, ISO scanning, editable boot entries, progress and logging.
- `ISOProcessor.cs` performs the ISO extraction, boot configuration changes, WIM handling and final ISO creation.
- `BOMRemover.cs`, `HexEdit.cs` and `InfFileEditor.cs` provide text encoding, binary patching and INF editing helpers.
- `UniversalAIOMaker.csproj` defines a .NET 6 Windows-targeted project.

The GUI itself targets Windows. Building requires the .NET 6 SDK (or a compatible later SDK) and Windows Forms support. The resulting program also depends on external utilities and boot assets described below.

## Repository contents and publishing

Keep `bin/` and `obj/` build output, generated ISOs, user ISO files, and the support assets out of the source repository. Publish the application binaries and the contents of `Required/` as separate release ZIP downloads. Before uploading support assets, check that their licenses permit redistribution and include all required notices. The GRUB4DOS files carry a GNU GPL version 2 notice; retain its license and required notices with any redistribution of those files.

## 🎯 New Features

### 1. **Graphical User Interface**
   - Easy-to-use tabbed interface
   - Visual ISO management
   - Real-time progress tracking
   - Detailed logging window

### 2. **Flexible ISO Path Selection**
   - Separate path selection for each Windows type:
     - Windows NT x86
     - Windows NT amd64
     - Windows 9x
     - Modern Windows (Vista and later)
   - Folder browser with path persistence
   - Automatic path saving between sessions

### 3. **Hierarchical Boot Menu System**
   - Multi-level menu structure with category sub-menus:
     
     **Main Menu:**
     ```
     ╔════════════════════════════════════════════╗
     ║    UniversalAIOMaker - All-in-One Boot Menu   ║
     ╚════════════════════════════════════════════╝

     1 - Windows NT x86 (15 editions)
     2 - Windows NT amd64 (5 editions)
     3 - Windows 9x (3 editions)
     4 - Windows Vista and Later
     
     Esc - Boot to first hard drive [Default]
     ```
     
     **Sub-Menu Example (Windows NT x86 - Page 1 of 2):**
     ```
     ╔════════════════════════════════════════════╗
     ║  Windows NT x86                            ║
     ║  Page 1 of 2                               ║
     ╚════════════════════════════════════════════╝

     1 - Windows XP Professional SP3
     2 - Windows XP Home Edition SP3
     3 - Windows 2000 Professional
     ...
     9 - Windows Server 2003 Standard

     0 - More options...
     B - Back to main menu
     ```
   
   - **Navigation Features:**
     - Press 1-4 in main menu to enter category sub-menus
     - Press 1-9 in sub-menus to boot selected OS
     - Press '0' to go to next page (if more than 9 entries)
     - Press 'B' to return to main menu from any sub-menu
     - Press 'Esc' at any time to boot to hard drive
     - 30-second timeout returns to main menu
   
   - **Smart Pagination:**
     - Up to 9 entries per page
     - Automatic pagination for categories with 10+ entries
     - Page numbers shown in header
     - Seamless navigation between pages

### 4. **Smart Name Generation**
   - **Auto-Generate Mode** (enabled by default):
     - Automatically generates 4-character boot folder names
     - Extracts meaningful menu entries from ISO filenames
     - Sequential numbering per category (NT01, NT02, N003, N004, W9x1, W9x2)
   
   - **Manual Edit Support**:
     - Review all generated names in a grid view
     - Edit any boot folder name or menu entry
     - Real-time validation (4 characters, alphanumeric only)
     - Duplicate detection
     - "Auto-Generate Names" button to regenerate if needed

### 5. **Automatic NT Boot File Patching**
   - Applies the `setupldr.bin` offset patch (`0x2060`: `74 03` → `EB 1A`) automatically when Server 2003 with a service pack is detected (NT x86), and always for NT amd64.
   - Patches both uppercase and lowercase path variants (`i386`/`I386`, `amd64`/`AMD64`) because GRUB4DOS and the NT loaders are case-sensitive.
   - Renames `setupldr.bin` → `SETUPLDR.BIN` and `txtsetup.sif` → `TXTSETUP.SIF` in every NT boot folder.
   - For NT amd64, merges the first `[SourceDisksNames.amd64]` and `[SourceDisksFiles.amd64]` sections into the generic `[SourceDisksNames]` and `[SourceDisksFiles]` sections, inserted at the top of each section. Text-mode setup reads the generic sections only, so this fixes the "value 1 was not set" error and lets it resolve files such as `ntkrnlmp.exe`. Disk numbers are copied unconditionally; file entries are copied only when the file (or its compressed form, e.g. `hal.dll` → `HAL.DL_`) exists in the boot folder. Duplicate keys are never created.

### 6. **Modern Windows (Vista and later)**
   - Each ISO's `sources\boot.wim` contributes only its **Setup image** (index 2, detected by image name). That image is applied to a temporary folder, modified, and captured back as `sources\bootN.wim` as a **single, bootable** image (`DISM /Capture-Image /Bootable`).
   - Inside every `bootN.wim`, `sources\setup.exe` and the root `setup.exe` are renamed to `setupN.exe`, and `Windows\System32\winpeshl.ini` is written to launch that version's loader. This keeps each Windows version's setup executable separate and avoids the "Select the driver to be installed" screen.
   - The media `sources` folder also receives `setupN.exe` copies.
   - `boot\BCD` is configured with one entry per ISO pointing at `ramdisk=[boot]\sources\bootN.wim`, with `displaybootmenu Yes` and a 30-second timeout so the Windows Boot Manager always lists the available setups instead of silently booting `boot1.wim`.
   - All `install.wim` indexes are exported into one combined image, then split into `install.swm`/`install2.swm` when larger than 3580 MB.

## 📁 Project Structure

```
UniversalAIOMaker/
├── Program.cs          - Application entry point
├── MainForm.cs            - Main GUI form with 3 tabs
├── ISOProcessor.cs        - Core processing logic with categorized menus
├── BOMRemover.cs          - UTF-8 BOM removal utility
├── HexEdit.cs             - Hex editing utility
├── InfFileEditor.cs       - INF file editor
└── UniversalAIOMaker.csproj - Project file with Windows Forms
```

## 🚀 Building

### Requirements:
- .NET 6.0 SDK or later (Windows Forms support)
- Windows OS (for Windows Forms)

### Build Commands:

```bash
# Build the GUI project
dotnet build UniversalAIOMaker.csproj

# Build in Release mode
dotnet build UniversalAIOMaker.csproj -c Release

# Publish as self-contained executable
dotnet publish UniversalAIOMaker.csproj -c Release -r win-x64 --self-contained
```

### Download and install a release

Release downloads are provided as two separate ZIP files:

- The versioned application ZIP contains the UniversalAIOMaker binaries.
- `UniversalAIOMaker-Required.zip` contains the support files from the `Required/` folder. This support ZIP is separate so it can be reused across application updates.

For the first installation, extract the application ZIP into a folder, then extract the **contents** of `UniversalAIOMaker-Required.zip` into that same folder, beside `UniversalAIOMaker.exe`. Do not leave the files inside an extra `Required` subfolder. Keep the `grub4dos/` directory and its subdirectories intact when extracting.

For later UniversalAIOMaker updates, replace the application binaries with the newer version and keep the existing support files. Download a new support ZIP only if a release's notes say the required files or their layout have changed. The two ZIPs must still be kept together in the same application folder.

## 📖 Usage Guide

### Step 1: ISO Path Configuration

1. Launch `UniversalAIOMaker.exe`
2. On the **ISO Paths** tab:
   - Click "Browse..." for each Windows type you want to include
   - Select folders containing your ISO files
   - Enable "Auto-generate boot folder names and menu entries" (recommended)
   - The `setupldr.bin` offset patch (`0x2060`: `74 03` → `EB 1A`) is applied automatically when Server 2003 with a service pack is detected (NT x86), and always for NT amd64.
   - Optionally enable **"Enable graphics mode"** to add `graphicsmode -1 800 600` to the GRUB4DOS menus.
   - Optionally enable **"Background image (BMP)"**, browse for a `.bmp` file, and it will be copied to the CD root as `background.bmp` with `splashimage background.bmp` added to the menus. This option is only available while graphics mode is enabled.
   - **Graphics mode** and **Background image (BMP)** features are **experimental** and might not as intended!
3. Click **"Scan ISOs"**

### Step 2: Review and Edit (Optional)

The app switches to the **ISO Configuration** tab showing all found ISOs:

| ISO File Name | Type | Boot Folder | Menu Entry |
|--------------|------|-------------|------------|
| winxp_sp3.iso | NT x86 | NT01 | Windows XP Professional SP3 |
| win2k_pro.iso | NT x86 | NT02 | Windows 2000 Professional |
| ... | ... | ... | ... |

**You can:**
- Edit boot folder names (must be exactly 4 alphanumeric characters)
- Edit menu entry text (shown in boot menu)
- Click "Auto-Generate Names" to regenerate all names
- Validation ensures no duplicates and correct format

### Step 3: Process ISOs

1. Click **"Start Processing"**
2. Switch to the **Processing** tab automatically
3. Monitor progress:
   - Progress bar shows overall completion
   - Log window shows detailed step-by-step operations
4. Wait for completion message

### Step 4: Result

Output file: `UniversalAIOMaker_Media.iso` in the application directory

## 🎨 GUI Features

### Tab 1: ISO Paths
- 4 path selectors with browse buttons
- Auto-generate checkbox
- Automatic setupldr.bin offset patch (Server 2003 + SP for NT x86, always for NT amd64)
- Optional graphics mode checkbox
- Optional BMP background image (only enabled when graphics mode is on)
- Scan ISOs button
- Settings persistence (saved to `settings.ini`)

### Tab 2: ISO Configuration
- Editable data grid
- Column-based entry management
- Auto-generate names button
- Start processing button with validation
- Real-time error checking

### Tab 3: Processing
- Progress bar (visual completion %)
- Console-style log output
- Detailed operation tracking
- Success/error notifications

## 🔧 Settings Persistence

The application saves your folder paths in `settings.ini`:

```ini
NT32Path=C:\ISOs\WindowsNT_x86
NT64Path=C:\ISOs\WindowsNT_x64
Win9xPath=C:\ISOs\Windows9x
ModernPath=C:\ISOs\WindowsModern
OutputPath=C:\Output\UniversalAIOMaker_Media.iso
GraphicsMode=0
UseBackgroundImage=0
BackgroundImagePath=
```

Paths and all checkboxes are automatically loaded on next launch.

## 📋 Hierarchical Menu Structure

The boot menu uses a hierarchical navigation system:

### Main Menu
- Shows only categories that have ISOs
- Displays count of editions per category
- Example: "Windows NT x86 (15 editions)"
- 4 possible categories maximum

### Sub-Menus
- Each category opens its own sub-menu
- Up to 9 entries per page (numbered 1-9)
- Automatic pagination for 10+ entries
- Navigation:
  - **1-9**: Boot selected OS
  - **0**: Next page (if available)
  - **B**: Back to main menu
  - **Esc**: Boot to hard drive

### Menu Flow
```
Main Menu (30s timeout → hard drive)
    ├─ 1: Windows NT x86 → Sub-Menu
    │   ├─ 1-9: Boot OS
    │   ├─ 0: Next page (if 10+ entries)
    │   └─ B: Back to Main
    │
    ├─ 2: Windows NT amd64 → Sub-Menu
    │   ├─ 1-9: Boot OS
    │   └─ B: Back to Main
    │
    ├─ 3: Windows 9x → Sub-Menu
    │   ├─ 1-9: Boot OS
    │   └─ B: Back to Main
    │
    └─ 4: Vista/Later → Boot directly
```

### Multi-Page Example
If you have 20 NT x86 ISOs:
- Page 1: Entries 1-9 + "0 - More options..."
- Page 2: Entries 1-9 + "0 - More options..."
- Page 3: Entries 1-2 + "B - Back to main menu"

Pressing 'B' from any page returns directly to the Main Menu.

### Color Scheme
- **White** (0x0F): Normal text
- **Yellow** (0x0E): Category headers, Back option
- **Gray** (0x07): Separators
- **Cyan** (0x0B): Prompt text

## 🔍 Validation Rules

Boot Folder Names:
- ✅ Exactly 4 characters
- ✅ Alphanumeric only (a-z, A-Z, 0-9)
- ✅ No duplicates (except Modern ISOs share "Modn")
- ❌ Special characters not allowed
- ❌ Spaces not allowed

Examples:
- ✅ `NT01`, `N003`, `W9x1`, `WXPS`, `2K03`
- ❌ `NT1` (too short), `WINXP` (too long), `NT-1` (special char)

## External tools and boot files

The separate `Required/` support bundle contains the runtime tools and boot assets. Its current contents include `7z.exe` and `7z.dll`, `xorriso.exe` and its companion MSYS DLLs, `oscdimg.exe`, boot images (`nt_32.bif`, `nt_64.bif`, `Win95.img`, `Win98.img`, `WinME.img`, and `etfsboot.com`), a root `grldr`, and the `grub4dos/` directory and its subdirectories. Preserve this directory structure when creating or extracting the ZIP. The current source creates ISOs with `xorriso.exe`; `oscdimg.exe` is included in the support bundle but is not currently invoked by the source.

Windows supplies the other system tools the app invokes, including DISM, BCDEdit, `takeown`, `icacls`, and `cmd.exe`; they are not part of `UniversalAIOMaker-Required.zip`. The support bundle can be reused for later application versions unless release notes announce that its files or layout have changed.

## 🆚 Comparison: Console vs GUI

| Feature | Console Version | GUI Version (Flat Menu) | GUI Version (Hierarchical) |
|---------|----------------|-------------------------|----------------------------|
| User Interface | Command line prompts | Windows Forms GUI | Windows Forms GUI |
| ISO Selection | Fixed folders | Flexible per-type paths | Flexible per-type paths |
| Name Entry | Manual per ISO | Auto-generate + edit | Auto-generate + edit |
| Boot Menu | Simple list | Categorized sections | Multi-level sub-menus |
| Menu Navigation | Direct selection | Direct selection | Hierarchical (Main → Sub) |
| Large Collections | All entries visible | All entries visible | Paginated (9 per page) |
| Progress Tracking | Text output | Progress bar + log | Progress bar + log |
| Settings | None | Persistent paths | Persistent paths |
| Validation | Minimal | Real-time with errors | Real-time with errors |
| Edit After Generate | No | Yes, in grid view | Yes, in grid view |

## 💡 Tips

1. **First Time Setup**: Use auto-generate, review in grid, adjust as needed
2. **Consistent Naming**: Use a pattern like `NT01`, `NT02` for clarity
3. **Menu Entries**: Keep them descriptive but concise (under 50 chars)
4. **Modern ISOs**: All modern Windows ISOs share one combined `install.wim`, while each gets its own `bootN.wim` and BCD boot entry
5. **Testing**: Test the generated ISO in a VM before physical media
6. **Large Collections**: The hierarchical menu handles 100+ ISOs elegantly
7. **Navigation**: 'B' key always returns to main menu from any sub-menu
8. **Pagination**: Each sub-menu page shows up to 9 entries for clarity

## 🐛 Troubleshooting

**"No ISOs Found"**
- Check that folders contain `.iso` files
- Verify folder paths are correct

**Validation Errors**
- Boot folder must be exactly 4 characters
- Check for duplicates in the grid
- Ensure only letters and numbers

**Processing Fails**
- Ensure required external tools (including 7z and xorriso) are available beside the application or on `PATH`
- Check that boot files (`.bif`, `.img`) are present
- Review log output for specific error

