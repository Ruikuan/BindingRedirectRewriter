using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace BindingRedirectRewriter
{
    internal static class AssemblyInfoReader
    {
        public static AssemblyInfo GetAssemblyInfo(string assemblyFile)
        {
            using var fs = new FileStream(assemblyFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var peReader = new PEReader(fs);

            MetadataReader mr = peReader.GetMetadataReader();
            var assemblyDef = mr.GetAssemblyDefinition();
            var assemblyName = assemblyDef.GetAssemblyName();

            return new AssemblyInfo(assemblyName.Name!, assemblyName.Version!);
        }
    }

    internal record struct AssemblyInfo(string AssemblyName, Version Version);
}
