# Python Runtime Auto-Detection

JellyPy now includes intelligent runtime auto-detection that works seamlessly in Docker containers and Unraid environments.

## 🚀 **How It Works**

The plugin automatically detects the best available Python interpreter on your system without any manual configuration required.

### **Detection Order (Python Example)**

1. **Bundled Runtime** (future feature)
   - `{plugin_dir}/runtime/bin/python3`
   - `{plugin_dir}/runtime/bin/python`

2. **Common Docker Locations**
   - `/usr/bin/python3`
   - `/usr/bin/python`
   - `/usr/local/bin/python3`
   - `/usr/local/bin/python`

3. **Alpine Linux** (common in Docker)
   - `/usr/bin/python3.12`
   - `/usr/bin/python3.11`
   - `/usr/bin/python3.10`
   - `/usr/bin/python3.9`

4. **System PATH**
   - `python3`
   - `python`

## 🐳 **Docker/Unraid Benefits**

✅ **Zero Configuration** - Works out of the box in most containers  
✅ **Container-Aware** - Checks Docker-specific paths first  
✅ **Diagnostic Logging** - Detailed logs help with troubleshooting  
✅ **Multiple Fallbacks** - Won't fail if one interpreter is missing  
✅ **Multi-Executor Support** - Works with Python, PowerShell, Bash, Node.js  

## 📋 **User Experience**

### **For Most Users (Docker/Unraid)**
1. Install JellyPy plugin through Jellyfin ✅
2. Configure scripts - ExecutablePath auto-detects ✅
3. Scripts work immediately - no manual setup required ✅

### **For Advanced Users**
- **Override Auto-Detection**: Set ExecutablePath to specific path
- **Use "auto" Value**: Explicitly request auto-detection
- **Troubleshooting**: Check Jellyfin logs for detection details

## 🔧 **Configuration Examples**

### **Auto-Detection (Recommended)**
```json
{
  "Execution": {
    "ExecutorType": "Python",
    "ExecutablePath": "/usr/bin/python3",  // Will auto-detect if default
    "ScriptPath": "/config/scripts/notify.py"
  }
}
```

### **Explicit Auto-Detection**
```json
{
  "Execution": {
    "ExecutorType": "Python", 
    "ExecutablePath": "auto",  // Forces auto-detection
    "ScriptPath": "/config/scripts/notify.py"
  }
}
```

### **Manual Override**
```json
{
  "Execution": {
    "ExecutorType": "Python",
    "ExecutablePath": "/opt/python/bin/python3",  // Custom path
    "ScriptPath": "/config/scripts/notify.py"
  }
}
```

## 🐛 **Troubleshooting**

If script execution fails, check Jellyfin logs for diagnostic information:

```
[INF] Found Python executable: /usr/bin/python3
```

If auto-detection fails, you'll see detailed diagnostics:
```
[WRN] Python auto-detection failed. Diagnostic information:
[INF] Operating System: Linux 5.4.0-74-generic #83-Ubuntu
[INF] Architecture: X64
[INF] Python-like files in /usr/bin: python3, python3.9
[INF] PATH contains 12 directories
```

## 🌟 **Supported Executors**

- **Python**: `python3`, `python`, version-specific variants
- **PowerShell**: `pwsh`, `powershell`  
- **Bash**: `/bin/bash`, `/usr/bin/bash`, `bash`, `sh`
- **Node.js**: `node`, `/usr/bin/node`, `nodejs`
- **Binary**: Direct executable execution (no interpreter needed)

The auto-detection system makes JellyPy much more user-friendly, especially in containerized environments where users don't have shell access to configure paths manually!