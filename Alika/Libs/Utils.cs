using Alika.Libs.VK.Responses;
using Microsoft.Graphics.Canvas;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Alika.Libs
{
    public static class Utils
    {
        public static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static char? IsCharKey(this VirtualKey key, bool shift)
        {
            if (32 == (int)key)
                return ' ';
            VirtualKey search;
            foreach (var letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
            {
                if (Enum.TryParse<VirtualKey>(letter.ToString(), out search) && search.Equals(key))
                    return (shift) ? letter : letter.ToString().ToLower()[0];
            }
            foreach (var number in "1234567890")
            {
                if (Enum.TryParse<VirtualKey>("Number" + number.ToString(), out search) && search.Equals(key))
                    return number;
            }
            return null;
        }

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern int ToUnicode(uint virtualKeyCode, uint scanCode,
            byte[] keyboardState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)]
            StringBuilder receivingBuffer,
            int bufferSize, uint flags);

        public static string GetCharsFromKeys(this VirtualKey key, bool shift, bool altGr)
        {
            var buf = new StringBuilder(256);
            var keyboardState = new byte[256];
            if (shift)
                keyboardState[(int)VirtualKey.Shift] = 0xff;
            if (altGr)
            {
                keyboardState[(int)VirtualKey.Control] = 0xff;
                keyboardState[(int)VirtualKey.Menu] = 0xff;
            }
            Utils.ToUnicode((uint)key, 0, keyboardState, buf, 256, 0);
            return buf.ToString();
        }

        public static string LocString(string name, bool quotes = true)
        {
            string result;
            if (name.Contains("/"))
            {
                string[] splitted = name.Split("/");
                result = ResourceLoader.GetForCurrentView(splitted[0]).GetString(splitted[1]);
            }
            else result = ResourceLoader.GetForCurrentView().GetString(name);
            if (quotes) result = result.Replace("<<", "«").Replace(">>", "»");
            return result;
        }

        public static async Task<byte[]> GetBytesAsync(this StorageFile file)
        {
            byte[] fileBytes = null;
            if (file == null) return null;
            using (var stream = await file.OpenReadAsync())
            {
                fileBytes = new byte[stream.Size];
                using (var reader = new DataReader(stream))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(fileBytes);
                }
            }
            return fileBytes;
        }

        public static byte[] ReadToBytes(this Stream input)
        {
            var buffer = new byte[16 * 1024];

            using (var ms = new MemoryStream())
            {
                int read;

                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    ms.Write(buffer, 0, read);

                return ms.ToArray();
            }
        }

        public static string AssetTheme(string filename, string path = "UI/") => "ms-appx:///Assets/" + path + (App.DarkTheme ? "Light" : "Dark") + "/" + filename;

        public static string FormatSize(int bytes, int round = 2)
        {
            string[] suffixes = new string[] {
                Utils.LocString("Byte"),
                Utils.LocString("Kibibyte"),
                Utils.LocString("Megabyte"),
                Utils.LocString("Gigabyte")
            };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) > 0)
            {
                number /= 1024;
                counter++;
                if (counter == suffixes.Count()) break;
            }
            return Math.Round(number, round).ToString() + " " + suffixes[counter];
        }

        public static DateTime ToDateTime(this int unixtime) => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixtime).ToLocalTime();

        public static string RuToEng(this string str) => str.StrTr("йцукенгшщзхъфывапролджэячсмитьбю.ё!\"№;%:?*", "qwertyuiop[]asdfghjkl;'zxcvbnm,./`!@#$%^&*");

        public static string StrTr(this string str, string from, string to)
        {
            if (from.Length != to.Length) return null;
            for (int x = 0; x < from.Length; x++) str = str.Replace(from[x], to[x]);
            return str;
        }

        public static int ToUnixTime(this DateTime time) => (int)(time.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

        public static string AppPath(string path) => Package.Current.InstalledLocation.Path + "/" + path;

        public static void RemoveParent(this FrameworkElement element)
        {
            var parent = element.Parent;
            if (parent == null) return;
            if (parent as Panel != null)
            {
                (parent as Panel).Children.Remove(element);
            }
            else if (parent as ContentControl != null)
            {
                (parent as ContentControl).Content = null;
            }
            else if (parent as ItemsControl != null)
            {
                (parent as ItemsControl).Items.Remove(element);
            }
            else if (parent as Border != null)
            {
                (parent as Border).Child = null;
            }
        }

        /// <summary>
        /// Is element visible on screen
        /// <see href="https://docs.microsoft.com/en-us/archive/blogs/llobo/determining-the-visibility-of-elements-inside-scrollviewer"/>
        /// </summary>
        /// <param name="control">Parent element (ScrollViewer, for example)</param>
        /// <param name="element">Element</param>
        /// <returns></returns>
        public static bool IsElementVisible(this ContentControl control, UIElement element)
        {
            var childTransform = element.TransformToVisual(control);
            var rect = childTransform.TransformBounds(new Rect(new Point { X = 0, Y = 0 }, control.RenderSize));
            var result = RectHelper.Intersect(new Rect(new Point { X = 0, Y = 0 }, control.RenderSize), rect);
            return result != Rect.Empty;
        }

        /// <summary>
        /// Short name
        /// </summary>
        /// <example>
        /// Sasha Vinogradov => Sasha V.
        /// </example>
        /// <param name="user"></param>
        /// <returns></returns>
        public static string ShortName(this string user)
        {
            if (user.Count(c => c == ' ') != 1) return user;
            var split = user.Split(" ");
            return split[0] + " " + split[1].Substring(0, 1) + ".";
        }

        public async static Task<byte[]> ToByteArray(this InkCanvas canvas)
        {
            var strokes = canvas.InkPresenter.StrokeContainer;
            if (strokes.GetStrokes().Count == 0) return null;
            var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("canvas.png", CreationCollisionOption.ReplaceExisting);
            if (file != null)
            {
                CanvasDevice device = CanvasDevice.GetSharedDevice();
                CanvasRenderTarget renderTarget = new CanvasRenderTarget(device, (int)canvas.ActualWidth, (int)canvas.ActualHeight, 96);
                using (var ds = renderTarget.CreateDrawingSession())
                {
                    ds.DrawInk(canvas.InkPresenter.StrokeContainer.GetStrokes());
                }
                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await renderTarget.SaveAsync(fileStream, CanvasBitmapFileFormat.Png, 1f);
                }
                return await ApplicationData.Current.TemporaryFolder.ReadBytesFromFileAsync("canvas.png");
            }
            else return null;
        }

        public static string ToCompactText(this Message msg)
        {
            string text = "";
            if (msg.Text.Length > 0)
            {
                string temptext = msg.Text.Replace("\n", " ");
                MatchCollection pushes = new Regex(@"\[(id|club)\d+\|[^\]]*]").Matches(msg.Text);
                if (pushes.Count > 0)
                {
                    foreach (Match push in pushes)
                    {
                        temptext = temptext.Replace(push.Value, push.Value.Split("|").Last().Replace("]", ""));
                    }
                }
                text += temptext;
            }
            else
            {
                if (msg.Attachments != null && msg.Attachments.Count > 0)
                {
                    switch (msg.Attachments.First().Type)
                    {
                        case "photo":
                            text += "📷 " + Utils.LocString("Attachments/Photo");
                            break;

                        case "video":
                            text += "📽 " + Utils.LocString("Attachments/Video");
                            break;

                        case "audio_message":
                            text += "🎤 " + Utils.LocString("Attachments/VoiceMessage");
                            break;

                        case "link":
                            text += "🔗 " + Utils.LocString("Attachments/Link");
                            break;

                        case "sticker":
                            text += "😀 " + Utils.LocString("Attachments/Sticker");
                            break;

                        case "gift":
                            text += "🎁 " + Utils.LocString("Attachments/Gift");
                            break;

                        case "doc":
                            text += "📂 " + Utils.LocString("Attachments/Document");
                            break;

                        case "graffiti":
                            text += "🖌 " + Utils.LocString("Attachments/Graffiti");
                            break;
                    }
                }
                else
                {
                    if (msg.FwdMessages?.Count > 0)
                    {
                        text += "↩ " + Utils.LocString("Dialog/FwdMessages");
                    }
                }
            }
            return text;
        }
    }
}