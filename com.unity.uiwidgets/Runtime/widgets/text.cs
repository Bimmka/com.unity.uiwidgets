using System.Collections.Generic;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.ui;
using TextStyle = Unity.UIWidgets.painting.TextStyle;

namespace Unity.UIWidgets.widgets {
    public class DefaultTextStyle : InheritedWidget {
        public DefaultTextStyle(
            Key key = null,
            TextStyle style = null,
            TextAlign? textAlign = null,
            bool softWrap = true,
            TextOverflow overflow = TextOverflow.clip,
            int? maxLines = null,
            Widget child = null
        ) : base(key, child) {
            D.assert(style != null);
            D.assert(maxLines == null || maxLines > 0);
            D.assert(child != null);

            this.style = style;
            this.textAlign = textAlign;
            this.softWrap = softWrap;
            this.overflow = overflow;
            this.maxLines = maxLines;
        }

        DefaultTextStyle() {
            style = new TextStyle();
            textAlign = null;
            softWrap = true;
            overflow = TextOverflow.clip;
            maxLines = null;
        }

        public static DefaultTextStyle fallback() {
            return _fallback;
        }

        static readonly DefaultTextStyle _fallback = new DefaultTextStyle();

        public static Widget merge(
            Key key = null,
            TextStyle style = null,
            TextAlign? textAlign = null,
            bool? softWrap = null,
            TextOverflow? overflow = null,
            int? maxLines = null,
            Widget child = null
        ) {
            D.assert(child != null);
            return new Builder(builder: (context => {
                var parent = of(context);
                return new DefaultTextStyle(
                    key: key,
                    style: parent.style.merge(style),
                    textAlign: textAlign ?? parent.textAlign,
                    softWrap: softWrap ?? parent.softWrap,
                    overflow: overflow ?? parent.overflow,
                    maxLines: maxLines ?? parent.maxLines,
                    child: child
                );
            }));
        }

        public readonly TextStyle style;
        public readonly TextAlign? textAlign;
        public readonly bool softWrap;
        public readonly TextOverflow overflow;
        public readonly int? maxLines;

        public static DefaultTextStyle of(BuildContext context) {
            var inherit = (DefaultTextStyle) context.inheritFromWidgetOfExactType(typeof(DefaultTextStyle));
            return inherit ?? fallback();
        }

        public override bool updateShouldNotify(InheritedWidget w) {
            var oldWidget = (DefaultTextStyle) w;
            return style != oldWidget.style ||
                   textAlign != oldWidget.textAlign ||
                   softWrap != oldWidget.softWrap ||
                   overflow != oldWidget.overflow ||
                   maxLines != oldWidget.maxLines;
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            if (style != null) {
                style.debugFillProperties(properties);
            }

            properties.add(new EnumProperty<TextAlign?>("textAlign", textAlign,
                defaultValue: foundation_.kNullDefaultValue));
            properties.add(new FlagProperty("softWrap", value: softWrap, ifTrue: "wrapping at box width",
                ifFalse: "no wrapping except at line break characters", showName: true));
            properties.add(new EnumProperty<TextOverflow>("overflow", overflow,
                defaultValue: foundation_.kNullDefaultValue));
            properties.add(new IntProperty("maxLines", maxLines,
                defaultValue: foundation_.kNullDefaultValue));
        }
    }

    public class Text : StatelessWidget {
        public Text(string data,
            Key key = null,
            TextStyle style = null,
            StrutStyle strutStyle = null,
            TextAlign? textAlign = null,
            bool? softWrap = null,
            TextOverflow? overflow = null,
            float? textScaleFactor = null,
            int? maxLines = null) : base(key) {
            D.assert(data != null, () => "A non-null string must be provided to a Text widget.");
            textSpan = null;
            this.data = data;
            this.style = style;
            this.strutStyle = strutStyle;
            this.textAlign = textAlign;
            this.softWrap = softWrap;
            this.overflow = overflow;
            this.textScaleFactor = textScaleFactor;
            this.maxLines = maxLines;
        }

        Text(TextSpan textSpan,
            Key key = null,
            TextStyle style = null,
            StrutStyle strutStyle = null,
            TextAlign? textAlign = null,
            bool? softWrap = null,
            TextOverflow? overflow = null,
            float? textScaleFactor = null,
            int? maxLines = null) : base(key) {
            D.assert(textSpan != null, () => "A non-null TextSpan must be provided to a Text.rich widget.");
            this.textSpan = textSpan;
            data = null;
            this.style = style;
            this.strutStyle = strutStyle;
            this.textAlign = textAlign;
            this.softWrap = softWrap;
            this.overflow = overflow;
            this.textScaleFactor = textScaleFactor;
            this.maxLines = maxLines;
        }

        public static Text rich(TextSpan textSpan,
            Key key = null,
            TextStyle style = null,
            StrutStyle strutStyle = null,
            TextAlign? textAlign = null,
            bool? softWrap = null,
            TextOverflow? overflow = null,
            float? textScaleFactor = null,
            int? maxLines = null) {
            return new Text(
                textSpan, key,
                style,
                strutStyle,
                textAlign,
                softWrap,
                overflow,
                textScaleFactor,
                maxLines);
        }

        public readonly string data;

        public readonly TextSpan textSpan;

        public readonly TextStyle style;

        public readonly StrutStyle strutStyle;

        public readonly TextAlign? textAlign;

        public readonly bool? softWrap;

        public readonly TextOverflow? overflow;

        public readonly float? textScaleFactor;

        public readonly int? maxLines;

        public override Widget build(BuildContext context) {
            DefaultTextStyle defaultTextStyle = DefaultTextStyle.of(context);
            TextStyle effectiveTextStyle = style;
            if (style == null || style.inherit) {
                effectiveTextStyle = defaultTextStyle.style.merge(style);
            }

            return new RichText(
                textAlign: textAlign ?? defaultTextStyle.textAlign ?? TextAlign.left,
                softWrap: softWrap ?? defaultTextStyle.softWrap,
                overflow: overflow ?? defaultTextStyle.overflow,
                textScaleFactor: textScaleFactor ?? MediaQuery.textScaleFactorOf(context),
                maxLines: maxLines ?? defaultTextStyle.maxLines,
                strutStyle: strutStyle,
                text: new TextSpan(
                    style: effectiveTextStyle,
                    text: data,
                    children: textSpan != null ? new List<TextSpan> {textSpan} : null
                )
            );
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new StringProperty("data", data, showName: false));
            if (textSpan != null) {
                properties.add(
                    textSpan.toDiagnosticsNode(name: "textSpan", style: DiagnosticsTreeStyle.transition));
            }

            if (style != null) {
                style.debugFillProperties(properties);
            }

            properties.add(new EnumProperty<TextAlign?>("textAlign", textAlign,
                defaultValue: foundation_.kNullDefaultValue));
            properties.add(new FlagProperty("softWrap", value: softWrap, ifTrue: "wrapping at box width",
                ifFalse: "no wrapping except at line break characters", showName: true));
            properties.add(new EnumProperty<TextOverflow?>("overflow", overflow,
                defaultValue: foundation_.kNullDefaultValue));
            properties.add(new FloatProperty("textScaleFactor", textScaleFactor,
                defaultValue: foundation_.kNullDefaultValue));
            properties.add(new IntProperty("maxLines", maxLines, defaultValue: foundation_.kNullDefaultValue));
        }
    }
}