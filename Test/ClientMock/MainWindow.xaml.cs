using EdcsClient.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EdcsClient.Model;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Threading;
using System.Collections.ObjectModel;
using EdcsClient.Model.Enum;

namespace EdcsClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker _worker = new BackgroundWorker();

        private ObservableCollection<Message> CurrentThread;
        public static User CurrentUser;
        private User Receiver;
        private static IEnumerable<User> Contacts;

        private readonly IRabbitService _rabbit;
        private readonly IDbService _context;
        private readonly IOptions<Settings> _config;
        public MainWindow(IRabbitService rabbit, IDbService dbService,
                IOptions<Settings> config)
        {
            _rabbit = rabbit;
            _context = dbService;
            _config = config;

            InitializeComponent();

            _worker.DoWork += ListenQueue;
            _worker.RunWorkerAsync();
        }
        public void Listen()
        {
            _rabbit.Listen();
        }
        public void InitializeUi()
        {
            Contacts = _context.GetUsers(CurrentUser.Id);
            contactsList.ItemsSource = Contacts;
        }
        private void ReloadThreadView()
        {
            ThreadHeader.Text = $"Wiadomość do {Receiver.Name}";
            messagesListView.ItemsSource = null;
            messagesListView.ItemsSource = CurrentThread;
        }
        private void PropagateMessage(Message msg)
        {
            // Korzystamy z tej metody żeby dodatkowo weryfikować,
            // czy użytkownik do którego chcemy wysłać wiadomość,
            // nie zablokował nas. 
            var result = _context.VerifyBan(msg.Sender, msg.Receiver);
            if (result == BanStatus.NONE)
            {
                _rabbit.LogMessage(msg);
                _rabbit.SendMessageToUser(msg);
                CurrentThread.Add(msg);
            }
            else if (result == BanStatus.OUT)
                MessageBox.Show("You cannot send a message to user you have blocked!", "Sending error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            else if (result == BanStatus.IN)
                CurrentThread.Add(msg);

        }
        private void ChangeThreadEvent(object sender, RoutedEventArgs e)
        {
            var user = (User)(sender as ListView).SelectedItem;
            CurrentThread = _context.GetThread(CurrentUser.Id, user.Id);
            Receiver = user;
            ReloadThreadView();
        }
        private void ListenQueue(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker wk = (BackgroundWorker)sender;
            while (!wk.CancellationPending)
            {
                var msg = _rabbit.GetLastMessage();
                if (msg != null)
                {
                    if (CurrentThread != null &&
                        CurrentThread.FirstOrDefault() != null)
                    {
                        var ct = CurrentThread.First();
                        if ((ct.Sender == msg.Sender && ct.Receiver == msg.Receiver) ||
                           (ct.Sender == msg.Receiver && ct.Receiver == msg.Sender))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                msg.CurrentUser = false;
                                CurrentThread.Add(msg);
                            });
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        }
        private void BanUserEvent(object sender, RoutedEventArgs e)
        {
            if (_context.UpdateBan(CurrentUser.Id, Receiver.Id))
            {
                MessageBox.Show("Ban status updates succesfully", "Ban success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("I was not able to update ban status", "Ban error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void SendMessageEvent(object sender, RoutedEventArgs e)
        {
            var msg = new Message
            {
                Content = messageBox.Text,
                Sender = CurrentUser.Id,
                Receiver = Receiver.Id,
                CurrentUser = true,
                Created = DateTime.Now,
                Modified = DateTime.Now
            };
            PropagateMessage(msg);
        }

    }
}
