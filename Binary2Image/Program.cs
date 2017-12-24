using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binary2Image
{
    class Program
    {
        // Not going to support multiple endianness, not worth the effort

        static void Main(string[] args)
        {
            ConvertFileToImage(@"test.txt", @"test.png");
            ConvertImageToFile(@"test.png", @"test-back.txt");
        }

        public static void ConvertFileToImage(string inFile, string outFile)
        {
            using (BinaryReader br = new BinaryReader(new FileStream(inFile, FileMode.Open)))
            {
                int width = (int)Math.Ceiling(Math.Sqrt(br.BaseStream.Length / 4));
                int height = (int)Math.Ceiling(Math.Sqrt(br.BaseStream.Length / 4)) + 1;

                using (Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                {
                    // Clear the image
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.FromArgb(0));
                    }

                    // Setup for writing to image
                    BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);
                    IntPtr ptr = data.Scan0;
                    byte[] bytes = new byte[Math.Abs(data.Stride) * bmp.Height];
                    System.Runtime.InteropServices.Marshal.Copy(ptr, bytes, 0, Math.Abs(data.Stride) * bmp.Height);

                    // Write metadata
                    Color length = Color.FromArgb((int)br.BaseStream.Length);
                    bytes[3] = length.R;
                    bytes[2] = length.G;
                    bytes[1] = length.B;
                    bytes[0] = length.A;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        br.BaseStream.CopyTo(ms);
                        ms.Seek(0, SeekOrigin.End);
                        ms.Write(new byte[] { 0, 0, 0, 0 }, 0, 4);
                        ms.Seek(0, SeekOrigin.Begin);

                        // Draw pixels
                        int x = 0;
                        int y = 1;
                        while (ms.Position < br.BaseStream.Length)
                        {
                            Color c = Color.Black;
                            // In case the file doesn't have a size that is a multiple of 4
                            if ((ms.Position + 4) > br.BaseStream.Length)
                            {
                                int remainingBytesToGet = 4 - (((int)ms.Position + 4) - (int)br.BaseStream.Position);
                                byte[] array = { 0, 0, 0, 0 };
                                for (int a = 0; a < remainingBytesToGet; a++)
                                {
                                    array[a] = (byte)ms.ReadByte();
                                }
                                c = Color.FromArgb(BitConverter.ToInt32(array, 0));
                            }
                            else
                            {
                                c = Color.FromArgb(BitConverter.ToInt32(new byte[] { (byte)ms.ReadByte(), (byte)ms.ReadByte(), (byte)ms.ReadByte(), (byte)ms.ReadByte() }, 0));
                            }
                            int i = ((y * bmp.Width) + x) * 4;
                            bytes[i] = c.R;
                            bytes[i + 1] = c.G;
                            bytes[i + 2] = c.B;
                            bytes[i + 3] = c.A;

                            // Advance position
                            x++;
                            if (x >= width) { x = 0; y++; }

                            if (y >= height)
                                break;
                        }
                    }

                    // Export the image
                    System.Runtime.InteropServices.Marshal.Copy(bytes, 0, ptr, bytes.Length);
                    bmp.UnlockBits(data);
                    bmp.Save(outFile, ImageFormat.Png);
                }
            }
        }

        public static void ConvertImageToFile(string imageFile, string outFile)
        {
            using (Bitmap bmp = new Bitmap(imageFile))
            {
                if (bmp.PixelFormat != PixelFormat.Format32bppArgb)
                {
                    Console.WriteLine("Improper image format.");
                    throw new Exception("Improper image format.");
                }

                // Setup for reading from image
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
                IntPtr ptr = data.Scan0;
                byte[] bytes = new byte[Math.Abs(data.Stride) * bmp.Height];
                System.Runtime.InteropServices.Marshal.Copy(ptr, bytes, 0, Math.Abs(data.Stride) * bmp.Height);

                // Get the length that the original file was
                int length = BitConverter.ToInt32(new byte[] { bytes[1], bytes[2], bytes[3], bytes[0] }, 0);

                // Decode and write to the output file
                using (BinaryWriter bw = new BinaryWriter(new FileStream(outFile, FileMode.Create)))
                {
                    int x = 0;
                    int y = 1;
                    for (int count = 0; count < length; count += 4)
                    {
                        int i = ((y * bmp.Width) + x) * 4;
                        byte[] originalbytes = new byte[] { bytes[i + 2], bytes[i + 1], bytes[i + 0], bytes[i + 3] };

                        // In case it isn't a multiple of 4
                        if ((count + 4) > length)
                        {
                            bw.Write(originalbytes, 0, 4 - ((count + 4) - length));
                        }
                        else
                        {
                            bw.Write(originalbytes);
                        }

                        // Advance position
                        x++;
                        if (x >= bmp.Width) { x = 0; y++; }
                    }
                }

                bmp.UnlockBits(data);
            }
        }
    }
}
