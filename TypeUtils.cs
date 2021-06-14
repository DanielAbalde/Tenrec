using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection; 
using Tenrec.Components;

namespace Tenrec
{ 
    public static class TypeUtils
    {     
        public static string GetAssemblyDirectory(Assembly assembly = null)
        {
            if (assembly == null)
                assembly = Assembly.GetExecutingAssembly();
            UriBuilder uri = new UriBuilder(assembly.CodeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
        public static IEnumerable<Type> GetTypesFromAssembly(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e2)
            {
                return e2.Types.Where(t => t != null);
            }
        }
        public static IEnumerable<Type> GetTypesFromAssemblyAssignableFrom(Assembly assembly, Type type, bool ignoreAbstracts = true, bool ignoreGenerics = false, bool ignorePrivates = true)
        {
            foreach (var t in GetTypesFromAssembly(assembly))
            {
                if (t == type)
                    continue;
                if (ignoreAbstracts && t.IsAbstract)
                    continue;
                if (ignoreGenerics && t.IsGenericType)
                    continue;
                if (ignorePrivates && !t.IsPublic)
                    continue;
                if (!IsType(t, type))
                    continue;
                yield return t;
            }
        }
        public static bool IsType(Type toCheck, Type type)
        {
            if (toCheck == null || type == null)
                return false;
            if (toCheck == type)
                return true;
            if (type.IsInterface)
            {
                if (type.IsGenericType)
                    return toCheck.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == type);
                else
                    return type.IsAssignableFrom(toCheck);
            }
            else if (type.IsGenericType)
            {
                while (toCheck != null)
                {
                    var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                    if (cur == type)
                        return true;
                    toCheck = toCheck.BaseType;
                }
            }
            else
            {
                while (toCheck != null)
                {
                    if (toCheck == type)
                        return true;
                    toCheck = toCheck.BaseType;
                }
            }

            return false;
        }
        public static IEnumerable<Assembly> LoadTenrecAssembliesFrom(string directory = "")
        {
            if (string.IsNullOrEmpty(directory))
                directory = GetAssemblyDirectory();// AppDomain.CurrentDomain.BaseDirectory;
            else
                if (!System.IO.Directory.Exists(directory))
                throw new DirectoryNotFoundException(directory);
            var assName = typeof(Group_UnitTest).Assembly.GetName().Name;
            foreach (var ass in Directory.GetFiles(directory, "*.dll")
                .Concat(Directory.GetFiles(directory, "*.gha")))
            {
                Assembly a = null;
                try
                {
                    var n = AssemblyName.GetAssemblyName(ass);
                    if (n == null)
                        continue;
                    a = Assembly.Load(n);
                    if (!(a.GetName().Name.Equals(assName) ||
                    a.GetReferencedAssemblies().Any(ra => ra.Name.Equals(assName))
                      && !Guid.TryParse(a.GetName().Name, out _)))
                        continue;
                }
                catch 
                {

                }
                if (a == null)
                    continue;
                yield return a;
            }

        }       
    } 
}
 
