// File: Runtime/MemoryDiagnosticsManager.cs

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace MemoryDiagnostics
{
    public readonly struct MemoryDiagSnapshot
    {
        public readonly long currentFootprintBytes;
        public readonly long peakFootprintBytes;
        public readonly float currentFootprintMB;
        public readonly float peakFootprintMB;

        public MemoryDiagSnapshot(
            long currentFootprintBytes,
            long peakFootprintBytes,
            float currentFootprintMB,
            float peakFootprintMB)
        {
            this.currentFootprintBytes = currentFootprintBytes;
            this.peakFootprintBytes = peakFootprintBytes;
            this.currentFootprintMB = currentFootprintMB;
            this.peakFootprintMB = peakFootprintMB;
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
            public static long MD_GetMemoryFootprintBytes() => MacMemoryReader.GetWorkingSetBytes();
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

        #if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        private static class MacMemoryReader
        {
            private static bool _initialized;
            private static Process _self;

            private static void EnsureInitialized()
            {
                if (_initialized) return;
                try
                {
                    _self = Process.GetCurrentProcess();
                }
                catch
                {
                    _self = null;
                }
                _initialized = true;
            }

            public static long GetWorkingSetBytes()
            {
                EnsureInitialized();
                try
                {
                    return _self != null ? _self.WorkingSet64 : 0L;
                }
                catch
                {
                    return 0L;
                }
            }

            public static void Shutdown()
            {
                if (_self != null)
                {
                    try { _self.Dispose(); } catch { }
                    _self = null;
                }
                _initialized = false;
            }
        }
        #endif

        public long CurrentFootprintBytes { get; private set; }
        public long PeakFootprintBytes { get; private set; }
        public float CurrentFootprintMB { get; private set; }
        public float PeakFootprintMB { get; private set; }
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

            CurrentFootprintBytes = currentBytes;
            if (currentBytes > PeakFootprintBytes) PeakFootprintBytes = currentBytes; // track peak locally
            CurrentFootprintMB = BytesToMB(CurrentFootprintBytes);
            PeakFootprintMB = BytesToMB(PeakFootprintBytes);

            var snap = BuildSnapshot(CurrentFootprintBytes, CurrentFootprintMB, PeakFootprintBytes);
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
            #if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            MacMemoryReader.Shutdown();
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
