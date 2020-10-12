using System;
using System.Collections.Generic;
using Unity.UIWidgets.foundation;
using UnityEngine;

namespace Unity.UIWidgets.uiOld{
    public enum TileMode : int {
        clamp = 0,
        mirror = 1,
        repeated = 2,
    }

    public abstract class PaintShader {
    }


    public class Gradient : PaintShader {
        public static Gradient linear(
            Offset start, Offset end, List<Color> colors,
            List<float> colorStops = null, TileMode tileMode = TileMode.clamp,
            uiMatrix3? matrix = null) {
            D.assert(PaintingUtils._offsetIsValid(start));
            D.assert(PaintingUtils._offsetIsValid(end));
            D.assert(colors != null && colors.Count >= 2);

            _validateColorStops(ref colors, ref colorStops);

            return new _LinearGradient(start, end, colors, colorStops, tileMode, matrix);
        }

        public static Gradient radial(
            Offset center, float radius, List<Color> colors,
            List<float> colorStops = null, TileMode tileMode = TileMode.clamp,
            uiMatrix3? matrix = null) {
            D.assert(PaintingUtils._offsetIsValid(center));
            D.assert(colors != null && colors.Count >= 2);

            _validateColorStops(ref colors, ref colorStops);

            return new _RadialGradient(center, radius, colors, colorStops, tileMode, matrix);
        }

        public static Gradient sweep(
            Offset center, List<Color> colors,
            List<float> colorStops = null, TileMode tileMode = TileMode.clamp,
            float startAngle = 0.0f, float endAngle = Mathf.PI * 2,
            uiMatrix3? matrix = null) {
            D.assert(PaintingUtils._offsetIsValid(center));
            D.assert(colors != null && colors.Count >= 2);
            D.assert(startAngle < endAngle);

            _validateColorStops(ref colors, ref colorStops);

            return new _SweepGradient(center, colors, colorStops, tileMode, startAngle, endAngle, matrix);
        }

        static void _validateColorStops(ref List<Color> colors, ref List<float> colorStops) {
            if (colorStops == null) {
                colors = new List<Color>(colors);

                colorStops = new List<float>(colors.Count);
                colorStops.Add(0);
                var stepCount = colors.Count - 1;
                var step = 1.0f / stepCount;
                for (int i = 1; i < stepCount; i++) {
                    colorStops.Add(colorStops[i - 1] + step);
                }

                colorStops.Add(1);

                return;
            }

            if (colors.Count != colorStops.Count) {
                throw new ArgumentException("\"colors\" and \"colorStops\" arguments must have equal length.");
            }

            var dummyFirst = colorStops[0] != 0;
            var dummyLast = colorStops[colorStops.Count - 1] != 1;
            var count = colors.Count + (dummyFirst ? 1 : 0) + (dummyFirst ? 1 : 0);

            var newColors = new List<Color>(count);
            if (dummyFirst) {
                newColors.Add(colors[0]);
            }

            for (int i = 0; i < colors.Count; i++) {
                newColors.Add(colors[i]);
            }

            if (dummyLast) {
                newColors.Add(colors[colors.Count - 1]);
            }

            var newColorStops = new List<float>(count);
            if (dummyFirst) {
                newColorStops.Add(0.0f);
            }

            var prevStop = 0.0f;
            for (int i = 0; i < colorStops.Count; i++) {
                var stop = Mathf.Max(Mathf.Min(colorStops[i], 1.0f), prevStop);
                newColorStops.Add(stop);
                prevStop = stop;
            }

            if (dummyLast) {
                newColorStops.Add(1.0f);
            }

            colors = newColors;
            colorStops = newColorStops;
        }

        static readonly GradientBitmapCache _cache = new GradientBitmapCache();

        internal static Image makeTexturedColorizer(List<Color> colors, List<float> positions) {
            int count = colors.Count;
            D.assert(count >= 2);

            bool bottomHardStop = ScalarUtils.ScalarNearlyEqual(positions[0], positions[1]);
            bool topHardStop =
                ScalarUtils.ScalarNearlyEqual(positions[count - 2], positions[count - 1]);

            int offset = 0;
            if (bottomHardStop) {
                offset += 1;
                count--;
            }

            if (topHardStop) {
                count--;
            }

            if (offset != 0 || count != colors.Count) {
                colors = colors.GetRange(offset, count);
                positions = positions.GetRange(offset, count);
            }

            return _cache.getGradient(colors, positions);
        }
    }

    class GradientBitmapCache : IDisposable {
        public GradientBitmapCache(int maxEntries = 32, int resolution = 256) {
            this.maxEntries = maxEntries;
            this.resolution = resolution;
            _entryCount = 0;
            _head = _tail = null;

            D.assert(validate);
        }

        public readonly int maxEntries;
        public readonly int resolution;

        int _entryCount;
        _Entry _head;
        _Entry _tail;

        public Image getGradient(List<Color> colors, List<float> positions) {
            var key = new _Key(colors, positions);

            if (!find(key, out var image)) {
                image = fillGradient(colors, positions);
                add(key, image);
            }

            return image;
        }

        public void Dispose() {
            D.assert(validate);

            // just remove the references, Image will dispose by themselves.
            _entryCount = 0;
            _head = _tail = null;
        }

        _Entry release(_Entry entry) {
            if (entry.prev != null) {
                D.assert(_head != entry);
                entry.prev.next = entry.next;
            }
            else {
                D.assert(_head == entry);
                _head = entry.next;
            }

            if (entry.next != null) {
                D.assert(_tail != entry);
                entry.next.prev = entry.prev;
            }
            else {
                D.assert(_tail == entry);
                _tail = entry.prev;
            }

            return entry;
        }

        void attachToHead(_Entry entry) {
            entry.prev = null;
            entry.next = _head;
            if (_head != null) {
                _head.prev = entry;
            }
            else {
                _tail = entry;
            }

            _head = entry;
        }

        bool find(_Key key, out Image image) {
            D.assert(validate);

            var entry = _head;
            while (entry != null) {
                if (entry.key == key) {
                    image = entry.image;

                    // move to the head of our list, so we purge it last
                    release(entry);
                    attachToHead(entry);
                    D.assert(validate);
                    return true;
                }

                entry = entry.next;
            }

            D.assert(validate);
            image = null;
            return false;
        }

        void add(_Key key, Image image) {
            if (_entryCount == maxEntries) {
                D.assert(_tail != null);
                release(_tail);
                _entryCount--;
            }

            var entry = new _Entry {key = key, image = image};
            attachToHead(entry);
            _entryCount++;
        }

        Image fillGradient(List<Color> colors, List<float> positions) {
            Texture2D tex = new Texture2D(resolution, 1, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;

            var bytes = new byte[resolution * 4];

            int count = colors.Count;
            int prevIndex = 0;
            for (int i = 1; i < count; i++) {
                // Historically, stops have been mapped to [0, 256], with 256 then nudged to the next
                // smaller value, then truncate for the texture index. This seems to produce the best
                // results for some common distributions, so we preserve the behavior.
                int nextIndex = (int) Mathf.Min(positions[i] * resolution, resolution - 1);

                if (nextIndex > prevIndex) {
                    var c0 = colors[i - 1];
                    var c1 = colors[i];

                    var step = 1.0f / (nextIndex - prevIndex);
                    var t = 0.0f;

                    for (int curIndex = prevIndex; curIndex <= nextIndex; ++curIndex) {
                        var c = Color.lerp(c0, c1, t);

                        var baseIndex = curIndex << 2;
                        bytes[baseIndex] = (byte) c.red;
                        bytes[baseIndex + 1] = (byte) c.green;
                        bytes[baseIndex + 2] = (byte) c.blue;
                        bytes[baseIndex + 3] = (byte) c.alpha;

                        t += step;
                    }
                }

                prevIndex = nextIndex;
            }

            D.assert(prevIndex == resolution - 1);

            tex.LoadRawTextureData(bytes);
            tex.Apply();
            return new Image(tex);
        }

        bool validate() {
            D.assert(_entryCount >= 0 && _entryCount <= maxEntries);

            if (_entryCount > 0) {
                D.assert(null == _head.prev);
                D.assert(null == _tail.next);

                if (_entryCount == 1) {
                    D.assert(_head == _tail);
                }
                else {
                    D.assert(_head != _tail);
                }

                var entry = _head;
                int count = 0;
                while (entry != null) {
                    count += 1;
                    entry = entry.next;
                }

                D.assert(count == _entryCount);

                entry = _tail;
                while (entry != null) {
                    count -= 1;
                    entry = entry.prev;
                }

                D.assert(0 == count);
            }
            else {
                D.assert(null == _head);
                D.assert(null == _tail);
            }

            return true;
        }

        class _Entry {
            public _Entry prev;
            public _Entry next;

            public _Key key;
            public Image image;
        }

        class _Key : IEquatable<_Key> {
            public _Key(List<Color> colors, List<float> positions) {
                D.assert(colors != null);
                D.assert(positions != null);
                D.assert(colors.Count == positions.Count);

                this.colors = colors;
                this.positions = positions;
                _hashCode = _getHashCode(this.colors) ^ _getHashCode(this.positions);
                ;
            }

            public readonly List<Color> colors;

            public readonly List<float> positions;

            readonly int _hashCode;

            public bool Equals(_Key other) {
                if (ReferenceEquals(null, other)) {
                    return false;
                }

                if (ReferenceEquals(this, other)) {
                    return true;
                }

                return colors.equalsList(other.colors) &&
                       positions.equalsList(other.positions);
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

                return Equals((_Key) obj);
            }

            public override int GetHashCode() {
                return _hashCode;
            }

            public static bool operator ==(_Key left, _Key right) {
                return Equals(left, right);
            }

            public static bool operator !=(_Key left, _Key right) {
                return !Equals(left, right);
            }

            static int _getHashCode<T>(List<T> list) {
                unchecked {
                    var hashCode = 0;
                    foreach (var item in list) {
                        hashCode = (hashCode * 397) ^ item.GetHashCode();
                    }

                    return hashCode;
                }
            }
        }
    }


    class _LinearGradient : Gradient {
        public _LinearGradient(
            Offset start, Offset end, List<Color> colors,
            List<float> colorStops, TileMode tileMode,
            uiMatrix3? matrix = null) {
            this.start = start;
            this.end = end;
            this.colors = colors;
            this.colorStops = colorStops;
            this.tileMode = tileMode;
            this.matrix = matrix;
            ptsToUnit = ptsToUnitMatrix(start, end);
            gradientTex = makeTexturedColorizer(colors, colorStops);
        }

        public readonly Offset start;
        public readonly Offset end;
        public readonly List<Color> colors;
        public readonly List<float> colorStops;
        public readonly TileMode tileMode;
        public readonly uiMatrix3? matrix;
        public readonly uiMatrix3 ptsToUnit;
        public readonly Image gradientTex;

        public Color leftColor {
            get { return colors[0]; }
        }

        public Color rightColor {
            get { return colors[colors.Count - 1]; }
        }

        public uiMatrix3 getGradientMat(uiMatrix3 mat) {
            if (matrix != null) {
                mat.postConcat(matrix.Value);
            }

            mat.postConcat(ptsToUnit);
            return mat;
        }

        static uiMatrix3 ptsToUnitMatrix(Offset start, Offset end) {
            var vec = end - start;
            var mag = vec.distance;
            var inv = mag != 0 ? 1 / mag : 0;
            vec = vec.scale(inv);

            var matrix = uiMatrix3.I();
            matrix.setSinCos(-vec.dy, vec.dx, start.dx, start.dy);
            matrix.postTranslate(-start.dx, -start.dy);
            matrix.postScale(inv, inv);
            return matrix;
        }
    }

    class _RadialGradient : Gradient {
        public _RadialGradient(
            Offset center, float radius, List<Color> colors,
            List<float> colorStops = null, TileMode tileMode = TileMode.clamp,
            uiMatrix3? matrix = null
        ) {
            this.center = center;
            this.radius = radius;
            this.colors = colors;
            this.colorStops = colorStops;
            this.tileMode = tileMode;
            this.matrix = matrix;
            ptsToUnit = radToUnitMatrix(center, radius);
            gradientTex = makeTexturedColorizer(colors, colorStops);
        }

        public readonly Offset center;
        public readonly float radius;
        public readonly List<Color> colors;
        public readonly List<float> colorStops;
        public readonly TileMode tileMode;
        public readonly uiMatrix3? matrix;
        public readonly uiMatrix3 ptsToUnit;
        public readonly Image gradientTex;

        public Color leftColor {
            get { return colors[0]; }
        }

        public Color rightColor {
            get { return colors[colors.Count - 1]; }
        }

        public uiMatrix3 getGradientMat(uiMatrix3 mat) {
            if (matrix != null) {
                mat.postConcat(matrix.Value);
            }

            mat.postConcat(ptsToUnit);
            return mat;
        }

        static uiMatrix3 radToUnitMatrix(Offset center, float radius) {
            var inv = radius != 0 ? 1 / radius : 0;

            var matrix = uiMatrix3.I();
            matrix.setTranslate(-center.dx, -center.dy);
            matrix.postScale(inv, inv);
            return matrix;
        }
    }

    class _SweepGradient : Gradient {
        public _SweepGradient(
            Offset center, List<Color> colors,
            List<float> colorStops = null, TileMode tileMode = TileMode.clamp,
            float startAngle = 0.0f, float endAngle = Mathf.PI * 2,
            uiMatrix3? matrix = null
        ) {
            this.center = center;
            this.colors = colors;
            this.colorStops = colorStops;
            this.tileMode = tileMode;
            this.startAngle = startAngle;
            this.endAngle = endAngle;
            this.matrix = matrix;

            var t0 = startAngle / (Mathf.PI * 2f);
            var t1 = endAngle / (Mathf.PI * 2f);
            bias = -t0;
            scale = 1f / (t1 - t0);

            var ptsToUnit = uiMatrix3.I();
            ptsToUnit.setTranslate(-center.dx, -center.dy);
            this.ptsToUnit = ptsToUnit;

            gradientTex = makeTexturedColorizer(colors, colorStops);
        }

        public readonly Offset center;
        public readonly List<Color> colors;
        public readonly List<float> colorStops;
        public readonly TileMode tileMode;
        public readonly float startAngle;
        public readonly float endAngle;
        public readonly uiMatrix3? matrix;
        public readonly uiMatrix3 ptsToUnit;
        public readonly Image gradientTex;
        public readonly float bias;
        public readonly float scale;

        public Color leftColor {
            get { return colors[0]; }
        }

        public Color rightColor {
            get { return colors[colors.Count - 1]; }
        }

        public uiMatrix3 getGradientMat(uiMatrix3 mat) {
            if (matrix != null) {
                mat.postConcat(matrix.Value);
            }

            mat.postConcat(ptsToUnit);
            return mat;
        }
    }

    public class ImageShader : PaintShader {
        public ImageShader(Image image,
            TileMode tileMode = TileMode.clamp, uiMatrix3? matrix = null) {
            this.image = image;
            this.tileMode = tileMode;
            this.matrix = matrix;
        }

        public readonly Image image;
        public readonly TileMode tileMode;
        public readonly uiMatrix3? matrix;

        public uiMatrix3 getShaderMat(uiMatrix3 mat) {
            if (matrix != null) {
                mat.postConcat(matrix.Value);
            }

            mat.postScale(1f / image.width, 1f / image.height);
            return mat;
        }
    }
}