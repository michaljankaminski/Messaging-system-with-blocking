using EdcsClient.Helper;
using EdcsClient.Model;
using EdcsClient.Service;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EdcsClient
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public static User CurrentUser;
        public static bool IsAuthenticated = false;
        private readonly IAuthenticationHelper _auth;

        public Login(IAuthenticationHelper auth)
        {
            _auth = auth;
            InitializeComponent();
        }
        private void LoginEvent(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(userLogin.Text) && !String.IsNullOrEmpty(userPassword.Text))
            {
                var user = new User {
                    Name = userLogin.Text,
                    Password = userPassword.Text
                };

                if (_auth.Authenticate(ref user) == true)
                {
                    IsAuthenticated = true;
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    IsAuthenticated = false;
                    MessageBox.Show("You have provided bad credentials", "Authentication error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Provide credentials", "Credentials missing",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        private void ClearFieldEvent(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = string.Empty;
            tb.GotFocus -= ClearFieldEvent;
        }
    }
}
