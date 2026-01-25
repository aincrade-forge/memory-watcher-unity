// File: Runtime/MemoryDiagnosticsManager.cs

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace MemoryDiagnostics
{
    public readonly struct MemoryDiagSnapshot
    {
        public readonly long currentMemoryBytes;
        public readonly long peakMemoryBytes;
        public readonly float currentMemoryMB;
        public readonly float peakMemoryMB;

        public MemoryDiagSnapshot(
            long currentMemoryBytes,
            long peakMemoryBytes,
            float currentMemoryMB,
            float peakMemoryMB)
        {
            this.currentMemoryBytes = currentMemoryBytes;
            this.peakMemoryBytes = peakMemoryBytes;
            this.currentMemoryMB = currentMemoryMB;
            this.peakMemoryMB = peakMemoryMB;
        }
    }

    public sealed class MemoryDiagnosticsManager
    {
        private float _sampleIntervalSeconds = 1.0f;

        private static MemoryDiagnosticsManager _instance;
        public static MemoryDiagnosticsManager Instance => _instance;
        public static bool IsInitialized => _instance != null;

        private static class Native
        {
            #if UNITY_IOS && !UNITY_EDITOR
            private const string DLL = "__Internal";
            [DllImport(DLL)] public static extern long MD_GetMemoryFootprintBytes();
            #elif UNITY_ANDROID && !UNITY_EDITOR
            public static long MD_GetMemoryFootprintBytes() => AndroidMemoryReader.GetPssBytes();
            #elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
            private const string DLL = "__Internal";
            [DllImport(DLL)] public static extern long MD_GetMemoryFootprintBytes();
            #elif UNITY_STANDALONE_WIN && !UNITY_EDITOR
            public static long MD_GetMemoryFootprintBytes() => WindowsMemoryReader.GetWorkingSetBytes();
            #else
            public static long MD_GetMemoryFootprintBytes() => 0L;
            #endif
        }

        #if UNITY_ANDROID && !UNITY_EDITOR
        private static class AndroidMemoryReader
        {
            private static bool _initialized;
            private static IntPtr _debugClass;
            private static IntPtr _getPssMethod;
            private static readonly jvalue[] _emptyArgs = new jvalue[0];

            private static void EnsureInitialized()
            {
                if (_initialized) return;
                try
                {
                    var localClass = AndroidJNI.FindClass("android/os/Debug");
                    _debugClass = AndroidJNI.NewGlobalRef(localClass);
                    AndroidJNI.DeleteLocalRef(localClass);
                    _getPssMethod = AndroidJNI.GetStaticMethodID(_debugClass, "getPss", "()J");
                }
                catch
                {
                    _debugClass = IntPtr.Zero;
                    _getPssMethod = IntPtr.Zero;
                }
                _initialized = true;
            }

            public static long GetPssBytes()
            {
                EnsureInitialized();
                if (_debugClass == IntPtr.Zero || _getPssMethod == IntPtr.Zero) return 0L;
                try
                {
                    long pssKb = AndroidJNI.CallStaticLongMethod(_debugClass, _getPssMethod, _emptyArgs);
                    if (pssKb < 0) return 0L;
                    return pssKb * 1024L;
                }
                catch
                {
                    return 0L;
                }
            }

            public static void Shutdown()
            {
                if (_debugClass != IntPtr.Zero)
                {
                    AndroidJNI.DeleteGlobalRef(_debugClass);
                    _debugClass = IntPtr.Zero;
                }
                _getPssMethod = IntPtr.Zero;
                _initialized = false;
            }
        }
        #endif

        #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private static class WindowsMemoryReader
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct PROCESS_MEMORY_COUNTERS
            {
                public uint cb;
                public uint PageFaultCount;
                public ulong PeakWorkingSetSize;
                public ulong WorkingSetSize;
                public ulong QuotaPeakPagedPoolUsage;
                public ulong QuotaPagedPoolUsage;
                public ulong QuotaPeakNonPagedPoolUsage;
                public ulong QuotaNonPagedPoolUsage;
                public ulong PagefileUsage;
                public ulong PeakPagefileUsage;
            }

            [DllImport("kernel32.dll")]
            private static extern IntPtr GetCurrentProcess();

            [DllImport("psapi.dll", SetLastError = true)]
            private static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS counters, uint size);

            public static long GetWorkingSetBytes()
            {
                try
                {
                    var process = GetCurrentProcess();
                    if (process == IntPtr.Zero) return 0L;
                    var counters = new PROCESS_MEMORY_COUNTERS { cb = (uint)Marshal.SizeOf<PROCESS_MEMORY_COUNTERS>() };
                    if (!GetProcessMemoryInfo(process, out counters, counters.cb)) return 0L;
                    return (long)counters.WorkingSetSize;
                }
                catch
                {
                    return 0L;
                }
            }
        }
        #endif

        public long CurrentMemoryBytes { get; private set; }
        public long PeakMemoryBytes { get; private set; }
        public float CurrentMemoryMB { get; private set; }
        public float PeakMemoryMB { get; private set; }
        public MemoryDiagSnapshot LatestSnapshot { get; private set; }

        public event Action<MemoryDiagSnapshot> OnSample;

        private float _nextSampleTime;

        private static bool _playerLoopInstalled;
        private static readonly PlayerLoopSystem.UpdateFunction _playerLoopTick = TickStatic;

        private sealed class PlayerLoopHook { }

        private void Tick()
        {
            if (Time.unscaledTime < _nextSampleTime) return;
            _nextSampleTime = Time.unscaledTime + _sampleIntervalSeconds;

            long currentBytes = 0;
            try
            {
                currentBytes = Native.MD_GetMemoryFootprintBytes();
            }
            catch { currentBytes = 0; }

            CurrentMemoryBytes = currentBytes;
            if (currentBytes > PeakMemoryBytes) PeakMemoryBytes = currentBytes; // track peak locally
            CurrentMemoryMB = BytesToMB(CurrentMemoryBytes);
            PeakMemoryMB = BytesToMB(PeakMemoryBytes);

            var snap = BuildSnapshot(CurrentMemoryBytes, CurrentMemoryMB, PeakMemoryBytes);
            LatestSnapshot = snap;

            SafeInvokeEvent(OnSample, snap);
        }

        private MemoryDiagSnapshot BuildSnapshot(long currentBytes, float currentMB, long peakBytes)
        {
            return new MemoryDiagSnapshot(
                currentBytes,
                peakBytes,
                currentMB,
                BytesToMB(peakBytes));
        }

        private static float BytesToMB(long bytes) => (float)((double)bytes / (1024.0 * 1024.0));
        private static void SafeInvokeEvent(Action<MemoryDiagSnapshot> evt, MemoryDiagSnapshot snap) { try { evt?.Invoke(snap); } catch { } }

        public static MemoryDiagnosticsManager Initialize(float? sampleIntervalSeconds = null)
        {
            if (_instance == null)
            {
                _instance = new MemoryDiagnosticsManager();
            }
            if (sampleIntervalSeconds.HasValue)
            {
                try { _instance.SetSampleInterval(sampleIntervalSeconds.Value); } catch { }
            }
            InstallPlayerLoop();
            return _instance;
        }

        public void SetSampleInterval(float seconds)
        {
            try
            {
                _sampleIntervalSeconds = Mathf.Max(0.05f, seconds);
            }
            catch { }
        }

        public static void Shutdown()
        {
            RemovePlayerLoop();
            #if UNITY_ANDROID && !UNITY_EDITOR
            AndroidMemoryReader.Shutdown();
            #endif
        }

        private static void TickStatic()
        {
            _instance?.Tick();
        }

        private static void InstallPlayerLoop()
        {
            if (_playerLoopInstalled) return;
            var root = PlayerLoop.GetCurrentPlayerLoop();
            var hookSystem = new PlayerLoopSystem
            {
                type = typeof(PlayerLoopHook),
                updateDelegate = _playerLoopTick
            };
            if (InsertSystem(ref root, typeof(Update), hookSystem))
            {
                PlayerLoop.SetPlayerLoop(root);
                _playerLoopInstalled = true;
            }
        }

        private static void RemovePlayerLoop()
        {
            if (!_playerLoopInstalled) return;
            var root = PlayerLoop.GetCurrentPlayerLoop();
            if (RemoveSystem(ref root, typeof(PlayerLoopHook)))
            {
                PlayerLoop.SetPlayerLoop(root);
            }
            _playerLoopInstalled = false;
        }

        private static bool InsertSystem(ref PlayerLoopSystem system, Type targetType, PlayerLoopSystem newSystem)
        {
            if (system.type == targetType)
            {
                var list = system.subSystemList != null ? new System.Collections.Generic.List<PlayerLoopSystem>(system.subSystemList) : new System.Collections.Generic.List<PlayerLoopSystem>();
                list.Add(newSystem);
                system.subSystemList = list.ToArray();
                return true;
            }

            if (system.subSystemList == null) return false;
            for (int i = 0; i < system.subSystemList.Length; i++)
            {
                var sub = system.subSystemList[i];
                if (InsertSystem(ref sub, targetType, newSystem))
                {
                    system.subSystemList[i] = sub;
                    return true;
                }
            }
            return false;
        }

        private static bool RemoveSystem(ref PlayerLoopSystem system, Type removeType)
        {
            if (system.subSystemList == null) return false;
            var list = new System.Collections.Generic.List<PlayerLoopSystem>(system.subSystemList);
            var removed = list.RemoveAll(s => s.type == removeType);
            for (int i = 0; i < list.Count; i++)
            {
                var sub = list[i];
                if (RemoveSystem(ref sub, removeType))
                {
                    list[i] = sub;
                }
            }
            if (removed > 0)
            {
                system.subSystemList = list.ToArray();
                return true;
            }
            return false;
        }
    }
}
