# XISF Renamer  
A simple tool to rename `XISF` files from [PixInsight](https://pixinsight.com/) using customizable patterns, inspired by [N.I.N.A](https://nighttime-imaging.eu/) and Total Commander.

---

### **Motivation**  

I had a PixInsight processing folder filled with master files (darks, flats, biases, lights). While keeping them in one folder was convenient, the filenames (like `masterLight_BIN-1_6024x4024_EXPOSURE-121.00s_FILTER-NoFilter_RGB_autocrop.xisf`) made it impossible to tell which target they belonged to (Andromeda, Orion, etc.), their acquisition date, or other details.

This tool lets you rename files using metadata patterns like target name, acquisition date, location, or sensor temperature. If the metadata exists in the file, you can use it for renaming — theoretically, anything that fits the keyword system works (e.g., `M31_2023-09-01_FLAT_5C_120s.xisf`).

---

### **Features**  
- 🎯 **Keyword-based patterns**: Use metadata like target name, date, filter, exposure time, etc.  
- 📝 **Pattern syntax**:  
  - `[N]`: Original filename.  
  - `[N2-4]`: Characters 2–4 from the original filename.  
  - `$$KEYWORD$$`: Insert metadata (e.g., `$$TARGET$$`, `$$DATETIME$$`).  
- 👁️ **Live preview**: See renamed filenames before applying changes.  
- 🛡️ **Safe renaming**: Automatically appends `_1`, `_2`, etc., to avoid overwriting files.  
- ✅ **Batch operations**: Select multiple files and rename them in one click.  

---

### **Usage**  
1. **Select a folder**: Click `Browse` to load XISF files.  
2. **Build your pattern**:  
   - Double-click keywords from the right panel (e.g., `$$TARGET$$`, `$$FILTER$$`).  
   - Use `[N]` to include parts of the original filename (e.g., `[N2-4]` for characters 2–4).  
3. **Preview**: Check the `Renamed` column to see how files will be renamed.  
4. **Select files**: Toggle checkboxes with:  
   - **Double-click** a row.  
   - **Spacebar** to toggle selected rows.  
5. **Rename**: Click `Rename Checked` to apply changes.  

![Screenshot](https://github.com/user-attachments/assets/1c359226-ce62-44dd-80c4-57d615dbd1a1)  

---

### **Installation**  
1. **Download**: Get the latest release [here](https://github.com/naixx/XisfRenamer/releases).  
2. **Run**: No installation needed – just launch `XisfRenamer.exe`.  

---

### **Notes**  
- **Metadata**: Ensure your XISF files contain metadata (most PixInsight processes auto-add this).  
- **Backups**: Always back up files before bulk renaming.  

---

### **Example Patterns**  
| Pattern | Result |  
|---------|--------|  
| `$$TARGET$$_$$DATETIME$$` | `M31_2023-09-01.xisf` |  
| `[N2-4]_$$FILTER$$` | `ter_LRGB.xisf` (if original name was `master.xisf`) |  
| `$$TARGET$$_$$FILTER$$_$$EXPOSURETIME$$s_$$FRAMENR$$` | `M31_L_120s_001.xisf` |  
