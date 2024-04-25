using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using TCP_chat.Client;

namespace TCP_chat.Client
{
    internal class NetworkClient
    {
        public static event EventHandler<string> eventMessage;
        public static event EventHandler<string> eventAddClient;
        public static event EventHandler<List<string>> eventAddClients;

        private readonly object syncFileObj = new object();
        private AutoResetEvent resetEventMessage;
        private AutoResetEvent resetEventFile;

        private string host;
        private int port;
        private TcpClient client;
        private string userName;
        private string destUserName;
        private string message;
        private FileManager fileManager;

        public NetworkClient(string name)
        {
            host = "127.0.0.1";
            port = 8888;
            client = new TcpClient();
            userName = name;
            message = "";
            fileManager = new FileManager();

            resetEventMessage = new AutoResetEvent(false);
            resetEventFile = new AutoResetEvent(false);
            MainWindow.enterMessage += MainWindow_enterMessage;
            MainWindow.enterFile += MainWindow_enterFile;
            MainWindow.eventSetDestClient += MainWindow_eventSetDestClient;
        }

        // выбор адресата сообщений
        private void MainWindow_eventSetDestClient(object sender, string e)
        {
            destUserName = e;
        }

        // нажатие отправки файла
        private void MainWindow_enterFile(object sender, EventArgs e)
        {
            resetEventFile.Set();
        }

        // нажатие отправки сообщения
        private void MainWindow_enterMessage(object sender, string e)
        {
            message = e.ToString();
            resetEventMessage.Set();
        }

        public async Task StartClient()
        {
            StreamReader Reader = null;
            StreamWriter Writer = null;
            NetworkStream stream = null;

            try
            {
                client.Connect(host, port); //подключение клиента
                stream = client.GetStream();
                Reader = new StreamReader(stream, Encoding.Default);
                Writer = new StreamWriter(stream, Encoding.Default);
                if (Writer is null || Reader is null) return;

                // запускаем новый поток для получения данных
                _ = Task.Run(() => ReceiveMessageAsync(Reader, stream));

                // запускаем поток отправки файлов
                _ = Task.Run(() => SendFileAsync(stream));

                // запускаем ввод сообщений
                await SendMessageAsync(Writer);
            }
            catch (Exception ex)
            {
                // сообщение о сбое
                eventMessage?.Invoke(null, ex.Message);
            }
        }


        // отправка сообщений
        async Task SendMessageAsync(StreamWriter writer)
        {
            // сначала отправляем имя
            await writer.WriteLineAsync(userName);
            await writer.FlushAsync();

            while (true)
            {
                // ждем отправки
                resetEventMessage.WaitOne();

                if (message != "")
                {
                    await writer.WriteLineAsync(destUserName + "&&&" + message);
                    await writer.FlushAsync();
                }
            }
        }

        // получение сообщений и файлов
        async Task ReceiveMessageAsync(StreamReader reader, NetworkStream stream)
        {
            while (true)
            {
                try
                {
                    // считываем ответ в виде строки
                    string message = await reader.ReadLineAsync();

                    // новый контакт вошел в чат
                    if (message.Contains("вошел в чат"))
                        eventAddClient?.Invoke(null, message);

                    // если пустой ответ, ничего не выводим на консоль
                    if (string.IsNullOrEmpty(message)) continue;
                    // если пришел файл
                    if (message.Contains("Content-length:") && message.Contains("Filename:"))
                        await fileManager.ReceiveFileAsync(stream, message);
                    else
                    {   // прием контактов в список контактов
                        if (message.Contains("ListUsersAfterIncoming:"))
                        {
                            var listUsers = message.Split(new string[] { "/" }, StringSplitOptions.None).ToList().Where(x => x != "ListUsersAfterIncoming:").ToList();

                            eventAddClients?.Invoke(null, listUsers);
                        }
                        // сообщение 
                        else eventMessage?.Invoke(null, message);
                    }
                }
                catch (Exception e)
                {
                    // сообщение о сбое
                    eventMessage?.Invoke(null, e.Message);
                    break;
                }
            }
        }


        // отправка файлов
        async Task SendFileAsync(NetworkStream stream)
        {
            while (true)
            {
                // ждем отправки
                resetEventFile.WaitOne();

                await fileManager.SendFileAsync(stream, userName, destUserName);
            }
        }
    }
}
