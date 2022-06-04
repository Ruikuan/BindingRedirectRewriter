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

    var (fileContent, encoding) = ReadConfigFileContext(configFile);
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
            writer.WriteLine(line);

            var assemblyName = GetAttributeValue(line, "name").ToString();

            var versionLine = reader.ReadLine();
            var originTargetVersionValue = GetAttributeValue(versionLine, "newVersion");

            if (assemblyMap.TryGetValue(assemblyName, out var targetVersion) && Version.TryParse(originTargetVersionValue, out var originVersion) && targetVersion != originVersion)
            {
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

static ReadOnlySpan<char> GetAttributeValue(ReadOnlySpan<char> line, string attributeName)
{
    var left = line.IndexOf($"{attributeName}=\"") + attributeName.Length + 2;
    line = line[left..];
    var attributeValue = line[..line.IndexOf("\"")];
    return attributeValue;
}

static (string content, Encoding encoding) ReadConfigFileContext(string fileName)
{
    using StreamReader streamReader = new StreamReader(fileName, new UTF8Encoding(false));
    string content = streamReader.ReadToEnd();
    return (content, streamReader.CurrentEncoding); // if file is utf-bom, the CurrentEncoding will be changed to UTF8Encoding(true) automatically.
}
