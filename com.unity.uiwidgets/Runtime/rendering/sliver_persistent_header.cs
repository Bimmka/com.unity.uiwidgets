using System;
using Unity.UIWidgets.animation;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.gestures;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.scheduler2;
using Unity.UIWidgets.ui;
using UnityEngine;
using AsyncCallback = Unity.UIWidgets.foundation.AsyncCallback;

namespace Unity.UIWidgets.rendering {
    
    public class OverScrollHeaderStretchConfiguration {
        public OverScrollHeaderStretchConfiguration(
            float stretchTriggerOffset = 100.0f,
            AsyncCallback onStretchTrigger = null
        ) {
            D.assert(stretchTriggerOffset != null);
            this.stretchTriggerOffset = stretchTriggerOffset;
            this.onStretchTrigger = onStretchTrigger;
        }


        public readonly float stretchTriggerOffset;
        public readonly AsyncCallback onStretchTrigger;
    }
    public abstract class RenderSliverPersistentHeader : RenderObjectWithChildMixinRenderSliver<RenderBox> {
        public RenderSliverPersistentHeader(
            RenderBox child = null,
            OverScrollHeaderStretchConfiguration stretchConfiguration = null
            ) {
            this.child = child;
            this.stretchConfiguration = stretchConfiguration;
        }

        public virtual float? maxExtent { get; }

        public virtual float? minExtent { get; }

        public float childExtent {
            get {
                if (child == null) {
                    return 0.0f;
                }

                D.assert(child.hasSize);
                switch (constraints.axis) {
                    case Axis.vertical:
                        return child.size.height;
                    case Axis.horizontal:
                        return child.size.width;
                    default:
                        throw new Exception("Unknown axis: " + constraints.axis);
                }
            }
        }

        bool _needsUpdateChild = true;
        float _lastShrinkOffset = 0.0f;
        bool _lastOverlapsContent = false;
        public OverScrollHeaderStretchConfiguration stretchConfiguration;

        protected void updateChild(float shrinkOffset, bool overlapsContent) {
        }

        public override void markNeedsLayout() {
            _needsUpdateChild = true;
            base.markNeedsLayout();
        }

        protected void layoutChild(float scrollOffset, float maxExtent, bool overlapsContent = false) {
            float shrinkOffset = Mathf.Min(scrollOffset, maxExtent);
            if (_needsUpdateChild || _lastShrinkOffset != shrinkOffset ||
                _lastOverlapsContent != overlapsContent) {
                invokeLayoutCallback<SliverConstraints>((SliverConstraints constraints) => {
                    D.assert(constraints == this.constraints);
                    updateChild(shrinkOffset, overlapsContent);
                });
                _lastShrinkOffset = shrinkOffset;
                _lastOverlapsContent = overlapsContent;
                _needsUpdateChild = false;
            }

            D.assert(minExtent != null);
            D.assert(() => {
                if (minExtent <= maxExtent) {
                    return true;
                }

                throw new UIWidgetsError(
                    "The maxExtent for this $runtimeType is less than its minExtent.\n" +
                    "The specified maxExtent was: ${maxExtent.toStringAsFixed(1)}\n" +
                    "The specified minExtent was: ${minExtent.toStringAsFixed(1)}\n"
                );
            });
            child?.layout(
                this.constraints.asBoxConstraints(
                    maxExtent: Mathf.Max(minExtent ?? 0.0f, maxExtent - shrinkOffset)),
                parentUsesSize: true
            );
        }

        public override float? childMainAxisPosition(RenderObject child) {
            return base.childMainAxisPosition(this.child);
        }

        protected override bool hitTestChildren(SliverHitTestResult result, float mainAxisPosition, float crossAxisPosition) {
            D.assert(geometry.hitTestExtent > 0.0f);
            if (child != null) {
                return RenderSliverHelpers.hitTestBoxChild(this, new BoxHitTestResult(result), child, mainAxisPosition: mainAxisPosition,
                    crossAxisPosition: crossAxisPosition);
            }

            return false;
        }

        public override void applyPaintTransform(RenderObject child, Matrix4 transform) {
            D.assert(child != null);
            D.assert(child == this.child);
            RenderSliverHelpers.applyPaintTransformForBoxChild(this, this.child, transform);
        }

        public override void paint(PaintingContext context, Offset offset) {
            if (child != null && geometry.visible) {
                switch (GrowthDirectionUtils.applyGrowthDirectionToAxisDirection(constraints.axisDirection,
                    constraints.growthDirection)) {
                    case AxisDirection.up:
                        offset += new Offset(0.0f,
                            geometry.paintExtent - childMainAxisPosition(child)?? 0.0f - childExtent);
                        break;
                    case AxisDirection.down:
                        offset += new Offset(0.0f, childMainAxisPosition(child) ?? 0.0f);
                        break;
                    case AxisDirection.left:
                        offset += new Offset(
                            geometry.paintExtent - childMainAxisPosition(child) ?? 0.0f - childExtent,
                            0.0f);
                        break;
                    case AxisDirection.right:
                        offset += new Offset(childMainAxisPosition(child) ?? 0.0f, 0.0f);
                        break;
                }

                context.paintChild(child, offset);
            }
        }

        protected bool excludeFromSemanticsScrolling {
            get { return _excludeFromSemanticsScrolling; }
            set {
                if (_excludeFromSemanticsScrolling == value) {
                    return;
                }

                _excludeFromSemanticsScrolling = value;
            }
        }

        bool _excludeFromSemanticsScrolling = false;

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(FloatProperty.lazy("maxExtent", () => maxExtent));
            properties.add(FloatProperty.lazy("child position", () => childMainAxisPosition(child)));
        }
    }

    public abstract class RenderSliverScrollingPersistentHeader : RenderSliverPersistentHeader {
        public RenderSliverScrollingPersistentHeader(
            RenderBox child = null,
            OverScrollHeaderStretchConfiguration stretchConfiguration = null
        ) : base(child: child,
            stretchConfiguration: stretchConfiguration) {
        }

        float _childPosition;

        protected float updateGeometry() {
            float? stretchOffset = 0.0f;
            if (stretchConfiguration != null && _childPosition == 0.0) {
                stretchOffset += constraints.overlap.abs();
            }
            float? maxExtent = this.maxExtent;
            float? paintExtent = maxExtent - constraints.scrollOffset;
            geometry = new SliverGeometry(
                scrollExtent: maxExtent ?? 0.0f,
                paintOrigin: Mathf.Min(constraints.overlap, 0.0f),
                paintExtent: paintExtent?.clamp(0.0f, constraints.remainingPaintExtent) ?? 0.0f,
                maxPaintExtent: maxExtent + stretchOffset ?? 0.0f,
                hasVisualOverflow: true// Conservatively say we do have overflow to avoid complexity.
            );
            return stretchOffset > 0 ? 0.0f :Mathf.Min(0.0f, paintExtent?? 0.0f - childExtent );
        }
        protected override void performLayout() {
            float? maxExtent = this.maxExtent;
            layoutChild(constraints.scrollOffset, maxExtent ?? 0.0f);
            float? paintExtent = maxExtent - constraints.scrollOffset;
            geometry = new SliverGeometry(
                scrollExtent: maxExtent ?? 0.0f,
                paintOrigin: Mathf.Min(constraints.overlap, 0.0f),
                paintExtent: paintExtent?.clamp(0.0f, constraints.remainingPaintExtent) ?? 0.0f,
                maxPaintExtent: maxExtent ?? 0.0f,
                hasVisualOverflow: true
            );
            _childPosition = Mathf.Min(0.0f, paintExtent ?? 0.0f - childExtent);
        }

        public override float? childMainAxisPosition(RenderObject child) {
            D.assert(child == this.child);
            return _childPosition;
        }
    }

    public abstract class RenderSliverPinnedPersistentHeader : RenderSliverPersistentHeader {
        public RenderSliverPinnedPersistentHeader(
            RenderBox child = null,
            OverScrollHeaderStretchConfiguration stretchConfiguration = null
        ) : base(child: child,
            stretchConfiguration: stretchConfiguration) {
        }

        protected override void performLayout() {
            float? maxExtent = this.maxExtent;
            bool overlapsContent = constraints.overlap > 0.0f;
            excludeFromSemanticsScrolling =
                overlapsContent || (constraints.scrollOffset > maxExtent - minExtent);
            layoutChild(constraints.scrollOffset, maxExtent ?? 0.0f, overlapsContent: overlapsContent);
            float? layoutExtent =
                (maxExtent - constraints.scrollOffset)?.clamp(0.0f, constraints.remainingPaintExtent);
            geometry = new SliverGeometry(
                scrollExtent: maxExtent ?? 0.0f,
                paintOrigin: constraints.overlap,
                paintExtent: Mathf.Min(childExtent, constraints.remainingPaintExtent),
                layoutExtent: layoutExtent,
                maxPaintExtent: maxExtent ?? 0.0f,
                maxScrollObstructionExtent: minExtent ?? 0.0f,
                cacheExtent: layoutExtent > 0.0f ? -constraints.cacheOrigin + layoutExtent : layoutExtent,
                hasVisualOverflow: true
            );
        }

        public override float? childMainAxisPosition(RenderObject child) {
            return 0.0f;
        }
    }

    public class FloatingHeaderSnapConfiguration {
        public FloatingHeaderSnapConfiguration(
            TickerProvider vsync,
            Curve curve = null,
            TimeSpan? duration = null
        ) {
            D.assert(vsync != null);
            this.vsync = vsync;
            this.curve = curve ?? Curves.ease;
            this.duration = duration ?? new TimeSpan(0, 0, 0, 0, 300);
        }

        public readonly TickerProvider vsync;

        public readonly Curve curve;

        public readonly TimeSpan duration;
    }


    public abstract class RenderSliverFloatingPersistentHeader : RenderSliverPersistentHeader {
        public RenderSliverFloatingPersistentHeader(
            RenderBox child = null,
            FloatingHeaderSnapConfiguration snapConfiguration = null,
            OverScrollHeaderStretchConfiguration stretchConfiguration = null
        ) : base(
            child: child,
            stretchConfiguration: stretchConfiguration) {
            _snapConfiguration = snapConfiguration;
        }

        AnimationController _controller;
        Animation<float> _animation;
        protected float _lastActualScrollOffset;
        protected float _effectiveScrollOffset;

        float _childPosition;

        public override void detach() {
            _controller?.dispose();
            _controller = null;
            base.detach();
        }

        public FloatingHeaderSnapConfiguration snapConfiguration {
            get { return _snapConfiguration; }
            set {
                if (value == _snapConfiguration) {
                    return;
                }

                if (value == null) {
                    _controller?.dispose();
                    _controller = null;
                }
                else {
                    if (_snapConfiguration != null && value.vsync != _snapConfiguration.vsync) {
                        _controller?.resync(value.vsync);
                    }
                }

                _snapConfiguration = value;
            }
        }

        FloatingHeaderSnapConfiguration _snapConfiguration;

        protected virtual float updateGeometry() {
            float? maxExtent = this.maxExtent;
            float? paintExtent = maxExtent - _effectiveScrollOffset;
            float? layoutExtent = maxExtent - constraints.scrollOffset;
            geometry = new SliverGeometry(
                scrollExtent: maxExtent ?? 0.0f,
                paintOrigin: Mathf.Min(constraints.overlap, 0.0f),
                paintExtent: paintExtent?.clamp(0.0f, constraints.remainingPaintExtent) ?? 0.0f,
                layoutExtent: layoutExtent?.clamp(0.0f, constraints.remainingPaintExtent),
                maxPaintExtent: maxExtent ?? 0.0f,
                maxScrollObstructionExtent: maxExtent ?? 0.0f,
                hasVisualOverflow: true
            );
            return Mathf.Min(0.0f, paintExtent ?? 0.0f - childExtent);
        }

        public void maybeStartSnapAnimation(ScrollDirection direction) {
            if (snapConfiguration == null) {
                return;
            }

            if (direction == ScrollDirection.forward && _effectiveScrollOffset <= 0.0f) {
                return;
            }

            if (direction == ScrollDirection.reverse && _effectiveScrollOffset >= maxExtent) {
                return;
            }

            TickerProvider vsync = snapConfiguration.vsync;
            TimeSpan duration = snapConfiguration.duration;
            _controller = _controller ?? new AnimationController(vsync: vsync, duration: duration);
            _controller.addListener(() => {
                if (_effectiveScrollOffset == _animation.value) {
                    return;
                }

                _effectiveScrollOffset = _animation.value;
                markNeedsLayout();
            });

            _animation = _controller.drive(
                new FloatTween(
                    begin: _effectiveScrollOffset,
                    end: direction == ScrollDirection.forward ? 0.0f : maxExtent ?? 0.0f
                ).chain(new CurveTween(
                    curve: snapConfiguration.curve
                ))
            );

            _controller.forward(from: 0.0f);
        }

        public void maybeStopSnapAnimation(ScrollDirection direction) {
            _controller?.stop();
        }

        protected override void performLayout() {
            float? maxExtent = this.maxExtent;
            if (((constraints.scrollOffset < _lastActualScrollOffset) ||
                 (_effectiveScrollOffset < maxExtent))) {
                float delta = _lastActualScrollOffset - constraints.scrollOffset;
                bool allowFloatingExpansion = constraints.userScrollDirection == ScrollDirection.forward;
                if (allowFloatingExpansion) {
                    if (_effectiveScrollOffset > maxExtent) {
                        _effectiveScrollOffset = maxExtent ?? 0.0f;
                    }
                }
                else {
                    if (delta > 0.0f) {
                        delta = 0.0f;
                    }
                }

                _effectiveScrollOffset =
                    (_effectiveScrollOffset - delta).clamp(0.0f, constraints.scrollOffset);
            }
            else {
                _effectiveScrollOffset = constraints.scrollOffset;
            }

            bool overlapsContent = _effectiveScrollOffset < constraints.scrollOffset;
            excludeFromSemanticsScrolling = overlapsContent;
            layoutChild(_effectiveScrollOffset, maxExtent ?? 0.0f, overlapsContent: overlapsContent);
            _childPosition = updateGeometry();
            _lastActualScrollOffset = constraints.scrollOffset;
        }

        public override float? childMainAxisPosition(RenderObject child) {
            D.assert(child == this.child);
            return _childPosition;
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new FloatProperty("effective scroll offset", _effectiveScrollOffset));
        }
    }

    public abstract class RenderSliverFloatingPinnedPersistentHeader : RenderSliverFloatingPersistentHeader {
        public RenderSliverFloatingPinnedPersistentHeader(
            RenderBox child = null,
            FloatingHeaderSnapConfiguration snapConfiguration = null,
            OverScrollHeaderStretchConfiguration stretchConfiguration = null
        ) : base(child: child,
            snapConfiguration: snapConfiguration,
            stretchConfiguration: stretchConfiguration) {
        }

        protected override float updateGeometry() {
            float? minExtent = this.minExtent;
            float? minAllowedExtent = constraints.remainingPaintExtent > minExtent
                ? minExtent
                : constraints.remainingPaintExtent;
            float? maxExtent = this.maxExtent;
            float? paintExtent = maxExtent - _effectiveScrollOffset;
            float? clampedPaintExtent =
                paintExtent?.clamp(minAllowedExtent ?? 0.0f, constraints.remainingPaintExtent);
            float? layoutExtent = maxExtent - constraints.scrollOffset;
            float? stretchOffset = stretchConfiguration != null ?
                constraints.overlap.abs() :
                0.0f;
            geometry = new SliverGeometry(
                scrollExtent: maxExtent ?? 0.0f,
                paintExtent: clampedPaintExtent ?? 0.0f,
                layoutExtent: layoutExtent?.clamp(0.0f, clampedPaintExtent ?? 0.0f),
                maxPaintExtent: maxExtent + stretchOffset ?? 0.0f,
                maxScrollObstructionExtent: maxExtent ?? 0.0f,
                hasVisualOverflow: true
            );
            return 0.0f;
        }
    }
}