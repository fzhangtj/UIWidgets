using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityAssembly = UnityEditor.Compilation.Assembly;

namespace Unity.UIWidgets.Editor.Weaver {

    public static class CompilationFinishedHook {
        const string UIWidgetsRuntimeAssemblyName = "Unity.UIWidgets";
        const string UIWidgetsWeaverAssemblyName = "UIWidgets.Weaver";

        public static Action<string> OnWeaverMessage;
        public static Action<string> OnWeaverWarning;
      //  public static Action<WeaveError> OnWeaverError;

        public static bool WeaverEnabled { get; set; }

        public static bool UnityLogEnabled = true;

        public static bool WeaveFailed { get; private set; }

        static void HandleMessage(string msg) {
            if (UnityLogEnabled) {
                Debug.Log(msg);
            }

            if (OnWeaverMessage != null) {
                OnWeaverMessage.Invoke(msg);
            }
        }

        static void HandleWarning(string msg) {
            if (UnityLogEnabled) {
                Debug.LogWarning(msg);
            }

            if (OnWeaverWarning != null) {
                OnWeaverWarning.Invoke(msg);
            }
        }

        static void HandleError(WeaveError error) {
            if (UnityLogEnabled) {
                if (error.exception != null) {
                    Debug.LogException(error.exception);    
                }
                else {
                    Debug.LogError(error.msg);
                }
            }

            if (OnWeaverError != null) {
                OnWeaverError.Invoke(error);
            }
        }

        [InitializeOnLoadMethod]
        static void OnInitializeOnLoad() {
            CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
        }

        static string FindUIWidgetsRuntime() {
            foreach (UnityAssembly assembly in CompilationPipeline.GetAssemblies()) {
                if (assembly.name == UIWidgetsRuntimeAssemblyName) {
                    return assembly.outputPath;
                }
            }

            return "";
        }

        static bool CompilerMessagesContainError(CompilerMessage[] messages) {
            return messages.Any(msg => msg.type == CompilerMessageType.Error);
        }

        static void OnCompilationFinished(string assemblyPath, CompilerMessage[] messages) {

            if (CompilerMessagesContainError(messages)) {
                Debug.Log("Weaver: stop because compile errors on target");
                return;
            }

            if (assemblyPath.Contains("-Editor") || assemblyPath.Contains(".Editor")) {
                return;
            }

            // don't weave weaver assembly
            string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
            if (assemblyName == UIWidgetsWeaverAssemblyName) {
                return;
            }

            if (assemblyName != "WeaverSample") {
               // return;   
            }

            string uiwidgetsRuntimeDll = FindUIWidgetsRuntime();
            if (string.IsNullOrEmpty(uiwidgetsRuntimeDll)) {
                Debug.LogError("Failed to find UIWidgets runtime assembly");
                return;
            }

            if (!File.Exists(uiwidgetsRuntimeDll)) {
                return;
            }

            HashSet<string> dependencyPaths = new HashSet<string>();
            dependencyPaths.Add(Path.GetDirectoryName(assemblyPath));
            foreach (UnityAssembly unityAsm in CompilationPipeline.GetAssemblies()) {
                if (unityAsm.outputPath != assemblyPath) {
                    continue;
                }

                foreach (string unityAsmRef in unityAsm.compiledAssemblyReferences) {
                    dependencyPaths.Add(Path.GetDirectoryName(unityAsmRef));
                }
            }

            if (Program.Process(assemblyPath, assemblyName == UIWidgetsRuntimeAssemblyName, 
                uiwidgetsRuntimeDll, null, dependencyPaths.ToArray(),
                HandleWarning, HandleError)) {
                WeaveFailed = false;
            }
            else {
                WeaveFailed = true;
                if (UnityLogEnabled) {
                    Debug.LogError("Weaving failed for: " + assemblyPath);
                }
            }
        }
    }
}