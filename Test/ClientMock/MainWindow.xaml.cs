using EdcsClient.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using EdcsClient.Model;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Threading;
using System.Collections.ObjectModel;

namespace EdcsClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker _worker = new BackgroundWorker();

        private static ObservableCollection<Message> CurrentThread;
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
            InitializeUser();
            InitializeUi();

            _worker.DoWork += ListenQueue;
            _worker.RunWorkerAsync();
        }
        private void InitializeUi()
        {
            Contacts = _context.GetUsers(Login.CurrentUser.Id);
            contactsList.ItemsSource = Contacts;
        }
        private void InitializeUser()
        {
            Login.CurrentUser = new User
            {
                Id = _config.Value.User.Id,
                Name = _config.Value.User.Name,
                IsActive = true,
                Password = _config.Value.User.Password
            };
        }
        private void ReloadThreadView()
        {
            messagesListView.ItemsSource = null;
            messagesListView.ItemsSource = CurrentThread;
        }
        private void ChangeThreadEvent(object sender, RoutedEventArgs e)
        {
            var user = (User)(sender as ListView).SelectedItem;
            CurrentThread = _context.GetThread(Login.CurrentUser.Id, user.Id);
            ReloadThreadView();
        }
        private void ListenQueue(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker wk = (BackgroundWorker)sender;
            while(!wk.CancellationPending)
            {
                var msg = _rabbit.GetLastMessage();
                if (msg != null)
                {
                    if(CurrentThread != null && 
                        CurrentThread.FirstOrDefault() != null)
                    {
                        var ct = CurrentThread.First();
                        if((ct.Sender == msg.Sender && ct.Receiver == msg.Receiver) ||
                           (ct.Sender == msg.Receiver && ct.Receiver == msg.Sender))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                CurrentThread.Add(msg);
                            });
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        }
        private void SendMessageEvent(object sender, RoutedEventArgs e)
        {
            var msg = new Message
            {
                Content = messageBox.Text,
                Sender = Login.CurrentUser.Id,
                Receiver = 2,
                CurrentUser = true,
                Created = DateTime.Now,
                Modified = DateTime.Now
            };
            _rabbit.LogMessage(msg);
            CurrentThread.Add(msg);
        }
    }
}
