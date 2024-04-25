using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using TCP_chat.Common;

namespace TCP_chat.Client
{
    internal class FileManager
    {
        private readonly object syncFileObj = new object();

        // отправка файлов
        public async Task SendFileAsync(NetworkStream stream, string userName, string destUserName)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();

                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                string SendFileName = null;

                if (openFileDialog.ShowDialog() == true)
                {
                    SendFileName = openFileDialog.FileName;

                    FileInfo fi = new FileInfo(SendFileName);

                    int bufferSize = 1024;
                    byte[] buffer = null;
                    byte[] header = null;

                    FileStream fs = null;
                    lock (syncFileObj)
                        fs = new FileStream(SendFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

                    int bufferCount = Convert.ToInt32(Math.Ceiling((double)fs.Length / (double)bufferSize));

                    string headerStr = "Content-length:" + fs.Length.ToString() + "$Filename:" + fi.Name + "$UserName:" + userName + "$DestUser:" + destUserName + "\r\n";
                    header = new byte[bufferSize];
                    Array.Copy(Encoding.Default.GetBytes(headerStr), header, Encoding.Default.GetBytes(headerStr).Length);

                    await stream.WriteAsync(header, 0, header.Length);
                    await stream.FlushAsync();

                    for (int i = 0; i < bufferCount; i++)
                    {
                        buffer = new byte[bufferSize];
                        int size = fs.Read(buffer, 0, bufferSize);

                        await stream.WriteAsync(buffer, 0, size);
                    }

                    NetworkClient.eventMessage?.Invoke(null, "Отправлен файл: " + fi.Name);
                    fs.Close();

                    await stream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                // сообщение о сбое
                NetworkClient.eventMessage?.Invoke(null, ex.Message);
            }
        }

        // получение файла
        public async Task ReceiveFileAsync(NetworkStream stream, string headMessage)
        {
            try
            {
                int bufferSize = 1024;
                byte[] buffer = null;
                string filename = "";
                string username = "";
                int filesize = 0;

                string[] splitted = headMessage.Split(new string[] { "$" }, StringSplitOptions.None);
                System.Collections.Generic.Dictionary<string, string> headers = new System.Collections.Generic.Dictionary<string, string>();

                // разбор заголовка
                foreach (string s in splitted)
                    if (s.Contains(":"))
                        if (s.Contains("Content-length:"))
                        {
                            var f = s.Substring(s.IndexOf("C"));
                            headers.Add(f.Substring(0, f.IndexOf(":")), f.Substring(f.IndexOf(":") + 1));
                        }
                        else headers.Add(s.Substring(0, s.IndexOf(":")), s.Substring(s.IndexOf(":") + 1));

                filesize = Convert.ToInt32(headers["Content-length"]);
                filename = headers["Filename"];
                username = headers["UserName"];

                int bufferCount = Convert.ToInt32(Math.Ceiling((double)filesize / (double)bufferSize));

                var pathFile = Environment.CurrentDirectory + "\\files\\";

                FileStream fs = null;
                lock (syncFileObj)
                    fs = new FileStream(pathFile + filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

                while (filesize > 0)
                {
                    buffer = new byte[bufferSize];
                    int size = await stream.ReadAsync(buffer, 0, buffer.Length);
                    fs.Write(buffer, 0, size);
                    filesize -= size;
                }

                NetworkClient.eventMessage?.Invoke(null, username + ": Получен файл: " + filename + "\r\n     В папке: " + pathFile);

                fs.Close();
            }
            catch (Exception ex)
            {
                // сообщение о сбое
                NetworkClient.eventMessage?.Invoke(null, ex.Message);
            }
        }
    }
}
