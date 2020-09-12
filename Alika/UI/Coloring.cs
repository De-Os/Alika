using System;
using Windows.UI.Xaml.Media;

namespace Alika.UI
{
    public static class Coloring
    {
        public static class Transparent
        {
            /// <summary>
            /// Fully transparent
            /// </summary>
            public static SolidColorBrush Full = new SolidColorBrush(new Windows.UI.Color
            {
                A = 0,
                R = 0,
                G = 0,
                B = 0
            });

            public static SolidColorBrush Percent(double percent)
            {
                return new SolidColorBrush(new Windows.UI.Color
                {
                    A = Convert.ToByte(Math.Round(255 * percent)),
                    R = (byte)(App.systemDarkTheme ? 0 : 255),
                    G = (byte)(App.systemDarkTheme ? 0 : 255),
                    B = (byte)(App.systemDarkTheme ? 0 : 255)
                });
            }

            /// <summary>
            /// Percent transparency
            /// </summary>
            /// <param name="percent">Opacity percents</param>
            public static SolidColorBrush Percent(int percent)
            {
                return Coloring.Transparent.Percent((double)percent / 100);
            }
        }
        public static class MessageBox
        {
            public static class TextBubble
            {
                public static SolidColorBrush Dark = new SolidColorBrush(new Windows.UI.Color
                {
                    A = 255,
                    R = 46,
                    G = 46,
                    B = 46
                });

                public static SolidColorBrush Light = new SolidColorBrush(new Windows.UI.Color
                {
                    A = 255,
                    R = 230,
                    G = 230,
                    B = 230
                });
            }

            public static class VoiceMessage
            {
                public static SolidColorBrush Dark = new SolidColorBrush(new Windows.UI.Color
                {
                    A = 255,
                    R = 0,
                    G = 0,
                    B = 0
                });

                public static SolidColorBrush Light = new SolidColorBrush(new Windows.UI.Color
                {
                    A = 255,
                    R = 255,
                    G = 255,
                    B = 255
                });
            }

            public static class Keyboard
            {
                public static string GetColor(string type)
                {
                    switch (type)
                    {
                        case "primary":
                            return "5181B8";
                        case "negative":
                            return "E64646";
                        case "positive":
                            return "4BB34B";
                        default:
                            return "FFFFFF";
                    }
                }
            }
        }

        public static Windows.UI.Color FromHash(string hash)
        {
            if (hash.StartsWith("#")) hash = hash.Substring(1);
            return new Windows.UI.Color
            {
                A = 255,
                R = Convert.ToByte(hash.Substring(0, 2), 16),
                G = Convert.ToByte(hash.Substring(2, 2), 16),
                B = Convert.ToByte(hash.Substring(4, 2), 16)
            };
        }
    }
}
