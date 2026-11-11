using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Script.Serialization;

namespace _86boxManager.LegacyMigration
{
    internal static class Program
    {
        private const string VirtualMachinesKey = @"SOFTWARE\86Box\Virtual Machines";

        private static int Main()
        {
            int result;
            try
            {
                result = Migrate();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Migration failed: " + ex.Message);
                result = 1;
            }

            Console.WriteLine("Press Enter to close.");
            Console.ReadLine();
            return result;
        }

        private static int Migrate()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(VirtualMachinesKey, true))
            {
                if (key == null)
                {
                    Console.Error.WriteLine("The 86Box Manager virtual-machines registry key was not found.");
                    return 1;
                }

                List<MigrationEntry> entries = new List<MigrationEntry>();
                foreach (string name in key.GetValueNames())
                {
                    object value = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    if (value is byte[] data)
                    {
                        entries.Add(new MigrationEntry(name, data, ConvertLegacyEntry(data)));
                    }
                    else if (!(value is string))
                    {
                        throw new InvalidOperationException("Unsupported registry value type for virtual machine \"" + name + "\".");
                    }
                }

                if (entries.Count == 0)
                {
                    Console.WriteLine("No legacy virtual-machine entries were found. Nothing needs to be migrated.");
                    return 0;
                }

                Console.WriteLine("This will convert " + entries.Count + " virtual-machine entries to JSON.");
                Console.WriteLine("Original binary entries will be retained under the \"Legacy Backup\" subkey.");
                Console.Write("Continue? [y/N] ");
                if (!string.Equals(Console.ReadLine(), "y", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Migration cancelled.");
                    return 0;
                }

                using (RegistryKey backupKey = key.CreateSubKey("Legacy Backup"))
                {
                    foreach (MigrationEntry entry in entries)
                    {
                        if (backupKey.GetValue(entry.Name) == null)
                        {
                            backupKey.SetValue(entry.Name, entry.OriginalData, RegistryValueKind.Binary);
                        }
                    }
                }

                foreach (MigrationEntry entry in entries)
                {
                    key.SetValue(entry.Name, entry.Json, RegistryValueKind.String);
                }

                Console.WriteLine("Successfully migrated " + entries.Count + " virtual-machine entries.");
                return 0;
            }
        }

        private static string ConvertLegacyEntry(byte[] data)
        {
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream(data))
            {
                BinaryFormatter formatter = new BinaryFormatter
                {
                    Binder = new VMSerializationBinder()
                };
                VM vm = (VM)formatter.Deserialize(stream);
                Dictionary<string, object> converted = new Dictionary<string, object>
                {
                    { "Name", vm.Name },
                    { "Desc", vm.Desc },
                    { "Path", vm.Path },
                    { "Status", vm.Status },
                    { "Pid", vm.Pid },
                    { "HWnd", vm.hWnd.ToInt64() }
                };
                return new JavaScriptSerializer().Serialize(converted);
            }
        }

        private sealed class MigrationEntry
        {
            internal MigrationEntry(string name, byte[] originalData, string json)
            {
                Name = name;
                OriginalData = originalData;
                Json = json;
            }

            internal string Name { get; }
            internal byte[] OriginalData { get; }
            internal string Json { get; }
        }

        private sealed class VMSerializationBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName == typeof(VM).FullName)
                {
                    return typeof(VM);
                }

                if (typeName == typeof(IntPtr).FullName)
                {
                    return typeof(IntPtr);
                }

                throw new SerializationException("Unsupported serialized type: " + typeName);
            }
        }
    }
}
