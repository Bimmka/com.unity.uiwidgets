using System.Collections.Generic;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.service;
using Unity.UIWidgets.ui;
using Unity.UIWidgets.widgets;
using UnityEngine;
using Canvas = Unity.UIWidgets.ui.Canvas;
using Color = Unity.UIWidgets.ui.Color;
using Rect = Unity.UIWidgets.ui.Rect;
using TextStyle = Unity.UIWidgets.painting.TextStyle;
using Transform = Unity.UIWidgets.widgets.Transform;

namespace Unity.UIWidgets.cupertino {
    public static class CupertinoTextSelectionUtils {
        public static readonly TextSelectionControls cupertinoTextSelectionControls = new _CupertinoTextSelectionControls();
        
        public const float _kHandlesPadding = 18.0f;

        public const float _kToolbarScreenPadding = 8.0f;

        public const float _kToolbarHeight = 36.0f;

        public static readonly Color _kToolbarBackgroundColor = new Color(0xFF2E2E2E);

        public static readonly Color _kToolbarDividerColor = new Color(0xFFB9B9B9);

        public static readonly Color _kHandlesColor = new Color(0xFF136FE0);

        public static readonly Size _kSelectionOffset = new Size(20.0f, 30.0f);

        public static readonly Size _kToolbarTriangleSize = new Size(18.0f, 9.0f);

        public static readonly EdgeInsets _kToolbarButtonPadding =
            EdgeInsets.symmetric(vertical: 10.0f, horizontal: 18.0f);

        public static readonly BorderRadius _kToolbarBorderRadius = BorderRadius.all(Radius.circular(7.5f));

        public static readonly TextStyle _kToolbarButtonFontStyle = new TextStyle(
            fontSize: 14.0f,
            letterSpacing: -0.11f,
            fontWeight: FontWeight.w300,
            color: CupertinoColors.white
        );
    }

    class _TextSelectionToolbarNotchPainter : AbstractCustomPainter {
        public override void paint(Canvas canvas, Size size) {
            Paint paint = new Paint();
            paint.color = CupertinoTextSelectionUtils._kToolbarBackgroundColor;
            paint.style = PaintingStyle.fill;

            Path triangle = new Path();
            triangle.lineTo(CupertinoTextSelectionUtils._kToolbarTriangleSize.width / 2, 0.0f);
            triangle.lineTo(0.0f, CupertinoTextSelectionUtils._kToolbarTriangleSize.height);
            triangle.lineTo(-(CupertinoTextSelectionUtils._kToolbarTriangleSize.width / 2), 0.0f);
            triangle.close();
            canvas.drawPath(triangle, paint);
        }

        public override bool shouldRepaint(CustomPainter oldPainter) {
            return false;
        }
    }

    class _TextSelectionToolbar : StatelessWidget {
        public _TextSelectionToolbar(
            Key key = null,
            VoidCallback handleCut = null,
            VoidCallback handleCopy = null,
            VoidCallback handlePaste = null,
            VoidCallback handleSelectAll = null
        ) : base(key: key) {
            this.handleCut = handleCut;
            this.handleCopy = handleCopy;
            this.handlePaste = handlePaste;
            this.handleSelectAll = handleSelectAll;
        }

        readonly VoidCallback handleCut;

        readonly VoidCallback handleCopy;

        readonly VoidCallback handlePaste;

        readonly VoidCallback handleSelectAll;


        public override Widget build(BuildContext context) {
            List<Widget> items = new List<Widget>();
            Widget onePhysicalPixelVerticalDivider =
                new SizedBox(width: 1.0f / MediaQuery.of(context).devicePixelRatio);
            CupertinoLocalizations localizations = CupertinoLocalizations.of(context);

            if (handleCut != null) {
                items.Add(_buildToolbarButton(localizations.cutButtonLabel, handleCut));
            }

            if (handleCopy != null) {
                if (items.isNotEmpty()) {
                    items.Add(onePhysicalPixelVerticalDivider);
                }

                items.Add(_buildToolbarButton(localizations.copyButtonLabel, handleCopy));
            }

            if (handlePaste != null) {
                if (items.isNotEmpty()) {
                    items.Add(onePhysicalPixelVerticalDivider);
                }

                items.Add(_buildToolbarButton(localizations.pasteButtonLabel, handlePaste));
            }

            if (handleSelectAll != null) {
                if (items.isNotEmpty()) {
                    items.Add(onePhysicalPixelVerticalDivider);
                }

                items.Add(_buildToolbarButton(localizations.selectAllButtonLabel, handleSelectAll));
            }

            Widget triangle = SizedBox.fromSize(
                size: CupertinoTextSelectionUtils._kToolbarTriangleSize,
                child: new CustomPaint(
                    painter: new _TextSelectionToolbarNotchPainter()
                )
            );

            return new Column(
                mainAxisSize: MainAxisSize.min,
                children: new List<Widget> {
                    new ClipRRect(
                        borderRadius: CupertinoTextSelectionUtils._kToolbarBorderRadius,
                        child: new DecoratedBox(
                            decoration: new BoxDecoration(
                                color: CupertinoTextSelectionUtils._kToolbarDividerColor,
                                borderRadius: CupertinoTextSelectionUtils._kToolbarBorderRadius,
                                border: Border.all(color: CupertinoTextSelectionUtils._kToolbarBackgroundColor,
                                    width: 0)
                            ),
                            child: new Row(mainAxisSize: MainAxisSize.min, children: items)
                        )
                    ),
                    triangle,
                    new Padding(padding: EdgeInsets.only(bottom: 10.0f))
                }
            );
        }

        CupertinoButton _buildToolbarButton(string text, VoidCallback onPressed) {
            return new CupertinoButton(
                child: new Text(text, style: CupertinoTextSelectionUtils._kToolbarButtonFontStyle),
                color: CupertinoTextSelectionUtils._kToolbarBackgroundColor,
                minSize: CupertinoTextSelectionUtils._kToolbarHeight,
                padding: CupertinoTextSelectionUtils._kToolbarButtonPadding,
                borderRadius: null,
                pressedOpacity: 0.7f,
                onPressed: onPressed
            );
        }
    }

    class _TextSelectionToolbarLayout : SingleChildLayoutDelegate {
        public _TextSelectionToolbarLayout(
            Size screenSize,
            Rect globalEditableRegion,
            Offset position) {
            this.screenSize = screenSize;
            this.globalEditableRegion = globalEditableRegion;
            this.position = position;
        }

        readonly Size screenSize;

        readonly Rect globalEditableRegion;

        readonly Offset position;

        public override BoxConstraints getConstraintsForChild(BoxConstraints constraints) {
            return constraints.loosen();
        }

        public override Offset getPositionForChild(Size size, Size childSize) {
            Offset globalPosition = globalEditableRegion.topLeft + position;

            float x = globalPosition.dx - childSize.width / 2.0f;
            float y = globalPosition.dy - childSize.height;

            if (x < CupertinoTextSelectionUtils._kToolbarScreenPadding) {
                x = CupertinoTextSelectionUtils._kToolbarScreenPadding;
            }
            else if (x + childSize.width > screenSize.width - CupertinoTextSelectionUtils._kToolbarScreenPadding) {
                x = screenSize.width - childSize.width - CupertinoTextSelectionUtils._kToolbarScreenPadding;
            }

            if (y < CupertinoTextSelectionUtils._kToolbarScreenPadding) {
                y = CupertinoTextSelectionUtils._kToolbarScreenPadding;
            }
            else if (y + childSize.height >
                     screenSize.height - CupertinoTextSelectionUtils._kToolbarScreenPadding) {
                y = screenSize.height - childSize.height - CupertinoTextSelectionUtils._kToolbarScreenPadding;
            }

            return new Offset(x, y);
        }

        public override bool shouldRelayout(SingleChildLayoutDelegate oldDelegate) {
            _TextSelectionToolbarLayout _oldDelegate = (_TextSelectionToolbarLayout) oldDelegate;
            return screenSize != _oldDelegate.screenSize
                   || globalEditableRegion != _oldDelegate.globalEditableRegion
                   || position != _oldDelegate.position;
        }
    }

    class _TextSelectionHandlePainter : AbstractCustomPainter {
        public _TextSelectionHandlePainter(Offset origin) {
            this.origin = origin;
        }

        readonly Offset origin;


        public override void paint(Canvas canvas, Size size) {
            Paint paint = new Paint();
            paint.color = CupertinoTextSelectionUtils._kHandlesColor;
            paint.strokeWidth = 2.0f;

            canvas.drawCircle(origin.translate(0.0f, 4.0f), 5.5f, paint);
            canvas.drawLine(
                origin,
                origin.translate(
                    0.0f,
                    -(size.height - 2.0f * CupertinoTextSelectionUtils._kHandlesPadding)
                ),
                paint
            );
        }

        public override bool shouldRepaint(CustomPainter oldPainter) {
            _TextSelectionHandlePainter _oldPainter = (_TextSelectionHandlePainter) oldPainter;
            return origin != _oldPainter.origin;
        }
    }

    class _CupertinoTextSelectionControls : TextSelectionControls {
        public override Size handleSize {
            get { return CupertinoTextSelectionUtils._kSelectionOffset; }
        }

        public override Widget buildToolbar(BuildContext context, Rect globalEditableRegion, Offset position,
            TextSelectionDelegate del) {
            D.assert(WidgetsD.debugCheckHasMediaQuery(context));
            return new ConstrainedBox(
                constraints: BoxConstraints.tight(globalEditableRegion.size),
                child: new CustomSingleChildLayout(
                    layoutDelegate: new _TextSelectionToolbarLayout(
                        MediaQuery.of(context).size,
                        globalEditableRegion,
                        position
                    ),
                    child: new _TextSelectionToolbar(
                        handleCut: canCut(del) ? () => handleCut(del) : (VoidCallback) null,
                        handleCopy: canCopy(del) ? () => handleCopy(del) : (VoidCallback) null,
                        handlePaste: canPaste(del) ? () => handlePaste(del) : (VoidCallback) null,
                        handleSelectAll: canSelectAll(del) ? () => handleSelectAll(del) : (VoidCallback) null
                    )
                )
            );
        }


        public override Widget buildHandle(BuildContext context, TextSelectionHandleType type, float textLineHeight) {
            Size desiredSize = new Size(
                2.0f * CupertinoTextSelectionUtils._kHandlesPadding,
                textLineHeight + 2.0f * CupertinoTextSelectionUtils._kHandlesPadding
            );

            Widget handle = SizedBox.fromSize(
                size: desiredSize,
                child: new CustomPaint(
                    painter: new _TextSelectionHandlePainter(
                        origin: new Offset(CupertinoTextSelectionUtils._kHandlesPadding,
                            textLineHeight + CupertinoTextSelectionUtils._kHandlesPadding)
                    )
                )
            );

            switch (type) {
                case TextSelectionHandleType.left:
                    Matrix4 matrix = new Matrix4().rotationZ(Mathf.PI);
                    matrix.translate(-CupertinoTextSelectionUtils._kHandlesPadding,
                        -CupertinoTextSelectionUtils._kHandlesPadding);

                    return new Transform(
                        transform: matrix,
                        child: handle
                    );
                case TextSelectionHandleType.right:
                    return new Transform(
                        transform:new Matrix4().translationValues(
                            -CupertinoTextSelectionUtils._kHandlesPadding,
                            -(textLineHeight + CupertinoTextSelectionUtils._kHandlesPadding), 
                            0
                        ),
                        child: handle
                    );
                case TextSelectionHandleType.collapsed:
                    return new Container();
            }

            return null;
        }
    }
}