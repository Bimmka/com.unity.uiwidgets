using Unity.UIWidgets.foundation;
using Unity.UIWidgets.painting;
using UnityEngine;
using Color = Unity.UIWidgets.ui.Color;

namespace Unity.UIWidgets.material {
    public class FloatingActionButtonThemeData : Diagnosticable {
        public FloatingActionButtonThemeData(
            Color backgroundColor = null,
            Color foregroundColor = null,
            float? elevation = null,
            float? disabledElevation = null,
            float? highlightElevation = null,
            ShapeBorder shape = null
        ) {
            this.backgroundColor = backgroundColor;
            this.foregroundColor = foregroundColor;
            this.elevation = elevation;
            this.disabledElevation = disabledElevation;
            this.highlightElevation = highlightElevation;
            this.shape = shape;
        }

        public readonly Color backgroundColor;

        public readonly Color foregroundColor;

        public readonly float? elevation;

        public readonly float? disabledElevation;

        public readonly float? highlightElevation;

        public readonly ShapeBorder shape;

        public FloatingActionButtonThemeData copyWith(
            Color backgroundColor,
            Color foregroundColor,
            float? elevation,
            float? disabledElevation,
            float? highlightElevation,
            ShapeBorder shape
        ) {
            return new FloatingActionButtonThemeData(
                backgroundColor: backgroundColor ?? this.backgroundColor,
                foregroundColor: foregroundColor ?? this.foregroundColor,
                elevation: elevation ?? this.elevation,
                disabledElevation: disabledElevation ?? this.disabledElevation,
                highlightElevation: highlightElevation ?? this.highlightElevation,
                shape: shape ?? this.shape
            );
        }

        public static FloatingActionButtonThemeData lerp(FloatingActionButtonThemeData a, FloatingActionButtonThemeData b,
            float t) {
            if (a == null && b == null) {
                return null;
            }

            return new FloatingActionButtonThemeData(
                backgroundColor: Color.lerp(a?.backgroundColor, b?.backgroundColor, t),
                foregroundColor: Color.lerp(a?.foregroundColor, b?.foregroundColor, t),
                elevation: Mathf.Lerp(a?.elevation ?? 0, b?.elevation ?? 0, t),
                disabledElevation: Mathf.Lerp(a?.disabledElevation ?? 0, b?.disabledElevation ?? 0, t),
                highlightElevation: Mathf.Lerp(a?.highlightElevation ?? 0, b?.highlightElevation ?? 0, t),
                shape: ShapeBorder.lerp(a?.shape, b?.shape, t)
            );
        }

        public override int GetHashCode() {
            var hashCode = backgroundColor?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ foregroundColor?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ elevation?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ disabledElevation?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ highlightElevation?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ shape?.GetHashCode() ?? 0;
            return hashCode;
        }

        public bool Equals(FloatingActionButtonThemeData other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            return Equals(backgroundColor, other.backgroundColor)
                   && Equals(elevation, other.elevation)
                   && Equals(shape, other.shape)
                   && Equals(foregroundColor, other.foregroundColor)
                   && Equals(disabledElevation, other.disabledElevation)
                   && Equals(highlightElevation, other.highlightElevation);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj.GetType() != GetType()) {
                return false;
            }

            return Equals((FloatingActionButtonThemeData) obj);
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            FloatingActionButtonThemeData defaultData = new FloatingActionButtonThemeData();

            properties.add(new DiagnosticsProperty<Color>("backgroundColor", backgroundColor,
                defaultValue: defaultData.backgroundColor));
            properties.add(new DiagnosticsProperty<Color>("foregroundColor", foregroundColor,
                defaultValue: defaultData.foregroundColor));
            properties.add(new DiagnosticsProperty<float?>("elevation", elevation,
                defaultValue: defaultData.elevation));
            properties.add(new DiagnosticsProperty<float?>("disabledElevation", disabledElevation,
                defaultValue: defaultData.disabledElevation));
            properties.add(new DiagnosticsProperty<float?>("highlightElevation", highlightElevation,
                defaultValue: defaultData.highlightElevation));
            properties.add(new DiagnosticsProperty<ShapeBorder>("shape", shape, defaultValue: defaultData.shape));
        }
    }
}