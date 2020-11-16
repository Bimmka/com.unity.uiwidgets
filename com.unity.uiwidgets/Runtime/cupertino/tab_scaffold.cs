using System.Collections.Generic;
using System.Linq;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.ui;
using Unity.UIWidgets.widgets;

namespace Unity.UIWidgets.cupertino {
    public class CupertinoTabScaffold : StatefulWidget {
        public CupertinoTabScaffold(
            Key key = null,
            CupertinoTabBar tabBar = null,
            IndexedWidgetBuilder tabBuilder = null,
            Color backgroundColor = null,
            bool resizeToAvoidBottomInset = true
        ) : base(key: key) {
            D.assert(tabBar != null);
            D.assert(tabBuilder != null);
            this.tabBar = tabBar;
            this.tabBuilder = tabBuilder;
            this.backgroundColor = backgroundColor;
            this.resizeToAvoidBottomInset = resizeToAvoidBottomInset;
        }


        public readonly CupertinoTabBar tabBar;

        public readonly IndexedWidgetBuilder tabBuilder;

        public readonly Color backgroundColor;

        public readonly bool resizeToAvoidBottomInset;

        public override State createState() {
            return new _CupertinoTabScaffoldState();
        }
    }

    class _CupertinoTabScaffoldState : State<CupertinoTabScaffold> {
        int _currentPage;

        public override void initState() {
            base.initState();
            _currentPage = widget.tabBar.currentIndex;
            
        }

        public override void didUpdateWidget(StatefulWidget _oldWidget) {
            CupertinoTabScaffold oldWidget = _oldWidget as CupertinoTabScaffold;
            base.didUpdateWidget(oldWidget);
            if (_currentPage >= widget.tabBar.items.Count) {
                _currentPage = widget.tabBar.items.Count - 1;
                D.assert(_currentPage >= 0,
                    () => "CupertinoTabBar is expected to keep at least 2 tabs after updating"
                );
            }

            if (widget.tabBar.currentIndex != oldWidget.tabBar.currentIndex) {
                _currentPage = widget.tabBar.currentIndex;
            }
        }

        public override Widget build(BuildContext context) {
            List<Widget> stacked = new List<Widget> { };

            MediaQueryData existingMediaQuery = MediaQuery.of(context);
            MediaQueryData newMediaQuery = MediaQuery.of(context);

            Widget content = new _TabSwitchingView(
                currentTabIndex: _currentPage,
                tabNumber: widget.tabBar.items.Count,
                tabBuilder: widget.tabBuilder
            );
            EdgeInsets contentPadding = EdgeInsets.zero;

            if (widget.resizeToAvoidBottomInset) {
                newMediaQuery = newMediaQuery.removeViewInsets(removeBottom: true);
                contentPadding = EdgeInsets.only(bottom: existingMediaQuery.viewInsets.bottom);
            }

            if (widget.tabBar != null &&
                (!widget.resizeToAvoidBottomInset ||
                 widget.tabBar.preferredSize.height > existingMediaQuery.viewInsets.bottom)) {
                float bottomPadding = widget.tabBar.preferredSize.height + existingMediaQuery.padding.bottom;

                if (widget.tabBar.opaque(context)) {
                    contentPadding = EdgeInsets.only(bottom: bottomPadding);
                }
                else {
                    newMediaQuery = newMediaQuery.copyWith(
                        padding: newMediaQuery.padding.copyWith(
                            bottom: bottomPadding
                        )
                    );
                }
            }

            content = new MediaQuery(
                data: newMediaQuery,
                child: new Padding(
                    padding: contentPadding,
                    child: content
                )
            );

            stacked.Add(content);

            if (widget.tabBar != null) {
                stacked.Add(new Align(
                    alignment: Alignment.bottomCenter,
                    child: widget.tabBar.copyWith(
                        currentIndex: _currentPage,
                        onTap: (int newIndex) => {
                            setState(() => { _currentPage = newIndex; });
                            if (widget.tabBar.onTap != null) {
                                widget.tabBar.onTap(newIndex);
                            }
                        }
                    )
                ));
            }

            return new DecoratedBox(
                decoration: new BoxDecoration(
                    color: widget.backgroundColor ?? CupertinoTheme.of(context).scaffoldBackgroundColor
                ),
                child: new Stack(
                    children: stacked
                )
            );
        }
    }

    class _TabSwitchingView : StatefulWidget {
        public _TabSwitchingView(
            int currentTabIndex,
            int tabNumber,
            IndexedWidgetBuilder tabBuilder
        ) {
            D.assert(tabNumber > 0);
            D.assert(tabBuilder != null);
            this.currentTabIndex = currentTabIndex;
            this.tabNumber = tabNumber;
            this.tabBuilder = tabBuilder;
        }

        public readonly int currentTabIndex;
        public readonly int tabNumber;
        public readonly IndexedWidgetBuilder tabBuilder;

        public override State createState() {
            return new _TabSwitchingViewState();
        }
    }

    class _TabSwitchingViewState : State<_TabSwitchingView> {
        List<Widget> tabs;
        List<FocusScopeNode> tabFocusNodes;

        public override void initState() {
            base.initState();
            tabs = new List<Widget>(widget.tabNumber);
            for (int i = 0; i < widget.tabNumber; i++) {
                tabs.Add(null);
            }
            tabFocusNodes = Enumerable.Repeat(new FocusScopeNode(), widget.tabNumber).ToList();
        }

        public override void didChangeDependencies() {
            base.didChangeDependencies();
            _focusActiveTab();
        }

        public override void didUpdateWidget(StatefulWidget _oldWidget) {
            _TabSwitchingView oldWidget = _oldWidget as _TabSwitchingView;
            base.didUpdateWidget(oldWidget);
            _focusActiveTab();
        }

        void _focusActiveTab() {
            FocusScope.of(context).setFirstFocus(tabFocusNodes[widget.currentTabIndex]);
        }

        public override void dispose() {
            foreach (FocusScopeNode focusScopeNode in tabFocusNodes) {
                focusScopeNode.detach();
            }

            base.dispose();
        }

        public override Widget build(BuildContext context) {
            List<Widget> children = new List<Widget>();
            for (int index = 0; index < widget.tabNumber; index++) {
                bool active = index == widget.currentTabIndex;

                var tabIndex = index;
                if (active || tabs[index] != null) {
                    tabs[index] = widget.tabBuilder(context, tabIndex);
                }

                children.Add(new Offstage(
                    offstage: !active,
                    child: new TickerMode(
                        enabled: active,
                        child: new FocusScope(
                            node: tabFocusNodes[index],
                            child: tabs[index] ?? new Container()
                        )
                    )
                ));
            }

            return new Stack(
                fit: StackFit.expand,
                children: children
            );
        }
    }
}