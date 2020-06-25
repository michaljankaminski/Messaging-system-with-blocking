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

namespace EdcsClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IRabbitService _rabbit;
        public MainWindow(IRabbitService rabbit)
        { 
            InitializeComponent();
            _rabbit = rabbit;
            
        }

        private void SendMessageEvent(object sender, RoutedEventArgs e)
        {

            var msg = new Message {
                Content = "Testowa wiadomość",
                Sender = 1,
                Receiver = 2
            };
            _rabbit.LogMessage(msg);

        }
    }
}
