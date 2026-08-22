using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightUpUI.Services;

public sealed class WindowsEverythingSearchClient : IEverythingSearchClient
{
    private const uint RequestFullPathAndFileName = 0x00000004;
    private const uint SortNameAscending = 1;
    private static readonly object QueryLock = new();
    private readonly IEverythingExecutableLocator _executableLocator;

    public WindowsEverythingSearchClient(IEverythingExecutableLocator? executableLocator = null)
    {
        _executableLocator = executableLocator ?? new WindowsEverythingExecutableLocator();
        NativeLibraryResolver.EnsureRegistered();
    }

    public Task<IReadOnlyList<EverythingFileResult>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(query))
            return Task.FromResult<IReadOnlyList<EverythingFileResult>>([]);
        if (_executableLocator.FindExecutablePath() is null)
            return Task.FromResult<IReadOnlyList<EverythingFileResult>>([]);

        return Task.Run(
            () => SearchCore(query.Trim(), Math.Clamp(maxResults, 1, 256), cancellationToken),
            cancellationToken);
    }

    private IReadOnlyList<EverythingFileResult> SearchCore(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        lock (QueryLock)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                EverythingApi.Reset();
                EverythingApi.SetSearch(query);
                EverythingApi.SetRequestFlags(RequestFullPathAndFileName);
                EverythingApi.SetSort(SortNameAscending);
                EverythingApi.SetMax((uint)maxResults);
                EverythingApi.SetOffset(0);

                if (!EverythingApi.Query(wait: true))
                {
                    // IPC failures are treated as an unavailable optional provider. The built-in
                    // action still lets users open or install Everything themselves.
                    return [];
                }

                var count = Math.Min(EverythingApi.GetNumResults(), (uint)maxResults);
                var results = new List<EverythingFileResult>((int)count);
                for (uint index = 0; index < count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var path = GetFullPath(index);
                    if (!string.IsNullOrWhiteSpace(path))
                        results.Add(new EverythingFileResult(path, EverythingApi.IsFolderResult(index)));
                }

                return results;
            }
            catch (DllNotFoundException)
            {
                return [];
            }
            catch (EntryPointNotFoundException)
            {
                return [];
            }
            catch (BadImageFormatException)
            {
                return [];
            }
        }
    }

    private static string GetFullPath(uint index)
    {
        var buffer = new StringBuilder(1024);
        var length = EverythingApi.GetResultFullPathName(index, buffer, (uint)buffer.Capacity);
        if (length >= buffer.Capacity)
        {
            buffer.EnsureCapacity((int)length + 1);
            EverythingApi.GetResultFullPathName(index, buffer, (uint)buffer.Capacity);
        }

        return buffer.ToString();
    }

    private static class NativeLibraryResolver
    {
        private static int _registered;

        public static void EnsureRegistered()
        {
            if (Interlocked.Exchange(ref _registered, 1) == 0)
                NativeLibrary.SetDllImportResolver(typeof(WindowsEverythingSearchClient).Assembly, Resolve);
        }

        private static IntPtr Resolve(
            string libraryName,
            Assembly assembly,
            DllImportSearchPath? searchPath)
        {
            if (!libraryName.Equals("Everything64.dll", StringComparison.OrdinalIgnoreCase))
                return IntPtr.Zero;

            var executablePath = new WindowsEverythingExecutableLocator().FindExecutablePath();
            var executableDirectory = string.IsNullOrWhiteSpace(executablePath)
                ? null
                : Path.GetDirectoryName(executablePath);
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, libraryName),
                string.IsNullOrWhiteSpace(executableDirectory) ? null : Path.Combine(executableDirectory, libraryName)
            };

            foreach (var candidate in candidates)
            {
                if (candidate is not null && File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var handle))
                    return handle;
            }

            return IntPtr.Zero;
        }
    }

    private static class EverythingApi
    {
        [DllImport("Everything64.dll", EntryPoint = "Everything_SetSearchW", CharSet = CharSet.Unicode)]
        internal static extern void SetSearch(string search);

        [DllImport("Everything64.dll", EntryPoint = "Everything_SetRequestFlags")]
        internal static extern void SetRequestFlags(uint flags);

        [DllImport("Everything64.dll", EntryPoint = "Everything_SetSort")]
        internal static extern void SetSort(uint sort);

        [DllImport("Everything64.dll", EntryPoint = "Everything_SetMax")]
        internal static extern void SetMax(uint max);

        [DllImport("Everything64.dll", EntryPoint = "Everything_SetOffset")]
        internal static extern void SetOffset(uint offset);

        [DllImport("Everything64.dll", EntryPoint = "Everything_QueryW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Query([MarshalAs(UnmanagedType.Bool)] bool wait);

        [DllImport("Everything64.dll", EntryPoint = "Everything_GetNumResults")]
        internal static extern uint GetNumResults();

        [DllImport("Everything64.dll", EntryPoint = "Everything_IsFolderResult")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsFolderResult(uint index);

        [DllImport("Everything64.dll", EntryPoint = "Everything_GetResultFullPathNameW", CharSet = CharSet.Unicode)]
        internal static extern uint GetResultFullPathName(uint index, StringBuilder buffer, uint maxCount);

        [DllImport("Everything64.dll", EntryPoint = "Everything_Reset")]
        internal static extern void Reset();
    }
}
