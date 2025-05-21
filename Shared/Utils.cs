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
        public static void invokeControl(Control c, Action act)
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

        public static void AppendLine(string line, RichTextBox rtb)
        {
            invokeControl(rtb, () =>
            {
                rtb.AppendText(line);
                rtb.SelectionStart = rtb.Text.Length;
                rtb.ScrollToCaret();                
            });
        }

        public static void Update(string line, Label lbl)
        {
            invokeControl(lbl, () =>
            {
                lbl.Text = line;                
            });
        }
    }
}
