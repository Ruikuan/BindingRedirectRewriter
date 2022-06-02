using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace BindingRedirectRewriter
{
    internal static class AssemblyInfoReader
    {
        public static bool TryGetAssemblyInfo(string assemblyFile, out AssemblyInfo assemblyInfo)
        {
            using var fs = new FileStream(assemblyFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var peReader = new PEReader(fs);

            if (!peReader.HasMetadata)
            {
                assemblyInfo = default;
                return false;
            }

            MetadataReader mr = peReader.GetMetadataReader();
            var assemblyDef = mr.GetAssemblyDefinition();
            var assemblyName = assemblyDef.GetAssemblyName();

            assemblyInfo = new AssemblyInfo(assemblyName.Name!, assemblyName.Version!);
            return true;
        }
    }

    internal record struct AssemblyInfo(string AssemblyName, Version Version);
}
