using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowPrivacy
{
    internal class Injection
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess,
            IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        // privileges
        const int PROCESS_CREATE_THREAD = 0x0002;
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_READ = 0x0010;

        // used for memory allocation
        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_READWRITE = 4;
        public enum Affinity
        {
            WDA_NONE = 0x00000000,
            WDA_MONITOR = 0x00000001,
            WDA_EXCLUDEFROMCAPTURE = 0x00000011
        }
        public static bool injectAffinity(int ProcID, Affinity state)
        {
            Process proc;
            try
            {
                proc = Process.GetProcessById(ProcID);
            }
            catch (ArgumentException)
            {
                //Process not Running or doesn't Exists
                return false;
            }
            IntPtr procHandle = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, proc.Id);
            if (procHandle == IntPtr.Zero) //failed to open process
                return false;

            // searching for the address of LoadLibraryA and storing it in a pointer
            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            string dllName = (state == Affinity.WDA_MONITOR) ? "Internal_SetWindowsAffinity.dll" : "Internal_RemWindowsAffinity.dll";
            dllName = Path.Combine(System.IO.Path.GetDirectoryName(Application.ExecutablePath), dllName);

            IntPtr allocMemAddress = VirtualAllocEx(procHandle, IntPtr.Zero, (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (allocMemAddress == IntPtr.Zero) //failed to allocate process memory
                return false;

            UIntPtr bytesWritten;
            bool results = WriteProcessMemory(procHandle, allocMemAddress, Encoding.Default.GetBytes(dllName), (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), out bytesWritten);
            if (!results || bytesWritten.Equals(0))
                return false; //couldn't write process name to allocated memory

            var crt_Results = CreateRemoteThread(procHandle, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
            if (crt_Results.Equals(0))
                return false; //couldn't call LoadLibraryA

            return true;
        }
    }
}
