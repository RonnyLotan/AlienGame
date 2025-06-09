using System;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace Shared
{
    public class Global
    {
        public static readonly string SERVER_IP = "127.0.0.1";
        public static readonly int SERVER_TCPPORT = 4950;

        public static readonly bool USE_ENCRYPTION = false;

        public static readonly string PEPPER = "a^hM3Rd2;nB]";

        public static readonly string PRIVATE_KEY = "BwIAAACkAABSU0EyAAgAAAEAAQDJqwit7LeqUwhcnmp164OX8SxBc3tyVmwSUxTZw2rEUhz4xXhVJgAY1q2UVufN6KIANc9qvjk6WCitoNHfRXaXuQzFr1Zvt0xy0qCwYZ6SUILPDHx0NTBxt65OiE55MfjMLOdy7Hc/gZBhDpi+6n+kp5E9uVhdFzLc+xilejQ78REUpVwDcqrRsnIgmfYEGduzk27QYcJain/Av9c2Z5FtBtzz6gQECt0qHGPqNI46c74+couAkAxKhSWVnl7kLF3dKOwF90PzSW31gyO2C7IEM4NbQE9relGc700M4IEU2MmbUulTkuVGHWD759rg2g9pwmbEXxeh734EyrI1HkLLB4rlQzHulG715y2t9CnINacuStJy0SJnuApGLn6xh8Cna0DI66JXh37RiY2jfBqPtv8zux0+q1S8ht4Mdtu1txSMLjPWJRLxEyP1HAgcXQUnBnnA1so3Et9oJS5/Oc5a6rhGhhrRFNDWXNPhrYdKZqibXL/5aUkuTddVoMEXR+yv5xVn8m9kxN7WzJpGdQ0gCVIfut4SNOU6VMlbqUt9xjhlW6XvfPpt2glyJRYBhBYLZbOOUF+he0RyHwRiIYYcWyZTurgiS0lIepk6TnG3d91Q69b65C0ER6LqjVlPWv6ad61kN8Q8lDSCPqKkhGLhB97bnJeVwe+4vjxTz3I53OtkgYLJNuBjJMddtzs10PTgHtQuoQZPbHn2bLgNGqXD3tFqGoK9HnSNYRiOqfWLSrmwFVhN2UqczTdBic54e0MnQDTpnlwShhOwG3CNCLGEXtB+QShfXmrNkUU4T7fKerwm/jQGHaXMLBinsu6S82kPvXXzFbroSRz26FTYrVtdXUiTCMiXRyl999UL7dlxohHYNHyeG4X2+hnTmGwKAAOqfdewtUi3ERaPtbPm1jp0bNrAuvnoiF5Zz+IbQqba4vnGsU/a6msN34kNAvCOZbfSPvAIwMcgd09Sa9POgy7DYOZ0z7fOxO0/6jAX/Q71Pks2TjLHY/fAfLZ+A0TqOBQ+6vaTN5PBPVWNpImV3QS8mz0GFOxkuy78DbmOjKkHzhL1dUhi8BIQ51PtdpvfDHGdCPzPoNUoQmpZQt2Bhh8HiTIfAErHbISKgeNxwzxTEUs2u9DGiQVUadTzXmhZBkJr+pyuLzEwmAovwfHdjvB5jWJs3O1CApWrhyR7JyJIcbFpPsB0ty03b3ubIzxSshawDQolIZf61ViEGpKSKiLuQeKYcFm4AaFL20J1r912A5ow2cdcFGC+EgFL/xCmJXqcW9PJzcQ56XlsDbUOac2210qHGuNZItoqOQL8jtA4r49DhnOhqcVgc65FbVsKRoTFrKA3M+os086f/2lSOvgfij62th0QJnXdb6sO5s0IbQegw3nPuQoub/wG+u4Dbumot44YX6gjDeOfvSpYZVkGzpzTrFBoeoU7hSrDrmCHHvv9HJ+C03jnmOeV8nabsuDIChEl4dUFtw/+/8CK6bIdhXUNHN+IVXh64wy4Chzeep/sCxGQ90yIf3gXRkAruyE=";
        public static readonly string PUBLIC_KEY = "BgIAAACkAABSU0ExAAgAAAEAAQDJqwit7LeqUwhcnmp164OX8SxBc3tyVmwSUxTZw2rEUhz4xXhVJgAY1q2UVufN6KIANc9qvjk6WCitoNHfRXaXuQzFr1Zvt0xy0qCwYZ6SUILPDHx0NTBxt65OiE55MfjMLOdy7Hc/gZBhDpi+6n+kp5E9uVhdFzLc+xilejQ78REUpVwDcqrRsnIgmfYEGduzk27QYcJain/Av9c2Z5FtBtzz6gQECt0qHGPqNI46c74+couAkAxKhSWVnl7kLF3dKOwF90PzSW31gyO2C7IEM4NbQE9relGc700M4IEU2MmbUulTkuVGHWD759rg2g9pwmbEXxeh734EyrI1HkLL";
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

    public static class JsonFile
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
            };

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
