# User-Defined Commands

Create interactive commands with dynamic item sources and custom execution templates.

## How It Works

1. **Type prefix** (e.g., `/docs`) → 2. **List appears** → 3. **Select item** → 4. **Execute template**

## Configuration Options

### Basic Settings

- **Prefix:** The prefix that activates the command (e.g., `/docs`, `/scripts`)
- **Source:** Where items come from:
  - **Directory** - List files from a folder (glob + recursive)
  - **Static** - Predefined list of strings

### Directory Source Options

- **Path:** Full directory path (e.g., `C:\Users\YourName\Documents\Projects`)
- **Glob Pattern:** Filter files (e.g., `*.md`, `*.py`, `*.*`)
- **Recursive:** Include subdirectories (Yes/No)

### Execute Template

The command to run when you select an item. Use placeholders:

- `{item.path}` - Full file path
- `{item.name}` - File name without extension
- `{item.extension}` - File extension (e.g., `.md`)
- `{query}` - The search query text (URL-encoded for web URLs)

### Terminal Option

- **Yes** - Run command in a visible terminal window
- **No** - Run silently in background

## Stored Format (commands.json)

Commands are stored in `%APPDATA%\\QuickLink\\commands.json`:

```json
[
  {
    "Prefix": "/docs",
    "Source": "Directory",
    "SourceConfig": {
      "Path": "C:\\Docs",
      "Recursive": true,
      "Glob": "*.md",
      "Items": []
    },
    "ExecuteTemplate": "code \"{item.path}\"",
    "Icon": "Document",
    "OpenInTerminal": false
  }
]
```

## Real-World Examples

### 📁 Browse Documentation Files

**Settings:**
- **Trigger:** `/docs`
- **Source:** Directory
- **Path:** `C:\Users\YourName\Documents\Documentation`
- **Glob Pattern:** `*.md`
- **Recursive:** Yes
- **Execute:** `code "{item.path}"`
- **Terminal:** No

**Usage:**
```
Type: /docs blog
Results: blog-setup.md, blog-workflow.md
Press Enter to open selected file in VS Code
```

### ⚙️ Run PowerShell Scripts

**Settings:**
- **Trigger:** `/scripts`
- **Source:** Directory
- **Path:** `C:\Scripts`
- **Glob Pattern:** `*.ps1`
- **Recursive:** Yes
- **Execute:** `powershell -ExecutionPolicy Bypass -File "{item.path}"`
- **Terminal:** Yes

**Usage:**
```
Type: /scripts backup
Results: backup-database.ps1, backup-files.ps1
Press Enter to execute the selected script in a terminal
```

### 📂 Open Project Folders

**Settings:**
- **Trigger:** `/projects`
- **Source:** Directory
- **Path:** `C:\Dev\Projects`
- **Glob Pattern:** `*.*`
- **Recursive:** No (only top-level folders)
- **Execute:** `explorer "{item.path}"`
- **Terminal:** No

**Usage:**
```
Type: /projects my-app
Results: my-app-react, my-app-backend
Press Enter to open in File Explorer
```

### 🌐 Search GitHub

**Purpose:** Search GitHub repositories from QuickLink

**Settings:**
- **Trigger:** `/gh`
- **Source:** Static
- **Items:** (Predefined list)
- **Execute:** `https://github.com/search?q={query}&type=repositories`
- **Terminal:** No

**Usage:**
```
Type: /gh quicklink
Opens: https://github.com/search?q=quicklink&type=repositories
```

### 🎨 Open Design Files in Figma

**Purpose:** Quick access to design files in Figma

**Settings:**
- **Trigger:** `/design`
- **Source:** Static
- **Items:**
  - `Landing Page`
  - `Dashboard UI`
  - `Mobile App`
- **Execute:** `https://figma.com/file/YOUR-FILE-ID?search={query}`
- **Terminal:** No

**Usage:**
```
Type: /design landing
Results: Landing Page
Press Enter to open in Figma
```

### 🐍 Run Python Scripts

**Settings:**
- **Trigger:** `/py`
- **Source:** Directory
- **Path:** `C:\Scripts\Python`
- **Glob Pattern:** `*.py`
- **Recursive:** Yes
- **Execute:** `python "{item.path}"`
- **Terminal:** Yes

**Usage:**
```
Type: /py convert
Results: convert-images.py, convert-pdf.py
Press Enter to run the selected script
```

### 📋 Quick Access to Tools

**Purpose:** Launch favorite tools or applications

**Settings:**
- **Trigger:** `/tools`
- **Source:** Static
- **Items:**
  - `Visual Studio Code`
  - `Notepad++`
  - `VS Code`
- **Execute:** `{query}`
- **Terminal:** No

**Usage:**
```
Type: /tools code
Results: Visual Studio Code
Press Enter to launch
```

## Performance Notes

- **Lazy Loading:** Commands are only processed when you type the trigger prefix
- **Caching:** Directory listings are cached for performance
- **HTTP Endpoints:** Results are fetched on-demand for efficiency

## Tips & Tricks

1. **Use consistent naming** - Make file names searchable (e.g., `backup-db.ps1`, `backup-files.ps1`)
2. **Combine prefixes** - Create multiple commands for different tasks (`/scripts`, `/docs`, `/projects`)
3. **Test templates** - Verify your execute template works before saving
4. **Use batch files** - Wrap complex commands in `.bat` or `.ps1` files for reusability
5. **HTTP sources** - Return JSON arrays for maximum flexibility

## Troubleshooting

**Items don't appear:**
- Check that the trigger prefix is correct
- Verify the directory path exists
- Ensure glob pattern matches your files

**Command fails to execute:**
- Test the command in PowerShell directly
- Check file paths (use quotes for paths with spaces)
- Verify permissions for the executing account

**Changes don't take effect:**
- Settings are saved automatically
- Restart QuickLink if needed
