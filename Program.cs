using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Versioning;

var assemblyName = new AssemblyName("MyAssembly");
assemblyName.Version=new Version(0, 0, 0, 1);

FrameworkName? fwkName = new FrameworkName(Assembly.GetExecutingAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName!);
// Get Program Files path
string basePath = Environment.OSVersion.Platform == PlatformID.Unix ? "/usr/share" : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
string netcoreAppRefPath = $"{basePath}/dotnet/packs/Microsoft.NETCore.App.Ref/{fwkName.Version.Major}.{fwkName.Version.Minor}.{(fwkName.Version.Build >= 0 ? fwkName.Version.Build : 0)}/ref/net{fwkName.Version.Major}.{fwkName.Version.Minor}";
IList<string> refAssembliesPath = Directory.GetFiles($"{netcoreAppRefPath}", "*.dll");

var resolver = new PathAssemblyResolver(refAssembliesPath);
using MetadataLoadContext context = new (resolver);
var assembly = new PersistedAssemblyBuilder(assemblyName, context.CoreAssembly);
var module = assembly.DefineDynamicModule("MyModule");

var poiType = module.DefineType("PurchaseOrderItem");
poiType.DefineProperty("Product", PropertyAttributes.None, context.CoreAssembly.GetType(typeof(string).FullName!), null);
// Omit the creation of getters and setters, and other properties for brevity
poiType.DefineProperty("Quantity", PropertyAttributes.None, context.CoreAssembly.GetType(typeof(int).FullName!), null);

var poType = module.DefineType("PurchaseOrder");
poType.DefineProperty("OrderDate", PropertyAttributes.None, context.CoreAssembly.GetType(typeof(DateTime).FullName!), null);

// This works. We can use directly the defined types.
poiType.DefineProperty("PurchaseOrder", PropertyAttributes.None, poType, null);

// This does not work, because it uses System.Private.CoreLib.dll
// context.LoadFromAssemblyName(typeof(List<>).Assembly.GetName());

context.LoadFromAssemblyName(new AssemblyName("System.Collections"));

var listContextType = context.CoreAssembly.GetType(typeof(List<>).FullName!);
// This will throw System.ArgumentException! The type is not available in MetadataLoadContext, and it fails.
var listOfPoiType = listContextType.MakeGenericType(poiType);

poType.DefineProperty("Items", PropertyAttributes.None, listOfPoiType, null);