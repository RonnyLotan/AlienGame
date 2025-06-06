using System;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Windows.Forms;

namespace Shared
{
    public class Global
    {
        public static readonly string SERVER_IP = "127.0.0.1";
        public static readonly int SERVER_TCPPORT = 4950;

        public static readonly bool USE_ENCRYPTION = false;
    }

    public class GUI
    {
        public static void ActionComponent(Control c, Action act)
        {
            if (c.InvokeRequired)
            {
                c.Invoke(new Action(() => act()));
            }
            else
            {
                act();
            }
        }

        public static void AppendText(string line, RichTextBox rtb, bool bold, bool newLine)
        {
            ActionComponent(rtb, () =>
            {
                if (bold)
                {
                    Font boldFont = new Font(rtb.Font, FontStyle.Bold);
                    rtb.SelectionFont = boldFont;
                }

                rtb.AppendText(line + (newLine ? Environment.NewLine : ""));
                rtb.SelectionStart = rtb.Text.Length;
                rtb.ScrollToCaret();

                if (bold)
                {
                    rtb.SelectionFont = rtb.Font;
                }
            });
        }

        public static void Update(string line, Label lbl)
        {
            ActionComponent(lbl, () =>
            {
                lbl.Text = line;
            });
        }

        public static Image MakeGrayscale(Image original)
        {
            Bitmap grayBitmap = new Bitmap(original.Width, original.Height);

            using (Graphics g = Graphics.FromImage(grayBitmap))
            {
                var colorMatrix = new System.Drawing.Imaging.ColorMatrix(
                    new float[][]
                    {
                new float[] { 0.3f, 0.3f, 0.3f, 0, 0 },
                new float[] { 0.59f, 0.59f, 0.59f, 0, 0 },
                new float[] { 0.11f, 0.11f, 0.11f, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 0, 0, 0, 0, 1 }
                    });

                var attributes = new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);

                g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                            0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            }

            return grayBitmap;
        }
    }
}
