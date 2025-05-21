using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.Json;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using DSS = System.Collections.Generic.Dictionary<string, string>;
using System.Runtime.ConstrainedExecution;
using System.Net.NetworkInformation;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace Shared
{
    public enum Protocols
    {
        fallback,       
        register,   
        login,      
        success,    
        fail,       
        chat,       
        photo,      
        disconnect  
    }
    
    enum Msg { canIbeTheServer, denay, welcome, requestServerPoint }
    enum Ctrl { debLog, logBoard, pass, user, connect, register }
    static class Trd
    {
        public static void start(Action act)
        {
            new Thread(() => act()) { IsBackground = true }.Start();
        }
    }
    static class UI
    {
        public static int cnt = 0;
        public static Dictionary<string, Control> dic = new Dictionary<string, Control>();
        public static Control get(string name) => dic.TryGetValue(name, out var control) ? control : null;
        public static Control get(Ctrl name) => dic.TryGetValue(name.ToString(), out var control) ? control : null;

        //public static Control get(string name)
        //{
        //    return dic.ContainsKey(name) ? dic[name] : null;
        //}

        public static void RegisterControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                dic[control.Name] = control;
                if (control.Controls.Count > 0)
                {
                    RegisterControls(control.Controls);
                }
            }
        }

        static void invokeControl(Control c, string msg, Action act)
        {

            if (c.InvokeRequired)
            {
                c.Invoke(act);
            }
            else
            {
                act();
            }
        }

        static void invokeControl(Control c, char chr, Action act)
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

        public static void append(Ctrl name, string message)
        {
            Control control = get(name);
            if (control is RichTextBox rtb)
            {
                invokeControl(control, message, () =>
                {
                    rtb.AppendText(message);
                    rtb.SelectionStart = rtb.Text.Length;
                    rtb.ScrollToCaret();
                });
            }
        }


        public static void scroll_rtb(Ctrl name)
        {
            Control control = get(name);
            if (control is RichTextBox rtb)
            {

                invokeControl(control, null, () =>
                {
                    rtb.SelectionStart = rtb.Text.Length;
                    rtb.ScrollToCaret();
                });


            }
        }

        public static void appendline(Control c, string msg, bool newline = true)
        {
            string n = newline ? "\n" : "";
            c.Text += msg + n;
        }

        public static void notify(string msg)
        {
            append(Ctrl.logBoard, msg + "\n");
        }



        static SemaphoreSlim typeSemaphore = new SemaphoreSlim(1, 1);

        public static async Task type(string msg,bool debug=true )
        {
            #region MyRegion
            void appendChar(Ctrl name, char chr)
            {
                Control control = get(name);
                if (control is RichTextBox rtb)
                {

                    invokeControl(control, chr, () =>
                    {
                        rtb.AppendText(chr.ToString());
                    });

                }
            }
            #endregion
            await typeSemaphore.WaitAsync();
            Ctrl ctrl = debug ? Ctrl.logBoard : Ctrl.debLog;
            int sec  = debug ? 10 : 1;
            try
            {
                msg += "\n";
                foreach (char c in msg)
                {
                    appendChar(ctrl, c);
                    if (c == '\n') scroll_rtb(ctrl);
                    await Task.Delay(sec); 
                }
            }
            finally
            {
                typeSemaphore.Release(); 
            }
        }

        public static async Task debugtype(string msg)
        {
            #region MyRegion
            void appendChar(Ctrl name, char chr)
            {
                Control control = get(name);
                if (control is RichTextBox rtb)
                {

                    invokeControl(control, chr, () =>
                    {
                        rtb.AppendText(chr.ToString());
                    });

                }
            }
            #endregion
            await typeSemaphore.WaitAsync();
            try
            {
                msg = cnt++.ToString() + ": " + msg;
                msg += "\n.........";
                foreach (char c in msg)
                {
                    appendChar(Ctrl.debLog, c);
                    if (c == '\n') scroll_rtb(Ctrl.debLog);
                    await Task.Delay(1);
                }
            }
            finally
            {
                typeSemaphore.Release(); 
            }
        }


        public static void Hide(string name)
        {
            if (dic.TryGetValue(name, out var control))
            {
                if (control.InvokeRequired)
                {
                    control.Invoke(new Action(() => control.Visible = false));
                }
                else
                {
                    control.Visible = false;
                }
            }
        }
        public static void Show(Control control)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new Action(() => control.Visible = true));
            }
            else
            {
                control.Visible = true;
            }
        }




        public static void Hide1(string name)
        {
            dic[name].Hide();
        }
        public static void Show1(Control c)
        {
            c.Show();
        }
    }

    static class Udp
    {
        static string bitTostr(byte[] bit) => Encoding.ASCII.GetString(bit);
        static byte[] strTobit(string str) => Encoding.ASCII.GetBytes(str);
        static byte[] strTobit(Msg m) => Encoding.ASCII.GetBytes(m.ToString());
        static bool equal(Msg m, byte[] bit)
        {
            return bitTostr(bit) == m.ToString();
        }
        static bool equal(string str, Msg m)
        {
            return str == m.ToString();
        }
        public static bool canBeServer()
        {

            #region data
            UdpClient bordcastSocket;
            IPEndPoint allPoints;
            Byte[] bitCanbe;

            IPEndPoint receivingPoint;
            byte[] bitReplay;

            #endregion
            #region inline
            void set_bordcastSocket_allPoints_and_bit_msg()
            {
                bordcastSocket = new UdpClient() { EnableBroadcast = true };
                allPoints = new IPEndPoint(IPAddress.Broadcast, 6000);
                bitCanbe = strTobit(Msg.canIbeTheServer);
            }


            void brodcast_bit_msg_and_hold_for_responce()
            {
                bordcastSocket.Send(bitCanbe, bitCanbe.Length, allPoints);
                bordcastSocket.Client.ReceiveTimeout = 500;
            }
            bool recieved_denay_to_being_server()
            {

                receivingPoint = new IPEndPoint(IPAddress.Any, 0);

                bitReplay = bordcastSocket.Receive(ref receivingPoint);
                bordcastSocket.Client.ReceiveTimeout = 500;

                bool res = equal(Msg.denay, bitReplay);

                return equal(Msg.denay, bitReplay);
            }


            #endregion

            set_bordcastSocket_allPoints_and_bit_msg();

            brodcast_bit_msg_and_hold_for_responce();

            try
            {
                if (recieved_denay_to_being_server())
                {
                    bordcastSocket.Close();
                    return false;
                }
            }
            catch (Exception) { }
            finally { bordcastSocket.Close(); }
            return true;
        }

        public static void denayOthers()
        {
            #region tools
            UdpClient socket6000;
            IPEndPoint point6000;

            #endregion
            #region inline
            void set_and_bind_socket_and_endpoint()
            {
                socket6000 = new UdpClient();
                socket6000.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);//1
                socket6000.Client.Bind(new IPEndPoint(IPAddress.Any, 6000));//1
                point6000 = new IPEndPoint(IPAddress.Any, 6000);
            }
            bool someone_else_is_asking_to_be_server() =>
                 equal(Msg.canIbeTheServer, socket6000.Receive(ref point6000));

            void deny_other()
            {
                byte[] bitDenay = strTobit(Msg.denay);
                socket6000.Send(bitDenay, bitDenay.Length, point6000);
            }
            #endregion
            set_and_bind_socket_and_endpoint();
            Trd.start(() =>
            {
                try
                {
                    while (true)
                        if (someone_else_is_asking_to_be_server())
                            deny_other();
                }
                catch (Exception) { }
            });
        }

        public static string GetIp()
        {
            UdpClient client = new UdpClient();
            IPEndPoint allpoints = new IPEndPoint(IPAddress.Broadcast, 6002);
            IPEndPoint recieve = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = Encoding.UTF8.GetBytes(Msg.requestServerPoint.ToString());
            client.Send(data, data.Length, allpoints);
            try
            {
                byte[] point = client.Receive(ref recieve);
                string ip = Encoding.UTF8.GetString(point);
                return ip;
            }
            catch (SocketException e) { }
            return "/";


        }
        static IPAddress FilterIp()
        {
            var host = Dns.GetHostAddresses(Dns.GetHostName());
            IPAddress[] ips = host;
            foreach (IPAddress ip in ips)
            {
                if (!IPAddress.IsLoopback(ip) && ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }

            }
            return null;


        }
        public static void GiveServerIp()
        {
            IPEndPoint clients = new IPEndPoint(IPAddress.Any, 6002);
            UdpClient server = new UdpClient(clients);
            while (true)
            {
                byte[] msg = server.Receive(ref clients);
                if (msg.SequenceEqual(Encoding.UTF8.GetBytes(Msg.requestServerPoint.ToString())))
                {
                    string ip = FilterIp().ToString();
                    byte[] data = Encoding.UTF8.GetBytes(ip);
                    server.Send(data, data.Length, clients);
                }
            }
        }
    }
    static class Strm
    {

        public static (StreamReader reader, StreamWriter writer)
            init_reader_writer(TcpClient socket, StreamReader reader, StreamWriter writer)
        {
            if (socket == null || !socket.Connected)
                throw new InvalidOperationException("Socket is not initialized or connected.");

            NetworkStream nws = socket.GetStream();
            reader = new StreamReader(nws, Encoding.UTF8);
            writer = new StreamWriter(nws, Encoding.UTF8) { AutoFlush = true };
            return (reader, writer);
        }        
    }

    public class User
    {
        public required string Name { get; set; }
        public required string Salt { get; set; }
        public required string HashedPassword { get; set; }
        public required string Email { get; set; }
    }

    public class Lobby
    {
        public required string Name { get; set; }
        public required string Salt { get; set; }
        public required string HashedEntryCode { get; set; }
        public required string Host { get; set; }
    }

    public static class Jsn
    {
        const string USERFOLDER = "users";
        const string LOBBYFOLDER = "lobbies";

        static string get_path(string dir, string name)
        {
            var path = Path.Combine(dir, name + ".json");
            return path;
        }

        public static User? getUser(string name)
        {
            var path = get_path(USERFOLDER, name);
            if (!File.Exists(path)) 
                return null;

            string jsonData = File.ReadAllText(get_path(USERFOLDER, name));
            return JsonSerializer.Deserialize<User>(jsonData);        
        }

        public static bool RegisterUser(string name, string password, string email)
        {
            bool not_valid_params() => string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(email);

            if (not_valid_params()) return false;

            string path = get_path(USERFOLDER, name);

            if (File.Exists(path)) 
                return false;

            var salt = Encryption.GenerateRandomString(10);

            Directory.CreateDirectory(USERFOLDER);
            User user = new User
            {
                Name = name,
                Salt = salt,
                HashedPassword = Encryption.ComputeHash(password, salt),
                Email = email
            }

            string jsonData = JsonSerializer.Serialize(user,
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(path, jsonData);
            return true;
        }

        public static Lobby? getLobby(string name)
        {
            var path = get_path(LOBBYFOLDER, name);
            if (!File.Exists(path))
                return null;

            string jsonData = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Lobby>(jsonData);
        }

        public static bool RegisterLobby(string name, string entryCode, string host)
        {
            bool not_valid_params() => string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(entryCode);

            if (not_valid_params()) return false;

            string path = get_path(LOBBYFOLDER, name);

            if (File.Exists(path))
                return false;

            var salt = Encryption.GenerateRandomString(10);

            Directory.CreateDirectory(LOBBYFOLDER);
            var lobby = new Lobby
            {
                Name = name,
                Salt = salt,
                HashedEntryCode = Encryption.ComputeHash(entryCode, salt),
                Host = host
            };

            string jsonData = JsonSerializer.Serialize(lobby,
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(path, jsonData);
            return true;
        }
    }
}
