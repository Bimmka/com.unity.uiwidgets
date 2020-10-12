using UnityEngine;

namespace Unity.UIWidgets.uiOld{
    public struct uiColor {
        public readonly long value;

        public uiColor(long value) {
            this.value = value & 0xFFFFFFFF;
        }

        public static readonly uiColor clear = new uiColor(0x00000000);

        public static readonly uiColor black = new uiColor(0xFF000000);

        public static readonly uiColor white = new uiColor(0xFFFFFFFF);

        public int alpha {
            get { return (int) ((0xff000000 & value) >> 24); }
        }

        public float opacity {
            get { return alpha / 255.0f; }
        }

        public int red {
            get { return (int) ((0x00ff0000 & value) >> 16); }
        }

        public int green {
            get { return (int) ((0x0000ff00 & value) >> 8); }
        }

        public int blue {
            get { return (int) ((0x000000ff & value) >> 0); }
        }

        public static uiColor fromColor(Color color) {
            return new uiColor(color.value);
        }

        public static uiColor fromARGB(int a, int r, int g, int b) {
            return new uiColor(
                (((a & 0xff) << 24) |
                 ((r & 0xff) << 16) |
                 ((g & 0xff) << 8) |
                 ((b & 0xff) << 0)) & 0xFFFFFFFF);
        }

        public static uiColor fromRGBO(int r, int g, int b, float opacity) {
            return new uiColor(
                ((((int) (opacity * 0xff) & 0xff) << 24) |
                 ((r & 0xff) << 16) |
                 ((g & 0xff) << 8) |
                 ((b & 0xff) << 0)) & 0xFFFFFFFF);
        }

        public uiColor withAlpha(int a) {
            return fromARGB(a, red, green, blue);
        }

        public uiColor withOpacity(float opacity) {
            return withAlpha((int) (opacity * 255));
        }

        static float _linearizeColorComponent(float component) {
            if (component <= 0.03928f) {
                return component / 12.92f;
            }

            return Mathf.Pow((component + 0.055f) / 1.055f, 2.4f);
        }

        public float computeLuminance() {
            float R = _linearizeColorComponent(red / 0xFF);
            float G = _linearizeColorComponent(green / 0xFF);
            float B = _linearizeColorComponent(blue / 0xFF);
            return 0.2126f * R + 0.7152f * G + 0.0722f * B;
        }
    }
}