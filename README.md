# Serilog Deduplication Filter

**Serilog Deduplication Filter** is a reusable log filtering component for Serilog that prevents duplicate log entries from being logged within a configurable time window. It is designed to work with any Serilog sink (MongoDB, File, Console, etc.) and supports both **.NET 6** and **.NET 8**.

## Features

- **Deduplication**: Prevents repeated log entries based on customizable criteria (e.g., time window, specific log properties).
- **Configurable**: Deduplication time window is easily configurable.
- **Sink Agnostic**: Works with any Serilog sink (MongoDB, File, Console, etc.).
- **Multi-targeted**: Supports both **.NET 6** and **.NET 8** applications.

---

## **How It Works**

This filter tracks log entries based on a configurable key (e.g., a combination of `Code`, `Process`, and `Message`) and prevents duplicate logs within a defined time window (in milliseconds).

- **Deduplication Key**: Logs are considered duplicates if they share the same key (e.g., `Code`, `Process`, and `Message`) and occur within the specified time window.
- **Configurable Window**: You can define the deduplication window (in milliseconds) during which repeated log entries will be skipped.

---

## **Installation**

To use this filter in your project, install the NuGet package (once published), or reference the project manually.

### **Manual Installation**

1. Clone the repository or download the source code.
2. Add the **DeduplicationFilter** project to your solution.
3. Reference the project in your main application.

---

## **Usage**

### **1. Setup the Filter in Your Project**

Add the **DeduplicationFilter** to your Serilog configuration. The following code shows how to configure it with any Serilog sink.

```csharp
using Serilog;
using DeduplicationFilter;

var deduplicationWindowMs = 5000;  // 5 seconds deduplication window

Log.Logger = new LoggerConfiguration()
    .Filter.With(new DeduplicationFilter(deduplicationWindowMs))  // Apply deduplication filter
    .WriteTo.Console()  // Example sink: Console
    .CreateLogger();
```

## **Configuration** 

### **2. Configure Deduplication in `appsettings.json`**

You can also configure the deduplication window in `appsettings.json`:

```json
{
  "Logging": {
    "DeduplicationWindowMilliseconds": 5000
  }
}
```

Then read the configuration in your startup file:

```csharp

var deduplicationWindowMs = builder.Configuration.GetValue<int>("Logging:DeduplicationWindowMilliseconds");

Log.Logger = new LoggerConfiguration()
    .Filter.With(new DeduplicationFilter(deduplicationWindowMs))
    .WriteTo.Console()  // Use your preferred sink
    .CreateLogger();

```

### **3. Build and Use the Deduplication Filter**

After configuring the deduplication filter in `appsettings.json`, build your project and start using the deduplication logic in your logs.

1. Ensure that **DeduplicationFilter** is referenced in your solution and the filter is configured correctly.
2. After configuring Serilog in your startup code with the deduplication window, logs that occur within the specified time window with the same deduplication key will be filtered out, preventing duplicate entries from cluttering your log storage.

For example, if your deduplication window is set to 5000 milliseconds (5 seconds), and two logs with the same key (`Code`, `Process`, `Message`) are triggered within this timeframe, only the first log will be stored, and the duplicate will be skipped.

---

### **Customizing the Deduplication Key**

You can modify the logic in the `DeduplicationFilter` to use different fields as part of the deduplication key. By default, the key is based on a combination of `Code`, `Process`, and `Message`. However, you can include other properties like `Source` or `Category` depending on your logging needs.

---

Once set up, your logs will be streamlined, reducing redundant entries and improving the clarity of your log data.

### **4. Testing the Deduplication Filter**

Once the deduplication filter is set up and your application is running, you can test it to ensure that duplicate log entries are properly filtered out.

#### **Steps for Testing:**

1. **Trigger Duplicate Logs**:
   - Generate logs that have the same deduplication key (e.g., logs with the same `Code`, `Process`, and `Message`) within the configured deduplication window.
   
   Example:
   - Log an informational message with the same content multiple times within a few seconds.

2. **Check Log Output**:
   - Verify that only the first log is written and subsequent logs within the deduplication window are filtered out. You can check your log storage (e.g., file, console, MongoDB) to ensure duplicates are not being recorded.

#### **Example Scenario:**

If your deduplication window is set to 5000 milliseconds, and the following logs are triggered:

- Log at `10:00:00` → This log should be recorded.
- Log at `10:00:03` (same key as previous) → This log should be filtered out.
- Log at `10:00:07` (same key as previous) → This log should be recorded, as it falls outside the 5-second window.

#### **Adjust the Deduplication Window**:

If the deduplication behavior is not as expected, ensure that the `DeduplicationWindowMilliseconds` in your configuration file is set appropriately. You can modify this value based on the log frequency and your desired behavior.

Testing with different values (e.g., 1000ms, 5000ms, 10000ms) can help you find the right balance between logging important events and reducing redundant log entries.

By carefully testing the deduplication filter, you can confirm that it works as expected, efficiently reducing noise in your logs without missing critical information.

### **5. Extending the Deduplication Filter**

The **Deduplication Filter** is designed to be flexible and customizable. You can extend or modify it to better fit your application's needs.

#### **Ways to Extend the Filter:**

1. **Custom Deduplication Keys**:
   - By default, the deduplication key is composed of the log's `Code`, `Process`, and `Message`. However, you can modify the `DeduplicationFilter` to use additional properties like `Source`, `Category`, or even custom fields in your logs.
   
   Example:
   - Add `Source` to the deduplication key to differentiate between logs generated from different parts of the application:
   
   ```csharp
   var logKey = $"{logEvent.Properties["Source"]}-{logEvent.Properties["Code"]}-{logEvent.MessageTemplate.Text}";

#### **Dynamic Deduplication Window**

Instead of using a static deduplication window for all logs, you could introduce logic to set the deduplication window dynamically based on log severity or other criteria. For example, you could apply a shorter window for `Information` logs and a longer window for `Error` logs.

##### **Example:**

You can configure different deduplication windows in `appsettings.json` for each log level:

```json
{
  "Logging": {
    "InformationDeduplicationWindowMilliseconds": 2000,
    "ErrorDeduplicationWindowMilliseconds": 10000
  }
}
```

#### **Conditional Deduplication**

You may want to apply deduplication selectively, depending on certain conditions (e.g., only deduplicating logs from specific modules or during specific time periods). You can extend the deduplication filter to apply deduplication based on these conditions.

##### **Example:**

Suppose you only want to deduplicate logs that come from a specific module, such as a `Database` module, while leaving logs from other modules untouched. You can modify the filter to apply deduplication conditionally:

```csharp
public bool IsEnabled(LogEvent logEvent)
{
    // Only deduplicate logs from the 'Database' module
    if (logEvent.Properties.TryGetValue("Module", out var module) && module.ToString() == "Database")
    {
        var deduplicationWindowMs = _configuration.GetValue<int>("Logging:DeduplicationWindowMilliseconds");
        var logKey = $"{logEvent.Properties["Code"]}-{logEvent.MessageTemplate.Text}";

        if (_logCache.TryGetValue(logKey, out var lastLoggedTime))
        {
            var timeSinceLastLog = DateTime.UtcNow - lastLoggedTime;

            if (timeSinceLastLog.TotalMilliseconds < deduplicationWindowMs)
            {
                return false;  // Skip duplicate log for this module
            }
        }

        _logCache[logKey] = DateTime.UtcNow;
    }

    return true;  // Log the entry for other modules
}
```

### **6. Troubleshooting and FAQ**

Even with a well-configured deduplication filter, you may encounter issues or have questions during implementation. Here are some common troubleshooting steps and frequently asked questions.

#### **Common Issues:**

1. **Logs Are Not Being Deduplicated**:
   - **Cause**: The deduplication key might not be correctly configured, or the deduplication window may be too short to filter out duplicates.
   - **Solution**: 
     - Ensure that the deduplication key includes all relevant log properties (e.g., `Code`, `Process`, `Message`).
     - Verify that the deduplication window in `appsettings.json` is long enough to capture duplicate logs.

2. **Too Many Logs Are Being Filtered**:
   - **Cause**: The deduplication window might be too long, or the deduplication key may be too broad, causing logs that should be distinct to be considered duplicates.
   - **Solution**:
     - Reduce the deduplication window by adjusting `DeduplicationWindowMilliseconds` in your configuration file.
     - Ensure that the deduplication key is specific enough to differentiate between important log entries.

3. **Logs Are Being Deduplicated Even After the Window Has Passed**:
   - **Cause**: The deduplication cache might not be clearing correctly after the deduplication window.
   - **Solution**: Review the implementation of the `DeduplicationFilter` to ensure the cache correctly tracks and clears entries after the deduplication window.

4. **Deduplication Filter Is Not Being Applied**:
   - **Cause**: The deduplication filter may not be registered in your Serilog configuration.
   - **Solution**:
     - Ensure that the filter is added to your Serilog configuration using `.Filter.With(new DeduplicationFilter(...))`.
     - Verify that the configuration file is properly loaded and that the deduplication filter is correctly initialized in the startup process.

---

#### **Frequently Asked Questions (FAQ)**:

1. **Can I customize the fields used for deduplication?**
   - Yes, the deduplication key is customizable. You can modify the `DeduplicationFilter` to include any log properties that are important for your specific use case (e.g., `Source`, `UserId`, `Category`).

2. **What is the optimal deduplication window?**
   - The optimal deduplication window depends on your logging frequency and system behavior. For most cases, a window of 3 to 5 seconds works well, but you can adjust this based on how often similar logs are generated in your application.

3. **Can I disable deduplication for certain logs?**
   - Yes, you can modify the filter to conditionally apply deduplication only to certain logs. For example, you might want to skip deduplication for `Error` logs while applying it to `Information` or `Debug` logs.

4. **Does the deduplication filter work with all Serilog sinks?**
   - Yes, the deduplication filter works independently of the sink, so you can apply it to any Serilog sink (e.g., Console, File, MongoDB, Elasticsearch). Just make sure the filter is correctly added to your Serilog pipeline.

5. **How can I contribute to this project?**
   - Contributions are always welcome! Feel free to open a pull request if you have improvements or fixes, or submit issues if you encounter any bugs. We also encourage sharing new features or enhancements to the deduplication logic that could benefit other users.

---

By following the troubleshooting steps and understanding the common issues, you should be able to effectively implement and customize the **Serilog Deduplication Filter** in your project. If you encounter any further problems, feel free to reach out or consult the FAQ for more guidance.

### **7. Performance Considerations**

When using the **Deduplication Filter**, it is essential to keep performance in mind, especially when dealing with high-volume logging or time-sensitive applications.

#### **1. Cache Size and Memory Usage**

The deduplication filter relies on an in-memory cache to track log entries and their timestamps. This cache grows as more unique logs are generated, so it’s important to monitor memory usage in environments with high log volume.

- **Solution**:
  - Periodically clear or prune the cache, especially for long-running applications. This can be done by removing entries that are older than the deduplication window, as they are no longer needed.
  - Example of cache pruning:
  
    ```csharp
    public void PruneCache()
    {
        var expirationTime = DateTime.UtcNow.AddMilliseconds(-_deduplicationWindowMs);
        var keysToRemove = _logCache.Where(kvp => kvp.Value < expirationTime)
                                    .Select(kvp => kvp.Key)
                                    .ToList();

        foreach (var key in keysToRemove)
        {
            _logCache.TryRemove(key, out _);
        }
    }
    ```

#### **2. Thread Safety**

Since the deduplication filter typically operates in multi-threaded environments (e.g., web applications or distributed systems), ensuring thread safety when accessing and modifying the in-memory cache is crucial.

- **Solution**:
  - Use thread-safe collections like `ConcurrentDictionary`, which ensures safe access and updates to the cache across multiple threads without needing manual locking.
  - Example:
  
    ```csharp
    private readonly ConcurrentDictionary<string, DateTime> _logCache = new ConcurrentDictionary<string, DateTime>();
    ```

#### **3. Log Frequency**

In systems with high-frequency logs, deduplication can help reduce unnecessary I/O operations (e.g., writing to disk or sending to a remote logging service). However, if the deduplication window is too short or the criteria are too broad, you may still experience high log volumes.

- **Solution**:
  - Adjust the **deduplication window** based on your system's logging frequency. For high-frequency systems, increasing the deduplication window (e.g., to 10 seconds or more) can significantly reduce log I/O.
  - Use filters to only log the most critical information and avoid redundant logging in highly repetitive areas of your system.

#### **4. Scalability**

If your application scales horizontally (e.g., running across multiple instances), each instance will maintain its own in-memory cache. This could lead to duplicate logs across instances, as they do not share a common cache.

- **Solution**:
  - For distributed systems, consider using a **centralized logging service** (e.g., Elasticsearch, Seq) where deduplication can be applied at the aggregate level, rather than per instance.
  - Alternatively, use a distributed caching mechanism (e.g., Redis) to share deduplication state across instances, though this can introduce additional complexity.

#### **5. Impact on Real-Time Systems**

In real-time systems where logs are expected to be processed immediately, the deduplication filter's cache-based approach might introduce minimal delays due to cache lookups or cache maintenance operations.

- **Solution**:
  - Regularly evaluate the performance impact of the deduplication filter, especially in low-latency or real-time environments. Use profiling tools to measure the overhead and adjust the filter settings accordingly (e.g., reducing the deduplication window or cache size).
  - For real-time systems with strict performance requirements, consider logging only critical events and minimizing the use of deduplication for non-critical logs.

#### **Summary**

By considering these performance factors and applying the appropriate optimizations, you can effectively use the **Deduplication Filter** without impacting the performance of your logging system. Adjust the deduplication window, monitor memory usage, and ensure thread safety to maintain an efficient and scalable logging system.

### **8. Future Improvements**

The **Deduplication Filter** is a powerful tool for reducing logging noise and improving the clarity of logs. However, there are several potential areas for improvement that can further enhance its capabilities and flexibility. Here are some ideas for future enhancements:

#### **1. Configurable Deduplication Key**

Currently, the deduplication key is defined in the filter code and often includes fields like `Code`, `Process`, and `Message`. A future enhancement could allow the deduplication key to be fully configurable via a configuration file (e.g., `appsettings.json`), enabling users to customize the fields used for deduplication without modifying the code.

- **Example:**
  Users could define which fields should be used to generate the deduplication key:
  
  ```json
  {
    "Logging": {
      "DeduplicationKeyFields": ["Code", "Message", "UserId"]
    }
  }

#### **8.2 Distributed Cache Support**

In horizontally scaled systems (e.g., microservices or multiple server instances), the deduplication filter works independently on each instance. A future enhancement could introduce support for a distributed cache (e.g., Redis, Memcached) to allow sharing of deduplication states across instances.

By leveraging a shared cache, log deduplication would be consistent across all running instances of the application. This would prevent duplicate logs from appearing across different instances, improving the efficiency of deduplication in distributed environments.

#### **8.3 Log Analytics Integration**

Future versions could include analytics that track metrics about how often logs are being deduplicated, the most frequently skipped logs, or the frequency of specific log entries. These metrics could be sent to log analysis platforms like **Elasticsearch**, **Grafana**, or **Kibana**.

This enhancement would help users monitor the effectiveness of deduplication and provide insights into system behavior. For example, you could track how much log volume is being reduced due to deduplication or identify patterns in log frequency that may need attention.

Integrating with log analytics platforms would allow real-time monitoring and make it easier to tune the deduplication settings to best suit your application’s needs.

#### **8.4 More Granular Control Over Deduplication**

Currently, the deduplication filter applies the same deduplication window across all logs. A future improvement could introduce more granular control, allowing users to configure different deduplication windows or behaviors based on specific conditions.

##### **Examples:**

- **Per-Log Level Deduplication Windows**: Allow the configuration of different deduplication windows based on log severity. For example, you could set a longer deduplication window for `Error` logs and a shorter window for `Debug` or `Information` logs.
  
  ```json
  {
    "Logging": {
      "ErrorDeduplicationWindowMilliseconds": 10000,
      "InformationDeduplicationWindowMilliseconds": 2000
    }
  }

#### **8.5 UI-Based Configuration**

A future enhancement could introduce a graphical user interface (GUI) for configuring deduplication settings, making it easier for non-developers or administrators to manage deduplication parameters without editing configuration files directly.

##### **Benefits of a UI-Based Configuration**:

- **User-Friendly Interface**: A web-based or desktop application could provide an intuitive way to adjust deduplication windows, enable or disable deduplication for specific log levels or modules, and view real-time statistics on log deduplication.
- **Dynamic Changes**: With a UI, users could change deduplication settings on the fly without needing to restart the application or redeploy code.
- **Enhanced Monitoring**: The UI could also display metrics such as how many logs have been deduplicated in a given period, which logs are frequently skipped, and more.

By providing a UI for configuration, the deduplication filter would become more accessible to users who may not be familiar with editing JSON or other configuration files, improving ease of use in large-scale systems.

#### **8.6 Multi-Tenant Support**

For applications that serve multiple tenants, a future improvement could introduce support for tenant-specific deduplication. This would ensure that logs generated by different tenants are handled independently, preventing logs from one tenant from affecting deduplication behavior for another.

##### **Example:**

- **Tenant-Specific Deduplication**: The deduplication filter could include the `TenantId` as part of the deduplication key, ensuring that logs for each tenant are tracked and deduplicated separately.

  ```csharp
  var logKey = $"{logEvent.Properties["TenantId"]}-{logEvent.Properties["Code"]}-{logEvent.MessageTemplate.Text}";

#### **8.7 AI-Powered Log Filtering**

An exciting future enhancement could involve integrating AI or machine learning to dynamically adjust deduplication behavior based on log patterns and system activity. By using AI, the deduplication filter could learn which logs are important and which are repetitive, automatically optimizing the deduplication settings.

##### **Potential AI-Powered Features**:

- **Adaptive Deduplication**: AI could analyze logging patterns and adjust the deduplication window dynamically based on real-time conditions, reducing or extending the window depending on system behavior.
  
  Example:
  - If a specific error occurs frequently over a short period, the AI could increase the deduplication window to prevent excessive logging noise.

- **Log Prioritization**: AI could classify logs into categories such as high-priority, medium-priority, and low-priority. Deduplication settings could then be applied differently to each category, ensuring critical logs are always recorded, while less important logs may be deduplicated more aggressively.

- **Predictive Log Management**: Machine learning models could predict when certain types of logs are likely to occur and proactively adjust deduplication parameters to optimize log handling.

By leveraging AI, the deduplication filter could become smarter and more efficient, evolving with the system’s needs and automatically optimizing logging performance based on real-time data.

#### **8.8 Community Contributions**

We welcome contributions from the community to enhance the **Deduplication Filter** and expand its functionality. Whether it's adding new features, improving existing functionality, or fixing bugs, your contributions are highly valued!

##### **How to Contribute**:

1. **Fork the Repository**: Start by forking the repository to create your own copy of the project.
2. **Create a Feature Branch**: Work on your feature or fix in a dedicated branch.
   - Example:
     ```bash
     git checkout -b feature/new-awesome-feature
     ```
3. **Commit Your Changes**: Commit your code changes with clear and concise commit messages.
   - Example:
     ```bash
     git commit -m "Add support for new configurable deduplication window"
     ```
4. **Push the Branch**: Push your changes to your forked repository.
   - Example:
     ```bash
     git push origin feature/new-awesome-feature
     ```
5. **Submit a Pull Request**: Open a pull request to the original repository, describing the changes and the motivation behind them.

##### **Contribution Guidelines**:

- Ensure your code follows the existing coding style and is well-documented.
- Provide tests for any new features or fixes to ensure reliability.
- Be open to feedback from maintainers and the community to improve your contributions.

By contributing, you help make the **Deduplication Filter** more powerful and flexible for users everywhere. We look forward to your ideas and improvements!

#### **8.9 Customizable Deduplication Policies**

A future enhancement could introduce support for customizable deduplication policies, allowing users to define different deduplication strategies based on their specific logging needs.

##### **Possible Customization Options**:

- **Deduplication Based on Log Source**: Allow users to configure deduplication windows or behavior based on the source of the log (e.g., specific modules, components, or services).
  
  Example:
  - Apply stricter deduplication to logs from high-traffic modules, such as the authentication service, while using a more lenient policy for less critical components.
  
  ```json
  {
    "Logging": {
      "ModulePolicies": {
        "AuthenticationService": {
          "DeduplicationWindowMilliseconds": 5000
        },
        "PaymentService": {
          "DeduplicationWindowMilliseconds": 10000
        }
      }
    }
  }

#### **8.9 Severity-Based Deduplication**

A future enhancement could introduce **severity-based deduplication**, allowing users to define different deduplication policies for each log level. This would enable more granular control over which logs are deduplicated and which are always recorded, based on their importance.

##### **Example:**

In this example, `Error` logs bypass deduplication entirely, while `Information` logs have a deduplication window of 3 seconds.

```json
{
  "Logging": {
    "SeverityPolicies": {
      "Error": {
        "DeduplicationEnabled": false
      },
      "Information": {
        "DeduplicationWindowMilliseconds": 3000
      },
      "Debug": {
        "DeduplicationWindowMilliseconds": 1000
      }
    }
  }
}

#### **8.9 Time-Based Policies**

A future enhancement could include **time-based deduplication policies**, allowing the deduplication behavior to change dynamically based on the time of day or system load. This approach can help reduce log volume during high-traffic periods while allowing more detailed logging during off-peak hours.

##### **Example:**

In this example, deduplication is more aggressive during peak hours (9 AM to 5 PM) with a 10-second window, while during off-peak hours, a shorter 2-second window is applied.

```json
{
  "Logging": {
    "TimeBasedPolicies": {
      "PeakHours": {
        "StartTime": "09:00:00",
        "EndTime": "17:00:00",
        "DeduplicationWindowMilliseconds": 10000
      },
      "OffPeakHours": {
        "DeduplicationWindowMilliseconds": 2000
      }
    }
  }
}
```

##### **How It Works**:

- **Peak Hours (9 AM - 5 PM)**: During times of heavy traffic or high system load, the deduplication window is extended to 10 seconds. This reduces the frequency of log entries, helping to prevent log overload and ensuring that only essential logs are recorded during busy periods.
  
- **Off-Peak Hours**: When traffic is lower, a shorter deduplication window of 2 seconds is applied, allowing more detailed logs to be captured for better analysis without overwhelming the system.

- **Dynamic Adjustment**: The system automatically adjusts the deduplication window based on the current time, ensuring the appropriate level of logging activity for each part of the day.

---

##### **Benefits**:

- **Optimized Log Management**: Time-based deduplication helps ensure that during peak times, the system doesn’t get overwhelmed by excessive logs, while allowing more granular logging during off-peak hours.
  
- **Reduced Log Overload**: By applying a longer deduplication window during periods of high activity, the system can filter out redundant logs, reducing unnecessary storage and processing overhead.

- **Improved System Performance**: With less log volume during peak hours, the overall system performance can be improved, reducing disk I/O and database strain in logging systems.

- **Greater Log Detail**: During off-peak hours, the shorter deduplication window allows more logs to be captured, enabling deeper analysis and better tracking of system behavior when it’s less busy.

## License

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

For more details, please refer to the [LICENSE](./LICENSE) file or the official GNU GPL 3.0 text [here](https://www.gnu.org/licenses/gpl-3.0.html).
