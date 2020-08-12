﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.ui;

namespace Unity.UIWidgets.ui2 {
    public delegate void VoidCallback();

    public delegate void FrameCallback(TimeSpan duration);

    public delegate void TimingsCallback(List<FrameTiming> timings);

    public delegate void PointerDataPacketCallback(PointerDataPacket packet);

    public unsafe delegate void PlatformMessageResponseCallback(byte* data, int dataLength);

    public unsafe delegate void PlatformMessageCallback(
        [MarshalAs(UnmanagedType.LPStr)] string name, byte* data, int dataLength,
        PlatformMessageResponseCallback callback);

    delegate void _SetNeedsReportTimingsFunc(IntPtr ptr, bool value);

    public enum FramePhase {
        buildStart,
        buildFinish,
        rasterStart,
        rasterFinish,
    }

    public class FrameTiming {
        public FrameTiming(List<long> timestamps) {
            D.assert(timestamps.Count == Enum.GetNames(typeof(FramePhase)).Length);
            _timestamps = timestamps;
        }

        public long timestampInMicroseconds(FramePhase phase) => _timestamps[(int) phase];

        TimeSpan _rawDuration(FramePhase phase) => TimeSpan.FromMilliseconds(_timestamps[(int) phase] / 1000.0);

        public TimeSpan buildDuration => _rawDuration(FramePhase.buildFinish) - _rawDuration(FramePhase.buildStart);

        public TimeSpan rasterDuration => _rawDuration(FramePhase.rasterFinish) - _rawDuration(FramePhase.rasterStart);

        public TimeSpan totalSpan => _rawDuration(FramePhase.rasterFinish) - _rawDuration(FramePhase.buildStart);

        List<long> _timestamps; // in microseconds

        string _formatMS(TimeSpan duration) => $"{duration.Milliseconds}ms";

        public override string ToString() {
            return
                $"{GetType()}(buildDuration: {_formatMS(buildDuration)}, rasterDuration: {_formatMS(rasterDuration)}, totalSpan: {_formatMS(totalSpan)})";
        }
    }

    public enum AppLifecycleState {
        resumed,
        inactive,
        paused,
        detached,
    }

    public class WindowPadding {
        internal WindowPadding(float left, float top, float right, float bottom) {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        public readonly float left;

        public readonly float top;

        public readonly float right;

        public readonly float bottom;

        public static readonly WindowPadding zero = new WindowPadding(left: 0.0f, top: 0.0f, right: 0.0f, bottom: 0.0f);

        public override string ToString() {
            return $"{GetType()}(left: {left}, top: {top}, right: {right}, bottom: {bottom})";
        }
    }

    public class Window {
        internal IntPtr _ptr;

        internal Window() {
            _setNeedsReportTimings = Window_setNeedsReportTimings;
        }

        public static Window instance {
            get {
                GCHandle gcHandle = (GCHandle) Window_instance();
                return (Window) gcHandle.Target;
            }
        }

        public float devicePixelRatio { get; internal set; } = 1.0f;

        public Size physicalSize { get; internal set; } = Size.zero;

        public float physicalDepth { get; internal set; } = float.MaxValue;

        public WindowPadding viewInsets { get; internal set; } = WindowPadding.zero;

        public WindowPadding viewPadding { get; internal set; } = WindowPadding.zero;

        public WindowPadding systemGestureInsets { get; internal set; } = WindowPadding.zero;

        public WindowPadding padding { get; internal set; } = WindowPadding.zero;

        public VoidCallback onMetricsChanged { get; set; }

        public string initialLifecycleState {
            get {
                _initialLifecycleStateAccessed = true;
                return _initialLifecycleState;
            }
        }

        string _initialLifecycleState;
        bool _initialLifecycleStateAccessed = false;
        public float textScaleFactor { get; internal set; } = 1.0f;

        public VoidCallback onTextScaleFactorChanged { get; set; }

        public bool alwaysUse24HourFormat { get; internal set; } = false;

        public Brightness platformBrightness { get; internal set; } = Brightness.light;

        public VoidCallback onPlatformBrightnessChanged { get; set; }

        public FrameCallback onBeginFrame { get; set; }

        public VoidCallback onDrawFrame { get; set; }

        TimingsCallback _onReportTimings;
        _SetNeedsReportTimingsFunc _setNeedsReportTimings;

        public TimingsCallback onReportTimings {
            get { return _onReportTimings; }
            set {
                if ((value == null) != (_onReportTimings == null)) {
                    _setNeedsReportTimings(_ptr, value != null);
                }

                _onReportTimings = value;
            }
        }

        public PointerDataPacketCallback onPointerDataPacket { get; set; }

        public string defaultRouteName {
            get {
                IntPtr routeNamePtr = Window_defaultRouteName(_ptr);
                string routeName = Marshal.PtrToStringAnsi(routeNamePtr);
                Window_freeDefaultRouteName(routeNamePtr);
                return routeName;
            }
        }

        public void scheduleFrame() {
            Window_scheduleFrame(_ptr);
        }

        public void render(Scene scene) {
            Window_render(_ptr, scene._ptr);
        }

        public AccessibilityFeatures accessibilityFeatures { get; internal set; } = AccessibilityFeatures.zero;

        public VoidCallback onAccessibilityFeaturesChanged { get; set; }

        public unsafe void sendPlatformMessage(string name,
            byte* data, int dataLength,
            PlatformMessageResponseCallback callback) {
            IntPtr errorPtr =
                Window_sendPlatformMessage(_ptr, name, callback, data, dataLength);
            if (errorPtr != IntPtr.Zero)
                throw new Exception(Marshal.PtrToStringAnsi(errorPtr));
        }

        public PlatformMessageCallback onPlatformMessage { get; set; }

        unsafe void _respondToPlatformMessage(int responseId, byte* data, int dataLength) {
            Window_respondToPlatformMessage(_ptr, responseId, data, dataLength);
        }

        [DllImport(NativeBindings.dllName)]
        static extern IntPtr Window_instance();

        [DllImport(NativeBindings.dllName)]
        static extern void Window_setNeedsReportTimings(IntPtr ptr, bool value);

        [DllImport(NativeBindings.dllName)]
        static extern IntPtr Window_defaultRouteName(IntPtr ptr);

        [DllImport(NativeBindings.dllName)]
        static extern void Window_freeDefaultRouteName(IntPtr routeNamePtr);

        [DllImport(NativeBindings.dllName)]
        static extern void Window_scheduleFrame(IntPtr ptr);

        [DllImport(NativeBindings.dllName)]
        static extern void Window_render(IntPtr ptr, IntPtr scene);

        [DllImport(NativeBindings.dllName)]
        static extern unsafe IntPtr Window_sendPlatformMessage(IntPtr ptr, string name,
            PlatformMessageResponseCallback callback,
            byte* data, int dataLength);

        [DllImport(NativeBindings.dllName)]
        static extern unsafe void Window_respondToPlatformMessage(IntPtr ptr, int responseId,
            byte* data, int dataLength);
    }

    public class AccessibilityFeatures : IEquatable<AccessibilityFeatures> {
        internal AccessibilityFeatures(int index) {
            _index = index;
        }

        const int _kAccessibleNavigation = 1 << 0;
        const int _kInvertColorsIndex = 1 << 1;
        const int _kDisableAnimationsIndex = 1 << 2;
        const int _kBoldTextIndex = 1 << 3;
        const int _kReduceMotionIndex = 1 << 4;
        const int _kHighContrastIndex = 1 << 5;

        readonly int _index;

        public static readonly AccessibilityFeatures zero = new AccessibilityFeatures(0);

        public bool accessibleNavigation => (_kAccessibleNavigation & _index) != 0;

        public bool invertColors => (_kInvertColorsIndex & _index) != 0;

        public bool disableAnimations => (_kDisableAnimationsIndex & _index) != 0;

        public bool boldText => (_kBoldTextIndex & _index) != 0;

        public bool reduceMotion => (_kReduceMotionIndex & _index) != 0;

        public bool highContrast => (_kHighContrastIndex & _index) != 0;

        public override string ToString() {
            List<String> features = new List<String>();
            if (accessibleNavigation)
                features.Add("accessibleNavigation");
            if (invertColors)
                features.Add("invertColors");
            if (disableAnimations)
                features.Add("disableAnimations");
            if (boldText)
                features.Add("boldText");
            if (reduceMotion)
                features.Add("reduceMotion");
            if (highContrast)
                features.Add("highContrast");
            return $"AccessibilityFeatures{features}";
        }

        public bool Equals(AccessibilityFeatures other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            return _index == other._index;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj.GetType() != GetType()) {
                return false;
            }

            return Equals((AccessibilityFeatures) obj);
        }

        public override int GetHashCode() {
            return _index;
        }

        public static bool operator ==(AccessibilityFeatures left, AccessibilityFeatures right) {
            return Equals(left, right);
        }

        public static bool operator !=(AccessibilityFeatures left, AccessibilityFeatures right) {
            return !Equals(left, right);
        }
    }

    public enum Brightness {
        dark,
        light,
    }
}