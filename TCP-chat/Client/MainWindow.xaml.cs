using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Input;
using Microsoft.AspNetCore.Http.HttpResults;

namespace TCP_chat.Client
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static event EventHandler<string> enterMessage;
        public static event EventHandler enterFile;
        public static event EventHandler<string> eventSetDestClient;

        NetworkClient networkClient;

        public MainWindow()
        {
            InitializeComponent();

            NetworkClient.eventMessage += NetworkClient_eventMessage;
            NetworkClient.eventAddClient += NetworkClient_eventAddClient;
            NetworkClient.eventAddClients += NetworkClient_eventAddClients;

            SendFileBtn.Source = new BitmapImage(new Uri(Environment.CurrentDirectory + "\\img\\forFile.jpg"));
            SelectingClient.Items.Add("Всем");
            SelectingClient.SelectedIndex = 0;
        }

        private void NetworkClient_eventAddClients(object sender, List<string> e)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() =>
            {
                foreach (var user in e)
                    SelectingClient.Items.Add(user);
            }));
        }

        private void NetworkClient_eventAddClient(object sender, string e)
        {
            var newUser = e.Replace("вошел в чат", "").Trim();

            if (!SelectingClient.Items.Contains(newUser))
                this.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() =>
                {
                    SelectingClient.Items.Add(newUser);
                }));
        }

        private void NetworkClient_eventMessage(object sender, string e)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() =>
            {
                Messager.Items.Add(e.ToString());
            }));
        }

        // Обработчик события нажатия отправки сообщения
        private void SendMessage_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() =>
            {
                enterMessage?.Invoke(null, InputMessage.Text);

                Messager.Items.Add("Отправлено: " + InputMessage.Text);

                InputMessage.Text = string.Empty;
            }));
        }

        // Обработчик кнопки "Войти"
        private void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() =>
            {
                InputArea.Visibility = Visibility.Visible;

                SelectingClient.Visibility = Visibility.Visible;

                // Инициализация клиента
                networkClient = new NetworkClient(ClientName.Text);

                ClientName.IsReadOnly = true;

                Ok.Visibility = Visibility.Collapsed;
            }));

            // Запускаем ввод/вывод сообщений
            Task.Run(() => networkClient.StartClient());
        }

        private void SendFileBtn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            enterFile?.Invoke(null, null);
        }

        public string SetDestinationClient()
        {
            return SelectingClient.SelectedItem.ToString();
        }

        private void SelectingClient_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            eventSetDestClient?.Invoke(null, SelectingClient.SelectedItem.ToString());
        }
    }
}
