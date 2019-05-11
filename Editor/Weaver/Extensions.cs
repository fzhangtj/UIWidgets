using Mono.Cecil;

namespace Unity.UIWidgets.Editor.Weaver {
    public static class Extensions {
        public static bool ImplementsInterface(this TypeDefinition td, TypeReference baseInterface) {
            TypeDefinition typedef = td;
            while (typedef != null) {
                foreach (InterfaceImplementation iface in typedef.Interfaces) {
                    if (iface.InterfaceType.FullName == baseInterface.FullName) {
                        return true;
                    }
                }

                try {
                    TypeReference parent = typedef.BaseType;
                    typedef = parent == null ? null : parent.Resolve();
                }
                catch (AssemblyResolutionException) {
                    // this can happen for pluins.
                    //Console.WriteLine("AssemblyResolutionException: "+ ex.ToString());
                    break;
                }
            }

            return false;
        }
    }
}