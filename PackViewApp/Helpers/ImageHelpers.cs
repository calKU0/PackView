using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace PackViewApp.Helpers
{
    public static class ImageHelpers
    {
        [DllImport("cdn_sys.dll")]
        private static extern int DekompresujWPamieci_Raw(byte[] dest, ref int destlen, byte[] src, int srclen);

        public static BitmapImage LoadImage(byte[] data, long compressedSize)
        {
            byte[] finalData;

            try
            {
                if (data == null || data.Length == 0)
                {
                    return GeneratePlaceholderImage("Brak zdjęcia");
                }

                if (compressedSize == 0)
                {
                    finalData = data;
                }
                else
                {
                    try
                    {
                        finalData = Uncompress(data, compressedSize);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show(ex.ToString(), "Błąd dekompresji");
                        return GeneratePlaceholderImage("Błąd dekompresji");
                    }
                }

                return ByteArrayToBitmapImage(finalData);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "Błąd odczytu");
                return GeneratePlaceholderImage("Błąd odczytu");
            }
        }

        private static byte[] Uncompress(byte[] data, long uncompressedLength)
        {
            try
            {
                byte[] dest = new byte[uncompressedLength];
                int destlen = (int)uncompressedLength;
                int ret = DekompresujWPamieci_Raw(dest, ref destlen, data, data.Length);
                if (ret == -5)
                    throw new Exception("Cdnsys_SmallBuffer");
                else if (ret != 0)
                    throw new Exception("Cdnsys_ErrorCompressingData");
                return dest;
            }
            catch
            {
                return new byte[1];
            }
        }

        private static BitmapImage ByteArrayToBitmapImage(byte[] imageData)
        {
            using (var ms = new MemoryStream(imageData))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }

        private static BitmapImage GeneratePlaceholderImage(string text)
        {
            Font font = new Font("Times New Roman", 100.0f);
            using (Image image = DrawText(text, font, Color.Black, Color.Gray))
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Jpeg);
                return ByteArrayToBitmapImage(ms.ToArray());
            }
        }

        private static Image DrawText(string text, Font font, Color textColor, Color backColor)
        {
            using (Bitmap tempImg = new Bitmap(1, 1))
            using (Graphics tempGraphics = Graphics.FromImage(tempImg))
            {
                SizeF textSize = tempGraphics.MeasureString(text, font);

                Bitmap img = new Bitmap((int)textSize.Width, (int)textSize.Height);
                using (Graphics drawing = Graphics.FromImage(img))
                {
                    drawing.Clear(backColor);
                    using (Brush textBrush = new SolidBrush(textColor))
                    {
                        drawing.DrawString(text, font, textBrush, 0, 0);
                    }

                    drawing.Save();
                    return (Image)img.Clone();
                }
            }
        }
    }
}