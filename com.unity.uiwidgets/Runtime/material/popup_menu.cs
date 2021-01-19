using System;
using System.Collections.Generic;
using RSG;
using Unity.UIWidgets.animation;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.gestures;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.service;
using Unity.UIWidgets.ui;
using Unity.UIWidgets.widgets;
using UnityEngine;
using Color = Unity.UIWidgets.ui.Color;
using Rect = Unity.UIWidgets.ui.Rect;
using TextStyle = Unity.UIWidgets.painting.TextStyle;

namespace Unity.UIWidgets.material {
    public static partial class PopupMenuUtils {
        internal static readonly TimeSpan _kMenuDuration = new TimeSpan(0, 0, 0, 0, 300);
        internal const float _kBaselineOffsetFromBottom = 20.0f;
        internal const float _kMenuCloseIntervalEnd = 2.0f / 3.0f;
        internal const float _kMenuHorizontalPadding = 16.0f;
        internal const float _kMenuItemHeight = 48.0f;
        internal const float _kMenuDividerHeight = 16.0f;
        internal const float _kMenuMaxWidth = 5.0f * _kMenuWidthStep;
        internal const float _kMenuMinWidth = 2.0f * _kMenuWidthStep;
        internal const float _kMenuVerticalPadding = 8.0f;
        internal const float _kMenuWidthStep = 56.0f;
        internal const float _kMenuScreenPadding = 8.0f;
    }

    public abstract class PopupMenuEntry<T> : StatefulWidget {
        protected PopupMenuEntry(Key key = null) : base(key: key) {
        }

        public abstract float height { get; }

        public abstract bool represents(T value);
    }


    public class PopupMenuDivider : PopupMenuEntry<object> {
        public PopupMenuDivider(Key key = null, float height = PopupMenuUtils._kMenuDividerHeight) : base(key: key) {
            _height = height;
        }

        readonly float _height;

        public override float height {
            get { return _height; }
        }

        public override bool represents(object value) {
            return false;
        }

        public override State createState() {
            return new _PopupMenuDividerState();
        }
    }

    class _PopupMenuDividerState : State<PopupMenuDivider> {
        public override Widget build(BuildContext context) {
            return new Divider(height: widget.height);
        }
    }

    public class PopupMenuItem<T> : PopupMenuEntry<T> {
        public PopupMenuItem(
            Key key = null,
            T value = default,
            bool enabled = true,
            float height = PopupMenuUtils._kMenuItemHeight,
            Widget child = null
        ) : base(key: key) {
            this.value = value;
            this.enabled = enabled;
            _height = height;
            this.child = child;
        }

        public readonly T value;

        public readonly bool enabled;

        readonly float _height;

        public override float height {
            get { return _height; }
        }

        public readonly Widget child;

        public override bool represents(T value) {
            return Equals(value, this.value);
        }

        public override State createState() {
            return new PopupMenuItemState<T, PopupMenuItem<T>>();
        }
    }

    public class PopupMenuItemState<T, W> : State<W> where W : PopupMenuItem<T> {
        protected virtual Widget buildChild() {
            return widget.child;
        }

        protected virtual void handleTap() {
            Navigator.pop(context, widget.value);
        }

        public override Widget build(BuildContext context) {
            ThemeData theme = Theme.of(context);
            TextStyle style = theme.textTheme.subhead;
            if (!widget.enabled) {
                style = style.copyWith(color: theme.disabledColor);
            }

            Widget item = new AnimatedDefaultTextStyle(
                style: style,
                duration: Constants.kThemeChangeDuration,
                child: new Baseline(
                    baseline: widget.height - PopupMenuUtils._kBaselineOffsetFromBottom,
                    baselineType: style.textBaseline,
                    child: buildChild()
                )
            );

            if (!widget.enabled) {
                bool isDark = theme.brightness == Brightness.dark;
                item = IconTheme.merge(
                    data: new IconThemeData(opacity: isDark ? 0.5f : 0.38f),
                    child: item
                );
            }

            return new InkWell(
                onTap: widget.enabled ? handleTap : (GestureTapCallback) null,
                child: new Container(
                    height: widget.height,
                    padding: EdgeInsets.symmetric(horizontal: PopupMenuUtils._kMenuHorizontalPadding),
                    child: item
                )
            );
        }
    }

    public class PopupMenuItemSingleTickerProviderState<T, W> : SingleTickerProviderStateMixin<W>
        where W : PopupMenuItem<T> {
        protected virtual Widget buildChild() {
            return widget.child;
        }

        protected virtual void handleTap() {
            Navigator.pop(context, widget.value);
        }

        public override Widget build(BuildContext context) {
            ThemeData theme = Theme.of(context);
            TextStyle style = theme.textTheme.subhead;
            if (!widget.enabled) {
                style = style.copyWith(color: theme.disabledColor);
            }

            Widget item = new AnimatedDefaultTextStyle(
                style: style,
                duration: Constants.kThemeChangeDuration,
                child: new Baseline(
                    baseline: widget.height - PopupMenuUtils._kBaselineOffsetFromBottom,
                    baselineType: style.textBaseline,
                    child: buildChild()
                )
            );

            if (!widget.enabled) {
                bool isDark = theme.brightness == Brightness.dark;
                item = IconTheme.merge(
                    data: new IconThemeData(opacity: isDark ? 0.5f : 0.38f),
                    child: item
                );
            }

            return new InkWell(
                onTap: widget.enabled ? handleTap : (GestureTapCallback) null,
                child: new Container(
                    height: widget.height,
                    padding: EdgeInsets.symmetric(horizontal: PopupMenuUtils._kMenuHorizontalPadding),
                    child: item
                )
            );
        }
    }

    class CheckedPopupMenuItem<T> : PopupMenuItem<T> {
        public CheckedPopupMenuItem(
            Key key = null,
            T value = default,
            bool isChecked = false,
            bool enabled = true,
            Widget child = null
        ) : base(
            key: key,
            value: value,
            enabled: enabled,
            child: child
        ) {
            this.isChecked = isChecked;
        }

        public readonly bool isChecked;

        public override State createState() {
            return new _CheckedPopupMenuItemState<T>();
        }
    }

    class _CheckedPopupMenuItemState<T> : PopupMenuItemSingleTickerProviderState<T, CheckedPopupMenuItem<T>> {
        static readonly TimeSpan _fadeDuration = new TimeSpan(0, 0, 0, 0, 150);

        AnimationController _controller;

        Animation<float> _opacity {
            get { return _controller.view; }
        }

        public override void initState() {
            base.initState();
            _controller = new AnimationController(duration: _fadeDuration, vsync: this);
            _controller.setValue(widget.isChecked ? 1.0f : 0.0f);
            _controller.addListener(() => setState(() => {
                /* animation changed */
            }));
        }

        protected override void handleTap() {
            if (widget.isChecked) {
                _controller.reverse();
            }
            else {
                _controller.forward();
            }

            base.handleTap();
        }

        protected override Widget buildChild() {
            return new ListTile(
                enabled: widget.enabled,
                leading: new FadeTransition(
                    opacity: _opacity,
                    child: new Icon(_controller.isDismissed ? null : Icons.done)
                ),
                title: widget.child
            );
        }
    }

    class _PopupMenu<T> : StatelessWidget {
        public _PopupMenu(
            Key key = null,
            _PopupMenuRoute<T> route = null
        ) : base(key: key) {
            this.route = route;
        }

        public readonly _PopupMenuRoute<T> route;

        public override Widget build(BuildContext context) {
            float unit = 1.0f / (route.items.Count + 1.5f);
            List<Widget> children = new List<Widget>();

            for (int i = 0; i < route.items.Count; i += 1) {
                float start = (i + 1) * unit;
                float end = (start + 1.5f * unit).clamp(0.0f, 1.0f);
                Widget item = route.items[i];
                if (route.initialValue != null && route.items[i].represents((T) route.initialValue)) {
                    item = new Container(
                        color: Theme.of(context).highlightColor,
                        child: item
                    );
                }

                children.Add(new FadeTransition(
                    opacity: new CurvedAnimation(
                        parent: route.animation,
                        curve: new Interval(start, end)
                    ),
                    child: item
                ));
            }

            CurveTween opacity = new CurveTween(curve: new Interval(0.0f, 1.0f / 3.0f));
            CurveTween width = new CurveTween(curve: new Interval(0.0f, unit));
            CurveTween height = new CurveTween(curve: new Interval(0.0f, unit * route.items.Count));

            Widget child = new ConstrainedBox(
                constraints: new BoxConstraints(
                    minWidth: PopupMenuUtils._kMenuMinWidth,
                    maxWidth: PopupMenuUtils._kMenuMaxWidth
                ),
                child: new IntrinsicWidth(
                    stepWidth: PopupMenuUtils._kMenuWidthStep,
                    child: new SingleChildScrollView(
                        padding: EdgeInsets.symmetric(
                            vertical: PopupMenuUtils._kMenuVerticalPadding
                        ),
                        child: new ListBody(children: children)
                    )
                )
            );

            return new AnimatedBuilder(
                animation: route.animation,
                builder: (_, builderChild) => {
                    return new Opacity(
                        opacity: opacity.evaluate(route.animation),
                        child: new Material(
                            type: MaterialType.card,
                            elevation: route.elevation,
                            child: new Align(
                                alignment: Alignment.topRight,
                                widthFactor: width.evaluate(route.animation),
                                heightFactor: height.evaluate(route.animation),
                                child: builderChild
                            )
                        )
                    );
                },
                child: child
            );
        }
    }

    class _PopupMenuRouteLayout : SingleChildLayoutDelegate {
        public _PopupMenuRouteLayout(RelativeRect position, float? selectedItemOffset) {
            this.position = position;
            this.selectedItemOffset = selectedItemOffset;
        }

        public readonly RelativeRect position;

        public readonly float? selectedItemOffset;

        public override BoxConstraints getConstraintsForChild(BoxConstraints constraints) {
            return BoxConstraints.loose(constraints.biggest -
                                        new Offset(
                                            PopupMenuUtils._kMenuScreenPadding * 2.0f,
                                            PopupMenuUtils._kMenuScreenPadding * 2.0f));
        }

        public override Offset getPositionForChild(Size size, Size childSize) {
            float y;
            if (selectedItemOffset == null) {
                y = position.top;
            }
            else {
                y = position.top + (size.height - position.top - position.bottom) / 2.0f -
                    selectedItemOffset.Value;
            }

            float x;
            if (position.left > position.right) {
                x = size.width - position.right - childSize.width;
            }
            else if (position.left < position.right) {
                x = position.left;
            }
            else {
                x = position.left;
            }

            if (x < PopupMenuUtils._kMenuScreenPadding) {
                x = PopupMenuUtils._kMenuScreenPadding;
            }
            else if (x + childSize.width > size.width - PopupMenuUtils._kMenuScreenPadding) {
                x = size.width - childSize.width - PopupMenuUtils._kMenuScreenPadding;
            }

            if (y < PopupMenuUtils._kMenuScreenPadding) {
                y = PopupMenuUtils._kMenuScreenPadding;
            }
            else if (y + childSize.height > size.height - PopupMenuUtils._kMenuScreenPadding) {
                y = size.height - childSize.height - PopupMenuUtils._kMenuScreenPadding;
            }

            return new Offset(x, y);
        }

        public override bool shouldRelayout(SingleChildLayoutDelegate oldDelegate) {
            return position != ((_PopupMenuRouteLayout) oldDelegate).position;
        }
    }

    class _PopupMenuRoute<T> : PopupRoute {
        public _PopupMenuRoute(
            RelativeRect position = null,
            List<PopupMenuEntry<T>> items = null,
            object initialValue = null,
            float elevation = 8.0f,
            ThemeData theme = null
        ) {
            this.position = position;
            this.items = items;
            this.initialValue = initialValue;
            this.elevation = elevation;
            this.theme = theme;
        }

        public readonly RelativeRect position;
        public readonly List<PopupMenuEntry<T>> items;
        public readonly object initialValue;
        public readonly float elevation;
        public readonly ThemeData theme;

        public override Animation<float> createAnimation() {
            return new CurvedAnimation(
                parent: base.createAnimation(),
                curve: Curves.linear,
                reverseCurve: new Interval(0.0f, PopupMenuUtils._kMenuCloseIntervalEnd)
            );
        }

        public override TimeSpan transitionDuration {
            get { return PopupMenuUtils._kMenuDuration; }
        }

        public override bool barrierDismissible {
            get { return true; }
        }

        public override Color barrierColor {
            get { return null; }
        }

        public override Widget buildPage(BuildContext context, Animation<float> animation,
            Animation<float> secondaryAnimation) {
            float? selectedItemOffset = null;
            if (initialValue != null) {
                float y = PopupMenuUtils._kMenuVerticalPadding;
                foreach (PopupMenuEntry<T> entry in items) {
                    if (entry.represents((T) initialValue)) {
                        selectedItemOffset = y + entry.height / 2.0f;
                        break;
                    }

                    y += entry.height;
                }
            }

            Widget menu = new _PopupMenu<T>(route: this);
            if (theme != null) {
                menu = new Theme(data: theme, child: menu);
            }

            return MediaQuery.removePadding(
                context: context,
                removeTop: true,
                removeBottom: true,
                removeLeft: true,
                removeRight: true,
                child: new Builder(
                    builder: _ => new CustomSingleChildLayout(
                        layoutDelegate: new _PopupMenuRouteLayout(
                            position,
                            selectedItemOffset
                        ),
                        child: menu
                    ))
            );
        }
    }

    public static partial class PopupMenuUtils {
        public static IPromise<object> showMenu<T>(
            BuildContext context,
            RelativeRect position,
            List<PopupMenuEntry<T>> items,
            T initialValue,
            float elevation = 8.0f
        ) {
            D.assert(context != null);
            D.assert(position != null);
            D.assert(items != null && items.isNotEmpty());
            D.assert(material_.debugCheckHasMaterialLocalizations(context));

            return Navigator.push(context, new _PopupMenuRoute<T>(
                position: position,
                items: items,
                initialValue: initialValue,
                elevation: elevation,
                theme: Theme.of(context, shadowThemeOnly: true)
            ));
        }
    }

    public delegate void PopupMenuItemSelected<T>(T value);

    public delegate void PopupMenuCanceled();

    public delegate List<PopupMenuEntry<T>> PopupMenuItemBuilder<T>(BuildContext context);

    public class PopupMenuButton<T> : StatefulWidget {
        public PopupMenuButton(
            Key key = null,
            PopupMenuItemBuilder<T> itemBuilder = null,
            T initialValue = default,
            PopupMenuItemSelected<T> onSelected = null,
            PopupMenuCanceled onCanceled = null,
            string tooltip = null,
            float elevation = 8.0f,
            EdgeInsets padding = null,
            Widget child = null,
            Icon icon = null,
            Offset offset = null
        ) : base(key: key) {
            offset = offset ?? Offset.zero;
            D.assert(itemBuilder != null);
            D.assert(offset != null);
            D.assert(!(child != null && icon != null));

            this.itemBuilder = itemBuilder;
            this.initialValue = initialValue;
            this.onSelected = onSelected;
            this.onCanceled = onCanceled;
            this.tooltip = tooltip;
            this.elevation = elevation;
            this.padding = padding ?? EdgeInsets.all(8.0f);
            this.child = child;
            this.icon = icon;
            this.offset = offset;
        }


        public readonly PopupMenuItemBuilder<T> itemBuilder;

        public readonly T initialValue;

        public readonly PopupMenuItemSelected<T> onSelected;

        public readonly PopupMenuCanceled onCanceled;

        public readonly string tooltip;

        public readonly float elevation;

        public readonly EdgeInsets padding;

        public readonly Widget child;

        public readonly Icon icon;

        public readonly Offset offset;

        public override State createState() {
            return new _PopupMenuButtonState<T>();
        }
    }

    class _PopupMenuButtonState<T> : State<PopupMenuButton<T>> {
        void showButtonMenu() {
            RenderBox button = (RenderBox) context.findRenderObject();
            RenderBox overlay = (RenderBox) Overlay.of(context).context.findRenderObject();
            RelativeRect position = RelativeRect.fromRect(
                Rect.fromPoints(
                    button.localToGlobal(widget.offset, ancestor: overlay),
                    button.localToGlobal(button.size.bottomRight(Offset.zero), ancestor: overlay)
                ),
                Offset.zero & overlay.size
            );
            PopupMenuUtils.showMenu(
                    context: context,
                    elevation: widget.elevation,
                    items: widget.itemBuilder(context),
                    initialValue: widget.initialValue,
                    position: position
                )
                .Then(newValue => {
                    if (!mounted) {
                        return;
                    }

                    if (newValue == null) {
                        if (widget.onCanceled != null) {
                            widget.onCanceled();
                        }

                        return;
                    }

                    if (widget.onSelected != null) {
                        widget.onSelected((T) newValue);
                    }
                });
        }

        Icon _getIcon(RuntimePlatform platform) {
            switch (platform) {
                case RuntimePlatform.IPhonePlayer:
                    return new Icon(Icons.more_horiz);
                default:
                    return new Icon(Icons.more_vert);
            }
        }

        public override Widget build(BuildContext context) {
            D.assert(material_.debugCheckHasMaterialLocalizations(context));
            return widget.child != null
                ? (Widget) new InkWell(
                    onTap: showButtonMenu,
                    child: widget.child
                )
                : new IconButton(
                    icon: widget.icon ?? _getIcon(Theme.of(context).platform),
                    padding: widget.padding,
                    tooltip: widget.tooltip ?? MaterialLocalizations.of(context).showMenuTooltip,
                    onPressed: showButtonMenu
                );
        }
    }
}