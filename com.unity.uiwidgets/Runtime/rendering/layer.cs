﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.gestures;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.ui;
using UnityEngine;
using Color = Unity.UIWidgets.ui.Color;
using Rect = Unity.UIWidgets.ui.Rect;

namespace Unity.UIWidgets.rendering {
    public abstract class Layer : AbstractNodeMixinDiagnosticableTree {
        public new ContainerLayer parent {
            get { return (ContainerLayer) base.parent; }
        }

        bool _needsAddToScene = true;

        protected void markNeedsAddToScene() {
            _needsAddToScene = true;
        }

        protected virtual bool alwaysNeedsAddToScene {
            get { return false; }
        }

        internal bool _subtreeNeedsAddToScene;

        flow.Layer _engineLayer;

        internal virtual void updateSubtreeNeedsAddToScene() {
            _subtreeNeedsAddToScene = _needsAddToScene || alwaysNeedsAddToScene;
        }

        public Layer nextSibling {
            get { return _nextSibling; }
        }

        internal Layer _nextSibling;

        public Layer previousSibling {
            get { return _previousSibling; }
        }

        internal Layer _previousSibling;

        protected override void dropChild(AbstractNodeMixinDiagnosticableTree child) {
            markNeedsAddToScene();
            base.dropChild(child);
        }

        protected override void adoptChild(AbstractNodeMixinDiagnosticableTree child) {
            markNeedsAddToScene();
            base.adoptChild(child);
        }

        public virtual void remove() {
            if (parent != null) {
                parent._removeChild(this);
            }
        }

        public void replaceWith(Layer newLayer) {
            D.assert(parent != null);
            D.assert(attached == parent.attached);
            D.assert(newLayer.parent == null);
            D.assert(newLayer._nextSibling == null);
            D.assert(newLayer._previousSibling == null);
            D.assert(!newLayer.attached);

            newLayer._nextSibling = nextSibling;
            if (_nextSibling != null) {
                _nextSibling._previousSibling = newLayer;
            }

            newLayer._previousSibling = previousSibling;
            if (_previousSibling != null) {
                _previousSibling._nextSibling = newLayer;
            }

            D.assert(() => {
                Layer node = this;
                while (node.parent != null) {
                    node = node.parent;
                }

                D.assert(node != newLayer);
                return true;
            });

            parent.adoptChild(newLayer);
            D.assert(newLayer.attached == parent.attached);

            if (parent.firstChild == this) {
                parent._firstChild = newLayer;
            }

            if (parent.lastChild == this) {
                parent._lastChild = newLayer;
            }

            _nextSibling = null;
            _previousSibling = null;
            parent.dropChild(this);
            D.assert(!attached);
        }

        internal abstract S find<S>(Offset regionOffset) where S : class;

        internal abstract flow.Layer addToScene(SceneBuilder builder, Offset layerOffset = null);

        internal void _addToSceneWithRetainedRendering(SceneBuilder builder) {
            if (!_subtreeNeedsAddToScene && _engineLayer != null) {
                builder.addRetained(_engineLayer);
                return;
            }

            _engineLayer = addToScene(builder);
            _needsAddToScene = false;
        }

        public object debugCreator;

        public override string toStringShort() {
            return base.toStringShort() + (owner == null ? " DETACHED" : "");
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<object>("owner", owner,
                level: parent != null ? DiagnosticLevel.hidden : DiagnosticLevel.info,
                defaultValue: foundation_.kNullDefaultValue));
            properties.add(new DiagnosticsProperty<object>("creator", debugCreator,
                defaultValue: foundation_.kNullDefaultValue, level: DiagnosticLevel.debug));
        }
    }

    public class PictureLayer : Layer {
        public PictureLayer(Rect canvasBounds) {
            this.canvasBounds = canvasBounds;
        }

        public readonly Rect canvasBounds;

        Picture _picture;

        public Picture picture {
            get { return _picture; }
            set {
                markNeedsAddToScene();
                _picture = value;
            }
        }

        bool _isComplexHint = false;

        public bool isComplexHint {
            get { return _isComplexHint; }
            set {
                if (value != _isComplexHint) {
                    _isComplexHint = value;
                    markNeedsAddToScene();
                }
            }
        }

        bool _willChangeHint = false;

        public bool willChangeHint {
            get { return _willChangeHint; }
            set {
                if (value != _willChangeHint) {
                    _willChangeHint = value;
                    markNeedsAddToScene();
                }
            }
        }

        internal override S find<S>(Offset regionOffset) {
            return null;
        }

        internal override flow.Layer addToScene(SceneBuilder builder, Offset layerOffset = null) {
            layerOffset = layerOffset ?? Offset.zero;

            builder.addPicture(layerOffset, picture,
                isComplexHint: isComplexHint, willChangeHint: willChangeHint);
            return null;
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<Rect>("paint bounds", canvasBounds));
        }
    }

    public class TextureLayer : Layer {
        public TextureLayer(
            Rect rect,
            Texture texture,
            bool freeze = false
        ) {
            D.assert(rect != null);
            D.assert(texture != null);

            this.rect = rect;
            this.texture = texture;
            this.freeze = freeze;
        }

        public readonly Rect rect;

        public readonly Texture texture;

        public readonly bool freeze;

        internal override S find<S>(Offset regionOffset) {
            return null;
        }

        internal override flow.Layer addToScene(SceneBuilder builder, Offset layerOffset = null) {
            layerOffset = layerOffset ?? Offset.zero;

            Rect shiftedRect = rect.shift(layerOffset);
            builder.addTexture(
                texture,
                offset: shiftedRect.topLeft,
                width: shiftedRect.width,
                height: shiftedRect.height,
                freeze: freeze
            );
            return null;
        }
    }

    public class ContainerLayer : Layer {
        public Layer firstChild {
            get { return _firstChild; }
        }

        internal Layer _firstChild;

        public Layer lastChild {
            get { return _lastChild; }
        }

        internal override S find<S>(Offset regionOffset) {
            Layer current = lastChild;
            while (current != null) {
                S value = current.find<S>(regionOffset);
                if (value != null) {
                    return value;
                }

                current = current.previousSibling;
            }

            return null;
        }

        internal Layer _lastChild;

        bool _debugUltimatePreviousSiblingOf(Layer child, Layer equals = null) {
            D.assert(child.attached == attached);
            while (child.previousSibling != null) {
                D.assert(child.previousSibling != child);
                child = child.previousSibling;
                D.assert(child.attached == attached);
            }

            return child == equals;
        }

        bool _debugUltimateNextSiblingOf(Layer child, Layer equals = null) {
            D.assert(child.attached == attached);
            while (child._nextSibling != null) {
                D.assert(child._nextSibling != child);
                child = child._nextSibling;
                D.assert(child.attached == attached);
            }

            return child == equals;
        }

        PictureLayer _highlightConflictingLayer(PhysicalModelLayer child) {
            PictureRecorder recorder = new PictureRecorder();
            var canvas = new RecorderCanvas(recorder);
            canvas.drawPath(child.clipPath, new Paint() {
                color = new Color(0xFFAA0000),
                style = PaintingStyle.stroke,
                strokeWidth = child.elevation + 10.0f,
            });
            PictureLayer pictureLayer = new PictureLayer(child.clipPath.getBounds());
            pictureLayer.picture = recorder.endRecording();
            pictureLayer.debugCreator = child;
            child.append(pictureLayer);
            return pictureLayer;
        }

        List<PictureLayer> _processConflictingPhysicalLayers(PhysicalModelLayer predecessor, PhysicalModelLayer child) {
            UIWidgetsError.reportError(new UIWidgetsErrorDetails(
                exception: new UIWidgetsError("Painting order is out of order with respect to elevation.\n" +
                                              "See https://api.flutter.dev/flutter/rendering/debugCheckElevations.html " +
                                              "for more details."),
                context: "during compositing",
                informationCollector: (StringBuilder builder) => {
                    builder.AppendLine("Attempted to composite layer");
                    builder.AppendLine(child.ToString());
                    builder.AppendLine("after layer");
                    builder.AppendLine(predecessor.ToString());
                    builder.AppendLine("which occupies the same area at a higher elevation.");
                }
            ));
            return new List<PictureLayer> {
                _highlightConflictingLayer(predecessor),
                _highlightConflictingLayer(child)
            };
        }

        protected List<PictureLayer> _debugCheckElevations() {
            List<PhysicalModelLayer> physicalModelLayers =
                depthFirstIterateChildren().OfType<PhysicalModelLayer>().ToList();
            List<PictureLayer> addedLayers = new List<PictureLayer>();

            for (int i = 0; i < physicalModelLayers.Count; i++) {
                PhysicalModelLayer physicalModelLayer = physicalModelLayers[i];
                D.assert(physicalModelLayer.lastChild?.debugCreator != physicalModelLayer,
                    () => "debugCheckElevations has either already visited this layer or failed to remove the" +
                          " added picture from it.");
                float accumulatedElevation = physicalModelLayer.elevation;
                Layer ancestor = physicalModelLayer.parent;
                while (ancestor != null) {
                    if (ancestor is PhysicalModelLayer modelLayer) {
                        accumulatedElevation += modelLayer.elevation;
                    }

                    ancestor = ancestor.parent;
                }

                for (int j = 0; j <= i; j++) {
                    PhysicalModelLayer predecessor = physicalModelLayers[j];
                    float predecessorAccumulatedElevation = predecessor.elevation;
                    ancestor = predecessor.parent;
                    while (ancestor != null) {
                        if (ancestor == predecessor) {
                            continue;
                        }

                        if (ancestor is PhysicalModelLayer modelLayer) {
                            predecessorAccumulatedElevation += modelLayer.elevation;
                        }

                        ancestor = ancestor.parent;
                    }

                    if (predecessorAccumulatedElevation <= accumulatedElevation) {
                        continue;
                    }

                    Path intersection = Path.combine(
                        PathOperation.intersect,
                        predecessor._debugTransformedClipPath,
                        physicalModelLayer._debugTransformedClipPath);

                    if (intersection != null && intersection.computeMetrics().Any((metric) => metric.length > 0)) {
                        addedLayers.AddRange(_processConflictingPhysicalLayers(predecessor, physicalModelLayer));
                    }
                }
            }

            return addedLayers;
        }

        internal override void updateSubtreeNeedsAddToScene() {
            base.updateSubtreeNeedsAddToScene();
            Layer child = firstChild;
            while (child != null) {
                child.updateSubtreeNeedsAddToScene();
                _subtreeNeedsAddToScene = _subtreeNeedsAddToScene || child._subtreeNeedsAddToScene;
                child = child.nextSibling;
            }
        }

        public override void attach(object owner) {
            base.attach(owner);

            var child = firstChild;
            while (child != null) {
                child.attach(owner);
                child = child.nextSibling;
            }
        }

        public override void detach() {
            base.detach();

            var child = firstChild;
            while (child != null) {
                child.detach();
                child = child.nextSibling;
            }
        }

        public void append(Layer child) {
            D.assert(child != this);
            D.assert(child != firstChild);
            D.assert(child != lastChild);
            D.assert(child.parent == null);
            D.assert(!child.attached);
            D.assert(child.nextSibling == null);
            D.assert(child.previousSibling == null);
            D.assert(() => {
                Layer node = this;
                while (node.parent != null) {
                    node = node.parent;
                }

                D.assert(node != child);
                return true;
            });

            adoptChild(child);
            child._previousSibling = lastChild;
            if (lastChild != null) {
                lastChild._nextSibling = child;
            }

            _lastChild = child;
            if (_firstChild == null) {
                _firstChild = child;
            }

            D.assert(child.attached == attached);
        }

        internal void _removeChild(Layer child) {
            D.assert(child.parent == this);
            D.assert(child.attached == attached);
            D.assert(_debugUltimatePreviousSiblingOf(child, equals: firstChild));
            D.assert(_debugUltimateNextSiblingOf(child, equals: lastChild));

            if (child._previousSibling == null) {
                D.assert(firstChild == child);
                _firstChild = child.nextSibling;
            }
            else {
                child._previousSibling._nextSibling = child.nextSibling;
            }

            if (child._nextSibling == null) {
                D.assert(lastChild == child);
                _lastChild = child.previousSibling;
            }
            else {
                child._nextSibling._previousSibling = child.previousSibling;
            }

            D.assert((firstChild == null) == (lastChild == null));
            D.assert(firstChild == null || firstChild.attached == attached);
            D.assert(lastChild == null || lastChild.attached == attached);
            D.assert(firstChild == null ||
                     _debugUltimateNextSiblingOf(firstChild, equals: lastChild));
            D.assert(lastChild == null ||
                     _debugUltimatePreviousSiblingOf(lastChild, equals: firstChild));

            child._nextSibling = null;
            child._previousSibling = null;
            dropChild(child);
            D.assert(!child.attached);
        }

        public void removeAllChildren() {
            Layer child = firstChild;
            while (child != null) {
                Layer next = child.nextSibling;
                child._previousSibling = null;
                child._nextSibling = null;
                D.assert(child.attached == attached);
                dropChild(child);
                child = next;
            }

            _firstChild = null;
            _lastChild = null;
        }

        internal override flow.Layer addToScene(SceneBuilder builder, Offset layerOffset = null) {
            addChildrenToScene(builder, layerOffset);
            return null;
        }

        public void addChildrenToScene(SceneBuilder builder, Offset childOffset = null) {
            Layer child = firstChild;
            while (child != null) {
                if (childOffset == null || childOffset == Offset.zero) {
                    child._addToSceneWithRetainedRendering(builder);
                }
                else {
                    child.addToScene(builder, childOffset);
                }

                child = child.nextSibling;
            }
        }

        public virtual void applyTransform(Layer child, Matrix4 transform) {
            D.assert(child != null);
            D.assert(transform != null);
        }

        public List<Layer> depthFirstIterateChildren() {
            if (firstChild == null) {
                return new List<Layer>();
            }

            List<Layer> children = new List<Layer>();
            Layer child = firstChild;
            while (child != null) {
                children.Add(child);
                if (child is ContainerLayer containerLayer) {
                    children.AddRange(containerLayer.depthFirstIterateChildren());
                }

                child = child.nextSibling;
            }

            return children;
        }

        public override List<DiagnosticsNode> debugDescribeChildren() {
            var children = new List<DiagnosticsNode>();
            if (firstChild == null) {
                return children;
            }

            Layer child = firstChild;
            int count = 1;
            while (true) {
                children.Add(child.toDiagnosticsNode(name: "child " + count));
                if (child == lastChild) {
                    break;
                }

                count += 1;
                child = child.nextSibling;
            }

            return children;
        }
    }

    public class OffsetLayer : ContainerLayer {
        public OffsetLayer(Offset offset = null) {
            _offset = offset ?? Offset.zero;
        }

        Offset _offset;

        public Offset offset {
            get { return _offset; }
            set {
                value = value ?? Offset.zero;
                if (value != _offset) {
                    _offset = value;
                    markNeedsAddToScene();
                }
            }
        }

        internal override S find<S>(Offset regionOffset) {
            return base.find<S>(regionOffset - offset);
        }

        public override void applyTransform(Layer child, Matrix4 transform) {
            D.assert(child != null);
            D.assert(transform != null);
            transform.translate(offset.dx, offset.dy);
        }

        public Scene buildScene(SceneBuilder builder) {
            List<PictureLayer> temporaryLayers = null;
            D.assert(() => {
                if (RenderingDebugUtils.debugCheckElevationsEnabled) {
                    temporaryLayers = _debugCheckElevations();
                }

                return true;
            });
            updateSubtreeNeedsAddToScene();
            addToScene(builder);
            Scene scene = builder.build();
            D.assert(() => {
                if (temporaryLayers != null) {
                    foreach (PictureLayer temporaryLayer in temporaryLayers) {
                        temporaryLayer.remove();
                    }
                }

                return true;
            });
            return scene;
        }

        internal override flow.Layer addToScene(SceneBuilder builder, Offset layerOffset = null) {
            layerOffset = layerOffset ?? Offset.zero;

            var engineLayer = builder.pushOffset(
                (float) (layerOffset.dx + offset.dx),
                (float) (layerOffset.dy + offset.dy));
            addChildrenToScene(builder);
            builder.pop();
            return engineLayer;
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<Offset>("offset", offset));
        }
    }

    public class ClipRectLayer : ContainerLayer {
        public ClipRectLayer(
            Rect clipRect = null,
            Clip clipBehavior = Clip.hardEdge
        ) {
            D.assert(clipRect != null);
            D.assert(clipBehavior != Clip.none);
            _clipRect = clipRect;
            _clipBehavior = clipBehavior;
        }

        Rect _clipRect;

        public Rect clipRect {
            get { return _clipRect; }
            set {
                if (value != _clipRect) {
                    _clipRect = value;
                    markNeedsAddToScene();
                }
            }
        }

        Clip _clipBehavior;

        public Clip clipBehavior {
            get { return _clipBehavior; }
            set {
                D.assert(value != Clip.none);
                if (value != _clipBehavior) {
                    _clipBehavior = value;
                    markNeedsAddToScene();
                }
            }
        }

        internal override S find<S>(Offset regionOffset) {
            if (!clipRect.contains(regionOffset)) {
                return null;
            }

            return base.find<S>(regionOffset);
        }

        internal override flow.Layer addToScene(SceneBuilder builder, Offset layerOffset = null) {
            layerOffset = layerOffset ?? Offset.zero;

            bool enabled = true;
            D.assert(() => {
                enabled = !D.debugDisableClipLayers;
                return true;
            });

            if (enabled) {
                builder.pushClipRect(clipRect.shift(layerOffset));
            }

            addChildrenToScene(builder, layerOffset);

            if (enabled) {
                builder.pop();
            }

            return null;
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<Rect>("clipRect", clipRect));
        }
    }

    public class ClipRRectLayer : ContainerLayer {
        public ClipRRectLayer(
            RRect clipRRect = null,
            Clip clipBehavior = Clip.hardEdge
        ) {
            D.assert(clipRRect != null);
            D.assert(clipBehavior != Clip.none);
            _clipRRect = clipRRect;
            _clipBehavior = clipBehavior;
        }

        RRect _clipRRect;

        public RRect clipRRect {
            get { return _clipRRect; }
            set {
                if (value != _clipRRect) {
                    _clipRRect = value;
                    markNeedsAddToScene();
                }
            }
        }

        Clip _clipBehavior;

        public Clip clipBehavior {
            get { return _clipBehavior; }
            set {
                D.assert(value != Clip.none);
                if (value != _clipBehavior) {
                    _clipBehavior = value;
                    markNeedsAddToScene();
                }
            }
        }

        internal override S find<S>(Offset regionOffset) {
            if (!clipRRect.contains(regionOffset)) {
                return null;
            }

            return base.find<S>(regionOffset);
        }

        internal override flow.Layer addToScene(SceneBuilder builder, Offset layerOffset = null) {
            layerOffset = layerOffset ?? Offset.zero;

            bool enabled = true;
            D.assert(() => {
                enabled = !D.debugDisableClipLayers;
                return true;
            });

            if (enabled) {
                builder.pushClipRRect(clipRRect.shift(layerOffset));
            }

            addChildrenToScene(builder, layerOffset);

            if (enabled) {
                builder.pop();
            }

            return null;
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<RRect>("clipRRect", clipRRect));
        }
    }

    public class ClipPathLayer : ContainerLayer {
        public ClipPathLayer(
            Path clipPath = null,
            Clip clipBehavior = Clip.hardEdge
        ) {
            D.assert(clipPath != null);
            D.assert(clipBehavior != Clip.none);
            _clipPath = clipPath;
            _clipBehavior = clipBehavior;
        }

        Path _clipPath;

        public Path clipPath {
            get { return _clipPath; }
            set {
                if (value != _clipPath) {
                    _clipPath = value;
                    markNeedsAddToScene();
                }
            }
        }

        Clip _clipBehavior;

        public Clip clipBehavior {
            get { return _clipBehavior; }
            set {
                D.assert(value != Clip.none);
                if (value != _clipBehavior) {
                    _clipBehavior = value;
                    markNeedsAddToScene();
                }
            }
        }

        internal override S find<S>(Offset regionOffset) {
            if (!clipPath.contains(regionOffset)) {
                return null;
            }

            return base.find<S>(regionOffset);
        }

        internal override flow.Layer addToScene(SceneBuilder builder, Offset layerOffset = null) {
            layerOffset = layerOffset ?? Offset.zero;

            bool enabled = true;
            D.assert(() => {
                enabled = !D.debugDisableClipLayers;
                return true;
            });

            if (enabled) {
                builder.pushClipPath(clipPath.shift(layerOffset));
            }

            addChildrenToScene(builder, layerOffset);

            if (enabled) {
                builder.pop();
            }

            return null;
        }
    }

    public class TransformLayer : OffsetLayer {
        public TransformLayer(Matrix4 transform = null, Offset offset = null) : base(offset) {
            _transform = transform ?? new Matrix4().identity();
        }

        public Matrix4 transform {
            get { return _transform; }
        }

        Matrix4 _transform;
        Matrix4 _lastEffectiveTransform;

        Matrix4 _invertedTransform;
        bool _inverseDirty = true;

        internal override S find<S>(Offset regionOffset) {
            if (_inverseDirty) {
                _invertedTransform = Matrix4.tryInvert(
                    PointerEvent.removePerspectiveTransform(transform)
                );
                _inverseDirty = false;
            }

            if (_invertedTransform == null) {
                return null;
            }
            Vector4 vector = new Vector4(regionOffset.dx, regionOffset.dy, 0, 1);
            Vector4 result = _invertedTransform.transform(vector);
            return base.find<S>(new Offset(result[0], result[1]));
        }

        internal override flow.Layer addToScene(SceneBuilder builder, Offset layerOffset = null) {
            layerOffset = layerOffset ?? Offset.zero;

            _lastEffectiveTransform = _transform;

            var totalOffset = offset + layerOffset;
            if (totalOffset != Offset.zero) {
                _lastEffectiveTransform = new Matrix4().translationValues(totalOffset.dx, totalOffset.dy, 0);
                _lastEffectiveTransform.multiply(transform);
            }

            builder.pushTransform(_lastEffectiveTransform.toMatrix3());
            addChildrenToScene(builder);
            builder.pop();
            return null;
        }

        public override void applyTransform(Layer child, Matrix4 transform) {
            D.assert(child != null);
            D.assert(transform != null);
            D.assert(_lastEffectiveTransform != null || this.transform != null);
            if (_lastEffectiveTransform == null) {
                transform.multiply(this.transform);
            }
            else {
                transform.multiply(_lastEffectiveTransform);
            }
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<Matrix4>("transform", transform));
        }
    }

    public class OpacityLayer : ContainerLayer {
        public OpacityLayer(int alpha = 255, Offset offset = null) {
            _alpha = alpha;
            _offset = offset ?? Offset.zero;
        }

        int _alpha;

        public int alpha {
            get { return _alpha; }
            set {
                if (value != _alpha) {
                    _alpha = value;
                    markNeedsAddToScene();
                }
            }
        }

        Offset _offset;

        public Offset offset {
            get { return _offset; }
            set {
                value = value ?? Offset.zero;
                if (value != _offset) {
                    _offset = value;
                    markNeedsAddToScene();
                }
            }
        }

        internal override flow.Layer addToScene(SceneBuilder builder, Offset layerOffset = null) {
            layerOffset = layerOffset ?? Offset.zero;

            bool enabled = true;
            D.assert(() => {
                enabled = !D.debugDisableOpacityLayers;
                return true;
            });
            if (enabled) {
                builder.pushOpacity(alpha, offset: offset + layerOffset);
            }

            addChildrenToScene(builder, layerOffset);
            if (enabled) {
                builder.pop();
            }

            return null;
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new IntProperty("alpha", alpha));
            properties.add(new DiagnosticsProperty<Offset>("offset", offset));
        }
    }

    public class BackdropFilterLayer : ContainerLayer {
        public BackdropFilterLayer(ImageFilter filter = null) {
            D.assert(filter != null);
            _filter = filter;
        }

        ImageFilter _filter;

        public ImageFilter filter {
            get { return _filter; }
            set {
                if (value != _filter) {
                    _filter = value;
                    markNeedsAddToScene();
                }
            }
        }

        internal override flow.Layer addToScene(SceneBuilder builder, Offset layerOffset = null) {
            builder.pushBackdropFilter(filter);
            addChildrenToScene(builder, layerOffset);
            builder.pop();
            return null;
        }
    }

    public class LayerLink {
        public LeaderLayer leader {
            get { return _leader; }
        }

        internal LeaderLayer _leader;

        public override string ToString() {
            return $"{foundation_.describeIdentity(this)}({(_leader != null ? "<linked>" : "<dangling>")})";
        }
    }

    public class LeaderLayer : ContainerLayer {
        public LeaderLayer(LayerLink link, Offset offset = null) {
            D.assert(link != null);
            offset = offset ?? Offset.zero;
            this.link = link;
            this.offset = offset;
        }

        public readonly LayerLink link;

        public Offset offset;

        protected override bool alwaysNeedsAddToScene {
            get { return true; }
        }

        public override void attach(object owner) {
            base.attach(owner);
            D.assert(link.leader == null);
            _lastOffset = null;
            link._leader = this;
        }

        public override void detach() {
            D.assert(link.leader == this);
            link._leader = null;
            _lastOffset = null;
            base.detach();
        }

        internal Offset _lastOffset;

        internal override S find<S>(Offset regionOffset) {
            return base.find<S>(regionOffset - offset);
        }

        internal override flow.Layer addToScene(SceneBuilder builder, Offset layerOffset = null) {
            layerOffset = layerOffset ?? Offset.zero;

            D.assert(offset != null);
            _lastOffset = offset + layerOffset;
            if (_lastOffset != Offset.zero) {
                builder.pushTransform(new Matrix4()
                    .translationValues(_lastOffset.dx, _lastOffset.dy,0)
                    .toMatrix3());
            }

            addChildrenToScene(builder, Offset.zero);
            if (_lastOffset != Offset.zero) {
                builder.pop();
            }

            return null;
        }

        public override void applyTransform(Layer child, Matrix4 transform) {
            D.assert(_lastOffset != null);
            if (_lastOffset != Offset.zero) {
                transform.translate(_lastOffset.dx, _lastOffset.dy);
            }
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<Offset>("offset", offset));
            properties.add(new DiagnosticsProperty<LayerLink>("link", link));
        }
    }

    public class FollowerLayer : ContainerLayer {
        public FollowerLayer(
            LayerLink link = null,
            bool showWhenUnlinked = true,
            Offset unlinkedOffset = null,
            Offset linkedOffset = null
        ) {
            D.assert(link != null);
            this.link = link;
            this.showWhenUnlinked = showWhenUnlinked;
            this.unlinkedOffset = unlinkedOffset ?? Offset.zero;
            this.linkedOffset = linkedOffset ?? Offset.zero;
        }

        public readonly LayerLink link;
        public bool showWhenUnlinked;
        public Offset unlinkedOffset;
        public Offset linkedOffset;

        Offset _lastOffset;
        Matrix4 _lastTransform;

        Matrix4 _invertedTransform = new Matrix4().identity();
        bool _inverseDirty = true;

        internal override S find<S>(Offset regionOffset) {
            if (link.leader == null) {
                return showWhenUnlinked ? base.find<S>(regionOffset - unlinkedOffset) : null;
            }

            if (_inverseDirty) {
                _invertedTransform = Matrix4.tryInvert(getLastTransform());
                _inverseDirty = false;
            }

            if (_invertedTransform == null) {
                return null;
            }

            Vector4 vector = new Vector4(regionOffset.dx, regionOffset.dy, 0, 1);
            Vector4 result = _invertedTransform.transform(vector);
            return base.find<S>(new Offset(result[0] - linkedOffset.dx, result[1] - linkedOffset.dy));
        }

        public Matrix4 getLastTransform() {
            if (_lastTransform == null) {
                return null;
            }

            Matrix4 result = new Matrix4().translationValues(-_lastOffset.dx, -_lastOffset.dy,0 );
            result.multiply(_lastTransform);
            return result;
        }

        Matrix4 _collectTransformForLayerChain(List<ContainerLayer> layers) {
            Matrix4 result = new Matrix4().identity();
            for (int index = layers.Count - 1; index > 0; index -= 1) {
                layers[index].applyTransform(layers[index - 1], result);
            }

            return result;
        }

        void _establishTransform() {
            D.assert(link != null);
            _lastTransform = null;
            if (link._leader == null) {
                return;
            }

            D.assert(link.leader.owner == owner,
                () => "Linked LeaderLayer anchor is not in the same layer tree as the FollowerLayer.");
            D.assert(link.leader._lastOffset != null,
                () => "LeaderLayer anchor must come before FollowerLayer in paint order, but the reverse was true.");

            HashSet<Layer> ancestors = new HashSet<Layer>();
            Layer ancestor = parent;
            while (ancestor != null) {
                ancestors.Add(ancestor);
                ancestor = ancestor.parent;
            }

            ContainerLayer layer = link.leader;
            List<ContainerLayer> forwardLayers = new List<ContainerLayer> {null, layer};
            do {
                layer = layer.parent;
                forwardLayers.Add(layer);
            } while (!ancestors.Contains(layer));

            ancestor = layer;

            layer = this;
            List<ContainerLayer> inverseLayers = new List<ContainerLayer> {layer};
            do {
                layer = layer.parent;
                inverseLayers.Add(layer);
            } while (layer != ancestor);

            Matrix4 forwardTransform = _collectTransformForLayerChain(forwardLayers);
            Matrix4 inverseTransform = _collectTransformForLayerChain(inverseLayers);
            if (inverseTransform.invert() == 0) {
                return;
            }

            inverseTransform.multiply(forwardTransform);
            inverseTransform.translate(linkedOffset.dx, linkedOffset.dy);
            _lastTransform = inverseTransform;
            _inverseDirty = true;
        }

        protected override bool alwaysNeedsAddToScene {
            get { return true; }
        }


        internal override flow.Layer addToScene(SceneBuilder builder, Offset layerOffset = null) {
            layerOffset = layerOffset ?? Offset.zero;

            D.assert(link != null);
            if (link.leader == null && !showWhenUnlinked) {
                _lastTransform = null;
                _lastOffset = null;
                _inverseDirty = true;
                return null;
            }

            _establishTransform();
            if (_lastTransform != null) {
                builder.pushTransform(_lastTransform.toMatrix3());
                addChildrenToScene(builder);
                builder.pop();
                _lastOffset = unlinkedOffset + layerOffset;
            }
            else {
                _lastOffset = null;
                var matrix = new Matrix4().translationValues(unlinkedOffset.dx, unlinkedOffset.dy, 0);
                builder.pushTransform(matrix.toMatrix3());
                addChildrenToScene(builder);
                builder.pop();
            }

            _inverseDirty = true;
            return null;
        }

        public override void applyTransform(Layer child, Matrix4 transform) {
            D.assert(child != null);
            D.assert(transform != null);
            if (_lastTransform != null) {
                transform.multiply(_lastTransform);
            }
            else {
                transform.multiply(new Matrix4().translationValues(unlinkedOffset.dx, unlinkedOffset.dy, 0));
            }
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<LayerLink>("link", link));
            properties.add(new TransformProperty("transform", getLastTransform(),
                defaultValue: foundation_.kNullDefaultValue));
        }
    }

    public class PerformanceOverlayLayer : Layer {
        public PerformanceOverlayLayer(
            Rect overlayRect = null,
            int? optionsMask = null
        ) {
            D.assert(overlayRect != null);
            D.assert(optionsMask != null);
            _overlayRect = overlayRect;
            this.optionsMask = optionsMask ?? 0;
        }

        public Rect overlayRect {
            get { return _overlayRect; }
            set {
                if (value != _overlayRect) {
                    _overlayRect = value;
                    markNeedsAddToScene();
                }
            }
        }

        Rect _overlayRect;

        public readonly int optionsMask;

        internal override S find<S>(Offset regionOffset) {
            return null;
        }

        internal override flow.Layer addToScene(SceneBuilder builder, Offset layerOffset = null) {
            layerOffset = layerOffset ?? Offset.zero;

            builder.addPerformanceOverlay(optionsMask, overlayRect.shift(layerOffset));
            return null;
        }
    }

    public class AnnotatedRegionLayer<T> : ContainerLayer
        where T : class {
        public AnnotatedRegionLayer(
            T value = null,
            Size size = null,
            Offset offset = null) {
            offset = offset ?? Offset.zero;
            D.assert(value != null);
            this.value = value;
            this.size = size;
            this.offset = offset;
        }

        public readonly T value;

        public readonly Size size;

        public readonly Offset offset;

        internal override S find<S>(Offset regionOffset) {
            S result = base.find<S>(regionOffset);
            if (result != null) {
                return result;
            }

            if (size != null && !(offset & size).contains(regionOffset)) {
                return null;
            }

            if (typeof(T) == typeof(S)) {
                S typedResult = value as S;
                return typedResult;
            }

            return base.find<S>(regionOffset);
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<T>("value", value));
            properties.add(new DiagnosticsProperty<Size>("size", size, defaultValue: null));
            properties.add(new DiagnosticsProperty<Offset>("offset", offset, defaultValue: null));
        }
    }


    public class PhysicalModelLayer : ContainerLayer {
        public PhysicalModelLayer(
            Path clipPath = null,
            Clip clipBehavior = Clip.none,
            float? elevation = null,
            Color color = null,
            Color shadowColor = null) {
            D.assert(clipPath != null);
            D.assert(elevation != null);
            D.assert(color != null);
            D.assert(shadowColor != null);
            _clipPath = clipPath;
            _clipBehavior = clipBehavior;
            _elevation = elevation.Value;
            _color = color;
            this.shadowColor = shadowColor;
        }

        public Path clipPath {
            get { return _clipPath; }
            set {
                if (value != _clipPath) {
                    _clipPath = value;
                    markNeedsAddToScene();
                }
            }
        }

        Path _clipPath;

        public Clip clipBehavior {
            get { return _clipBehavior; }
            set {
                if (value != _clipBehavior) {
                    _clipBehavior = value;
                    markNeedsAddToScene();
                }
            }
        }

        internal Path _debugTransformedClipPath {
            get {
                ContainerLayer ancestor = parent;
                Matrix4 matrix = new Matrix4().identity();
                while (ancestor != null && ancestor.parent != null) {
                    ancestor.applyTransform(this, matrix);
                    ancestor = ancestor.parent;
                }

                return clipPath.transform(matrix.toMatrix3());
            }
        }


        Clip _clipBehavior;

        public float elevation {
            get { return _elevation; }
            set {
                if (value != _elevation) {
                    _elevation = value;
                    markNeedsAddToScene();
                }
            }
        }

        float _elevation;

        public Color color {
            get { return _color; }
            set {
                if (value != _color) {
                    _color = value;
                    markNeedsAddToScene();
                }
            }
        }

        Color _color;

        public Color shadowColor {
            get { return _shadowColor; }
            set {
                if (value != _shadowColor) {
                    _shadowColor = value;
                    markNeedsAddToScene();
                }
            }
        }

        Color _shadowColor;

        internal override S find<S>(Offset regionOffset) {
            if (!clipPath.contains(regionOffset)) {
                return null;
            }

            return base.find<S>(regionOffset);
        }

        internal override flow.Layer addToScene(SceneBuilder builder, Offset layerOffset = null) {
            layerOffset = layerOffset ?? Offset.zero;

            builder.pushPhysicalShape(
                path: clipPath.shift(layerOffset),
                elevation: elevation,
                color: color,
                shadowColor: shadowColor,
                clipBehavior: clipBehavior);

            addChildrenToScene(builder, layerOffset);

            builder.pop();
            return null;
        }


        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new FloatProperty("elevation", elevation));
            properties.add(new DiagnosticsProperty<Color>("color", color));
        }
    }
}