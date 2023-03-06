using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using ZoDream.HttpRequester.ViewModels;
using ZoDream.Shared.Controls;

namespace ZoDream.HttpRequester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainViewModel ViewModel => (DataContext as MainViewModel)!;

        private void ExecuteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsLoading)
            {
                if (MessageBox.Show("Are you confirm cancel?", "Tip", MessageBoxButton.YesNo) 
                    == MessageBoxResult.No)
                {
                    return;
                }
                ViewModel.Cancel();
                return;
            }
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            await Browser.EnsureCoreWebView2Async();
            Browser.NavigateToString("");
            HexTb.Length = 0;
            RawBodyTb.Text = "";
            ViewModel.ClearResponseStream();
            try
            {
                await ViewModel.ExecuteAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                ViewModel.Cancel();
                return;
            }
            try
            {
                var page = await ViewModel.GetFormatHtmlAsync();
                var raw = await ViewModel.GetRawAsync();
                App.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        ViewModel.CreateResponseStream();
                        HexTb.Length = ViewModel.ResponseStream!.Length;
                        RawBodyTb.Text = raw;
                        Browser.NavigateToString(page);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        private void Browser_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            var coreWebView = Browser.CoreWebView2;
            // coreWebView.NewWindowRequested += CoreWebView2_NewWindowRequested;
            // coreWebView.DOMContentLoaded += CoreWebView_DOMContentLoaded;
        }


        private void HexView_ByteLoad(object sender, Shared.HexView.HexLoadEventArgs e)
        {
            var stream = ViewModel.ResponseStream;
            if (stream is null)
            {
                return;
            }
            stream.Seek(e.Position, System.IO.SeekOrigin.Begin);
            var buffer = new byte[e.Length];
            stream.Read(buffer, 0, e.Length);
            HexTb.Append(e, buffer);
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.SaveOptionAsync();
            ViewModel.Dispose();
        }


        private void AddToCookieBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddCookieToRequest();
            RequestTab.SelectedIndex = 2;
        }
    }
}
