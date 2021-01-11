using Unity.UIWidgets.foundation;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.ui;

namespace Unity.UIWidgets.widgets {
    
        public class ColorFiltered : SingleChildRenderObjectWidget {
            /// Creates a widget that applies a [ColorFilter] to its child.
            ///
            /// The [colorFilter] must not be null.
            protected ColorFiltered(ColorFilter colorFilter = null, Widget child = null, Key key = null)
                : base(key: key, child: child) {
                D.assert(colorFilter != null);
            }

            /// The color filter to apply to the child of this widget.
            public readonly ColorFilter colorFilter;
            
            public override RenderObject createRenderObject(BuildContext context) => new _ColorFilterRenderObject(colorFilter);
            
            public override void updateRenderObject(BuildContext context, RenderObject renderObject) {
                ((_ColorFilterRenderObject)renderObject).colorFilter = colorFilter;
            }
            
            public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
                base.debugFillProperties(properties);
                properties.add(new DiagnosticsProperty<ColorFilter>("colorFilter", colorFilter));
            }
        }

    public class _ColorFilterRenderObject : RenderProxyBox {
        public _ColorFilterRenderObject(ColorFilter _colorFilter) {
            this._colorFilter = _colorFilter;
        }

    public ColorFilter colorFilter {
        get { return _colorFilter; }
        set {
            D.assert(value != null);
            if (value != _colorFilter) {
                _colorFilter = value;
                markNeedsPaint();
            }
        }
    }

    ColorFilter _colorFilter;
    
    public  bool alwaysNeedsCompositing {
        get { return child != null; }
    }
    
    public override void paint(PaintingContext context, Offset offset) {
        layer = context.pushColorFilter(offset, colorFilter, base.paint, oldLayer: layer as ColorFilterLayer);
    }
    }

    
}