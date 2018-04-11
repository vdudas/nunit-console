// ***********************************************************************
// Copyright (c) 2016 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using Mono.Cecil;
using NUnit.Common;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Services
{
    internal class ExtensionAssembly
    {
        public ExtensionAssembly(string filePath, bool fromWildCard)
        {
            FilePath = filePath;
            FromWildCard = fromWildCard;
            Assembly = GetAssemblyDefinition();
            TargetFrameworkHelper = new TargetFrameworkHelper(Assembly);
        }

        public string FilePath { get; }
        public bool FromWildCard { get; }
        public AssemblyDefinition Assembly { get; }
        internal TargetFrameworkHelper TargetFrameworkHelper { get; }

        public string AssemblyName
        {
            get { return Assembly.Name.Name; }
        }

        public Version AssemblyVersion
        {
            get { return Assembly.Name.Version; }
        }

        public ModuleDefinition MainModule
        {
            get { return Assembly.MainModule; }
        }

        /// <summary>
        /// IsDuplicateOf returns true if two assemblies have the same name.
        /// </summary>
        public bool IsDuplicateOf(ExtensionAssembly other)
        {
            return AssemblyName == other.AssemblyName;
        }
        
        /// <summary>
        /// IsBetterVersion determines whether another assembly is
        /// a better than the current assembly. It first looks at 
        /// for the highest assembly version, and then the highest target
        /// framework. With a tie situation, assemblies specified directly
        /// are prefered to those located via wildcards.
        /// 
        /// It is only intended to be called if IsDuplicateOf
        /// has already returned true. This method does no work to check if
        /// the target framework found is available under the current engine.
        /// </summary>
        public bool IsBetterVersionOf(ExtensionAssembly other)
        {
            Guard.OperationValid(IsDuplicateOf(other), "IsBetterVersionOf should only be called on duplicate assemblies");

            //Look at assembly version
            var version = AssemblyVersion;
            var otherVersion = other.AssemblyVersion;
            if (version > otherVersion)
                return true;

            if (version < otherVersion)
                return false;

            //Look at target runtime
            var targetRuntime = TargetFrameworkHelper.TargetRuntimeVersion;
            var otherTargetRuntime = other.TargetFrameworkHelper.TargetRuntimeVersion;
            if (targetRuntime > otherTargetRuntime)
                return true;

            if (targetRuntime < otherTargetRuntime)
                return false;

            //Everything is equal, override only if this one was specified exactly while the other wasn't
            return !FromWildCard && other.FromWildCard;
        }

        private AssemblyDefinition GetAssemblyDefinition()
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(System.IO.Path.GetDirectoryName(FilePath));
            resolver.AddSearchDirectory(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            var parameters = new ReaderParameters { AssemblyResolver = resolver };

            return AssemblyDefinition.ReadAssembly(FilePath, parameters);
        }
    }
}
