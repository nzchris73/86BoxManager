using System;
using System.Text.Json;

namespace _86boxManager
{
    internal static class VMRegistryStorage
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        internal static string Serialize(VM vm)
        {
            return JsonSerializer.Serialize(new VMData
            {
                Name = vm.Name,
                Desc = vm.Desc,
                Path = vm.Path,
                Status = vm.Status,
                Pid = vm.Pid,
                HWnd = vm.hWnd.ToInt64()
            });
        }

        internal static VM Deserialize(string json)
        {
            VMData data = JsonSerializer.Deserialize<VMData>(json, JsonOptions);
            if (data == null)
            {
                throw new InvalidOperationException("A virtual-machine registry entry contains invalid JSON.");
            }

            return new VM
            {
                Name = data.Name,
                Desc = data.Desc,
                Path = data.Path,
                Status = data.Status,
                Pid = data.Pid,
                hWnd = new IntPtr(data.HWnd)
            };
        }

        private sealed class VMData
        {
            public string Name { get; set; }
            public string Desc { get; set; }
            public string Path { get; set; }
            public int Status { get; set; }
            public int Pid { get; set; }
            public long HWnd { get; set; }
        }
    }
}
