using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UniversalAIOMaker
{
    public class ISOProcessor
    {
        private MainForm mainForm;
        private string scriptDirectory;
        private int currentProgress = 0;
        
        // Categorized entries
        private List<ISOEntry> nt32Entries = new List<ISOEntry>();
        private List<ISOEntry> nt64Entries = new List<ISOEntry>();
        private List<ISOEntry> win9xEntries = new List<ISOEntry>();
        private List<ISOEntry> modernEntries = new List<ISOEntry>();

        // Set when the background image was successfully copied to CDRoot
        private bool backgroundImageReady = false;
        
        public ISOProcessor(MainForm form)
        {
            mainForm = form;
            scriptDirectory = AppDomain.CurrentDomain.BaseDirectory;
        }
        
        public void ProcessISOs(List<ISOEntry> entries)
        {
            try
            {
                mainForm.LogMessage("=== UniversalAIOMaker Processing Started ===");
                mainForm.LogMessage($"Total ISOs to process: {entries.Count}");
                mainForm.LogMessage("");
                
                // Create CDRoot directory
                string cdRootPath = Path.Combine(scriptDirectory, "CDRoot");
                if (Directory.Exists(cdRootPath))
                {
                    mainForm.LogMessage("Cleaning up existing CDRoot directory...");
                    Directory.Delete(cdRootPath, true);
                }
                Directory.CreateDirectory(cdRootPath);
                mainForm.LogMessage("Created CDRoot directory");
                mainForm.LogMessage("");
                
                // Group by type
                nt32Entries = entries.Where(e => e.Type == ISOType.NT32).ToList();
                nt64Entries = entries.Where(e => e.Type == ISOType.NT64).ToList();
                win9xEntries = entries.Where(e => e.Type == ISOType.Win9x).ToList();
                modernEntries = entries.Where(e => e.Type == ISOType.Modern).ToList();
                
                // Process each type
                if (nt32Entries.Count > 0)
                {
                    mainForm.LogMessage($">>> Processing Windows NT x86 ISOs ({nt32Entries.Count})");
                    foreach (var entry in nt32Entries)
                    {
                        ProcessNT32ISO(entry);
                        currentProgress++;
                        mainForm.UpdateProgress(currentProgress);
                    }
                    mainForm.LogMessage("");
                }
                
                if (nt64Entries.Count > 0)
                {
                    mainForm.LogMessage($">>> Processing Windows NT amd64 ISOs ({nt64Entries.Count})");
                    foreach (var entry in nt64Entries)
                    {
                        ProcessNT64ISO(entry);
                        currentProgress++;
                        mainForm.UpdateProgress(currentProgress);
                    }
                    mainForm.LogMessage("");
                }
                
                if (win9xEntries.Count > 0)
                {
                    mainForm.LogMessage($">>> Processing Windows 9x ISOs ({win9xEntries.Count})");
                    foreach (var entry in win9xEntries)
                    {
                        ProcessWin9xISO(entry);
                        currentProgress++;
                        mainForm.UpdateProgress(currentProgress);
                    }
                    mainForm.LogMessage("");
                }
                
                if (modernEntries.Count > 0)
                {
                    mainForm.LogMessage($">>> Processing Modern Windows ISOs ({modernEntries.Count})");
                    ProcessModernISOs(modernEntries);
                    currentProgress += modernEntries.Count;
                    mainForm.UpdateProgress(currentProgress);
                    mainForm.LogMessage("");
                }
                
                // Pause before creating ISO if user enabled the option
                if (mainForm.PauseBeforeISO)
                {
                    mainForm.LogMessage(">>> Pausing before ISO creation");
                    mainForm.LogMessage("    You can now make manual modifications to CDRoot folder");
                    mainForm.LogMessage($"    CDRoot location: {cdRootPath}");
                    mainForm.LogMessage("");
                    
                    // Show dialog and wait for user confirmation
                    bool shouldContinue = false;
                    mainForm.Invoke(new Action(() => {
                        var result = System.Windows.Forms.MessageBox.Show(
                            $"Processing paused before ISO creation.\n\n" +
                            $"CDRoot folder location:\n{cdRootPath}\n\n" +
                            $"You can now make manual modifications to the files.\n\n" +
                            $"Click OK to continue with ISO creation, or Cancel to abort.",
                            "Pause Before ISO Creation",
                            System.Windows.Forms.MessageBoxButtons.OKCancel,
                            System.Windows.Forms.MessageBoxIcon.Information);
                        
                        shouldContinue = (result == System.Windows.Forms.DialogResult.OK);
                    }));
                    
                    if (!shouldContinue)
                    {
                        mainForm.LogMessage(">>> User cancelled ISO creation");
                        mainForm.LogMessage("=== Processing Cancelled ===");
                        return;
                    }
                    
                    mainForm.LogMessage(">>> User confirmed - continuing with ISO creation");
                    mainForm.LogMessage("");
                }
                
                // Uppercase all boot files in NT folders (GRUB4DOS is case-sensitive)
                mainForm.LogMessage(">>> Ensuring NT boot files are uppercase");
                UppercaseBootFiles();
                
                // Create ISO
                mainForm.LogMessage(">>> Creating bootable ISO image");
                CreateISO(modernEntries.Count, entries.Count);
                
                // Cleanup
                mainForm.LogMessage(">>> Cleaning up temporary files");
                CleanupFiles();
                
                mainForm.LogMessage("");
                mainForm.LogMessage("=== Processing Completed Successfully ===");
            }
            catch (Exception ex)
            {
                mainForm.LogMessage($"ERROR: {ex.Message}");
                mainForm.LogMessage($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        
        private void ProcessNT32ISO(ISOEntry entry)
        {
            mainForm.LogMessage($"  Processing: {entry.FileName}");
            mainForm.LogMessage($"    Boot folder: {entry.BootFolder}");
            mainForm.LogMessage($"    Menu entry: {entry.MenuEntry}");
            
            // Extract ISO
            string targetPath = Path.Combine(scriptDirectory, "CDRoot", "ROOT", entry.BootFolder);
            mainForm.LogMessage("    Extracting ISO...");
            RunCommand("7z.exe", $"x \"{entry.FilePath}\" -o\"{targetPath}\"", true);
            
            // Copy cdrom files
            string[] cdromFiles = { "cdrom_ip.5", "cdrom_is.5", "cdrom_ia.5", "cdrom_id.5", "cdrom_iw.5", "cdrom_nt.5" };
            foreach (var file in cdromFiles)
            {
                string srcFile = Path.Combine(targetPath, file);
                if (File.Exists(srcFile))
                {
                    File.Copy(srcFile, Path.Combine(scriptDirectory, "CDRoot", file), true);
                }
            }
            
            // Copy cdromsp files
            for (int s = 1; s <= 4; s++)
            {
                string spFile = $"cdromsp{s}.tst";
                string srcFile = Path.Combine(targetPath, spFile);
                if (File.Exists(srcFile))
                {
                    File.Copy(srcFile, Path.Combine(scriptDirectory, "CDRoot", spFile), true);
                }
            }
            
            // Copy win51 files
            string[] win51Files = { "win51ip", "win51ic", "win51id", "win51ia", "win51is", "win51ib", "win51il" };
            foreach (var file in win51Files)
            {
                string srcFile = Path.Combine(targetPath, file);
                if (File.Exists(srcFile))
                {
                    File.Copy(srcFile, Path.Combine(scriptDirectory, "CDRoot", file), true);
                }
                
                string win51File = Path.Combine(targetPath, "WIN51");
                if (File.Exists(win51File))
                {
                    File.Copy(win51File, Path.Combine(scriptDirectory, "CDRoot", file), true);
                }
                
                // Copy SP files
                for (int sp = 1; sp <= 3; sp++)
                {
                    string spFile = $"{file}.SP{sp}";
                    srcFile = Path.Combine(targetPath, spFile);
                    if (File.Exists(srcFile))
                    {
                        File.Copy(srcFile, Path.Combine(scriptDirectory, "CDRoot", spFile), true);
                    }
                }
            }
            
            // Create boot folder
            string bootFolder = Path.Combine(scriptDirectory, "CDRoot", entry.BootFolder);
            Directory.CreateDirectory(bootFolder);
            
            // Process txtsetup.sif to extract required files BEFORE copying setupldr.bin and txtsetup.sif
            mainForm.LogMessage("    Processing txtsetup.sif and extracting required files...");
            string txtsetupPath = Path.Combine(targetPath, "i386", "txtsetup.sif");
            string i386Path = Path.Combine(targetPath, "i386");
            ProcessTxtSetup(txtsetupPath, i386Path, bootFolder, "i386");
            
            // Always copy NTLDR (required for boot, even if not in txtsetup.sif)
            string ntldrSource = Path.Combine(targetPath, "i386", "NTLDR");
            if (File.Exists(ntldrSource))
            {
                File.Copy(ntldrSource, Path.Combine(bootFolder, "NTLDR"), true);
                if (mainForm.DebugMode)
                {
                    mainForm.LogMessage("[DEBUG] Copied NTLDR to boot folder");
                }
            }
            else
            {
                mainForm.LogMessage("    WARNING: NTLDR not found in i386 folder");
            }
            
            // Always copy NTDETECT.COM (required for boot, even if not in txtsetup.sif)
            string ntdetectSource = Path.Combine(targetPath, "i386", "NTDETECT.COM");
            if (File.Exists(ntdetectSource))
            {
                File.Copy(ntdetectSource, Path.Combine(bootFolder, "NTDETECT.COM"), true);
                if (mainForm.DebugMode)
                {
                    mainForm.LogMessage("[DEBUG] Copied NTDETECT.COM to boot folder");
                }
            }
            else
            {
                mainForm.LogMessage("    WARNING: NTDETECT.COM not found in i386 folder");
            }
            
            // Copy specific files
            File.Copy(
                Path.Combine(targetPath, "I386", "setupldr.bin"),
                Path.Combine(bootFolder, "setupldr.bin"),
                true);
            
            File.Copy(
                Path.Combine(targetPath, "I386", "txtsetup.sif"),
                Path.Combine(bootFolder, "txtsetup.sif"),
                true);
            
            // Copy system32 directory
            mainForm.LogMessage("    Copying system32 files...");
            CopyDirectory(
                Path.Combine(targetPath, "I386", "system32"),
                Path.Combine(bootFolder, "system32"));
            
            // Copy and modify .bif file
            string bifFile = Path.Combine(scriptDirectory, "CDRoot", $"{entry.BootFolder}.bif");
            File.Copy(Path.Combine(scriptDirectory, "nt_32.bif"), bifFile, true);
            
            // Run HexEdit commands
            mainForm.LogMessage("    Patching boot files...");

            // nt_32.bif searches are case-sensitive, so patch both "i386" and "I386"
            CallHexEdit(new[] { "-a", "I386", entry.BootFolder, bifFile, "-n" });
            CallHexEdit(new[] { "-a", "i386", entry.BootFolder, bifFile, "-n" });

            // setupldr.bin searches are case-sensitive, so patch both "i386" and "I386"
            CallHexEdit(new[] { "-a", "i386", entry.BootFolder, Path.Combine(bootFolder, "SETUPLDR.BIN"), "-n" });
            CallHexEdit(new[] { "-a", "I386", entry.BootFolder, Path.Combine(bootFolder, "SETUPLDR.BIN"), "-n" });
            
            // Run InfFileEditor
            CallInfFileEditor(new[] {
                "-i", Path.Combine(bootFolder, "txtsetup.sif"),
                "-m", "readwrite",
                "-n", "SetupSourcePath",
                "-d", $"\\Root\\{entry.BootFolder}\\"
            });
            
            // Run BOMRemover
            CallBOMRemover(new[] { "-i", Path.Combine(bootFolder, "txtsetup.sif") });
            
            // Check conditions for offset patch on NT x86:
            // Apply if Hivesxs.inf exists (Server 2003-based) AND win51 service pack file exists at root
            bool isServer2003 = File.Exists(Path.Combine(targetPath, "i386", "Hivesxs.inf"));

            // Check for specific win51 service pack files:
            // Base names: win51ip, win51ic, win51id, win51ia, win51is, win51ib, win51il
            // Extensions: .sp1 or .sp2
            // Examples: win51ip.sp1, win51ia.sp2, win51is.sp1
            string[] validSpBases = { "win51ip", "win51ic", "win51id", "win51ia", "win51is", "win51ib", "win51il" };
            bool hasServicePack = validSpBases.Any(baseName =>
                File.Exists(Path.Combine(targetPath, baseName + ".sp1")) ||
                File.Exists(Path.Combine(targetPath, baseName + ".sp2")));

            if (isServer2003 && hasServicePack)
            {
                mainForm.LogMessage("    Server 2003 and service pack detected - applying offset patch to setupldr.bin...");
                PatchSetupldrOffset(Path.Combine(bootFolder, "setupldr.bin"));
            }
            else
            {
                mainForm.LogMessage("    Skipping offset patch (requires both Server 2003 and service pack).");
            }
            mainForm.LogMessage($"    Completed: {entry.BootFolder}");
        }
        
        private void ProcessNT64ISO(ISOEntry entry)
        {
            mainForm.LogMessage($"  Processing: {entry.FileName}");
            mainForm.LogMessage($"    Boot folder: {entry.BootFolder}");
            mainForm.LogMessage($"    Menu entry: {entry.MenuEntry}");
            
            // Extract ISO
            string targetPath = Path.Combine(scriptDirectory, "CDRoot", "ROOT", entry.BootFolder);
            mainForm.LogMessage("    Extracting ISO...");
            RunCommand("7z.exe", $"x \"{entry.FilePath}\" -o\"{targetPath}\"", true);
            
            // Copy win51 files for 64-bit
            string[] win51Files = { "win51ap", "win51ad", "win51aa", "win51as" };
            foreach (var file in win51Files)
            {
                string srcFile = Path.Combine(targetPath, file);
                if (File.Exists(srcFile))
                {
                    File.Copy(srcFile, Path.Combine(scriptDirectory, "CDRoot", file), true);
                }
                
                string sp2File = $"{file}.SP2";
                string srcSP2 = Path.Combine(targetPath, sp2File);
                if (File.Exists(srcSP2))
                {
                    File.Copy(srcSP2, Path.Combine(scriptDirectory, "CDRoot", sp2File), true);
                }
                
                string win51File = Path.Combine(targetPath, "WIN51");
                if (File.Exists(win51File))
                {
                    File.Copy(win51File, Path.Combine(scriptDirectory, "CDRoot", file), true);
                }
            }
            
            // Create boot folder
            string bootFolder = Path.Combine(scriptDirectory, "CDRoot", entry.BootFolder);
            Directory.CreateDirectory(bootFolder);
            
            // Process txtsetup.sif for amd64
            mainForm.LogMessage("    Processing txtsetup.sif...");
            string txtsetupPath = Path.Combine(targetPath, "amd64", "txtsetup.sif");
            ProcessTxtSetup(txtsetupPath, Path.Combine(targetPath, "amd64"), bootFolder, "amd64");
            
            // Copy specific files
            File.Copy(
                Path.Combine(targetPath, "i386", "NTDETECT.COM"),
                Path.Combine(bootFolder, "NTDETECT.COM"),
                true);
            
            File.Copy(
                Path.Combine(targetPath, "i386", "NTLDR"),
                Path.Combine(bootFolder, "NTLDR"),
                true);
            
            File.Copy(
                Path.Combine(targetPath, "i386", "setupldr.bin"),
                Path.Combine(bootFolder, "setupldr.bin"),
                true);
            
            File.Copy(
                Path.Combine(targetPath, "amd64", "txtsetup.sif"),
                Path.Combine(bootFolder, "txtsetup.sif"),
                true);
            
            // Copy system32 directory
            mainForm.LogMessage("    Copying system32 files...");
            CopyDirectory(
                Path.Combine(targetPath, "amd64", "system32"),
                Path.Combine(bootFolder, "system32"));

            // Merge amd64 disk numbers and files into the generic sections so text-mode
            // setup can resolve disk 1 and files such as ntkrnlmp.exe.
            mainForm.LogMessage("    Synchronizing amd64 SourceDisksNames/SourceDisksFiles entries...");
            AddSourceDisksEntries(Path.Combine(bootFolder, "txtsetup.sif"), bootFolder, "amd64");
            
            // Copy and modify .bif file
            string bifFile = Path.Combine(scriptDirectory, "CDRoot", $"{entry.BootFolder}.bif");
            File.Copy(Path.Combine(scriptDirectory, "nt_64.bif"), bifFile, true);
            
            // Run HexEdit commands
            mainForm.LogMessage("    Patching boot files...");
            string setupLdrPath = Path.Combine(bootFolder, "setupldr.bin");

            // nt_64.bif searches are case-sensitive, so patch both "I386" and "i386"
            CallHexEdit(new[] { "-a", "I386", entry.BootFolder, bifFile, "-n" });
            CallHexEdit(new[] { "-a", "i386", entry.BootFolder, bifFile, "-n" });

            // Branch patch required by Server 2003 / x64 setupldr.bin
            CallHexEdit(new[] { "46 DA 74 03", "46 DA EB 1A", setupLdrPath, "-n" });

            // setupldr.bin searches are case-sensitive: patch both "i386" and "I386"
            CallHexEdit(new[] { "-a", "i386", entry.BootFolder, setupLdrPath, "-n" });
            CallHexEdit(new[] { "-a", "I386", entry.BootFolder, setupLdrPath, "-n" });

            // AMD64 paths are case-sensitive too; 5-character architecture names must be
            // padded to keep the original byte length (4-character boot folder + separator).
            CallHexEdit(new[] { "-m", "AMD64:x5C", $"{entry.BootFolder}:x5C:x00", setupLdrPath, "-n" });
            CallHexEdit(new[] { "-m", "amd64:x5C", $"{entry.BootFolder}:x5C:x00", setupLdrPath, "-n" });
            CallHexEdit(new[] { "-m", "AMD64:x00", $"{entry.BootFolder}:x00:x00", setupLdrPath, "-n" });
            CallHexEdit(new[] { "-m", "amd64:x00", $"{entry.BootFolder}:x00:x00", setupLdrPath, "-n" });

            // Patch setupldr.bin at offset 0x2060: 74 03 -> EB 1A (always for amd64)
            PatchSetupldrOffset(setupLdrPath);
            
            // Run InfFileEditor
            CallInfFileEditor(new[] {
                "-i", Path.Combine(bootFolder, "txtsetup.sif"),
                "-m", "readwrite",
                "-n", "SetupSourcePath",
                "-d", $"\\Root\\{entry.BootFolder}\\"
            });
            
            // Run BOMRemover
            CallBOMRemover(new[] { "-i", Path.Combine(bootFolder, "txtsetup.sif") });

            mainForm.LogMessage($"    Completed: {entry.BootFolder}");
        }
        
        private void ProcessWin9xISO(ISOEntry entry)
        {
            mainForm.LogMessage($"  Processing: {entry.FileName}");
            mainForm.LogMessage($"    Boot folder: {entry.BootFolder}");
            mainForm.LogMessage($"    Menu entry: {entry.MenuEntry}");
            
            // Extract ISO
            string targetPath = Path.Combine(scriptDirectory, "CDRoot", entry.BootFolder);
            mainForm.LogMessage("    Extracting ISO...");
            RunCommand("7z.exe", $"x \"{entry.FilePath}\" -o\"{targetPath}\"", true);
            
            // Determine bin9x file based on which version marker exists
            string bin9x = null;
            if (Directory.Exists(Path.Combine(targetPath, "WIN95")) || File.Exists(Path.Combine(targetPath, "WIN95")))
            {
                bin9x = "Win95.img";
                mainForm.LogMessage("    Detected: Windows 95");
            }
            else if (Directory.Exists(Path.Combine(targetPath, "WIN98")) || File.Exists(Path.Combine(targetPath, "WIN98")))
            {
                bin9x = "Win98.img";
                mainForm.LogMessage("    Detected: Windows 98");
            }
            else if (Directory.Exists(Path.Combine(targetPath, "WIN9X")) || File.Exists(Path.Combine(targetPath, "WIN9X")))
            {
                bin9x = "WinME.img";
                mainForm.LogMessage("    Detected: Windows ME");
            }
            
            if (!string.IsNullOrEmpty(bin9x))
            {
                mainForm.LogMessage($"    Using boot image: {bin9x}");
                
                // Use boot folder name for output file with .img extension (e.g., W9x1 -> W9x1.img)
                string imgFile = Path.Combine(scriptDirectory, "CDRoot", $"{entry.BootFolder}.img");
                
                // Copy and rename the appropriate boot image
                string sourceImg = Path.Combine(scriptDirectory, bin9x);
                if (File.Exists(sourceImg))
                {
                    File.Copy(sourceImg, imgFile, true);
                    mainForm.LogMessage($"    Created: {entry.BootFolder}.img");
                    
                    // Run HexEdit to patch the boot folder name
                    CallHexEdit(new[] { "-a", "DCBA", entry.BootFolder, imgFile, "-n" });
                }
                else
                {
                    mainForm.LogMessage($"    WARNING: {bin9x} not found in application directory");
                }
            }
            else
            {
                mainForm.LogMessage("    WARNING: Could not detect Windows 9x version (no WIN95/WIN98/WIN9X marker found)");
            }
            
            mainForm.LogMessage($"    Completed: {entry.BootFolder}");
        }
        
        private void ProcessModernISOs(List<ISOEntry> entries)
        {
            if (entries.Count == 0)
                return;
            
            string cdRootPath = Path.Combine(scriptDirectory, "CDRoot");
            string sourcesPath = Path.Combine(cdRootPath, "sources");
            string tempInstallWim = Path.Combine(scriptDirectory, "temp_install.wim");
            
            // Ensure sources directory exists
            Directory.CreateDirectory(sourcesPath);
            
            // Delete temp install.wim if it exists from previous run
            if (File.Exists(tempInstallWim))
            {
                File.Delete(tempInstallWim);
            }
            
            for (int i = 0; i < entries.Count; i++)
            {
                int isoNum = i + 1;
                var entry = entries[i];
                
                mainForm.LogMessage($"  Processing: {entry.FileName}");
                
                // Step I: Extract ISO to tempwim folder
                string tempWimPath = Path.Combine(scriptDirectory, "CDRoot", "tempwim");
                mainForm.LogMessage($"    Extracting ISO to temporary folder...");
                RunCommand("7z.exe", $"x \"{entry.FilePath}\" -o\"{tempWimPath}\"", true);
                
                if (i == 0)
                {
                    // Step II: Copy boot files/folders from first ISO to CDRoot
                    mainForm.LogMessage("    Copying boot files to CDRoot...");
                    
                    // Copy bootmgr
                    string bootmgrSrc = Path.Combine(tempWimPath, "bootmgr");
                    if (File.Exists(bootmgrSrc))
                    {
                        File.Copy(bootmgrSrc, Path.Combine(cdRootPath, "bootmgr"), true);
                        if (mainForm.DebugMode)
                            mainForm.LogMessage("[DEBUG] Copied bootmgr");
                    }
                    
                    // Copy bootmgr.efi if exists
                    string bootmgrEfiSrc = Path.Combine(tempWimPath, "bootmgr.efi");
                    if (File.Exists(bootmgrEfiSrc))
                    {
                        File.Copy(bootmgrEfiSrc, Path.Combine(cdRootPath, "bootmgr.efi"), true);
                        if (mainForm.DebugMode)
                            mainForm.LogMessage("[DEBUG] Copied bootmgr.efi");
                    }
                    
                    // Copy efi folder if exists
                    string efiSrc = Path.Combine(tempWimPath, "efi");
                    if (Directory.Exists(efiSrc))
                    {
                        string efiDest = Path.Combine(cdRootPath, "efi");
                        CopyDirectory(efiSrc, efiDest);
                        if (mainForm.DebugMode)
                            mainForm.LogMessage("[DEBUG] Copied efi folder");
                    }
                    
                    // Copy boot folder
                    string bootSrc = Path.Combine(tempWimPath, "boot");
                    if (Directory.Exists(bootSrc))
                    {
                        string bootDest = Path.Combine(cdRootPath, "boot");
                        CopyDirectory(bootSrc, bootDest);
                        if (mainForm.DebugMode)
                            mainForm.LogMessage("[DEBUG] Copied boot folder");
                    }
                }
                
                // Export install.wim's all indexes to temp_install.wim
                string sourceInstallWim = Path.Combine(tempWimPath, "sources", "install.wim");
                
                if (File.Exists(sourceInstallWim))
                {
                    mainForm.LogMessage("    Getting install.wim information...");
                    
                    var getInfoStartInfo = new ProcessStartInfo
                    {
                        FileName = "DISM.exe",
                        Arguments = $"/Get-WimInfo /WimFile:\"{sourceInstallWim}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    
                    int indexCount = 0;
                    try
                    {
                        using (var process = Process.Start(getInfoStartInfo))
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();
                            
                            if (mainForm.DebugMode)
                            {
                                mainForm.LogMessage($"[DEBUG] DISM Get-WimInfo output:");
                                mainForm.LogMessage($"[DEBUG] {output}");
                            }
                            
                            indexCount = System.Text.RegularExpressions.Regex.Matches(output, @"Index\s*:\s*\d+").Count;
                        }
                    }
                    catch (Exception ex)
                    {
                        mainForm.LogMessage($"    ERROR: Failed to get WIM info: {ex.Message}");
                        indexCount = 1;
                    }
                    
                    mainForm.LogMessage($"    Found {indexCount} index(es) in install.wim");
                    
                    // Export all indexes to temp_install.wim
                    for (int idx = 1; idx <= indexCount; idx++)
                    {
                        mainForm.LogMessage($"    Exporting install.wim index {idx} of {indexCount}...");
                        RunCommand("DISM.exe",
                            $"/Export-Image /SourceImageFile:\"{sourceInstallWim}\" /SourceIndex:{idx} /DestinationImageFile:\"{tempInstallWim}\" /Compress:max /CheckIntegrity");
                    }
                }
                else
                {
                    mainForm.LogMessage($"    WARNING: install.wim not found in {entry.FileName}");
                }
                
                // Step IV & VI: Copy lang.ini, setup.exe, and boot.wim (first ISO only gets lang.ini)
                if (i == 0)
                {
                    // Copy lang.ini
                    string langIniSrc = Path.Combine(tempWimPath, "sources", "lang.ini");
                    if (File.Exists(langIniSrc))
                    {
                        File.Copy(langIniSrc, Path.Combine(sourcesPath, "lang.ini"), true);
                        mainForm.LogMessage("    Copied lang.ini");
                    }
                }
                
                // Copy setup.exe as setupN.exe
                string setupSrc = Path.Combine(tempWimPath, "sources", "setup.exe");
                if (File.Exists(setupSrc))
                {
                    string setupDest = Path.Combine(sourcesPath, $"setup{isoNum}.exe");
                    File.Copy(setupSrc, setupDest, true);
                    mainForm.LogMessage($"    Copied setup.exe to setup{isoNum}.exe");
                }
                
                // Build sources\bootN.wim from the Setup image of boot.wim:
                // single bootable index, with setupN.exe + winpeshl.ini inside.
                string bootWimSrc = Path.Combine(tempWimPath, "sources", "boot.wim");
                if (File.Exists(bootWimSrc))
                {
                    string bootWimDest = Path.Combine(sourcesPath, $"boot{isoNum}.wim");
                    PrepareBootWimImage(bootWimSrc, bootWimDest, isoNum);
                }
                else
                {
                    mainForm.LogMessage($"    WARNING: boot.wim not found in {entry.FileName}");
                }
                
                // Step V: Clean tempwim folder
                if (Directory.Exists(tempWimPath))
                {
                    Directory.Delete(tempWimPath, true);
                    if (mainForm.DebugMode)
                        mainForm.LogMessage("[DEBUG] Cleaned tempwim folder");
                }
                
                mainForm.LogMessage($"    Completed: {entry.FileName}");
            }
            
            // Process temp_install.wim - check size and split or copy
            if (File.Exists(tempInstallWim))
            {
                FileInfo wimInfo = new FileInfo(tempInstallWim);
                long wimSizeMB = wimInfo.Length / 1024 / 1024;
                
                mainForm.LogMessage($"  Combined install.wim size: {wimSizeMB} MB");
                
                // Only split if larger than 3580 MB
                if (wimSizeMB > 3580)
                {
                    mainForm.LogMessage("  Splitting install.wim into SWM files...");
                    string swmFile = Path.Combine(sourcesPath, "install.swm");
                    
                    try
                    {
                        RunCommand("DISM.exe",
                            $"/Split-Image /ImageFile:\"{tempInstallWim}\" /SWMFile:\"{swmFile}\" /FileSize:3580");
                        
                        // Verify install.swm was created
                        if (File.Exists(swmFile))
                        {
                            mainForm.LogMessage("  Split successful: install.swm, install2.swm, etc.");
                            
                            if (mainForm.DebugMode)
                            {
                                mainForm.LogMessage("[DEBUG] Using install*.swm files");
                            }
                        }
                        else
                        {
                            mainForm.LogMessage("  WARNING: Split did not create install.swm, copying install.wim instead");
                            File.Copy(tempInstallWim, Path.Combine(sourcesPath, "install.wim"), true);
                        }
                    }
                    catch (Exception ex)
                    {
                        mainForm.LogMessage($"  WARNING: Failed to split install.wim: {ex.Message}");
                        mainForm.LogMessage("  Copying install.wim to sources folder instead");
                        File.Copy(tempInstallWim, Path.Combine(sourcesPath, "install.wim"), true);
                    }
                }
                else
                {
                    mainForm.LogMessage($"  install.wim is smaller than 3580 MB, copying as-is");
                    File.Copy(tempInstallWim, Path.Combine(sourcesPath, "install.wim"), true);
                }
                
                // Delete temp_install.wim
                try
                {
                    File.Delete(tempInstallWim);
                    if (mainForm.DebugMode)
                        mainForm.LogMessage("[DEBUG] Deleted temp_install.wim");
                }
                catch (Exception ex)
                {
                    mainForm.LogMessage($"  WARNING: Could not delete temp_install.wim: {ex.Message}");
                }
            }
            
            // Configure BCD with boot entries
            ConfigureBCD(entries);
        }
        
        /// <summary>
        /// Builds sources\bootN.wim from the Setup image of the source boot.wim.
        /// The Setup image (usually index 2) is applied to a temporary folder, setup.exe is
        /// renamed to setupN.exe so every Windows version keeps its own setup loader, a
        /// winpeshl.ini that starts that loader is written, and the folder is captured back
        /// into a single-index WIM marked as bootable.
        /// </summary>
        private void PrepareBootWimImage(string sourceWim, string destinationWim, int isoNum)
        {
            string workDir = Path.Combine(scriptDirectory, "CDRoot", $"tempboot{isoNum}");

            try
            {
                int setupIndex = FindSetupImageIndex(sourceWim);
                mainForm.LogMessage($"    Using boot.wim image index {setupIndex} (Setup image)");

                if (File.Exists(destinationWim))
                {
                    File.Delete(destinationWim);
                }

                if (Directory.Exists(workDir))
                {
                    DeleteDirectoryForce(workDir);
                }
                Directory.CreateDirectory(workDir);

                mainForm.LogMessage($"    Applying boot.wim index {setupIndex}...");
                RunCommand("DISM.exe",
                    $"/Apply-Image /ImageFile:\"{sourceWim}\" /Index:{setupIndex} /ApplyDir:\"{workDir}\"");

                string winpeshlExe = Path.Combine(workDir, "Windows", "System32", "winpeshl.exe");
                if (!File.Exists(winpeshlExe))
                {
                    mainForm.LogMessage("    WARNING: Setup image could not be applied, copying original boot.wim");
                    File.Copy(sourceWim, destinationWim, true);
                    return;
                }

                RenameSetupLoader(workDir, isoNum);
                WriteWinpeshlIni(workDir, isoNum);

                mainForm.LogMessage($"    Capturing boot{isoNum}.wim as a bootable image...");
                RunCommand("DISM.exe",
                    $"/Capture-Image /ImageFile:\"{destinationWim}\" /CaptureDir:\"{workDir}\" " +
                    $"/Name:\"Windows Setup {isoNum}\" /Description:\"Windows Setup {isoNum}\" " +
                    "/Compress:max /Bootable");

                if (File.Exists(destinationWim))
                {
                    mainForm.LogMessage($"    Created boot{isoNum}.wim (single bootable Setup image)");
                }
                else
                {
                    mainForm.LogMessage($"    WARNING: boot{isoNum}.wim was not created, copying original boot.wim");
                    File.Copy(sourceWim, destinationWim, true);
                }
            }
            catch (Exception ex)
            {
                mainForm.LogMessage($"    WARNING: Failed to prepare boot{isoNum}.wim: {ex.Message}");

                if (!File.Exists(destinationWim))
                {
                    File.Copy(sourceWim, destinationWim, true);
                }
            }
            finally
            {
                try
                {
                    if (Directory.Exists(workDir))
                    {
                        DeleteDirectoryForce(workDir);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        
        /// <summary>
        /// Deletes a directory tree that may contain files whose ACL was inherited from a
        /// WIM image (TrustedInstaller owner), which blocks normal deletion.
        /// </summary>
        private void DeleteDirectoryForce(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            RunCommand("takeown.exe", $"/f \"{path}\" /r /d y", true);
            RunCommand("icacls.exe", $"\"{path}\" /grant *S-1-5-32-544:(OI)(CI)F /t /c /q", true);

            try
            {
                Directory.Delete(path, true);
            }
            catch
            {
                RunCommand("cmd.exe", $"/c rd /s /q \"{path}\"", true);
            }
        }
        
        private List<KeyValuePair<int, string>> GetWimImages(string wimPath)
        {
            var images = new List<KeyValuePair<int, string>>();

            var startInfo = new ProcessStartInfo
            {
                FileName = "DISM.exe",
                Arguments = $"/Get-WimInfo /WimFile:\"{wimPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (mainForm.DebugMode)
                {
                    mainForm.LogMessage($"[DEBUG] DISM Get-WimInfo output for {Path.GetFileName(wimPath)}:");
                    mainForm.LogMessage($"[DEBUG] {output}");
                }

                int currentIndex = -1;

                foreach (string rawLine in output.Split('\n'))
                {
                    string line = rawLine.Trim();

                    var indexMatch = System.Text.RegularExpressions.Regex.Match(line, @"^Index\s*:\s*(\d+)");
                    if (indexMatch.Success)
                    {
                        currentIndex = int.Parse(indexMatch.Groups[1].Value);
                        images.Add(new KeyValuePair<int, string>(currentIndex, string.Empty));
                        continue;
                    }

                    var nameMatch = System.Text.RegularExpressions.Regex.Match(line, @"^Name\s*:\s*(.+)$");
                    if (nameMatch.Success && images.Count > 0)
                    {
                        images[images.Count - 1] = new KeyValuePair<int, string>(
                            currentIndex, nameMatch.Groups[1].Value.Trim());
                    }
                }
            }

            return images;
        }
        
        /// <summary>
        /// Returns the index of the Setup image (index 2 on Windows media). Images are
        /// matched by name first, then by position, and index 1 is used as a last resort.
        /// </summary>
        private int FindSetupImageIndex(string wimPath)
        {
            List<KeyValuePair<int, string>> images;

            try
            {
                images = GetWimImages(wimPath);
            }
            catch (Exception ex)
            {
                mainForm.LogMessage($"    WARNING: Failed to read boot.wim info: {ex.Message}");
                return 2;
            }

            foreach (var image in images)
            {
                if (image.Value.IndexOf("Setup", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return image.Key;
                }
            }

            if (images.Count >= 2)
            {
                return images[1].Key;
            }

            return images.Count > 0 ? images[0].Key : 1;
        }
        
        /// <summary>
        /// Renames setup.exe (both sources\setup.exe and the root loader) to setupN.exe so
        /// that every Windows version keeps its own setup executable inside its bootN.wim.
        /// Setup files inherit a TrustedInstaller ACL from the WIM, which blocks renames
        /// until ownership/permissions are adjusted.
        /// </summary>
        private void RenameSetupLoader(string workDir, int isoNum)
        {
            string[] setupFiles =
            {
                Path.Combine(workDir, "sources", "setup.exe"),
                Path.Combine(workDir, "setup.exe")
            };

            bool renamed = false;

            foreach (string setupPath in setupFiles)
            {
                if (!File.Exists(setupPath))
                {
                    continue;
                }

                string newPath = Path.Combine(Path.GetDirectoryName(setupPath), $"setup{isoNum}.exe");
                string folder = Path.GetFileName(Path.GetDirectoryName(setupPath));

                if (TryRenameFile(setupPath, newPath))
                {
                    renamed = true;
                    mainForm.LogMessage($"    Renamed {folder}\\setup.exe to setup{isoNum}.exe inside boot{isoNum}.wim");
                }
                else
                {
                    mainForm.LogMessage($"    WARNING: Could not rename {folder}\\setup.exe to setup{isoNum}.exe");
                }
            }

            if (!renamed)
            {
                mainForm.LogMessage($"    WARNING: setup.exe not found inside boot{isoNum}.wim");
            }
        }

        /// <summary>
        /// Renames a file, taking ownership and granting full control first when the
        /// image ACL (for example TrustedInstaller) blocks the operation. Falls back to
        /// copy + delete, and finally to copy only.
        /// </summary>
        private bool TryRenameFile(string sourcePath, string destinationPath)
        {
            try
            {
                File.Move(sourcePath, destinationPath);
                return true;
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }

            // WIM-applied files are owned by TrustedInstaller and not writable/deletable.
            GrantFullControl(sourcePath);

            try
            {
                File.Move(sourcePath, destinationPath);
                return true;
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }

            try
            {
                File.Copy(sourcePath, destinationPath, true);
                File.Delete(sourcePath);
                return true;
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }

            try
            {
                // Keep the new name even if the original cannot be removed; winpeshl.ini
                // starts the renamed loader, so the leftover file is not used.
                File.Copy(sourcePath, destinationPath, true);
                return true;
            }
            catch (Exception ex)
            {
                mainForm.LogMessage($"    WARNING: Copy failed for {Path.GetFileName(sourcePath)}: {ex.Message}");
                return false;
            }
        }

        private void GrantFullControl(string path)
        {
            RunCommand("takeown.exe", $"/f \"{path}\"");
            RunCommand("icacls.exe", $"\"{path}\" /grant *S-1-5-32-544:(F)");
        }

        /// <summary>
        /// Writes winpeshl.ini so WinPE starts this version's setup loader instead of the
        /// original sources\setup.exe.
        /// </summary>
        private void WriteWinpeshlIni(string workDir, int isoNum)
        {
            string sourcesSetup = Path.Combine(workDir, "sources", $"setup{isoNum}.exe");
            string rootSetup = Path.Combine(workDir, $"setup{isoNum}.exe");

            string setupCommand = File.Exists(sourcesSetup)
                ? $@"%SystemDrive%\sources\setup{isoNum}.exe"
                : $@"%SystemDrive%\setup{isoNum}.exe";

            string winpeshlPath = Path.Combine(workDir, "Windows", "System32", "winpeshl.ini");
            Directory.CreateDirectory(Path.GetDirectoryName(winpeshlPath));

            if (File.Exists(winpeshlPath))
            {
                GrantFullControl(winpeshlPath);
            }

            string[] winpeshlContent =
            {
                "[LaunchApps]",
                @"%SystemRoot%\system32\wpeinit.exe",
                setupCommand
            };

            File.WriteAllLines(winpeshlPath, winpeshlContent);
            mainForm.LogMessage($"    Created winpeshl.ini launching {setupCommand}");

            if (mainForm.DebugMode)
            {
                mainForm.LogMessage($"[DEBUG] {winpeshlPath}:");
                foreach (string line in winpeshlContent)
                {
                    mainForm.LogMessage($"[DEBUG]   {line}");
                }
            }
        }
        
        private void ConfigureBCD(List<ISOEntry> modernEntries)
        {
            if (modernEntries.Count == 0)
                return;
            
            mainForm.LogMessage("  Configuring BCD boot entries...");
            
            string bcdPath = Path.Combine(scriptDirectory, "CDRoot", "boot", "BCD");
            
            if (!File.Exists(bcdPath))
            {
                mainForm.LogMessage("    WARNING: BCD file not found, boot entries not configured");
                return;
            }
            
            try
            {
                // Set bootmgr nointegritychecks
                mainForm.LogMessage("    Setting bootmgr integrity checks...");
                RunCommand("bcdedit.exe", $"/store \"{bcdPath}\" /set {{bootmgr}} nointegritychecks Yes");
                
                // Always show the OS selection menu instead of booting the default image
                mainForm.LogMessage("    Enabling boot manager OS selection menu...");
                RunCommand("bcdedit.exe", $"/store \"{bcdPath}\" /set {{bootmgr}} displaybootmenu Yes");
                RunCommand("bcdedit.exe", $"/store \"{bcdPath}\" /set {{bootmgr}} timeout 30");
                RunCommand("bcdedit.exe", $"/store \"{bcdPath}\" /set {{bootmgr}} default {{default}}");
                
                // Entries that must appear in the boot manager menu
                var displayOrderGuids = new List<string> { "{default}" };

                // Configure first entry (default)
                mainForm.LogMessage($"    Configuring boot entry 1: {modernEntries[0].MenuEntry}");
                RunCommand("bcdedit.exe", $"/store \"{bcdPath}\" /set {{default}} description \"{modernEntries[0].MenuEntry}\"");
                RunCommand("bcdedit.exe", $"/store \"{bcdPath}\" /set {{default}} device ramdisk=[boot]\\sources\\boot1.wim,{{7619dcc8-fafe-11d9-b411-000476eba25f}}");
                RunCommand("bcdedit.exe", $"/store \"{bcdPath}\" /set {{default}} osdevice ramdisk=[boot]\\sources\\boot1.wim,{{7619dcc8-fafe-11d9-b411-000476eba25f}}");
                RunCommand("bcdedit.exe", $"/store \"{bcdPath}\" /set {{default}} nointegritychecks Yes");
                
                // Copy and configure additional entries
                for (int i = 1; i < modernEntries.Count; i++)
                {
                    int entryNum = i + 1;
                    mainForm.LogMessage($"    Configuring boot entry {entryNum}: {modernEntries[i].MenuEntry}");
                    
                    // Copy default entry to create new entry
                    string copyOutput = "";
                    var copyStartInfo = new ProcessStartInfo
                    {
                        FileName = "bcdedit.exe",
                        Arguments = $"/store \"{bcdPath}\" /copy {{default}} /d \"{modernEntries[i].MenuEntry}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    
                    using (var process = Process.Start(copyStartInfo))
                    {
                        copyOutput = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                    }
                    
                    // Extract GUID from output
                    var guidMatch = System.Text.RegularExpressions.Regex.Match(copyOutput, @"\{([0-9a-f-]+)\}");
                    if (guidMatch.Success)
                    {
                        string newGuid = "{" + guidMatch.Groups[1].Value + "}";
                        
                        if (mainForm.DebugMode)
                        {
                            mainForm.LogMessage($"[DEBUG] New entry GUID: {newGuid}");
                        }
                        
                        // Configure the new entry
                        RunCommand("bcdedit.exe", $"/store \"{bcdPath}\" /set {newGuid} description \"{modernEntries[i].MenuEntry}\"");
                        RunCommand("bcdedit.exe", $"/store \"{bcdPath}\" /set {newGuid} device ramdisk=[boot]\\sources\\boot{entryNum}.wim,{{7619dcc8-fafe-11d9-b411-000476eba25f}}");
                        RunCommand("bcdedit.exe", $"/store \"{bcdPath}\" /set {newGuid} osdevice ramdisk=[boot]\\sources\\boot{entryNum}.wim,{{7619dcc8-fafe-11d9-b411-000476eba25f}}");
                        RunCommand("bcdedit.exe", $"/store \"{bcdPath}\" /set {newGuid} nointegritychecks Yes");
                        displayOrderGuids.Add(newGuid);
                    }
                    else
                    {
                        mainForm.LogMessage($"    WARNING: Could not extract GUID for entry {entryNum}");
                    }
                }
                
                // Rebuild displayorder so only our entries are listed. The source media BCD
                // may contain extra entries (for example from a previously built AIO),
                // which would otherwise show up as duplicate or stale menu items.
                mainForm.LogMessage("    Rebuilding boot manager display order...");
                var quotedGuids = displayOrderGuids.Select(g => $"\"{g}\"").ToArray();
                RunCommand("bcdedit.exe",
                    $"/store \"{bcdPath}\" /set {{bootmgr}} displayorder {string.Join(" ", quotedGuids)}");

                mainForm.LogMessage("  BCD configuration completed");
                
                // bcdedit leaves log files behind; keep them out of the ISO
                foreach (string logFile in Directory.GetFiles(Path.GetDirectoryName(bcdPath), "BCD.LOG*"))
                {
                    try { File.Delete(logFile); } catch { }
                }
            }
            catch (Exception ex)
            {
                mainForm.LogMessage($"  ERROR: BCD configuration failed: {ex.Message}");
                if (mainForm.DebugMode)
                {
                    mainForm.LogMessage($"[DEBUG] BCD error details: {ex.ToString()}");
                }
            }
        }
        
        private void UppercaseBootFiles()
        {
            string cdRootPath = Path.Combine(scriptDirectory, "CDRoot");
            int renamedCount = 0;

            // Get all directories in CDRoot
            var directories = Directory.GetDirectories(cdRootPath);

            foreach (var dir in directories)
            {
                string dirName = Path.GetFileName(dir);

                // Check if this is an NT folder (NT01, NT02, N001, etc.)
                if (dirName.StartsWith("NT") || dirName.StartsWith("N0"))
                {
                    renamedCount += UppercaseFile(dir, dirName, "setupldr.bin", "SETUPLDR.BIN");
                    renamedCount += UppercaseFile(dir, dirName, "txtsetup.sif", "TXTSETUP.SIF");
                }
            }

            if (renamedCount > 0)
            {
                mainForm.LogMessage($"  Renamed {renamedCount} NT boot file(s) to uppercase");
            }
            else
            {
                mainForm.LogMessage("  All NT boot files already uppercase");
            }
        }

        private int UppercaseFile(string directory, string dirName, string lowercaseName, string uppercaseName)
        {
            string lowercasePath = Path.Combine(directory, lowercaseName);
            string uppercasePath = Path.Combine(directory, uppercaseName);

            // Check if lowercase or mixed-case version exists
            if (File.Exists(lowercasePath) && lowercasePath != uppercasePath)
            {
                try
                {
                    // Use temp file to handle case-only renames on case-insensitive filesystems
                    string tempPath = lowercasePath + ".tmp";
                    File.Move(lowercasePath, tempPath);
                    File.Move(tempPath, uppercasePath);

                    if (mainForm.DebugMode)
                    {
                        mainForm.LogMessage($"[DEBUG] Renamed {dirName}\\{lowercaseName} to {uppercaseName}");
                    }

                    return 1;
                }
                catch (Exception ex)
                {
                    mainForm.LogMessage($"  WARNING: Failed to rename {dirName}\\{lowercaseName}: {ex.Message}");
                }
            }
            else if (File.Exists(uppercasePath))
            {
                // Already uppercase
                if (mainForm.DebugMode)
                {
                    mainForm.LogMessage($"[DEBUG] {dirName}\\{uppercaseName} already uppercase");
                }
            }

            return 0;
        }
        
        private void CreateISO(int modernCount, int totalCount)
        {
            string cdRootPath = Path.Combine(scriptDirectory, "CDRoot");
            
            // Use configured output path or default
            string outputIso;
            if (!string.IsNullOrEmpty(mainForm.OutputISOPath))
            {
                outputIso = mainForm.OutputISOPath;
            }
            else
            {
                outputIso = Path.Combine(scriptDirectory, "UniversalAIOMaker_Media.iso");
            }
            
            // Ensure directory exists
            string outputDir = Path.GetDirectoryName(outputIso);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            
            mainForm.LogMessage($"  Output ISO: {outputIso}");
            
            // Determine boot-load-size based on whether modern Windows is included
            int bootLoadSize = (modernCount > 0) ? 8 : 4;
            
            mainForm.LogMessage($"  Using xorriso with GRUB4DOS boot loader...");
            mainForm.LogMessage($"  Boot load size: {bootLoadSize} sectors");
            
            // Copy grub4dos folder contents to CDRoot (without the folder itself)
            // BUT DO NOT OVERWRITE menu.lst
            string grub4dosFolder = Path.Combine(scriptDirectory, "grub4dos");
            if (Directory.Exists(grub4dosFolder))
            {
                mainForm.LogMessage("  Copying GRUB4DOS files...");
                CopyDirectoryContentsPreserveMenuLst(grub4dosFolder, cdRootPath);
                
                if (mainForm.DebugMode)
                {
                    mainForm.LogMessage("[DEBUG] Copied grub4dos folder contents (menu.lst preserved)");
                }
            }
            else
            {
                mainForm.LogMessage("  ERROR: grub4dos folder not found next to executable!");
                mainForm.LogMessage($"  Expected location: {grub4dosFolder}");
                return;
            }
            
            // Create GRUB4DOS menu system
            mainForm.LogMessage("  Creating GRUB4DOS menu system...");
            CreateGRUB4DOSMenus(cdRootPath);
            
            // Create ISO with xorriso using simplified CDRoot path
            mainForm.LogMessage("  Creating ISO image (progress window will appear)...");
            
            // Change to script directory to use relative path
            string originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(scriptDirectory);
                
                string xorrisoArgs = $"-as mkisofs -iso-level 3 -full-iso9660-filenames " +
                                    $"-volid LEGACYAIO -eltorito-boot grldr -no-emul-boot " +
                                    $"-boot-load-size {bootLoadSize} -boot-info-table " +
                                    $"-joliet -joliet-long -rock -omit-version-number " +
                                    $"-o \"{outputIso}\" CDRoot";
                
                RunCommand("xorriso.exe", xorrisoArgs, showProgressWindow: true);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
            
            // Verify ISO was created
            if (File.Exists(outputIso))
            {
                FileInfo isoInfo = new FileInfo(outputIso);
                mainForm.LogMessage($"  ISO created successfully: {outputIso}");
                mainForm.LogMessage($"  ISO size: {isoInfo.Length / 1024 / 1024} MB");
            }
            else
            {
                mainForm.LogMessage($"  WARNING: ISO file not found at: {outputIso}");
                mainForm.LogMessage($"  CDRoot folder preserved at: {cdRootPath}");
                mainForm.LogMessage($"  You can create the ISO manually using xorriso");
            }
        }
        
        private void CopyDirectoryContentsPreserveMenuLst(string sourceDir, string destDir)
        {
            // Copy all files from source directory to destination (not the directory itself)
            // BUT DO NOT OVERWRITE menu.lst
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                
                // Skip menu.lst if it already exists in destination
                if (fileName.Equals("menu.lst", StringComparison.OrdinalIgnoreCase))
                {
                    string destFile = Path.Combine(destDir, fileName);
                    if (File.Exists(destFile))
                    {
                        if (mainForm.DebugMode)
                        {
                            mainForm.LogMessage("[DEBUG] Skipping menu.lst from grub4dos folder (preserved generated version)");
                        }
                        continue;
                    }
                }
                
                string dest = Path.Combine(destDir, fileName);
                File.Copy(file, dest, true);
            }
            
            // Copy all subdirectories
            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dir);
                string destSubDir = Path.Combine(destDir, dirName);
                CopyDirectory(dir, destSubDir);
            }
        }
        
        private void CreateGRUB4DOSMenus(string cdRootPath)
        {
            // Copy the optional background image before writing the menus
            PrepareBackgroundImage(cdRootPath);

            // Create .lst files directly in CDRoot (not boot/grub/)
            string menuLst = Path.Combine(cdRootPath, "menu.lst");
            CreateMainMenu(menuLst);
            
            // Create submenus
            if (nt32Entries.Count > 0 || nt64Entries.Count > 0)
            {
                string menuNt = Path.Combine(cdRootPath, "menu_nt.lst");
                CreateNTSubmenu(menuNt);
            }
            
            if (win9xEntries.Count > 0)
            {
                string menu9x = Path.Combine(cdRootPath, "menu_9x.lst");
                Create9xSubmenu(menu9x);
            }
            
            if (modernEntries.Count > 0)
            {
                string menuModern = Path.Combine(cdRootPath, "menu_modern.lst");
                CreateModernSubmenu(menuModern);
            }
            
            mainForm.LogMessage("  GRUB4DOS menus created");
        }
        
        private void PrepareBackgroundImage(string cdRootPath)
        {
            backgroundImageReady = false;

            if (!mainForm.GraphicsMode)
            {
                if (mainForm.UseBackgroundImage)
                {
                    mainForm.LogMessage("  Background image ignored because graphics mode is disabled.");
                }
                return;
            }

            if (!mainForm.UseBackgroundImage)
            {
                return;
            }

            string sourcePath = mainForm.BackgroundImagePath;

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                mainForm.LogMessage("  Background image option enabled, but no image was specified - skipping splash image.");
                return;
            }

            if (!File.Exists(sourcePath))
            {
                mainForm.LogMessage($"  WARNING: Background image not found: {sourcePath} - skipping splash image.");
                return;
            }

            if (!sourcePath.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
            {
                mainForm.LogMessage($"  WARNING: Background image is not a BMP file: {sourcePath} - skipping splash image.");
                return;
            }

            try
            {
                string destination = Path.Combine(cdRootPath, "background.bmp");
                File.Copy(sourcePath, destination, true);
                backgroundImageReady = true;
                mainForm.LogMessage($"  Copied background image to CDRoot\\background.bmp");
            }
            catch (Exception ex)
            {
                mainForm.LogMessage($"  WARNING: Could not copy background image: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes the optional graphics mode and splash image entries at the top of a menu file.
        /// </summary>
        private void WriteGraphicsSettings(StreamWriter writer)
        {
            if (mainForm.GraphicsMode)
            {
                writer.WriteLine("graphicsmode -1 800 600");
            }

            if (backgroundImageReady)
            {
                writer.WriteLine("splashimage background.bmp");
            }

            if (mainForm.GraphicsMode || backgroundImageReady)
            {
                writer.WriteLine();
            }
        }

        private void CreateMainMenu(string menuPath)
        {
            using (var writer = new StreamWriter(menuPath, false, Encoding.UTF8))
            {
                // Timeout and default
                writer.WriteLine("timeout 30");
                writer.WriteLine("default 0");
                writer.WriteLine();

                // Optional graphics mode / splash image
                WriteGraphicsSettings(writer);
                
                // Title entry (displays menu)
                writer.WriteLine("title UniversalAIOMaker - All-in-One Boot Menu\\nMain Menu");
                writer.WriteLine("clear");
                writer.WriteLine("echo -e \"\\n\"");
                writer.WriteLine("echo \"  +----------------------------------------------------------------+\"");
                writer.WriteLine("echo \"  |          UniversalAIOMaker - All-in-One Boot Menu                |\"");
                writer.WriteLine("echo \"  +----------------------------------------------------------------+\"");
                writer.WriteLine("echo -e \"\\n\"");
                
                int optionNum = 1;
                
                // Windows NT option
                if (nt32Entries.Count > 0 || nt64Entries.Count > 0)
                {
                    int totalNT = nt32Entries.Count + nt64Entries.Count;
                    writer.WriteLine($"echo \"           {optionNum} - Windows NT x86/x64 ({totalNT} editions)\"");
                    optionNum++;
                }
                
                // Windows 9x option
                if (win9xEntries.Count > 0)
                {
                    writer.WriteLine($"echo \"           {optionNum} - Windows 9x ({win9xEntries.Count} editions)\"");
                    optionNum++;
                }
                
                // Modern Windows option
                if (modernEntries.Count > 0)
                {
                    writer.WriteLine($"echo \"           {optionNum} - Windows Vista and Later\"");
                    optionNum++;
                }
                
                writer.WriteLine("echo -e \"\\n\"");
                writer.WriteLine("echo \"  ----------------------------------------------------------------\"");
                writer.WriteLine("echo \"  Esc - Boot to first hard drive (0x80) [Default]\"");
                writer.WriteLine("echo -e \"\\n\"");
                writer.WriteLine("echo -n \"  Choose one: \"");
                writer.WriteLine();
                
                // Boot from hard disk entry (default)
                writer.WriteLine("title Boot from first hard disk\\nBoot from (hd0,0)");
                writer.WriteLine("rootnoverify (hd0,0)");
                writer.WriteLine("chainloader +1 || chainloader (hd0,0)+1");
                writer.WriteLine("boot");
                writer.WriteLine();
                
                // Menu options
                optionNum = 1;
                
                if (nt32Entries.Count > 0 || nt64Entries.Count > 0)
                {
                    int totalNT = nt32Entries.Count + nt64Entries.Count;
                    writer.WriteLine($"title {optionNum} - Windows NT x86/x64 ({totalNT} editions)\\nLoad Windows NT submenu");
                    writer.WriteLine("configfile /menu_nt.lst");
                    writer.WriteLine();
                    optionNum++;
                }
                
                if (win9xEntries.Count > 0)
                {
                    writer.WriteLine($"title {optionNum} - Windows 9x ({win9xEntries.Count} editions)\\nLoad Windows 9x submenu");
                    writer.WriteLine("configfile /menu_9x.lst");
                    writer.WriteLine();
                    optionNum++;
                }
                
                if (modernEntries.Count > 0)
                {
                    writer.WriteLine($"title {optionNum} - Windows Vista and Later\\nLoad Windows Vista+ submenu");
                    writer.WriteLine("configfile /menu_modern.lst");
                    writer.WriteLine();
                }
            }
        }
        
        private void CreateNTSubmenu(string menuPath)
        {
            using (var writer = new StreamWriter(menuPath, false, Encoding.UTF8))
            {
                // Combine NT32 and NT64 entries
                var allNTEntries = new List<ISOEntry>();
                allNTEntries.AddRange(nt32Entries);
                allNTEntries.AddRange(nt64Entries);

                // Optional graphics mode / splash image
                WriteGraphicsSettings(writer);
                
                // Title display
                writer.WriteLine("title Windows NT x86/x64\\nNT Submenu");
                writer.WriteLine("clear");
                writer.WriteLine("echo -e \"\\n\"");
                writer.WriteLine("echo \"  +----------------------------------------------------------------+\"");
                writer.WriteLine("echo \"  |  Windows NT x86/x64                                            |\"");
                writer.WriteLine("echo \"  +----------------------------------------------------------------+\"");
                writer.WriteLine("echo -e \"\\n\"");
                
                // List entries
                for (int i = 0; i < allNTEntries.Count; i++)
                {
                    writer.WriteLine($"echo \"           {i + 1} - {allNTEntries[i].MenuEntry}\"");
                }
                
                writer.WriteLine("echo -e \"\\n\"");
                writer.WriteLine("echo \"           B - Back to main menu\"");
                writer.WriteLine("echo -e \"\\n\"");
                writer.WriteLine("echo -n \"  Choose one: \"");
                writer.WriteLine("pause --test-key --non-blocking");
                writer.WriteLine();
                
                // Boot entries - chainload from \NT01\SETUPLDR.BIN
                for (int i = 0; i < allNTEntries.Count; i++)
                {
                    var entry = allNTEntries[i];
                    writer.WriteLine($"title {i + 1} - {entry.MenuEntry}\\nBoot {entry.MenuEntry}");
                    writer.WriteLine($"chainloader /{entry.BootFolder}/SETUPLDR.BIN");
                    writer.WriteLine("boot");
                    writer.WriteLine();
                }
                
                // Back option
                writer.WriteLine("title B - Back to main menu\\nReturn to main menu");
                writer.WriteLine("configfile /menu.lst");
                writer.WriteLine();
            }
        }
        
        private void Create9xSubmenu(string menuPath)
        {
            using (var writer = new StreamWriter(menuPath, false, Encoding.UTF8))
            {
                // Optional graphics mode / splash image
                WriteGraphicsSettings(writer);

                // Title display
                writer.WriteLine("title Windows 9x\\n9x Submenu");
                writer.WriteLine("clear");
                writer.WriteLine("echo -e \"\\n\"");
                writer.WriteLine("echo \"  +----------------------------------------------------------------+\"");
                writer.WriteLine("echo \"  |  Windows 9x                                                    |\"");
                writer.WriteLine("echo \"  +----------------------------------------------------------------+\"");
                writer.WriteLine("echo -e \"\\n\"");
                
                // List entries
                for (int i = 0; i < win9xEntries.Count; i++)
                {
                    writer.WriteLine($"echo \"           {i + 1} - {win9xEntries[i].MenuEntry}\"");
                }
                
                writer.WriteLine("echo -e \"\\n\"");
                writer.WriteLine("echo \"           B - Back to main menu\"");
                writer.WriteLine("echo -e \"\\n\"");
                writer.WriteLine("echo -n \"  Choose one: \"");
                writer.WriteLine("pause --test-key --non-blocking");
                writer.WriteLine();
                
                // Boot entries
                for (int i = 0; i < win9xEntries.Count; i++)
                {
                    var entry = win9xEntries[i];
                    writer.WriteLine($"title {i + 1} - {entry.MenuEntry}\\nBoot {entry.MenuEntry}");
                    writer.WriteLine($"map --mem /{entry.BootFolder}.img (fd0)");
                    writer.WriteLine("map --hook");
                    writer.WriteLine("root (fd0)");
                    writer.WriteLine("chainloader +1");
                    writer.WriteLine("boot");
                    writer.WriteLine();
                }
                
                // Back option
                writer.WriteLine("title B - Back to main menu\\nReturn to main menu");
                writer.WriteLine("configfile /menu.lst");
                writer.WriteLine();
            }
        }
        
        private void CreateModernSubmenu(string menuPath)
        {
            using (var writer = new StreamWriter(menuPath, false, Encoding.UTF8))
            {
                // Optional graphics mode / splash image
                WriteGraphicsSettings(writer);

                // Title display and boot
                writer.WriteLine("title Windows Vista and Later\\nModern Windows Submenu");
                writer.WriteLine("clear");
                writer.WriteLine("echo \"Loading modern Windows...\"");
                writer.WriteLine("chainloader /bootmgr");
                writer.WriteLine("boot");
                writer.WriteLine();
                
                // Back option
                writer.WriteLine("title B - Back to main menu\\nReturn to main menu");
                writer.WriteLine("configfile /menu.lst");
                writer.WriteLine();
            }
        }
        

        
        private void CleanupFiles()
        {
            string cdRootPath = Path.Combine(scriptDirectory, "CDRoot");
            
            // Determine output ISO path
            string outputIso;
            if (!string.IsNullOrEmpty(mainForm.OutputISOPath))
            {
                outputIso = mainForm.OutputISOPath;
            }
            else
            {
                outputIso = Path.Combine(scriptDirectory, "UniversalAIOMaker_Media.iso");
            }
            
            // Only cleanup if ISO was created successfully
            if (File.Exists(outputIso))
            {
                try
                {
                    if (Directory.Exists(cdRootPath))
                    {
                        Directory.Delete(cdRootPath, true);
                        if (mainForm.DebugMode)
                        {
                            mainForm.LogMessage("[DEBUG] Deleted CDRoot folder");
                        }
                    }
                }
                catch (Exception ex)
                {
                    mainForm.LogMessage($"  WARNING: Could not delete CDRoot: {ex.Message}");
                }
            }
            else
            {
                mainForm.LogMessage($"  CDRoot preserved at: {cdRootPath}");
                mainForm.LogMessage($"  ISO creation may have failed - folder kept for manual ISO creation");
            }
        }
        
        private void ProcessTxtSetup(string txtsetupPath, string sourcePath, string destPath, string arch)
        {
            if (!File.Exists(txtsetupPath))
                return;
            
            var content = File.ReadAllText(txtsetupPath);
            var regex = new Regex(@",_[1-9]|,[1-9]_");
            var matches = regex.Matches(content);
            
            var fileNames = new HashSet<string>();
            foreach (Match match in matches)
            {
                var line = GetLineContaining(content, match.Index);
                var parts = line.Split('=');
                if (parts.Length > 0)
                {
                    var fileName = parts[0].Trim();
                    fileNames.Add(fileName);
                }
            }
            
            // Copy files with all extensions, including compressed versions
            foreach (var fileName in fileNames)
            {
                // Try to find files with exact name and any extension
                var sourceFiles = Directory.GetFiles(sourcePath, $"{fileName}.*");
                foreach (var sourceFile in sourceFiles)
                {
                    string destFile = Path.Combine(destPath, Path.GetFileName(sourceFile));
                    try
                    {
                        File.Copy(sourceFile, destFile, true);
                    }
                    catch { }
                }
                
                // Also check for compressed versions
                // Windows Setup uses compression: DLL -> DL_, SYS -> SY_, EXE -> EX_, etc.
                // The pattern is: replace last character with underscore
                if (fileName.Contains('.'))
                {
                    string baseName = Path.GetFileNameWithoutExtension(fileName);
                    string extension = Path.GetExtension(fileName).TrimStart('.');
                    
                    if (extension.Length > 0)
                    {
                        // Create compressed extension (last char becomes _)
                        string compressedExt = extension.Length > 1 
                            ? extension.Substring(0, extension.Length - 1) + "_"
                            : "_";
                        
                        string compressedFileName = $"{baseName}.{compressedExt}";
                        
                        // Look for compressed version
                        var compressedFiles = Directory.GetFiles(sourcePath, compressedFileName);
                        foreach (var compressedFile in compressedFiles)
                        {
                            string destFile = Path.Combine(destPath, Path.GetFileName(compressedFile));
                            try
                            {
                                File.Copy(compressedFile, destFile, true);
                                if (mainForm.DebugMode)
                                {
                                    mainForm.LogMessage($"[DEBUG] Copied compressed file: {Path.GetFileName(compressedFile)}");
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Merges the first [SourceDisksNames.&lt;arch&gt;] and [SourceDisksFiles.&lt;arch&gt;] sections
        /// into the generic [SourceDisksNames] / [SourceDisksFiles] sections.
        /// Text-mode setup resolves disk numbers and files from these generic sections only,
        /// which is why it previously reported that value 1 was not set and could not find
        /// files such as ntkrnlmp.exe.
        /// </summary>
        private void AddSourceDisksEntries(string txtsetupPath, string bootFolder, string arch)
        {
            // Every disk number from the architecture section is required.
            MergeSectionEntries(txtsetupPath, "SourceDisksNames", arch, null);

            // Only add file entries whose file was actually copied into the boot folder.
            MergeSectionEntries(txtsetupPath, "SourceDisksFiles", arch, bootFolder);
        }

        /// <summary>
        /// Inserts the entries of the first [&lt;baseSection&gt;.&lt;arch&gt;] section at the top of
        /// [&lt;baseSection&gt;]. Entries already present (case-insensitive key match) or repeated
        /// inside the source section are skipped. When <paramref name="bootFolder"/> is set,
        /// an entry is added only if the file (or its compressed form) exists in that folder.
        /// </summary>
        private void MergeSectionEntries(string txtsetupPath, string baseSection, string arch, string bootFolder)
        {
            if (!File.Exists(txtsetupPath))
            {
                return;
            }

            string content;
            Encoding encoding;

            // Latin1 preserves any existing single-byte characters byte-for-byte.
            using (var reader = new StreamReader(txtsetupPath, Encoding.Latin1, true))
            {
                content = reader.ReadToEnd();
                encoding = reader.CurrentEncoding;
            }

            string newLine = content.Contains("\r\n") ? "\r\n" : (content.Contains("\n") ? "\n" : "\r");
            string[] lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

            string sourceSection = $"{baseSection}.{arch}";

            // Use the first architecture section only (later ones contain WOW64 files).
            int sourceStart = FindSection(lines, sourceSection, 0);
            if (sourceStart < 0)
            {
                mainForm.LogMessage($"    WARNING: [{sourceSection}] not found in txtsetup.sif");
                return;
            }
            int sourceEnd = FindSectionEnd(lines, sourceStart);

            int destinationStart = FindSection(lines, baseSection, 0);
            if (destinationStart < 0)
            {
                mainForm.LogMessage($"    WARNING: [{baseSection}] not found in txtsetup.sif");
                return;
            }
            int destinationEnd = FindSectionEnd(lines, destinationStart);

            var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = destinationStart + 1; i < destinationEnd; i++)
            {
                string existingKey;
                if (TryGetEntryName(lines[i], out existingKey))
                {
                    existingKeys.Add(existingKey);
                }
            }

            var additions = new List<string>();
            var addedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = sourceStart + 1; i < sourceEnd; i++)
            {
                string key;
                if (!TryGetEntryName(lines[i], out key))
                {
                    continue;
                }

                // Skip entries that already exist or were added more than once.
                if (existingKeys.Contains(key) || !addedKeys.Add(key))
                {
                    continue;
                }

                if (bootFolder != null && !BootFolderContainsFile(bootFolder, key))
                {
                    continue;
                }

                additions.Add(lines[i]);
            }

            if (additions.Count == 0)
            {
                mainForm.LogMessage($"    No missing [{sourceSection}] entries to add to [{baseSection}]");
                return;
            }

            // Insert directly below the section header so the architecture values win.
            var output = new List<string>(lines.Length + additions.Count);
            for (int i = 0; i < lines.Length; i++)
            {
                output.Add(lines[i]);
                if (i == destinationStart)
                {
                    output.AddRange(additions);
                }
            }

            File.WriteAllText(txtsetupPath, string.Join(newLine, output), encoding);
            mainForm.LogMessage($"    Added {additions.Count} [{sourceSection}] entries at the top of [{baseSection}]");

            if (mainForm.DebugMode)
            {
                foreach (string addition in additions)
                {
                    mainForm.LogMessage($"[DEBUG] Added [{baseSection}] entry: {addition.Trim()}");
                }
            }
        }

        private int FindSection(string[] lines, string sectionName, int startIndex)
        {
            string header = "[" + sectionName + "]";

            for (int i = startIndex; i < lines.Length; i++)
            {
                if (lines[i].Trim().Equals(header, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindSectionEnd(string[] lines, int headerIndex)
        {
            for (int i = headerIndex + 1; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("["))
                {
                    return i;
                }
            }

            return lines.Length;
        }

        private bool TryGetEntryName(string line, out string name)
        {
            name = null;

            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith(";"))
            {
                return false;
            }

            int equalsIndex = trimmed.IndexOf('=');
            if (equalsIndex <= 0)
            {
                return false;
            }

            name = trimmed.Substring(0, equalsIndex).Trim();
            return name.Length > 0;
        }

        /// <summary>
        /// Checks for the file itself or its compressed form (example: hal.dll -> HAL.DL_).
        /// </summary>
        private bool BootFolderContainsFile(string bootFolder, string fileName)
        {
            if (File.Exists(Path.Combine(bootFolder, fileName)))
            {
                return true;
            }

            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName).TrimStart('.');
            if (string.IsNullOrEmpty(baseName) || extension.Length == 0)
            {
                return false;
            }

            string compressedExtension = extension.Length > 1
                ? extension.Substring(0, extension.Length - 1) + "_"
                : "_";

            string compressedName = $"{baseName}.{compressedExtension}";
            return File.Exists(Path.Combine(bootFolder, compressedName));
        }

        private string GetLineContaining(string content, int index)
        {
            int start = content.LastIndexOf('\n', index) + 1;
            int end = content.IndexOf('\n', index);
            if (end == -1) end = content.Length;
            return content.Substring(start, end - start).Trim();
        }
        
        private void CopyDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(sourceDir))
                return;
            
            Directory.CreateDirectory(destDir);
            
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }
        
        private void RunCommand(string executable, string arguments, bool suppressOutput = false, bool showProgressWindow = false)
        {
            if (mainForm.DebugMode)
            {
                mainForm.LogMessage($"[DEBUG] Executing: {executable} {arguments}");
            }
            
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = showProgressWindow, // Show window for progress
                RedirectStandardOutput = !showProgressWindow,
                RedirectStandardError = !showProgressWindow,
                CreateNoWindow = !showProgressWindow,
                WindowStyle = showProgressWindow ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden
            };
            
            try
            {
                using (var process = Process.Start(startInfo))
                {
                    if (!showProgressWindow)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        
                        process.WaitForExit();
                        
                        if (mainForm.DebugMode)
                        {
                            mainForm.LogMessage($"[DEBUG] Exit code: {process.ExitCode}");
                            
                            if (!string.IsNullOrWhiteSpace(output) && !suppressOutput)
                            {
                                mainForm.LogMessage($"[DEBUG] Output: {output.Trim()}");
                            }
                            
                            if (!string.IsNullOrWhiteSpace(error))
                            {
                                mainForm.LogMessage($"[DEBUG] Error: {error.Trim()}");
                            }
                        }
                        
                        // Special handling for mkisofs and oscdimg
                        bool isMkisofs = executable.ToLower().Contains("mkisofs");
                        bool isOscdimg = executable.ToLower().Contains("oscdimg");
                        
                        if (isMkisofs || isOscdimg)
                        {
                            // mkisofs returns 1 even on success, and writes progress to stderr
                            // Check if output file was created instead
                            string outputFile = ExtractOutputFilePath(arguments);
                            if (!string.IsNullOrEmpty(outputFile) && File.Exists(outputFile))
                            {
                                // ISO was created successfully, ignore exit code
                                if (mainForm.DebugMode)
                                {
                                    mainForm.LogMessage($"[DEBUG] ISO file created successfully: {outputFile}");
                                }
                                return;
                            }
                        }
                        
                        if (process.ExitCode != 0)
                        {
                            string errorMsg = $"Command failed with exit code {process.ExitCode}: {executable} {arguments}";
                            if (!string.IsNullOrWhiteSpace(error))
                            {
                                errorMsg += $"\nError output: {error}";
                            }
                            mainForm.LogMessage($"ERROR: {errorMsg}");
                            
                            if (!mainForm.DebugMode)
                            {
                                mainForm.LogMessage("TIP: Enable debug mode to see detailed command execution");
                            }
                        }
                    }
                    else
                    {
                        // When showing window, just wait for completion
                        process.WaitForExit();
                        
                        if (mainForm.DebugMode)
                        {
                            mainForm.LogMessage($"[DEBUG] Process completed with exit code: {process.ExitCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                mainForm.LogMessage($"ERROR: Failed to execute {executable}: {ex.Message}");
                if (mainForm.DebugMode)
                {
                    mainForm.LogMessage($"[DEBUG] Exception: {ex.ToString()}");
                }
            }
        }
        
        private string ExtractOutputFilePath(string arguments)
        {
            // Extract output file path from mkisofs/oscdimg arguments
            // mkisofs: -o "path"
            // oscdimg: last parameter before source is output file
            
            var match = System.Text.RegularExpressions.Regex.Match(arguments, @"-o\s+""([^""]+)""");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            
            return null;
        }
        
        private void CallHexEdit(string[] args)
        {
            if (mainForm.DebugMode)
            {
                mainForm.LogMessage($"[DEBUG] Calling HexEdit with args: {string.Join(" ", args)}");
            }
            
            try
            {
                var hexEditType = typeof(HexEdit);
                var hexEditMethod = hexEditType.GetMethod("HexEdit2",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                
                if (hexEditMethod != null)
                {
                    // Capture output if in debug mode
                    var originalOut = Console.Out;
                    var originalErr = Console.Error;
                    
                    if (mainForm.DebugMode)
                    {
                        using (var writer = new StringWriter())
                        {
                            Console.SetOut(writer);
                            Console.SetError(writer);
                            
                            hexEditMethod.Invoke(null, new object[] { args });
                            
                            Console.SetOut(originalOut);
                            Console.SetError(originalErr);
                            
                            string output = writer.ToString();
                            if (!string.IsNullOrWhiteSpace(output))
                            {
                                mainForm.LogMessage($"[DEBUG] HexEdit output: {output.Trim()}");
                            }
                        }
                    }
                    else
                    {
                        // Silent mode
                        using (var writer = new StringWriter())
                        {
                            Console.SetOut(writer);
                            Console.SetError(writer);
                            hexEditMethod.Invoke(null, new object[] { args });
                            Console.SetOut(originalOut);
                            Console.SetError(originalErr);
                        }
                    }
                    
                    if (mainForm.DebugMode)
                    {
                        mainForm.LogMessage("[DEBUG] HexEdit completed successfully");
                    }
                }
                else
                {
                    mainForm.LogMessage("ERROR: Could not find HexEdit.HexEdit2 method");
                }
            }
            catch (Exception ex)
            {
                mainForm.LogMessage($"ERROR: HexEdit failed: {ex.Message}");
                if (mainForm.DebugMode)
                {
                    mainForm.LogMessage($"[DEBUG] HexEdit exception: {ex.ToString()}");
                }
            }
        }
        
        private void CallInfFileEditor(string[] args)
        {
            if (mainForm.DebugMode)
            {
                mainForm.LogMessage($"[DEBUG] Calling InfFileEditor with args: {string.Join(" ", args)}");
            }
            
            try
            {
                var infEditorType = typeof(InfFileEditor);
                var infEditMethod = infEditorType.GetMethod("InfEdit",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                
                if (infEditMethod != null)
                {
                    var originalOut = Console.Out;
                    var originalErr = Console.Error;
                    
                    if (mainForm.DebugMode)
                    {
                        using (var writer = new StringWriter())
                        {
                            Console.SetOut(writer);
                            Console.SetError(writer);
                            
                            var result = infEditMethod.Invoke(null, new object[] { args });
                            
                            Console.SetOut(originalOut);
                            Console.SetError(originalErr);
                            
                            string output = writer.ToString();
                            if (!string.IsNullOrWhiteSpace(output))
                            {
                                mainForm.LogMessage($"[DEBUG] InfFileEditor output: {output.Trim()}");
                            }
                            
                            if (result != null)
                            {
                                mainForm.LogMessage($"[DEBUG] InfFileEditor exit code: {result}");
                            }
                        }
                    }
                    else
                    {
                        using (var writer = new StringWriter())
                        {
                            Console.SetOut(writer);
                            Console.SetError(writer);
                            infEditMethod.Invoke(null, new object[] { args });
                            Console.SetOut(originalOut);
                            Console.SetError(originalErr);
                        }
                    }
                    
                    if (mainForm.DebugMode)
                    {
                        mainForm.LogMessage("[DEBUG] InfFileEditor completed");
                    }
                }
                else
                {
                    mainForm.LogMessage("ERROR: Could not find InfFileEditor.InfEdit method");
                }
            }
            catch (Exception ex)
            {
                mainForm.LogMessage($"ERROR: InfFileEditor failed: {ex.Message}");
                if (mainForm.DebugMode)
                {
                    mainForm.LogMessage($"[DEBUG] InfFileEditor exception: {ex.ToString()}");
                }
            }
        }
        
        /// <summary>
        /// Patches setupldr.bin at offset 0x2060: replaces bytes 74 03 with EB 1A.
        /// </summary>
        private void PatchSetupldrOffset(string setupldrPath)
        {
            if (!File.Exists(setupldrPath))
            {
                mainForm.LogMessage($"    WARNING: setupldr.bin not found at {setupldrPath}, skipping offset patch.");
                return;
            }

            try
            {
                byte[] fileBytes = File.ReadAllBytes(setupldrPath);
                const int offset = 0x2060;

                if (fileBytes.Length < offset + 2)
                {
                    mainForm.LogMessage($"    WARNING: setupldr.bin too small for offset 0x2060 patch.");
                    return;
                }

                // Check if bytes at offset match 74 03
                if (fileBytes[offset] == 0x74 && fileBytes[offset + 1] == 0x03)
                {
                    fileBytes[offset] = 0xEB;
                    fileBytes[offset + 1] = 0x1A;
                    File.WriteAllBytes(setupldrPath, fileBytes);
                    mainForm.LogMessage($"    Patched setupldr.bin at offset 0x2060: 74 03 -> EB 1A");
                }
                else
                {
                    mainForm.LogMessage($"    Offset 0x2060 does not contain 74 03 (found {fileBytes[offset]:X2} {fileBytes[offset + 1]:X2}), skipping patch.");
                }
            }
            catch (Exception ex)
            {
                mainForm.LogMessage($"    ERROR: Failed to patch setupldr.bin at offset 0x2060: {ex.Message}");
            }
        }

        private void CallBOMRemover(string[] args)
        {
            if (mainForm.DebugMode)
            {
                mainForm.LogMessage($"[DEBUG] Calling BOMRemover with args: {string.Join(" ", args)}");
            }
            
            try
            {
                var bomRemoverType = typeof(BOMRemover);
                var bomRemoveMethod = bomRemoverType.GetMethod("BOMRemove",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                
                if (bomRemoveMethod != null)
                {
                    var originalOut = Console.Out;
                    var originalErr = Console.Error;
                    
                    if (mainForm.DebugMode)
                    {
                        using (var writer = new StringWriter())
                        {
                            Console.SetOut(writer);
                            Console.SetError(writer);
                            
                            bomRemoveMethod.Invoke(null, new object[] { args });
                            
                            Console.SetOut(originalOut);
                            Console.SetError(originalErr);
                            
                            string output = writer.ToString();
                            if (!string.IsNullOrWhiteSpace(output))
                            {
                                mainForm.LogMessage($"[DEBUG] BOMRemover output: {output.Trim()}");
                            }
                        }
                    }
                    else
                    {
                        using (var writer = new StringWriter())
                        {
                            Console.SetOut(writer);
                            Console.SetError(writer);
                            bomRemoveMethod.Invoke(null, new object[] { args });
                            Console.SetOut(originalOut);
                            Console.SetError(originalErr);
                        }
                    }
                    
                    if (mainForm.DebugMode)
                    {
                        mainForm.LogMessage("[DEBUG] BOMRemover completed");
                    }
                }
                else
                {
                    mainForm.LogMessage("ERROR: Could not find BOMRemover.BOMRemove method");
                }
            }
            catch (Exception ex)
            {
                mainForm.LogMessage($"ERROR: BOMRemover failed: {ex.Message}");
                if (mainForm.DebugMode)
                {
                    mainForm.LogMessage($"[DEBUG] BOMRemover exception: {ex.ToString()}");
                }
            }
        }
    }
}

