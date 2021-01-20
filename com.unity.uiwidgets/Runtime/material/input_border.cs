using System.Runtime.CompilerServices;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.ui;
using UnityEngine;
using Canvas = Unity.UIWidgets.ui.Canvas;
using Rect = Unity.UIWidgets.ui.Rect;

namespace Unity.UIWidgets.material {
    public abstract class InputBorder : ShapeBorder {
        public InputBorder(
            BorderSide borderSide = null
        ) {
            this.borderSide = borderSide ?? BorderSide.none;
        }

        public static readonly InputBorder none = new _NoInputBorder();

        public readonly BorderSide borderSide;

        public abstract InputBorder copyWith(BorderSide borderSide = null);

        public virtual bool isOutline { get; }

        public abstract void paint(Canvas canvas, Rect rect,
            float gapStart,
            float gapExtent = 0.0f,
            float gapPercentage = 0.0f
        );

        public override void paint(Canvas canvas, Rect rect, TextDirection? textDirection) {
            paint(canvas, rect, 0.0f);
        }
    }

    class _NoInputBorder : InputBorder {
        public _NoInputBorder() : base(borderSide: BorderSide.none) {
        }

        public override InputBorder copyWith(BorderSide borderSide) {
            return new _NoInputBorder();
        }

        public override bool isOutline {
            get { return false; }
        }

        public override EdgeInsetsGeometry dimensions {
            get { return EdgeInsets.zero; }
        }

        public override ShapeBorder scale(float t) {
            return new _NoInputBorder();
        }

        public override Path getInnerPath(Rect rect, TextDirection? textDirection) {
            Path path = new Path();
            path.addRect(rect);
            return path;
        }

        public override void paint(Canvas canvas, Rect rect, TextDirection? textDirection) {
            throw new System.NotImplementedException();
        }

        public override Path getOuterPath(Rect rect, TextDirection? textDirection) {
            Path path = new Path();
            path.addRect(rect);
            return path;
        }

        public override void paint(Canvas canvas, Rect rect,
            float gapStart,
            float gapExtent = 0.0f,
            float gapPercentage = 0.0f
        ) {
        }
    }

    public class UnderlineInputBorder : InputBorder {
        public UnderlineInputBorder(
            BorderSide borderSide = null,
            BorderRadius borderRadius = null
        ) : base(borderSide: borderSide ?? new BorderSide()) {
            this.borderRadius = borderRadius ?? BorderRadius.only(
                                    topLeft: Radius.circular(4.0f),
                                    topRight: Radius.circular(4.0f)
                                );
        }

        public readonly BorderRadius borderRadius;

        public override bool isOutline {
            get { return false; }
        }

        public UnderlineInputBorder copyWith(BorderSide borderSide = null, BorderRadius borderRadius = null) {
            return new UnderlineInputBorder(
                borderSide: borderSide ?? this.borderSide,
                borderRadius: borderRadius ?? this.borderRadius
            );
        }

        public override InputBorder copyWith(BorderSide borderSide = null) {
            return new UnderlineInputBorder(
                borderSide: borderSide ?? this.borderSide
            );
        }

        public override EdgeInsetsGeometry dimensions {
            get { return EdgeInsets.only(bottom: borderSide.width); }
        }

        public override ShapeBorder scale(float t) {
            return new UnderlineInputBorder(borderSide: borderSide.scale(t));
        }

        public override Path getInnerPath(Rect rect, TextDirection? textDirection) {
            Path path = new Path();
            path.addRect(Rect.fromLTWH(rect.left, rect.top, rect.width,
                Mathf.Max(0.0f, rect.height - borderSide.width)));
            return path;
        }

        public override Path getOuterPath(Rect rect, TextDirection? textDirection) {
            Path path = new Path();
            path.addRRect(borderRadius.toRRect(rect));
            return path;
        }

        public override ShapeBorder lerpFrom(ShapeBorder a, float t) {
            if (a is UnderlineInputBorder) {
                return new UnderlineInputBorder(
                    borderSide: BorderSide.lerp((a as UnderlineInputBorder).borderSide, borderSide, t),
                    borderRadius: BorderRadius.lerp((a as UnderlineInputBorder).borderRadius, borderRadius, t)
                );
            }

            return base.lerpFrom(a, t);
        }

        public override ShapeBorder lerpTo(ShapeBorder b, float t) {
            if (b is UnderlineInputBorder) {
                return new UnderlineInputBorder(
                    borderSide: BorderSide.lerp(borderSide, (b as UnderlineInputBorder).borderSide, t),
                    borderRadius: BorderRadius.lerp(borderRadius, (b as UnderlineInputBorder).borderRadius, t)
                );
            }

            return base.lerpTo(b, t);
        }

        public override void paint(Canvas canvas, Rect rect,
            float gapStart,
            float gapExtent = 0.0f,
            float gapPercentage = 0.0f
        ) {
            if (borderRadius.bottomLeft != Radius.zero || borderRadius.bottomRight != Radius.zero) {
                canvas.clipPath(getOuterPath(rect, null));
            }

            canvas.drawLine(rect.bottomLeft, rect.bottomRight, borderSide.toPaint());
        }

        public static bool operator ==(UnderlineInputBorder left, UnderlineInputBorder right) {
            return left.Equals(right);
        }

        public static bool operator !=(UnderlineInputBorder left, UnderlineInputBorder right) {
            return !left.Equals(right);
        }

        public bool Equals(UnderlineInputBorder other) {
            return borderSide == other.borderSide;
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
            return Equals((UnderlineInputBorder) obj);
        }

        public override int GetHashCode() {
            return borderSide.GetHashCode();
        }
    }

    public class OutlineInputBorder : InputBorder {
        public OutlineInputBorder(
            BorderSide borderSide = null,
            BorderRadius borderRadius = null,
            float gapPadding = 4.0f
        ) : base(borderSide: borderSide ?? new BorderSide()) {
            D.assert(gapPadding >= 0.0f);
            this.gapPadding = gapPadding;
            this.borderRadius = borderRadius ?? BorderRadius.all(Radius.circular(4.0f));
        }

        static bool _cornersAreCircular(BorderRadius borderRadius) {
            return borderRadius.topLeft.x == borderRadius.topLeft.y
                   && borderRadius.bottomLeft.x == borderRadius.bottomLeft.y
                   && borderRadius.topRight.x == borderRadius.topRight.y
                   && borderRadius.bottomRight.x == borderRadius.bottomRight.y;
        }

        public readonly float gapPadding;

        public readonly BorderRadius borderRadius;

        public override bool isOutline {
            get { return true; }
        }

        public OutlineInputBorder copyWith(
            BorderSide borderSide,
            BorderRadius borderRadius,
            float? gapPadding
        ) {
            return new OutlineInputBorder(
                borderSide: borderSide ?? this.borderSide,
                borderRadius: borderRadius ?? this.borderRadius,
                gapPadding: gapPadding ?? this.gapPadding
            );
        }

        public override InputBorder copyWith(BorderSide borderSide) {
            return new OutlineInputBorder(
                borderSide: borderSide ?? this.borderSide,
                borderRadius: borderRadius,
                gapPadding: gapPadding
            );
        }

        public override EdgeInsetsGeometry dimensions {
            get { return EdgeInsets.all(borderSide.width); }
        }

        public override ShapeBorder scale(float t) {
            return new OutlineInputBorder(
                borderSide: borderSide.scale(t),
                borderRadius: (BorderRadius)(borderRadius * t),
                gapPadding: gapPadding * t
            );
        }

        public override ShapeBorder lerpFrom(ShapeBorder a, float t) {
            if (a is OutlineInputBorder) {
                OutlineInputBorder outline = a as OutlineInputBorder;
                return new OutlineInputBorder(
                    borderRadius: BorderRadius.lerp(outline.borderRadius, borderRadius, t),
                    borderSide: BorderSide.lerp(outline.borderSide, borderSide, t),
                    gapPadding: outline.gapPadding
                );
            }

            return base.lerpFrom(a, t);
        }

        public override ShapeBorder lerpTo(ShapeBorder b, float t) {
            if (b is OutlineInputBorder) {
                OutlineInputBorder outline = b as OutlineInputBorder;
                return new OutlineInputBorder(
                    borderRadius: BorderRadius.lerp(borderRadius, outline.borderRadius, t),
                    borderSide: BorderSide.lerp(borderSide, outline.borderSide, t),
                    gapPadding: outline.gapPadding
                );
            }

            return base.lerpTo(b, t);
        }

        public override Path getInnerPath(Rect rect, TextDirection? textDirection) {
            Path path = new Path();
            path.addRRect(borderRadius.toRRect(rect).deflate(borderSide.width));
            return path;
        }

        public override Path getOuterPath(Rect rect, TextDirection? textDirection) {
            Path path = new Path();
            path.addRRect(borderRadius.toRRect(rect));
            return path;
        }

        Path _gapBorderPath(Canvas canvas, RRect center, float start, float extent) {
            Rect tlCorner = Rect.fromLTWH(
                center.left,
                center.top,
                center.tlRadiusX * 2.0f,
                center.tlRadiusY * 2.0f
            );
            Rect trCorner = Rect.fromLTWH(
                center.right - center.trRadiusX * 2.0f,
                center.top,
                center.trRadiusX * 2.0f,
                center.trRadiusY * 2.0f
            );
            Rect brCorner = Rect.fromLTWH(
                center.right - center.brRadiusX * 2.0f,
                center.bottom - center.brRadiusY * 2.0f,
                center.brRadiusX * 2.0f,
                center.brRadiusY * 2.0f
            );
            Rect blCorner = Rect.fromLTWH(
                center.left,
                center.bottom - center.brRadiusY * 2.0f,
                center.blRadiusX * 2.0f,
                center.blRadiusY * 2.0f
            );

            const float cornerArcSweep = Mathf.PI / 2.0f;
            float tlCornerArcSweep = start < center.tlRadiusX
                ? Mathf.Asin((start / center.tlRadiusX).clamp(-1.0f, 1.0f))
                : Mathf.PI / 2.0f;

            Path path = new Path();
            path.addArc(tlCorner, Mathf.PI, tlCornerArcSweep);
            path.moveTo(center.left + center.tlRadiusX, center.top);

            if (start > center.tlRadiusX) {
                path.lineTo(center.left + start, center.top);
            }

            const float trCornerArcStart = (3 * Mathf.PI) / 2.0f;
            const float trCornerArcSweep = cornerArcSweep;
            if (start + extent < center.width - center.trRadiusX) {
                path.relativeMoveTo(extent, 0.0f);
                path.lineTo(center.right - center.trRadiusX, center.top);
                path.addArc(trCorner, trCornerArcStart, trCornerArcSweep);
            }
            else if (start + extent < center.width) {
                float dx = center.width - (start + extent);
                float sweep = Mathf.Acos(dx / center.trRadiusX);
                path.addArc(trCorner, trCornerArcStart + sweep, trCornerArcSweep - sweep);
            }

            path.moveTo(center.right, center.top + center.trRadiusY);
            path.lineTo(center.right, center.bottom - center.brRadiusY);
            path.addArc(brCorner, 0.0f, cornerArcSweep);
            path.lineTo(center.left + center.blRadiusX, center.bottom);
            path.addArc(blCorner, Mathf.PI / 2.0f, cornerArcSweep);
            path.lineTo(center.left, center.top + center.trRadiusY);
            return path;
        }

        public override void paint(Canvas canvas, Rect rect,
            float gapStart,
            float gapExtent = 0.0f,
            float gapPercentage = 0.0f
        ) {
            D.assert(gapPercentage >= 0.0f && gapPercentage <= 1.0f);
            D.assert(_cornersAreCircular(borderRadius));

            Paint paint = borderSide.toPaint();
            RRect outer = borderRadius.toRRect(rect);
            RRect center = outer.deflate(borderSide.width / 2.0f);
            if (gapExtent <= 0.0f || gapPercentage == 0.0f) {
                canvas.drawRRect(center, paint);
            }
            else {
                float extent = MathUtils.lerpFloat(0.0f, gapExtent + gapPadding * 2.0f, gapPercentage);
                Path path = _gapBorderPath(canvas, center, Mathf.Max(0.0f,gapStart - gapPadding), extent);
                canvas.drawPath(path, paint);
            }
        }

        public static bool operator ==(OutlineInputBorder left, OutlineInputBorder right) {
            return left.Equals(right);
        }

        public static bool operator !=(OutlineInputBorder left, OutlineInputBorder right) {
            return !left.Equals(right);
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
            return Equals((OutlineInputBorder) obj);
        }

        public bool Equals(OutlineInputBorder other) {
            return other.borderSide == borderSide
                   && other.borderRadius == borderRadius
                   && other.gapPadding == gapPadding;
        }

        public override int GetHashCode() {
            var hashCode = borderSide.GetHashCode();
            hashCode = (hashCode * 397) ^ borderRadius.GetHashCode();
            hashCode = (hashCode * 397) ^ gapPadding.GetHashCode();
            return hashCode;
        }
    }
}