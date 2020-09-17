﻿using System.Collections.Generic;
using Unity.UIWidgets.foundation;

namespace Unity.UIWidgets.widgets {
    class _FocusScopeMarker : InheritedWidget {
        public _FocusScopeMarker(FocusScopeNode node, Widget child, Key key = null) : base(key, child) {
            D.assert(node != null);
            this.node = node;
        }

        public readonly FocusScopeNode node;

        public override bool updateShouldNotify(InheritedWidget oldWidget) {
            return node != ((_FocusScopeMarker) oldWidget).node;
        }
    }

    public class FocusScope : StatefulWidget {
        public FocusScope(FocusScopeNode node, Widget child, Key key = null, bool autofocus = false) : base(key) {
            this.node = node;
            this.child = child;
            this.autofocus = autofocus;
        }

        public readonly FocusScopeNode node;

        public readonly bool autofocus;

        public readonly Widget child;

        public static FocusScopeNode of(BuildContext context) {
            D.assert(context != null);
            var scope = (_FocusScopeMarker) context.inheritFromWidgetOfExactType(typeof(_FocusScopeMarker));
            if (scope != null && scope.node != null) {
                return scope.node;
            }

            return context.owner.focusManager.rootScope;
        }

        public static List<FocusScopeNode> ancestorsOf(BuildContext context) {
            D.assert(context != null);
            List<FocusScopeNode> ancestors = new List<FocusScopeNode> { };
            while (true) {
                context = context.ancestorInheritedElementForWidgetOfExactType(typeof(_FocusScopeMarker));
                if (context == null) {
                    return ancestors;
                }

                _FocusScopeMarker scope = (_FocusScopeMarker) context.widget;
                ancestors.Add(scope.node);
                context.visitAncestorElements((Element parent) => {
                    context = parent;
                    return false;
                });
            }
        }

        public override State createState() {
            return new _FocusScopeState();
        }
    }

    class _FocusScopeState : State<FocusScope> {
        bool _didAutofocus = false;

        public override void didChangeDependencies() {
            base.didChangeDependencies();
            if (!_didAutofocus && widget.autofocus) {
                FocusScope.of(context).setFirstFocus(widget.node);
                _didAutofocus = true;
            }
        }

        public override void dispose() {
            widget.node.detach();
            base.dispose();
        }

        public override Widget build(BuildContext context) {
            FocusScope.of(context).reparentScopeIfNeeded(widget.node);
            return new _FocusScopeMarker(node: widget.node, child: widget.child);
        }
    }
}