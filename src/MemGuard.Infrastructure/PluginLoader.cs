using MemGuard.Plugins;
using System.Reflection;

namespace MemGuard.Infrastructure;

/// <summary>
/// Loads and manages plugin assemblies
/// </summary>
public class PluginLoader
{
    private readonly string _pluginDirectory;
    private readonly List<IDetectorPlugin> _detectorPlugins = new();
    private readonly List<IExporterPlugin> _exporterPlugins = new();

    public PluginLoader(string? pluginDirectory = null)
    {
        _pluginDirectory = pluginDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".memguard",
            "plugins");

        EnsurePluginDirectoryExists();
    }

    public IReadOnlyList<IDetectorPlugin> DetectorPlugins => _detectorPlugins.AsReadOnly();
    public IReadOnlyList<IExporterPlugin> ExporterPlugins => _exporterPlugins.AsReadOnly();

    /// <summary>
    /// Discover and load all plugins from the plugin directory
    /// </summary>
    public async Task LoadPluginsAsync()
    {
        await Task.Run(() =>
        {
            if (!Directory.Exists(_pluginDirectory))
                return;

            var pluginFiles = Directory.GetFiles(_pluginDirectory, "*.dll", SearchOption.AllDirectories);

            foreach (var pluginFile in pluginFiles)
            {
                try
                {
                    LoadPluginAssembly(pluginFile);
                }
                catch (Exception ex)
                {
                    // Log plugin loading error but continue
                    Console.WriteLine($"Failed to load plugin {pluginFile}: {ex.Message}");
                }
            }
        });
    }

    private void LoadPluginAssembly(string assemblyPath)
    {
        // Load the assembly
        var assembly = Assembly.LoadFrom(assemblyPath);

        // Find all types implementing plugin interfaces
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        // Load detector plugins
        var detectorTypes = types.Where(t => typeof(IDetectorPlugin).IsAssignableFrom(t));
        foreach (var type in detectorTypes)
        {
            try
            {
                if (Activator.CreateInstance(type) is IDetectorPlugin detector)
                {
                    _detectorPlugins.Add(detector);
                    Console.WriteLine($"Loaded detector plugin: {detector.Name} v{detector.Version}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to instantiate detector {type.Name}: {ex.Message}");
            }
        }

        // Load exporter plugins
        var exporterTypes = types.Where(t => typeof(IExporterPlugin).IsAssignableFrom(t));
        foreach (var type in exporterTypes)
        {
            try
            {
                if (Activator.CreateInstance(type) is IExporterPlugin exporter)
                {
                    _exporterPlugins.Add(exporter);
                    Console.WriteLine($"Loaded exporter plugin: {exporter.Name} v{exporter.Version}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to instantiate exporter {type.Name}: {ex.Message}");
            }
        }
    }

    private void EnsurePluginDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_pluginDirectory))
            {
                Directory.CreateDirectory(_pluginDirectory);
                
                // Create a README in the plugins directory
                var readmePath = Path.Combine(_pluginDirectory, "README.txt");
                File.WriteAllText(readmePath, @"MemGuard Plugin Directory
=======================================

Place your custom MemGuard plugins (.dll files) in this directory.

Plugins should implement:
- IDetectorPlugin for custom analysis detectors
- IExporterPlugin for custom report exporters

Visit https://github.com/FahadAkash/MemGuard for plugin development documentation.
");
            }
        }
        catch
        {
            // Ignore directory creation errors
        }
    }
    
    /// <summary>
    /// Get all loaded detector plugins
    /// </summary>
    public IEnumerable<IDetectorPlugin> GetDetectorPlugins() => _detectorPlugins;

    /// <summary>
    /// Get all loaded exporter plugins
    /// </summary>
    public IEnumerable<IExporterPlugin> GetExporterPlugins() => _exporterPlugins;

    /// <summary>
    /// Get exporter plugin by name
    /// </summary>
    public IExporterPlugin? GetExporter(string name)
    {
        return _exporterPlugins.FirstOrDefault(e => 
            e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
