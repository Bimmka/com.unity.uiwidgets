using System;
using System.Collections.Generic;
using System.Linq;
using uiwidgets;
using Unity.UIWidgets.animation;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.gestures;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.ui;
using Unity.UIWidgets.widgets;
using UnityEngine;
using Transform = Unity.UIWidgets.widgets.Transform;

namespace Unity.UIWidgets.material {
    class UserAccountsDrawerHeaderUtils {
        public const float _kAccountDetailsHeight = 56.0f;
    }

    class _AccountPictures : StatelessWidget {
        public _AccountPictures(
            Key key = null,
            Widget currentAccountPicture = null,
            List<Widget> otherAccountsPictures = null
        ) : base(key: key) {
            this.currentAccountPicture = currentAccountPicture;
            this.otherAccountsPictures = otherAccountsPictures;
        }

        public readonly Widget currentAccountPicture;
        public readonly List<Widget> otherAccountsPictures;

        public override Widget build(BuildContext context) {
            return new Stack(
                children: new List<Widget> {
                    new Positioned(
                        top: 0.0f,
                        right: 0.0f,
                        child: new Row(
                            children: (otherAccountsPictures ?? new List<Widget> { })
                            .GetRange(0, Mathf.Min(3, otherAccountsPictures?.Count ?? 0))
                            .Select<Widget, Widget>(
                                (Widget picture) => {
                                    return new Padding(
                                        padding: EdgeInsets.only(left: 8.0f),
                                        child: new Container(
                                            padding: EdgeInsets.only(left: 8.0f, bottom: 8.0f),
                                            width: 48.0f,
                                            height: 48.0f,
                                            child: picture
                                        )
                                    );
                                }).ToList()
                        )
                    ),
                    new Positioned(
                        top: 0.0f,
                        child: new SizedBox(
                            width: 72.0f,
                            height: 72.0f,
                            child: currentAccountPicture
                        )
                    )
                }
            );
        }
    }

    class _AccountDetails : StatefulWidget {
        public _AccountDetails(
            Key key = null,
            Widget accountName = null,
            Widget accountEmail = null,
            VoidCallback onTap = null,
            bool? isOpen = null
        ) : base(key: key) {
            D.assert(accountName != null);
            D.assert(accountEmail != null);
            this.accountName = accountName;
            this.accountEmail = accountEmail;
            this.onTap = onTap;
            this.isOpen = isOpen;
        }

        public readonly Widget accountName;
        public readonly Widget accountEmail;
        public readonly VoidCallback onTap;
        public readonly bool? isOpen;

        public override State createState() {
            return new _AccountDetailsState();
        }
    }

    class _AccountDetailsState : SingleTickerProviderStateMixin<_AccountDetails> {
        Animation<float> _animation;
        AnimationController _controller;

        public override void initState() {
            base.initState();
            _controller = new AnimationController(
                value: widget.isOpen == true ? 1.0f : 0.0f,
                duration: new TimeSpan(0, 0, 0, 0, 200),
                vsync: this
            );
            _animation = new CurvedAnimation(
                parent: _controller,
                curve: Curves.fastOutSlowIn,
                reverseCurve: Curves.fastOutSlowIn.flipped
            );
            _animation.addListener(() => setState(() => { }));
        }

        public override void dispose() {
            _controller.dispose();
            base.dispose();
        }

        public override void didUpdateWidget(StatefulWidget _oldWidget) {
            base.didUpdateWidget(_oldWidget);
            _AccountDetails oldWidget = _oldWidget as _AccountDetails;
            if (oldWidget.isOpen == widget.isOpen) {
                return;
            }
            
            if(widget.isOpen ?? false) {
                _controller.forward();
            }
            else {
                _controller.reverse();
            }
        }

        public override Widget build(BuildContext context) {
            D.assert(WidgetsD.debugCheckHasDirectionality(context));
            D.assert(material_.debugCheckHasMaterialLocalizations(context));
            D.assert(material_.debugCheckHasMaterialLocalizations(context));

            ThemeData theme = Theme.of(context);
            List<Widget> children = new List<Widget> { };

            if (widget.accountName != null) {
                Widget accountNameLine = new LayoutId(
                    id: _AccountDetailsLayout.accountName,
                    child: new Padding(
                        padding: EdgeInsets.symmetric(vertical: 2.0f),
                        child: new DefaultTextStyle(
                            style: theme.primaryTextTheme.body2,
                            overflow: TextOverflow.ellipsis,
                            child: widget.accountName
                        )
                    )
                );
                children.Add(accountNameLine);
            }

            if (widget.accountEmail != null) {
                Widget accountEmailLine = new LayoutId(
                    id: _AccountDetailsLayout.accountEmail,
                    child: new Padding(
                        padding: EdgeInsets.symmetric(vertical: 2.0f),
                        child: new DefaultTextStyle(
                            style: theme.primaryTextTheme.body1,
                            overflow: TextOverflow.ellipsis,
                            child: widget.accountEmail
                        )
                    )
                );
                children.Add(accountEmailLine);
            }

            if (widget.onTap != null) {
                MaterialLocalizations localizations = MaterialLocalizations.of(context);
                Widget dropDownIcon = new LayoutId(
                    id: _AccountDetailsLayout.dropdownIcon,
                    child: new SizedBox(
                        height: UserAccountsDrawerHeaderUtils._kAccountDetailsHeight,
                        width: UserAccountsDrawerHeaderUtils._kAccountDetailsHeight,
                        child: new Center(
                            child: Transform.rotate(
                                degree: _animation.value * Mathf.PI,
                                child: new Icon(
                                    Icons.arrow_drop_down,
                                    color: Colors.white
                                )
                            )
                        )
                    )
                );
                children.Add(dropDownIcon);
            }

            Widget accountDetails = new CustomMultiChildLayout(
                layoutDelegate: new _AccountDetailsLayout(),
                children: children
            );

            if (widget.onTap != null) {
                accountDetails = new InkWell(
                    onTap: widget.onTap == null ? (GestureTapCallback) null : () => { widget.onTap(); },
                    child: accountDetails
                );
            }

            return new SizedBox(
                height: UserAccountsDrawerHeaderUtils._kAccountDetailsHeight,
                child: accountDetails
            );
        }
    }


    class _AccountDetailsLayout : MultiChildLayoutDelegate {
        public _AccountDetailsLayout() {
        }

        public const string accountName = "accountName";
        public const string accountEmail = "accountEmail";
        public const string dropdownIcon = "dropdownIcon";

        public override void performLayout(Size size) {
            Size iconSize = null;
            if (hasChild(dropdownIcon)) {
                iconSize = layoutChild(dropdownIcon, BoxConstraints.loose(size));
                positionChild(dropdownIcon, _offsetForIcon(size, iconSize));
            }

            string bottomLine = hasChild(accountEmail)
                ? accountEmail
                : (hasChild(accountName) ? accountName : null);

            if (bottomLine != null) {
                Size constraintSize = iconSize == null ? size : size - new Offset(iconSize.width, 0.0f);
                iconSize = iconSize ?? new Size(UserAccountsDrawerHeaderUtils._kAccountDetailsHeight,
                               UserAccountsDrawerHeaderUtils._kAccountDetailsHeight);

                Size bottomLineSize = layoutChild(bottomLine, BoxConstraints.loose(constraintSize));
                Offset bottomLineOffset = _offsetForBottomLine(size, iconSize, bottomLineSize);
                positionChild(bottomLine, bottomLineOffset);

                if (bottomLine == accountEmail && hasChild(accountName)) {
                    Size nameSize = layoutChild(accountName, BoxConstraints.loose(constraintSize));
                    positionChild(accountName, _offsetForName(size, nameSize, bottomLineOffset));
                }
            }
        }

        public override bool shouldRelayout(MultiChildLayoutDelegate oldDelegate) {
            return true;
        }

        Offset _offsetForIcon(Size size, Size iconSize) {
            return new Offset(size.width - iconSize.width, size.height - iconSize.height);
        }

        Offset _offsetForBottomLine(Size size, Size iconSize, Size bottomLineSize) {
            float y = size.height - 0.5f * iconSize.height - 0.5f * bottomLineSize.height;
            return new Offset(0.0f, y);
        }

        Offset _offsetForName(Size size, Size nameSize, Offset bottomLineOffset) {
            float y = bottomLineOffset.dy - nameSize.height;
            return new Offset(0.0f, y);
        }
    }

    public class UserAccountsDrawerHeader : StatefulWidget {
        public UserAccountsDrawerHeader(
            Key key = null,
            Decoration decoration = null,
            EdgeInsets margin = null,
            Widget currentAccountPicture = null,
            List<Widget> otherAccountsPictures = null,
            Widget accountName = null,
            Widget accountEmail = null,
            VoidCallback onDetailsPressed = null
        ) : base(key: key) {
            D.assert(accountName != null);
            D.assert(accountEmail != null);
            this.decoration = decoration;
            this.margin = margin ?? EdgeInsets.only(bottom: 8.0f);
            this.currentAccountPicture = currentAccountPicture;
            this.otherAccountsPictures = otherAccountsPictures;
            this.accountName = accountName;
            this.accountEmail = accountEmail;
            this.onDetailsPressed = onDetailsPressed;
        }

        public readonly Decoration decoration;

        public readonly EdgeInsets margin;

        public readonly Widget currentAccountPicture;

        public readonly List<Widget> otherAccountsPictures;

        public readonly Widget accountName;

        public readonly Widget accountEmail;

        public readonly VoidCallback onDetailsPressed;

        public override State createState() {
            return new _UserAccountsDrawerHeaderState();
        }
    }

    class _UserAccountsDrawerHeaderState : State<UserAccountsDrawerHeader> {
        bool _isOpen = false;

        void _handleDetailsPressed() {
            setState(() => { _isOpen = !_isOpen; });
            widget.onDetailsPressed();
        }

        public override Widget build(BuildContext context) {
            D.assert(material_.debugCheckHasMaterial(context));
            D.assert(material_.debugCheckHasMaterialLocalizations(context));
            return new DrawerHeader(
                decoration: widget.decoration ?? new BoxDecoration(
                                color: Theme.of(context).primaryColor
                            ),
                margin: widget.margin,
                padding: EdgeInsets.only(top: 16.0f, left: 16.0f),
                child: new SafeArea(
                    bottom: false,
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: new List<Widget> {
                            new Expanded(
                                child: new Padding(
                                    padding: EdgeInsets.only(right: 16.0f),
                                    child: new _AccountPictures(
                                        currentAccountPicture: widget.currentAccountPicture,
                                        otherAccountsPictures: widget.otherAccountsPictures
                                    )
                                )
                            ),
                            new _AccountDetails(
                                accountName: widget.accountName,
                                accountEmail: widget.accountEmail,
                                isOpen: _isOpen,
                                onTap: widget.onDetailsPressed == null
                                    ? (VoidCallback) null
                                    : () => { _handleDetailsPressed(); })
                        }
                    )
                )
            );
        }
    }
}