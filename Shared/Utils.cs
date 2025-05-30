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
        public static void InvokeControl(Control c, Action act)
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
            InvokeControl(rtb, () =>
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
            InvokeControl(lbl, () =>
            {
                lbl.Text = line;                
            });
        }
    }
}
