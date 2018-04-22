using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace NUnit.Engine.Internal.RuntimeFrameworks
{
    internal static class DotNetFrameworkLocator
    {
        // Note: this method cannot be generalized past V4, because (a)  it has
        // specific code for detecting .NET 4.5 and (b) we don't know what
        // microsoft will do in the future
        public static IEnumerable<RuntimeFramework> FindDotNetFrameworks()
        {
            // Handle Version 1.0, using a different registry key
            foreach (var framework in FindExtremelyOldDotNetFrameworkVersions())
                yield return framework;

            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\NET Framework Setup\NDP");
            if (key != null)
            {
                foreach (string name in key.GetSubKeyNames())
                {
                    if (name.StartsWith("v") && name != "v4.0") // v4.0 is a duplicate, legacy key
                    {
                        var versionKey = key.OpenSubKey(name);
                        if (versionKey == null) continue;

                        if (name.StartsWith("v4", StringComparison.Ordinal))
                        {
                            // Version 4 and 4.5
                            foreach (var framework in FindDotNetFourFrameworkVersions(versionKey))
                            {
                                yield return framework;
                            }
                        }
                        else if (CheckInstallDword(versionKey))
                        {
                            // Versions 1.1 through 3.5
                            yield return new RuntimeFramework(RuntimeType.Net, new Version(name.Substring(1)));
                        }
                    }
                }
            }
        }

        private static IEnumerable<RuntimeFramework> FindExtremelyOldDotNetFrameworkVersions()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\.NETFramework\policy\v1.0");
            if (key == null)
                yield break;

            foreach (var build in key.GetValueNames())
                yield return new RuntimeFramework(RuntimeType.Net, new Version("1.0." + build));
        }

        private static IEnumerable<RuntimeFramework> FindDotNetFourFrameworkVersions(RegistryKey versionKey)
        {
            foreach (string profile in new string[] { "Full", "Client" })
            {
                var profileKey = versionKey.OpenSubKey(profile);
                if (profileKey == null) continue;

                if (CheckInstallDword(profileKey))
                {
                    yield return new RuntimeFramework(RuntimeType.Net, new Version(4, 0), profile);

                    var release = (int)profileKey.GetValue("Release", 0);
                    if (release > 0) // TODO: Other higher versions?
                        yield return new RuntimeFramework(RuntimeType.Net, new Version(4, 5));

                    yield break;     //If full profile found don't check for client profile
                }
            }
        }

        private static bool CheckInstallDword(RegistryKey key)
        {
            return ((int)key.GetValue("Install", 0) == 1);
        }
    }
}