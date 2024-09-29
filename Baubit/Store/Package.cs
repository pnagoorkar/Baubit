using System.Reflection;

namespace Baubit.Store
{
    public class Package
    {
        public AssemblyName AssemblyName { get; init; }
        public string DllRelativePath { get; init; }
        public IReadOnlyList<Package> Dependencies { get; init; }
    }

    public class SerializablePackage
    {
        public string AssemblyName { get; init; }
        public string DllRelativePath { get; set; }
        public List<string> Dependencies { get; init; } = new List<string>();
    }

    public static class PackageExtnsns
    {
        public static SerializablePackage AsSerializable(this Package package)
        {
            return new SerializablePackage
            {
                AssemblyName = package.AssemblyName.GetPersistableAssemblyName(),
                DllRelativePath = package.DllRelativePath,
                Dependencies = package.Dependencies.Select(dep => dep.AssemblyName.GetPersistableAssemblyName()).ToList(),
            };
        }

        public static IEnumerable<SerializablePackage> AsSerializable(this IEnumerable<Package> packages)
        {
            return packages.SelectMany(package => package.GetAllTrees().Select(p => p.AsSerializable()));
        }

        public static IEnumerable<Package> GetAllTrees(this Package package) 
        {
            yield return package;

            foreach (var dependency in package.Dependencies.SelectMany(package => package.GetAllTrees()))
            {
                yield return dependency;
            }
        }

        public static Package? Search(this Package package, AssemblyName assemblyName)
        {
            return package.Search(assemblyName);
        }

        public static Package? Search(this Package package, string assemblyName)
        {
            return package.GetAllTrees().FirstOrDefault(pkg => pkg.AssemblyName.GetPersistableAssemblyName().Equals(assemblyName, StringComparison.OrdinalIgnoreCase));
        }

        public static Package? Search(this IEnumerable<Package> packages, AssemblyName assemblyName)
        {
            return packages.Search(assemblyName.GetPersistableAssemblyName());
        }

        public static Package? Search(this IEnumerable<Package> packages, string assemblyName)
        {
            return packages.SelectMany(package => package.GetAllTrees()).FirstOrDefault(pkg => pkg.AssemblyName.GetPersistableAssemblyName().Equals(assemblyName, StringComparison.OrdinalIgnoreCase));
        }

        public static SerializablePackage? Search(this IEnumerable<SerializablePackage> serializablePackages, AssemblyName assemblyName)
        {
            return serializablePackages.Search(assemblyName.GetPersistableAssemblyName());
        }

        public static SerializablePackage? Search(this IEnumerable<SerializablePackage> serializablePackages, string assemblyName)
        {
            return serializablePackages.FirstOrDefault(serializablePackage => serializablePackage.AssemblyName.Equals(assemblyName, StringComparison.OrdinalIgnoreCase));
        }

        public static Package AsPackage(this SerializablePackage serializablePackage,
                                        IEnumerable<SerializablePackage> serializablePackages, 
                                        List<Package> cache)
        {
            var dependencies = new List<Package>();

            foreach (var serializableDependency in serializablePackage.Dependencies)
            {
                var cachedPackage = cache.Search(serializableDependency);
                var dependencyPackage = cachedPackage ?? serializablePackages.Search(serializableDependency)!.AsPackage(serializablePackages, cache);
                dependencies.Add(dependencyPackage);
                if (cachedPackage == null) cache.Add(dependencyPackage);
            }
            
            var currentPackage = new Package
            {
                AssemblyName = AssemblyExtensions.GetAssemblyNameFromPersistableString(serializablePackage.AssemblyName),
                DllRelativePath = serializablePackage.DllRelativePath,
                Dependencies = dependencies
            };
            return currentPackage;
        }

        public static List<Package> AsPackages(this IEnumerable<SerializablePackage> serializablePackages)
        {
            var result = new List<Package>();
            foreach (var serializablePackage in serializablePackages)
            {
                var cachedPackage = result.Search(serializablePackage.AssemblyName);
                var package = cachedPackage ?? serializablePackage.AsPackage(serializablePackages, result);

                if(cachedPackage == null) result.Add(package);
            }
            return result;
        }
    }
}
