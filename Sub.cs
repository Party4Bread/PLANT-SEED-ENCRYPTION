using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Subfunc
{
    class Sub
    {
        
        public static string Base64Encoding(string EncodingText, System.Text.Encoding oEncoding = null)
        {
            if (oEncoding == null)
                oEncoding = System.Text.Encoding.UTF8;

            byte[] arr = oEncoding.GetBytes(EncodingText);
            return System.Convert.ToBase64String(arr);
        }

        public static string Base64Decoding(string DecodingText, System.Text.Encoding oEncoding = null)
        {
            if (oEncoding == null)
                oEncoding = Encoding.UTF8;

            byte[] arr = Convert.FromBase64String(DecodingText);
            return oEncoding.GetString(arr);
        }

        public static string GetfileMD5(string path)
        {
            using (var fs = File.OpenRead(path))
            using (var md5 = new MD5CryptoServiceProvider())
                return string.Join("", md5.ComputeHash(fs).ToArray().Select(i => i.ToString("X2")));
        }
        public string GetstringMD5(string input)
        {
            byte[] bs = GetByteMD5(input);
            string password = "";
            foreach (byte b in bs)
            {
                password += b.ToString("x2").ToLower();
            }
            return password;
        }

        public static byte[] GetByteMD5(string input)
        {
            MD5CryptoServiceProvider x = new MD5CryptoServiceProvider();
            byte[] bs = Encoding.UTF8.GetBytes(input);
            bs = x.ComputeHash(bs);
            return bs;
        }



        public static byte[] stringToByteArray(string input)
        {
            char[] charArray = input.ToCharArray();
            return Encoding.UTF8.GetBytes(charArray);
        }


        public static string ByteArrayToString(byte[] input)
        {
            return Encoding.UTF8.GetString(input);
        }

        public class ConvMatrix
        {
            public int TopLeft = 0, TopMid = 0, TopRight = 0;
            public int MidLeft = 0, Pixel = 1, MidRight = 0;
            public int BottomLeft = 0, BottomMid = 0, BottomRight = 0;
            public int Factor = 1;
            public int Offset = 0;
            public void SetAll(int nVal)
            {
                TopLeft = TopMid = TopRight = MidLeft = Pixel = MidRight = BottomLeft = BottomMid = BottomRight = nVal;
            }
        }

        public class BitmapFilter
        {
            public static bool Conv3x3(Bitmap b, ConvMatrix m)
            {
                // Avoid divide by zero errors
                if (0 == m.Factor) return false;

                Bitmap bSrc = (Bitmap)b.Clone();

                // GDI+ still lies to us - the return format is BGR, NOT RGB.
                BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                BitmapData bmSrc = bSrc.LockBits(new Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

                int stride = bmData.Stride;
                int stride2 = stride * 2;
                System.IntPtr Scan0 = bmData.Scan0;
                System.IntPtr SrcScan0 = bmSrc.Scan0;

                unsafe
                {
                    byte* p = (byte*)(void*)Scan0;
                    byte* pSrc = (byte*)(void*)SrcScan0;

                    int nOffset = stride + 6 - b.Width * 3;
                    int nWidth = b.Width - 2;
                    int nHeight = b.Height - 2;

                    int nPixel;

                    for (int y = 0; y < nHeight; ++y)
                    {
                        for (int x = 0; x < nWidth; ++x)
                        {
                            nPixel = ((((pSrc[2] * m.TopLeft) + (pSrc[5] * m.TopMid) + (pSrc[8] * m.TopRight) +
                                (pSrc[2 + stride] * m.MidLeft) + (pSrc[5 + stride] * m.Pixel) + (pSrc[8 + stride] * m.MidRight) +
                                (pSrc[2 + stride2] * m.BottomLeft) + (pSrc[5 + stride2] * m.BottomMid) + (pSrc[8 + stride2] * m.BottomRight)) / m.Factor) + m.Offset);

                            if (nPixel < 0) nPixel = 0;
                            if (nPixel > 255) nPixel = 255;

                            p[5 + stride] = (byte)nPixel;

                            nPixel = ((((pSrc[1] * m.TopLeft) + (pSrc[4] * m.TopMid) + (pSrc[7] * m.TopRight) +
                                (pSrc[1 + stride] * m.MidLeft) + (pSrc[4 + stride] * m.Pixel) + (pSrc[7 + stride] * m.MidRight) +
                                (pSrc[1 + stride2] * m.BottomLeft) + (pSrc[4 + stride2] * m.BottomMid) + (pSrc[7 + stride2] * m.BottomRight)) / m.Factor) + m.Offset);

                            if (nPixel < 0) nPixel = 0;
                            if (nPixel > 255) nPixel = 255;

                            p[4 + stride] = (byte)nPixel;

                            nPixel = ((((pSrc[0] * m.TopLeft) + (pSrc[3] * m.TopMid) + (pSrc[6] * m.TopRight) +
                                (pSrc[0 + stride] * m.MidLeft) + (pSrc[3 + stride] * m.Pixel) + (pSrc[6 + stride] * m.MidRight) +
                                (pSrc[0 + stride2] * m.BottomLeft) + (pSrc[3 + stride2] * m.BottomMid) + (pSrc[6 + stride2] * m.BottomRight)) / m.Factor) + m.Offset);

                            if (nPixel < 0) nPixel = 0;
                            if (nPixel > 255) nPixel = 255;

                            p[3 + stride] = (byte)nPixel;

                            p += 3;
                            pSrc += 3;
                        }

                        p += nOffset;
                        pSrc += nOffset;
                    }
                }

                b.UnlockBits(bmData);
                bSrc.UnlockBits(bmSrc);

                return true;
            }

            public static bool GaussianBlur(Bitmap b, int nWeight /* default to 4*/)
            {
                ConvMatrix m = new ConvMatrix();
                m.SetAll(1);
                m.Pixel = nWeight;
                m.TopMid = m.MidLeft = m.MidRight = m.BottomMid = 2;
                m.Factor = nWeight + 12;

                return BitmapFilter.Conv3x3(b, m);
            }
        }

        public class Screenshot
        {
            public static Bitmap TakeSnapshot(Control ctl)
            {
                Bitmap bmp = new Bitmap(ctl.Size.Width, ctl.Size.Height);
                using (Graphics g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(
                        ctl.PointToScreen(ctl.ClientRectangle.Location),
                        new Point(0, 0), ctl.ClientRectangle.Size
                    );
                }
                return bmp;
            }
        }
    }
}

