using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;

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
        public static string AssetTheme(string filename, string path = "UI/")
        {
            return "ms-appx:///Assets/" + path + (App.systemDarkTheme ? "Light" : "Dark") + "/" + filename;
        }

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
                number = number / 1024;
                counter++;
                if (counter == suffixes.Count()) break;
            }
            return Math.Round(number, round).ToString() + " " + suffixes[counter];
        }

        public static DateTime ToDateTime(this int unixtime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(unixtime).ToLocalTime();
        }
    }
}
