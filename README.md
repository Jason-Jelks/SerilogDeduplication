# Serilog Deduplication Filter

**Serilog Deduplication Filter** is a reusable log filtering component for Serilog that prevents duplicate log entries from being logged within a configurable time window. It is designed to work with any Serilog sink (MongoDB, File, Console, etc.) and supports both **.NET 6** and **.NET 8**.

## Features

- **Deduplication**: Prevents repeated log entries based on customizable criteria (e.g., time window, specific log properties).
- **Severity-Based Deduplication**: Allows different deduplication windows for different log levels (e.g., Error, Warning, Information).
- **Configurable**: Deduplication time window, deduplication behavior, and deduplication key components are fully configurable.
- **Dynamic Key Generation**: Deduplication key can include specific log properties and/or the message template.
- **Sink Agnostic**: Works with any Serilog sink (MongoDB, File, Console, etc.).
- **Multi-targeted**: Supports both **.NET 6** and **.NET 8** applications.

---

## **How It Works**

This filter tracks log entries based on a configurable key (e.g., a combination of properties like `Code`, `Source`, and `Message`) and prevents duplicate logs within a defined time window (in milliseconds). It also supports **severity-based deduplication** for better control.

- **Deduplication Key**: Logs are considered duplicates if they share the same key (e.g., `Code`, `Process`, and `Message`) and occur within the specified time window.
- **Configurable Window**: You can define different deduplication windows (in milliseconds) for each log level (e.g., Error, Warning, Information).

---

## **Installation**

To use this filter in your project, install the NuGet package (once published) or reference the project manually.

### **Manual Installation**

1. Clone the repository or download the source code.
2. Add the **DeduplicationFilter** project to your solution.
3. Reference the project in your main application.

---

## **Usage**

### **1. Setup the Filter in Your Project**

Add the **DeduplicationFilter** to your Serilog configuration. The following code shows how to configure it with any Serilog sink.

```
using Serilog;
using DeduplicationFilter;

var deduplicationSettings = new DeduplicationSettings
{
    Error = new DeduplicationLevel { DeduplicationEnabled = true, DeduplicationWindowMilliseconds = 10000 },
    Warning = new DeduplicationLevel { DeduplicationEnabled = true, DeduplicationWindowMilliseconds = 5000 },
    Information = new DeduplicationLevel { DeduplicationEnabled = true, DeduplicationWindowMilliseconds = 3000 }
};

Log.Logger = new LoggerConfiguration()
    .Filter.With(new DeduplicationFilter(deduplicationSettings))
    .WriteTo.Console()  // Example sink: Console
    .CreateLogger();
```

### **2. Configure Severity-Based Deduplication in `appsettings.json`**

You can also configure different deduplication windows for each log level (e.g., Error, Warning, Information) in `appsettings.json`:

```
{
  "Logging": {
    "Deduplication": {
      "Error": {
        "DeduplicationEnabled": false,    // No deduplication for errors
        "DeduplicationWindowMilliseconds": 10000
      },
      "Warning": {
        "DeduplicationEnabled": true,     // Deduplication enabled for warnings
        "DeduplicationWindowMilliseconds": 5000
      },
      "Information": {
        "DeduplicationEnabled": true,     // Deduplication enabled for information
        "DeduplicationWindowMilliseconds": 3000
      },
      "Debug": {
        "DeduplicationEnabled": true,     // Deduplication enabled for debug logs
        "DeduplicationWindowMilliseconds": 1000
      }
    }
  }
}
```

Then read the configuration in your startup file:

```
var deduplicationSettings = DeduplicationSettings.LoadFromConfiguration(builder.Configuration);

Log.Logger = new LoggerConfiguration()
    .Filter.With(new DeduplicationFilter(deduplicationSettings))
    .WriteTo.Console()  // Use your preferred sink
    .CreateLogger();
```

### **3. Adding Deduplication via IServiceCollection**

To simplify the setup, you can now use the `AddDeduplication` extension method in `IServiceCollection`. This method registers the deduplication filter and settings directly from configuration.

```
using Serilog.Deduplication.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add deduplication from configuration
builder.Services.AddDeduplication(builder.Configuration);

var deduplicationSettings = DeduplicationSettings.LoadFromConfiguration(builder.Configuration);

Log.Logger = new LoggerConfiguration()
    .Filter.With(new DeduplicationFilter(deduplicationSettings))
    .WriteTo.Console()  // Use your preferred sink
    .CreateLogger();
```

### **4. Customizing the Deduplication Key**

You can now set deduplication key components in `appsettings.json` to allow flexible control over which properties are used for deduplication:

```
{
  "Logging": {
    "Deduplication": {
      "KeyProperties": ["Code", "Process", "DeviceName"],
      "IncludeMessageTemplate": true
    }
  }
}
```

In this configuration, the deduplication key will include `Code`, `Process`, `DeviceName`, and the `MessageTemplate`. If `IncludeMessageTemplate` is set to `false`, the message template will not be part of the deduplication key.

--- 

### **5. Testing the Deduplication Filter**

After configuring, you can test the deduplication logic to ensure duplicate log entries are filtered out.

#### **Steps for Testing:**

1. **Trigger Duplicate Logs**:
   - Generate logs with the same deduplication key (e.g., logs with the same `Code`, `Process`, and `Message`) within the configured deduplication window.

2. **Check Log Output**:
   - Verify that only the first log is written and that duplicate logs within the deduplication window are filtered out.

#### **Example Scenario:**

If your deduplication window is set to 5000 milliseconds, and the following logs are triggered:

- Log at `10:00:00` → This log should be recorded.
- Log at `10:00:03` (same key as previous) → This log should be filtered out.
- Log at `10:00:07` (same key as previous) → This log should be recorded, as it falls outside the 5-second window.

---

### **6. Performance Considerations**

#### **1. Cache Size and Memory Usage**

The deduplication filter relies on an in-memory cache to track log entries and their timestamps. This cache grows as more unique logs are generated, so it’s important to monitor memory usage in environments with high log volume.

- **Pruning Feature**: The deduplication filter includes a configurable pruning feature. You can configure the `PruneIntervalMilliseconds` (how often the cache is checked) and the `CacheExpirationMilliseconds` (how long entries remain in the cache) in `appsettings.json`.

```
{
  "Logging": {
    "Deduplication": {
      "PruneIntervalMilliseconds": 60000,  // Prune the cache every 60 seconds
      "CacheExpirationMilliseconds": 300000  // Remove entries older than 5 minutes
    }
  }
}
```

The cache will automatically remove entries older than the configured expiration time. Adjust these values based on your log volume and system requirements.

#### **2. Thread Safety**

The deduplication filter uses thread-safe collections (like `ConcurrentDictionary`) to ensure multiple threads can safely access the cache. This is essential in multi-threaded environments like web applications or services.

- **Solution**:
  - Use `ConcurrentDictionary` to ensure thread safety across all logging operations without manual locking.
  
#### **3. Log Frequency**

If your system generates logs at a high frequency, configuring appropriate deduplication windows can help reduce I/O overhead. Increasing the deduplication window for frequent log types (e.g., debug logs) can further reduce noise and improve performance.

---

### **7. License**

This project is licensed under the **GNU General Public License v3.0**.

You are free to:
- **Use**: The software can be used for any purpose.
- **Modify**: You can modify the software to meet your needs.
- **Distribute**: You are allowed to distribute the original or modified versions of this software.
- **Contribute**: You can contribute back to the project by submitting improvements or bug fixes.

However, any distributed copies or modifications of this software must:
- Be licensed under **GNU GPL v3.0**.
- Include the original license text.
- Include the source code or a way to access it, if you distribute a modified version.

For more details, please refer to the [LICENSE](./LICENSE) file. 
