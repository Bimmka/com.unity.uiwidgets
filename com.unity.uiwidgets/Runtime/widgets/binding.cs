using System;
using System.Collections.Generic;
using RSG;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.ui;
using UnityEngine;

namespace Unity.UIWidgets.widgets {
    public interface WidgetsBindingObserver {
        void didChangeMetrics();

        void didChangeTextScaleFactor();

        void didChangePlatformBrightness();

        void didChangeLocales(List<Locale> locale);

        IPromise<bool> didPopRoute();

        IPromise<bool> didPushRoute(string route);
    }

    public class WidgetsBinding : RendererBinding {
        public new static WidgetsBinding instance {
            get { return (WidgetsBinding) RendererBinding.instance; }
            set { RendererBinding.instance = value; }
        }

        public WidgetsBinding(bool inEditorWindow = false) : base(inEditorWindow) {
            buildOwner.onBuildScheduled = _handleBuildScheduled;
            Window.instance.onLocaleChanged += handleLocaleChanged;
            widgetInspectorService = new WidgetInspectorService(this);
            addPersistentFrameCallback((duration) => {
                TextBlobMesh.tickNextFrame();
                TessellationGenerator.tickNextFrame();
                uiTessellationGenerator.tickNextFrame();
                uiPathCacheManager.tickNextFrame();
            });
        }

        public BuildOwner buildOwner {
            get { return _buildOwner; }
        }

        readonly BuildOwner _buildOwner = new BuildOwner();

        public FocusManager focusManager {
            get { return _buildOwner.focusManager; }
        }

        readonly List<WidgetsBindingObserver> _observers = new List<WidgetsBindingObserver>();

        public void addObserver(WidgetsBindingObserver observer) {
            _observers.Add(observer);
        }

        public bool removeObserver(WidgetsBindingObserver observer) {
            return _observers.Remove(observer);
        }

        public void handlePopRoute() {
            var idx = -1;
            
            void _handlePopRouteSub(bool result) {
                if (!result) {
                    idx++;
                    if (idx >= _observers.Count) {
                        Application.Quit();
                        return;
                    }
                    _observers[idx].didPopRoute().Then((Action<bool>) _handlePopRouteSub);
                }
            }
            
            _handlePopRouteSub(false);
        }

        public readonly WidgetInspectorService widgetInspectorService;

        protected override void handleMetricsChanged() {
            base.handleMetricsChanged();
            foreach (WidgetsBindingObserver observer in _observers) {
                observer.didChangeMetrics();
            }
        }

        protected override void handleTextScaleFactorChanged() {
            base.handleTextScaleFactorChanged();
            foreach (WidgetsBindingObserver observer in _observers) {
                observer.didChangeTextScaleFactor();
            }
        }
        
        protected override void handlePlatformBrightnessChanged() {
            base.handlePlatformBrightnessChanged();
            foreach (WidgetsBindingObserver observer in _observers) {
                observer.didChangePlatformBrightness();
            }
        }

        protected virtual void handleLocaleChanged() {
            dispatchLocalesChanged(Window.instance.locales);
        }

        protected virtual void dispatchLocalesChanged(List<Locale> locales) {
            foreach (WidgetsBindingObserver observer in _observers) {
                observer.didChangeLocales(locales);
            }
        }

        void _handleBuildScheduled() {
            D.assert(() => {
                if (debugBuildingDirtyElements) {
                    throw new UIWidgetsError(
                        "Build scheduled during frame.\n" +
                        "While the widget tree was being built, laid out, and painted, " +
                        "a new frame was scheduled to rebuild the widget tree. " +
                        "This might be because setState() was called from a layout or " +
                        "paint callback. " +
                        "If a change is needed to the widget tree, it should be applied " +
                        "as the tree is being built. Scheduling a change for the subsequent " +
                        "frame instead results in an interface that lags behind by one frame. " +
                        "If this was done to make your build dependent on a size measured at " +
                        "layout time, consider using a LayoutBuilder, CustomSingleChildLayout, " +
                        "or CustomMultiChildLayout. If, on the other hand, the one frame delay " +
                        "is the desired effect, for example because this is an " +
                        "animation, consider scheduling the frame in a post-frame callback " +
                        "using SchedulerBinding.addPostFrameCallback or " +
                        "using an AnimationController to trigger the animation."
                    );
                }

                return true;
            });

            ensureVisualUpdate();
        }

        protected bool debugBuildingDirtyElements = false;

        protected override void drawFrame() {
            D.assert(!debugBuildingDirtyElements);
            D.assert(() => {
                debugBuildingDirtyElements = true;
                return true;
            });
            try {
                if (renderViewElement != null) {
                    buildOwner.buildScope(renderViewElement);
                }

                base.drawFrame();
                buildOwner.finalizeTree();
            }
            finally {
                D.assert(() => {
                    debugBuildingDirtyElements = false;
                    return true;
                });
            }
        }

        public RenderObjectToWidgetElement<RenderBox> renderViewElement {
            get { return _renderViewElement; }
        }

        RenderObjectToWidgetElement<RenderBox> _renderViewElement;

        public void detachRootWidget() {
            if (_renderViewElement == null) {
                return;
            }
            
            //The former widget tree must be layout first before its destruction
            drawFrame();
            attachRootWidget(null);
            buildOwner.buildScope(_renderViewElement);
            buildOwner.finalizeTree();
            
            pipelineOwner.rootNode = null;
            _renderViewElement.deactivate();
            _renderViewElement.unmount();
            _renderViewElement = null;
        }

        public void attachRootWidget(Widget rootWidget) {
            _renderViewElement = new RenderObjectToWidgetAdapter<RenderBox>(
                container: renderView,
                debugShortDescription: "[root]",
                child: rootWidget
            ).attachToRenderTree(buildOwner, _renderViewElement);
        }
    }

    public class RenderObjectToWidgetAdapter<T> : RenderObjectWidget where T : RenderObject {
        public RenderObjectToWidgetAdapter(
            Widget child = null,
            RenderObjectWithChildMixin<T> container = null,
            string debugShortDescription = null
        ) : base(
            new GlobalObjectKey<State>(container)) {
            this.child = child;
            this.container = container;
            this.debugShortDescription = debugShortDescription;
        }

        public readonly Widget child;

        public readonly RenderObjectWithChildMixin<T> container;

        public readonly string debugShortDescription;

        public override Element createElement() {
            return new RenderObjectToWidgetElement<T>(this);
        }

        public override RenderObject createRenderObject(BuildContext context) {
            return (RenderObject) container;
        }

        public override void updateRenderObject(BuildContext context, RenderObject renderObject) {
        }

        public RenderObjectToWidgetElement<T> attachToRenderTree(BuildOwner owner,
            RenderObjectToWidgetElement<T> element) {
            if (element == null) {
                owner.lockState(() => {
                    element = (RenderObjectToWidgetElement<T>) createElement();
                    D.assert(element != null);
                    element.assignOwner(owner);
                });
                owner.buildScope(element, () => { element.mount(null, null); });
            }
            else {
                element._newWidget = this;
                element.markNeedsBuild();
            }

            return element;
        }

        public override string toStringShort() {
            return debugShortDescription ?? base.toStringShort();
        }
    }

    public class RenderObjectToWidgetElement<T> : RootRenderObjectElement where T : RenderObject {
        public RenderObjectToWidgetElement(RenderObjectToWidgetAdapter<T> widget) : base(widget) {
        }

        public new RenderObjectToWidgetAdapter<T> widget {
            get { return (RenderObjectToWidgetAdapter<T>) base.widget; }
        }

        Element _child;

        static readonly object _rootChildSlot = new object();

        public override void visitChildren(ElementVisitor visitor) {
            if (_child != null) {
                visitor(_child);
            }
        }

        protected override void forgetChild(Element child) {
            D.assert(child == _child);
            _child = null;
        }

        public override void mount(Element parent, object newSlot) {
            D.assert(parent == null);
            base.mount(parent, newSlot);
            _rebuild();
        }

        public override void update(Widget newWidget) {
            base.update(newWidget);
            D.assert(widget == newWidget);
            _rebuild();
        }

        internal Widget _newWidget;

        protected override void performRebuild() {
            if (_newWidget != null) {
                Widget newWidget = _newWidget;
                _newWidget = null;
                update(newWidget);
            }

            base.performRebuild();
            D.assert(_newWidget == null);
        }

        void _rebuild() {
            try {
                _child = updateChild(_child, widget.child,
                    _rootChildSlot);
                // allow 
            }
            catch (Exception ex) {
                var details = new UIWidgetsErrorDetails(
                    exception: ex,
                    library: "widgets library",
                    context: "attaching to the render tree"
                );
                UIWidgetsError.reportError(details);

                Widget error = ErrorWidget.builder(details);
                _child = updateChild(null, error, _rootChildSlot);
            }
        }

        public new RenderObjectWithChildMixin<T> renderObject {
            get { return (RenderObjectWithChildMixin<T>) base.renderObject; }
        }

        protected override void insertChildRenderObject(RenderObject child, object slot) {
            D.assert(slot == _rootChildSlot);
            D.assert(renderObject.debugValidateChild(child));
            renderObject.child = (T) child;
        }

        protected override void moveChildRenderObject(RenderObject child, object slot) {
            D.assert(false);
        }

        protected override void removeChildRenderObject(RenderObject child) {
            D.assert(renderObject.child == child);
            renderObject.child = null;
        }
    }
}