using System;
using Unity.UIWidgets.animation;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.ui;
using UnityEngine;
using Color = Unity.UIWidgets.ui.Color;
using Rect = Unity.UIWidgets.ui.Rect;
using TextStyle = Unity.UIWidgets.painting.TextStyle;

namespace Unity.UIWidgets.widgets {
    public class BoxConstraintsTween : Tween<BoxConstraints> {
        public BoxConstraintsTween(
            BoxConstraints begin = null,
            BoxConstraints end = null
        ) : base(begin: begin, end: end) {
        }

        public override BoxConstraints lerp(float t) {
            return BoxConstraints.lerp(begin, end, t);
        }
    }


    public class DecorationTween : Tween<Decoration> {
        public DecorationTween(
            Decoration begin = null,
            Decoration end = null) : base(begin: begin, end: end) {
        }

        public override Decoration lerp(float t) {
            return Decoration.lerp(begin, end, t);
        }
    }


    public class EdgeInsetsTween : Tween<EdgeInsets> {
        public EdgeInsetsTween(
            EdgeInsets begin = null,
            EdgeInsets end = null) : base(begin: begin, end: end) {
        }

        public override EdgeInsets lerp(float t) {
            return EdgeInsets.lerp(begin, end, t);
        }
    }


    public class BorderRadiusTween : Tween<BorderRadius> {
        public BorderRadiusTween(
            BorderRadius begin = null,
            BorderRadius end = null) : base(begin: begin, end: end) {
        }

        public override BorderRadius lerp(float t) {
            return BorderRadius.lerp(begin, end, t);
        }
    }


    public class BorderTween : Tween<Border> {
        public BorderTween(
            Border begin = null,
            Border end = null) : base(begin: begin, end: end) {
        }

        public override Border lerp(float t) {
            return Border.lerp(begin, end, t);
        }
    }


    public class Matrix4Tween : Tween<Matrix4> {
        public Matrix4Tween(
            Matrix4 begin = null,
            Matrix4 end = null) : base(begin: begin, end: end) {
        }

        public override Matrix4 lerp(float t) {
            D.assert(begin != null);
            D.assert(end != null);

            Vector3 beginTranslation = Vector3.zero;
            Vector3 endTranslation = Vector3.zero;
            Quaternion beginRotation = Quaternion.identity;
            Quaternion endRotation = Quaternion.identity;
            Vector3 beginScale = Vector3.zero;
            Vector3 endScale = Vector3.zero;
            begin.decompose(ref beginTranslation, ref beginRotation, ref beginScale);
            end.decompose(ref endTranslation, ref endRotation, ref endScale);
            Vector3 lerpTranslation =
                beginTranslation * (1.0f - t) + endTranslation * t;
            // TODO(alangardner): Implement slerp for constant rotation
            Quaternion lerpRotation =
                beginRotation
                    .scaled(1.0f - t)
                    .add(endRotation.scaled(t)).normalized;
            Vector3 lerpScale = beginScale * (1.0f - t) + endScale * t;
            return Matrix4.compose(lerpTranslation, lerpRotation, lerpScale);
        }
    }

    public class TextStyleTween : Tween<TextStyle> {
        public TextStyleTween(
            TextStyle begin = null,
            TextStyle end = null) : base(begin: begin, end: end) {
        }

        public override TextStyle lerp(float t) {
            return TextStyle.lerp(begin, end, t);
        }
    }

    public abstract class ImplicitlyAnimatedWidget : StatefulWidget {
        public ImplicitlyAnimatedWidget(
            Key key = null,
            Curve curve = null,
            TimeSpan? duration = null,
            VoidCallback onEnd = null
        ) : base(key: key) {
            D.assert(duration != null);
            this.curve = curve ?? Curves.linear;
            this.duration = duration ?? TimeSpan.Zero;
            this.onEnd = onEnd;
        }

        public readonly Curve curve;

        public readonly TimeSpan duration;
        
        public readonly VoidCallback onEnd;

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new IntProperty("duration", (int) duration.TotalMilliseconds, unit: "ms"));
        }
    }


    public delegate Tween<T> TweenConstructor<T>(T targetValue);

    public interface TweenVisitor {
        Tween<T> visit<T, T2>(ImplicitlyAnimatedWidgetState<T2> state, Tween<T> tween, T targetValue,
            TweenConstructor<T> constructor) where T2 : ImplicitlyAnimatedWidget;
    }

    public class TweenVisitorUpdateTween : TweenVisitor {
        public Tween<T> visit<T, T2>(ImplicitlyAnimatedWidgetState<T2> state, Tween<T> tween, T targetValue,
            TweenConstructor<T> constructor)
            where T2 : ImplicitlyAnimatedWidget {
            state._updateTween(tween, targetValue);
            return tween;
        }
    }

    public class TweenVisitorCheckStartAnimation : TweenVisitor {
        public bool shouldStartAnimation;

        public TweenVisitorCheckStartAnimation() {
            shouldStartAnimation = false;
        }

        public Tween<T> visit<T, T2>(ImplicitlyAnimatedWidgetState<T2> state, Tween<T> tween, T targetValue,
            TweenConstructor<T> constructor)
            where T2 : ImplicitlyAnimatedWidget {
            if (targetValue != null) {
                tween = tween ?? constructor(targetValue);
                if (state._shouldAnimateTween(tween, targetValue)) {
                    shouldStartAnimation = true;
                }
            }
            else {
                tween = null;
            }

            return tween;
        }
    }


    public abstract class ImplicitlyAnimatedWidgetState<T> : SingleTickerProviderStateMixin<T>
        where T : ImplicitlyAnimatedWidget {
        protected AnimationController controller {
            get { return _controller; }
        }

        AnimationController _controller;

        public Animation<float> animation {
            get { return _animation; }
        }

        Animation<float> _animation;

        public override void initState() {
            base.initState();
            _controller = new AnimationController(
                duration: widget.duration,
                debugLabel: "{" + widget.toStringShort() + "}",
                vsync: this
            );
            _updateCurve();
            _constructTweens();
            didUpdateTweens();
        }

        public override void didUpdateWidget(StatefulWidget oldWidget) {
            base.didUpdateWidget(oldWidget);

            if (widget.curve != ((ImplicitlyAnimatedWidget) oldWidget).curve) {
                _updateCurve();
            }

            _controller.duration = widget.duration;
            if (_constructTweens()) {
                var visitor = new TweenVisitorUpdateTween();
                forEachTween(visitor);
                _controller.setValue(0.0f);
                _controller.forward();
                didUpdateTweens();
            }
        }

        void _updateCurve() {
            if (widget.curve != null) {
                _animation = new CurvedAnimation(parent: _controller, curve: widget.curve);
            }
            else {
                _animation = _controller;
            }
        }

        public override void dispose() {
            _controller.dispose();
            base.dispose();
        }

        public bool _shouldAnimateTween<T2>(Tween<T2> tween, T2 targetValue) {
            return !targetValue.Equals(tween.end == null ? tween.begin : tween.end);
        }

        public void _updateTween<T2>(Tween<T2> tween, T2 targetValue) {
            if (tween == null) {
                return;
            }

            tween.begin = tween.evaluate(_animation);
            tween.end = targetValue;
        }

        bool _constructTweens() {
            var visitor = new TweenVisitorCheckStartAnimation();
            forEachTween(visitor);
            return visitor.shouldStartAnimation;
        }

        protected abstract void forEachTween(TweenVisitor visitor);

        protected virtual void didUpdateTweens() {
        }
    }


    public abstract class AnimatedWidgetBaseState<T> : ImplicitlyAnimatedWidgetState<T>
        where T : ImplicitlyAnimatedWidget {
        public override void initState() {
            base.initState();
            controller.addListener(_handleAnimationChanged);
        }

        void _handleAnimationChanged() {
            setState(() => { });
        }
    }


    public class AnimatedContainer : ImplicitlyAnimatedWidget {
        public AnimatedContainer(
            Key key = null,
            Alignment alignment = null,
            EdgeInsets padding = null,
            Color color = null,
            Decoration decoration = null,
            Decoration foregroundDecoration = null,
            float? width = null,
            float? height = null,
            BoxConstraints constraints = null,
            EdgeInsets margin = null,
            Matrix4 transform = null,
            Widget child = null,
            Curve curve = null,
            TimeSpan? duration = null
        ) : base(key: key, curve: curve ?? Curves.linear, duration: duration) {
            D.assert(duration != null);
            D.assert(margin == null || margin.isNonNegative);
            D.assert(padding == null || padding.isNonNegative);
            D.assert(decoration == null || decoration.debugAssertIsValid());
            D.assert(constraints == null || constraints.debugAssertIsValid());
            D.assert(color == null || decoration == null,
                () => "Cannot provide both a color and a decoration\n" +
                "The color argument is just a shorthand for \"decoration: new BoxDecoration(backgroundColor: color)\".");
            this.alignment = alignment;
            this.padding = padding;
            this.foregroundDecoration = foregroundDecoration;
            this.margin = margin;
            this.transform = transform;
            this.child = child;
            this.decoration = decoration ?? (color != null ? new BoxDecoration(color: color) : null);
            this.constraints =
                (width != null || height != null)
                    ? constraints?.tighten(width: width, height: height)
                      ?? BoxConstraints.tightFor(width: width, height: height)
                    : constraints;
        }

        public readonly Widget child;

        public readonly Alignment alignment;

        public readonly EdgeInsets padding;

        public readonly Decoration decoration;

        public readonly Decoration foregroundDecoration;

        public readonly BoxConstraints constraints;

        public readonly EdgeInsets margin;

        public readonly Matrix4 transform;


        public override State createState() {
            return new _AnimatedContainerState();
        }


        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<Alignment>("alignment", alignment, showName: false,
                defaultValue: null));
            properties.add(new DiagnosticsProperty<EdgeInsets>("padding", padding, defaultValue: null));
            properties.add(new DiagnosticsProperty<Decoration>("bg", decoration, defaultValue: null));
            properties.add(
                new DiagnosticsProperty<Decoration>("fg", foregroundDecoration, defaultValue: null));
            properties.add(new DiagnosticsProperty<BoxConstraints>("constraints", constraints,
                defaultValue: null,
                showName: false));
            properties.add(new DiagnosticsProperty<EdgeInsets>("margin", margin, defaultValue: null));
            properties.add(ObjectFlagProperty<Matrix4>.has("transform", transform));
        }
    }

    class _AnimatedContainerState : AnimatedWidgetBaseState<AnimatedContainer> {
        AlignmentTween _alignment;
        EdgeInsetsTween _padding;
        DecorationTween _decoration;
        DecorationTween _foregroundDecoration;
        BoxConstraintsTween _constraints;
        EdgeInsetsTween _margin;
        Matrix4Tween _transform;


        protected override void forEachTween(TweenVisitor visitor) {
            _alignment = (AlignmentTween) visitor.visit(this, _alignment, widget.alignment,
                (Alignment value) => new AlignmentTween(begin: value));
            _padding = (EdgeInsetsTween) visitor.visit(this, _padding, widget.padding,
                (EdgeInsets value) => new EdgeInsetsTween(begin: value));
            _decoration = (DecorationTween) visitor.visit(this, _decoration, widget.decoration,
                (Decoration value) => new DecorationTween(begin: value));
            _foregroundDecoration = (DecorationTween) visitor.visit(this, _foregroundDecoration,
                widget.foregroundDecoration, (Decoration value) => new DecorationTween(begin: value));
            _constraints = (BoxConstraintsTween) visitor.visit(this, _constraints, widget.constraints,
                (BoxConstraints value) => new BoxConstraintsTween(begin: value));
            _margin = (EdgeInsetsTween) visitor.visit(this, _margin, widget.margin,
                (EdgeInsets value) => new EdgeInsetsTween(begin: value));
            _transform = (Matrix4Tween) visitor.visit(this, _transform, widget.transform,
                (Matrix4 value) => new Matrix4Tween(begin: value));
        }


        public override Widget build(BuildContext context) {
            return new Container(
                child: widget.child,
                alignment: _alignment?.evaluate(animation),
                padding: _padding?.evaluate(animation),
                decoration: _decoration?.evaluate(animation),
                forgroundDecoration: _foregroundDecoration?.evaluate(animation),
                constraints: _constraints?.evaluate(animation),
                margin: _margin?.evaluate(animation),
                transfrom: _transform?.evaluate(animation)
            );
        }


        public override void debugFillProperties(DiagnosticPropertiesBuilder description) {
            base.debugFillProperties(description);
            description.add(new DiagnosticsProperty<AlignmentTween>("alignment", _alignment, showName: false,
                defaultValue: null));
            description.add(new DiagnosticsProperty<EdgeInsetsTween>("padding", _padding, defaultValue: null));
            description.add(new DiagnosticsProperty<DecorationTween>("bg", _decoration, defaultValue: null));
            description.add(
                new DiagnosticsProperty<DecorationTween>("fg", _foregroundDecoration, defaultValue: null));
            description.add(new DiagnosticsProperty<BoxConstraintsTween>("constraints", _constraints,
                showName: false, defaultValue: null));
            description.add(new DiagnosticsProperty<EdgeInsetsTween>("margin", _margin, defaultValue: null));
            description.add(ObjectFlagProperty<Matrix4Tween>.has("transform", _transform));
        }
    }

    public class AnimatedPadding : ImplicitlyAnimatedWidget {
        public AnimatedPadding(
            Key key = null,
            EdgeInsets padding = null,
            Widget child = null,
            Curve curve = null,
            TimeSpan? duration = null
        ) : base(key: key, curve: curve, duration: duration) {
            D.assert(padding != null);
            D.assert(padding.isNonNegative);
            this.padding = padding;
            this.child = child;
        }

        public readonly EdgeInsets padding;

        public readonly Widget child;

        public override State createState() {
            return new _AnimatedPaddingState();
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<EdgeInsets>("padding", padding));
        }
    }

    class _AnimatedPaddingState : AnimatedWidgetBaseState<AnimatedPadding> {
        EdgeInsetsTween _padding;

        protected override void forEachTween(TweenVisitor visitor) {
            _padding = (EdgeInsetsTween) visitor.visit(this, _padding, widget.padding,
                (EdgeInsets value) => new EdgeInsetsTween(begin: value));
        }

        public override Widget build(BuildContext context) {
            return new Padding(
                padding: _padding.evaluate(animation),
                child: widget.child
            );
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder description) {
            base.debugFillProperties(description);
            description.add(new DiagnosticsProperty<EdgeInsetsTween>("padding", _padding,
                defaultValue: foundation_.kNullDefaultValue));
        }
    }

    public class AnimatedAlign : ImplicitlyAnimatedWidget {
        public AnimatedAlign(
            Key key = null,
            Alignment alignment = null,
            Widget child = null,
            Curve curve = null,
            TimeSpan? duration = null
        ) : base(key: key, curve: curve ?? Curves.linear, duration: duration) {
            D.assert(alignment != null);
            this.alignment = alignment;
            this.child = child;
        }

        public readonly Alignment alignment;

        public readonly Widget child;

        public override State createState() {
            return new _AnimatedAlignState();
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties: properties);
            properties.add(new DiagnosticsProperty<Alignment>("alignment", value: alignment));
        }
    }

    class _AnimatedAlignState : AnimatedWidgetBaseState<AnimatedAlign> {
        AlignmentTween _alignment;

        protected override void forEachTween(TweenVisitor visitor) {
            _alignment = (AlignmentTween) visitor.visit(this, tween: _alignment,
                targetValue: widget.alignment, constructor: value => new AlignmentTween(begin: value));
        }

        public override Widget build(BuildContext context) {
            return new Align(
                alignment: _alignment.evaluate(animation: animation),
                child: widget.child
            );
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder description) {
            base.debugFillProperties(properties: description);
            description.add(new DiagnosticsProperty<AlignmentTween>("alignment", value: _alignment, defaultValue: null));
        }
    }

    public class AnimatedPositioned : ImplicitlyAnimatedWidget {
        public AnimatedPositioned(
            Key key = null,
            Widget child = null,
            float? left = null,
            float? top = null,
            float? right = null,
            float? bottom = null,
            float? width = null,
            float? height = null,
            Curve curve = null,
            TimeSpan? duration = null
        ) : base(key: key, curve: curve ?? Curves.linear, duration: duration) {
            D.assert(left == null || right == null || width == null);
            D.assert(top == null || bottom == null || height == null);
            this.child = child;
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
            this.width = width;
            this.height = height;
        }

        public static AnimatedPositioned fromRect(
            Key key = null,
            Widget child = null,
            Rect rect = null,
            Curve curve = null,
            TimeSpan? duration = null
        ) {
            return new AnimatedPositioned(
                child: child,
                duration: duration,
                left: rect.left,
                top: rect.top,
                right: null,
                bottom: null,
                width: rect.width,
                height: rect.height,
                curve: curve ?? Curves.linear,
                key: key
            );
        }

        public readonly Widget child;

        public readonly float? left;

        public readonly float? top;

        public readonly float? right;

        public readonly float? bottom;

        public readonly float? width;

        public readonly float? height;

        public override State createState() {
            return new _AnimatedPositionedState();
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties: properties);
            properties.add(new FloatProperty("left", value: left, defaultValue: null));
            properties.add(new FloatProperty("top", value: top, defaultValue: null));
            properties.add(new FloatProperty("right", value: right, defaultValue: null));
            properties.add(new FloatProperty("bottom", value: bottom, defaultValue: null));
            properties.add(new FloatProperty("width", value: width, defaultValue: null));
            properties.add(new FloatProperty("height", value: height, defaultValue: null));
        }
    }

    class _AnimatedPositionedState : AnimatedWidgetBaseState<AnimatedPositioned> {
        Tween<float?> _left;
        Tween<float?> _top;
        Tween<float?> _right;
        Tween<float?> _bottom;
        Tween<float?> _width;
        Tween<float?> _height;

        protected override void forEachTween(TweenVisitor visitor) {
            _left = visitor.visit(this, tween: _left, targetValue: widget.left,
                constructor: value => new NullableFloatTween(begin: value));
            _top = visitor.visit(this, tween: _top, targetValue: widget.top,
                constructor: value => new NullableFloatTween(begin: value));
            _right = visitor.visit(this, tween: _right, targetValue: widget.right,
                constructor: value => new NullableFloatTween(begin: value));
            _bottom = visitor.visit(this, tween: _bottom, targetValue: widget.bottom,
                constructor: value => new NullableFloatTween(begin: value));
            _width = visitor.visit(this, tween: _width, targetValue: widget.width,
                constructor: value => new NullableFloatTween(begin: value));
            _height = visitor.visit(this, tween: _height, targetValue: widget.height,
                constructor: value => new NullableFloatTween(begin: value));
        }

        public override Widget build(BuildContext context) {
            return new Positioned(
                child: widget.child,
                left: _left?.evaluate(animation: animation),
                top: _top?.evaluate(animation: animation),
                right: _right?.evaluate(animation: animation),
                bottom: _bottom?.evaluate(animation: animation),
                width: _width?.evaluate(animation: animation),
                height: _height?.evaluate(animation: animation)
            );
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder description) {
            base.debugFillProperties(properties: description);
            description.add(ObjectFlagProperty<Tween<float?>>.has("left", value: _left));
            description.add(ObjectFlagProperty<Tween<float?>>.has("top", value: _top));
            description.add(ObjectFlagProperty<Tween<float?>>.has("right", value: _right));
            description.add(ObjectFlagProperty<Tween<float?>>.has("bottom", value: _bottom));
            description.add(ObjectFlagProperty<Tween<float?>>.has("width", value: _width));
            description.add(ObjectFlagProperty<Tween<float?>>.has("height", value: _height));
        }
    }

    public class AnimatedPositionedDirectional : ImplicitlyAnimatedWidget {
        public AnimatedPositionedDirectional(
            Key key = null,
            Widget child = null,
            float? start = null,
            float? top = null,
            float? end = null,
            float? bottom = null,
            float? width = null,
            float? height = null,
            Curve curve = null,
            TimeSpan? duration = null
        ) : base(key: key, curve: curve, duration: duration) {
            D.assert(start == null || end == null || width == null);
            D.assert(top == null || bottom == null || height == null);
            this.child = child;
            this.start = start;
            this.top = top;
            this.end = end;
            this.bottom = bottom;
            this.width = width;
            this.height = height;
        }

        public readonly Widget child;

        public readonly float? start;

        public readonly float? top;

        public readonly float? end;

        public readonly float? bottom;

        public readonly float? width;

        public readonly float? height;

        public override State createState() {
            return new _AnimatedPositionedDirectionalState();
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties: properties);
            properties.add(new FloatProperty("start", value: start, defaultValue: null));
            properties.add(new FloatProperty("top", value: top, defaultValue: null));
            properties.add(new FloatProperty("end", value: end, defaultValue: null));
            properties.add(new FloatProperty("bottom", value: bottom, defaultValue: null));
            properties.add(new FloatProperty("width", value: width, defaultValue: null));
            properties.add(new FloatProperty("height", value: height, defaultValue: null));
        }
    }

    class _AnimatedPositionedDirectionalState : AnimatedWidgetBaseState<AnimatedPositionedDirectional> {
        Tween<float?> _start;
        Tween<float?> _top;
        Tween<float?> _end;
        Tween<float?> _bottom;
        Tween<float?> _width;
        Tween<float?> _height;

        protected override void forEachTween(TweenVisitor visitor) {
            _start = visitor.visit(this, tween: _start, targetValue: widget.start,
                constructor: value => new NullableFloatTween(begin: value));
            _top = visitor.visit(this, tween: _top, targetValue: widget.top,
                constructor: value => new NullableFloatTween(begin: value));
            _end = visitor.visit(this, tween: _end, targetValue: widget.end,
                constructor: value => new NullableFloatTween(begin: value));
            _bottom = visitor.visit(this, tween: _bottom, targetValue: widget.bottom,
                constructor: value => new NullableFloatTween(begin: value));
            _width = visitor.visit(this, tween: _width, targetValue: widget.width,
                constructor: value => new NullableFloatTween(begin: value));
            _height = visitor.visit(this, tween: _height, targetValue: widget.height,
                constructor: value => new NullableFloatTween(begin: value));
        }

        public override Widget build(BuildContext context) {
            D.assert(WidgetsD.debugCheckHasDirectionality(context));
            return Positioned.directional(
                textDirection: Directionality.of(context: context),
                child: widget.child,
                start: _start?.evaluate(animation: animation),
                top: _top?.evaluate(animation: animation),
                end: _end?.evaluate(animation: animation),
                bottom: _bottom?.evaluate(animation: animation),
                width: _width?.evaluate(animation: animation),
                height: _height?.evaluate(animation: animation)
            );
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder description) {
            base.debugFillProperties(properties: description);
            description.add(ObjectFlagProperty<Tween<float?>>.has("start", value: _start));
            description.add(ObjectFlagProperty<Tween<float?>>.has("top", value: _top));
            description.add(ObjectFlagProperty<Tween<float?>>.has("end", value: _end));
            description.add(ObjectFlagProperty<Tween<float?>>.has("bottom", value: _bottom));
            description.add(ObjectFlagProperty<Tween<float?>>.has("width", value: _width));
            description.add(ObjectFlagProperty<Tween<float?>>.has("height", value: _height));
        }
    }

    public class AnimatedOpacity : ImplicitlyAnimatedWidget {
        public AnimatedOpacity(
            Key key = null,
            Widget child = null,
            float? opacity = null,
            Curve curve = null,
            TimeSpan? duration = null
        ) :
            base(key: key, curve: curve ?? Curves.linear, duration: duration) {
            D.assert(opacity != null && opacity >= 0.0 && opacity <= 1.0);
            this.child = child;
            this.opacity = opacity ?? 1.0f;
        }

        public readonly Widget child;

        public readonly float opacity;

        public override State createState() {
            return new _AnimatedOpacityState();
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new FloatProperty("opacity", opacity));
        }
    }

    class _AnimatedOpacityState : ImplicitlyAnimatedWidgetState<AnimatedOpacity> {
        NullableFloatTween _opacity;
        Animation<float> _opacityAnimation;

        protected override void forEachTween(TweenVisitor visitor) {
            _opacity = (NullableFloatTween) visitor.visit(this, _opacity, widget.opacity,
                (float? value) => new NullableFloatTween(begin: value));
        }

        protected override void didUpdateTweens() {
            float? endValue = _opacity.end ?? _opacity.begin ?? null;
            D.assert(endValue != null);
            _opacityAnimation = animation.drive(new FloatTween(begin: _opacity.begin.Value, end: endValue.Value));
        }

        public override Widget build(BuildContext context) {
            return new FadeTransition(
                opacity: _opacityAnimation,
                child: widget.child
            );
        }
    }

    public class AnimatedDefaultTextStyle : ImplicitlyAnimatedWidget {
        public AnimatedDefaultTextStyle(
            Key key = null,
            Widget child = null,
            TextStyle style = null,
            TextAlign? textAlign = null,
            bool softWrap = true,
            TextOverflow? overflow = null,
            int? maxLines = null,
            Curve curve = null,
            TimeSpan? duration = null
        ) : base(key: key, curve: curve ?? Curves.linear, duration: duration) {
            D.assert(duration != null);
            D.assert(style != null);
            D.assert(child != null);
            D.assert(maxLines == null || maxLines > 0);
            this.child = child;
            this.style = style;
            this.textAlign = textAlign;
            this.softWrap = softWrap;
            this.overflow = overflow ?? TextOverflow.clip;
            this.maxLines = maxLines;
        }

        public readonly Widget child;

        public readonly TextStyle style;

        public readonly bool softWrap;

        public readonly TextAlign? textAlign;

        public readonly TextOverflow overflow;

        public readonly int? maxLines;

        public override State createState() {
            return new _AnimatedDefaultTextStyleState();
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            style?.debugFillProperties(properties);
            properties.add(new EnumProperty<TextAlign>("textAlign", textAlign ?? TextAlign.center,
                defaultValue: null));
            properties.add(new FlagProperty("softWrap", value: softWrap, ifTrue: "wrapping at box width",
                ifFalse: "no wrapping except at line break characters", showName: true));
            properties.add(new EnumProperty<TextOverflow>("overflow", overflow, defaultValue: null));
            properties.add(new IntProperty("maxLines", maxLines, defaultValue: null));
        }
    }


    class _AnimatedDefaultTextStyleState : AnimatedWidgetBaseState<AnimatedDefaultTextStyle> {
        TextStyleTween _style;

        protected override void forEachTween(TweenVisitor visitor) {
            _style = (TextStyleTween) visitor.visit(this, _style, widget.style,
                (TextStyle value) => new TextStyleTween(begin: value));
        }

        public override Widget build(BuildContext context) {
            return new DefaultTextStyle(
                style: _style.evaluate(animation),
                textAlign: widget.textAlign,
                softWrap: widget.softWrap,
                overflow: widget.overflow,
                maxLines: widget.maxLines,
                child: widget.child);
        }
    }


    public class AnimatedPhysicalModel : ImplicitlyAnimatedWidget {
        public AnimatedPhysicalModel(
            Key key = null,
            Widget child = null,
            BoxShape? shape = null,
            Clip clipBehavior = Clip.none,
            BorderRadius borderRadius = null,
            float? elevation = null,
            Color color = null,
            bool animateColor = true,
            Color shadowColor = null,
            bool animateShadowColor = true,
            Curve curve = null,
            TimeSpan? duration = null
        ) : base(key: key, curve: curve ?? Curves.linear, duration: duration) {
            D.assert(child != null);
            D.assert(shape != null);
            D.assert(elevation != null && elevation >= 0.0f);
            D.assert(color != null);
            D.assert(shadowColor != null);
            D.assert(duration != null);
            this.child = child;
            this.shape = shape ?? BoxShape.circle;
            this.clipBehavior = clipBehavior;
            this.borderRadius = borderRadius ?? BorderRadius.zero;
            this.elevation = elevation ?? 0.0f;
            this.color = color;
            this.animateColor = animateColor;
            this.shadowColor = shadowColor;
            this.animateShadowColor = animateShadowColor;
        }

        public readonly Widget child;

        public readonly BoxShape shape;

        public readonly Clip clipBehavior;

        public readonly BorderRadius borderRadius;

        public readonly float elevation;

        public readonly Color color;

        public readonly bool animateColor;

        public readonly Color shadowColor;

        public readonly bool animateShadowColor;

        public override State createState() {
            return new _AnimatedPhysicalModelState();
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new EnumProperty<BoxShape>("shape", shape));
            properties.add(new DiagnosticsProperty<BorderRadius>("borderRadius", borderRadius));
            properties.add(new FloatProperty("elevation", elevation));
            properties.add(new DiagnosticsProperty<Color>("color", color));
            properties.add(new DiagnosticsProperty<bool>("animateColor", animateColor));
            properties.add(new DiagnosticsProperty<Color>("shadowColor", shadowColor));
            properties.add(new DiagnosticsProperty<bool>("animateShadowColor", animateShadowColor));
        }
    }

    class _AnimatedPhysicalModelState : AnimatedWidgetBaseState<AnimatedPhysicalModel> {
        BorderRadiusTween _borderRadius;
        FloatTween _elevation;
        ColorTween _color;
        ColorTween _shadowColor;

        protected override void forEachTween(TweenVisitor visitor) {
            _borderRadius = (BorderRadiusTween) visitor.visit(this, _borderRadius, widget.borderRadius,
                (BorderRadius value) => new BorderRadiusTween(begin: value));
            _elevation = (FloatTween) visitor.visit(this, _elevation, widget.elevation,
                (float value) => new FloatTween(begin: value, end: value));
            _color = (ColorTween) visitor.visit(this, _color, widget.color,
                (Color value) => new ColorTween(begin: value));
            _shadowColor = (ColorTween) visitor.visit(this, _shadowColor, widget.shadowColor,
                (Color value) => new ColorTween(begin: value));
        }

        public override Widget build(BuildContext context) {
            return new PhysicalModel(
                child: widget.child,
                shape: widget.shape,
                clipBehavior: widget.clipBehavior,
                borderRadius: _borderRadius.evaluate(animation),
                elevation: _elevation.evaluate(animation),
                color: widget.animateColor ? _color.evaluate(animation) : widget.color,
                shadowColor: widget.animateShadowColor
                    ? _shadowColor.evaluate(animation)
                    : widget.shadowColor);
        }
    }
}