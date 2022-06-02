using BindingRedirectRewriter;
using System.Collections.Concurrent;
using System.Text;

if (args.Length == 0)
{
    Console.WriteLine("root dir is required.");
    return;
}
List<string> rootDirs = new List<string>();
rootDirs.Add(args[0]);

ConcurrentQueue<string> noBinQueue = new();

foreach (string rootDir in rootDirs)
{
    FindAppDirAndDoChange(rootDir, 0);
}

if (noBinQueue.Count > 0)
{
    Console.WriteLine("No bin and skipped dirs:");
}
while (noBinQueue.Count > 0 && noBinQueue.TryDequeue(out var path))
{
    Console.WriteLine($"\t\t{path}");
}
Console.WriteLine("done.");

void FindAppDirAndDoChange(string dirPath, int level)
{
    if (level > 6) return;
    if (File.Exists(Path.Combine(dirPath, "Web.config")) || File.Exists(Path.Combine(dirPath, "App.config")))
    {
        Console.WriteLine($"processing: {dirPath}");
        TryCorrectBindingRedirect(dirPath);
    }
    else
    {
        Parallel.ForEach(Directory.GetDirectories(dirPath), subDir => FindAppDirAndDoChange(subDir, level + 1));
    }
}

void TryCorrectBindingRedirect(string dirPath)
{

    string configFile = Path.Combine(dirPath, "Web.config");
    if (!File.Exists(configFile))
    {
        configFile = Path.Combine(dirPath, "App.config");
    }

    string fileContent = File.ReadAllText(configFile, Encoding.UTF8);
    if (!fileContent.Contains("<assemblyBinding"))
    {
        return;
    }

    string binPath = Path.Combine(dirPath, "bin");
    List<string> dllPaths = new List<string>()
    {
        binPath,
        Path.Combine(binPath,"debug"),
        Path.Combine(binPath,"release")
    };

    Dictionary<string, Version> assemblyMap = new();
    foreach (string dllPath in dllPaths)
    {
        if (!Directory.Exists(dllPath))
        {
            continue;
        }

        Parallel.ForEach(Directory.GetFiles(dllPath, "*.dll", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }), dllFile =>
        {
            try
            {
                if (AssemblyInfoReader.TryGetAssemblyInfo(dllFile, out var assemblyInfo))
                {
                    lock (assemblyMap)
                    {
                        if (assemblyMap.ContainsKey(assemblyInfo.AssemblyName))
                        {
                            if (assemblyMap[assemblyInfo.AssemblyName] < assemblyInfo.Version)
                            {
                                assemblyMap[assemblyInfo.AssemblyName] = assemblyInfo.Version;
                            }
                        }
                        else
                        {
                            assemblyMap.Add(assemblyInfo.AssemblyName, assemblyInfo.Version);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{dllFile} {ex.Message}");
            }
        });
    }

    if (assemblyMap.Count == 0)
    {
        noBinQueue.Enqueue(dirPath);
        return;
    }

    using StringReader reader = new StringReader(fileContent);
    using StringWriter writer = new StringWriter();
    bool firstLine = true;
    while (reader.ReadLine() is string line)
    {
        if (!firstLine)
        {
            writer.WriteLine();
        }
        firstLine = false;

        if (line.Contains("assemblyIdentity")) // found a binding redirect
        {
            var lineSpan = line.AsSpan();
            var left = lineSpan.IndexOf("name=\"") + 6;
            lineSpan = lineSpan[left..];
            var assemblyName = lineSpan[..lineSpan.IndexOf("\"")].ToString();
            writer.WriteLine(line);

            var versionLine = reader.ReadLine();
            if (assemblyMap.ContainsKey(assemblyName))
            {
                var targetVersion = assemblyMap[assemblyName];
                var indent = versionLine.AsSpan()[..versionLine!.IndexOf('<')];
                writer.Write(indent);
                writer.Write($"""
                    <bindingRedirect oldVersion="0.0.0.0-{targetVersion}" newVersion="{targetVersion}" />
                    """);
            }
            else
            {
                writer.Write(versionLine);
            }
        }
        else
        {
            writer.Write(line);
        }
    }
    if (fileContent.EndsWith('\n'))
    {
        writer.WriteLine();
    }
    File.WriteAllText(configFile, writer.ToString(), new UTF8Encoding(false));
}