using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Lession1
{
    public partial class Welcome : Page
    {
        public Welcome()
        {
            InitializeComponent();
        }

        private void Go(object sender, RoutedEventArgs e)
        {
            Uri uri = new Uri("NumericOperatorTest.xaml", UriKind.Relative);
            NavigationService.Navigate(uri);
        }
    }
}