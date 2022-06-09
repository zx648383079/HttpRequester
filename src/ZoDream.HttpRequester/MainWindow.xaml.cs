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
            DataContext = ViewModel;
        }

        public MainViewModel ViewModel = new();

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
            var page = await ViewModel.GetFormatHtmlAsync();
            var raw = await ViewModel.GetRawAsync();
            App.Current.Dispatcher.Invoke(() =>
            {
                ViewModel.CreateResponseStream();
                HexTb.Length = ViewModel.ResponseStream!.Length;
                RawBodyTb.Text = raw;
                Browser.NavigateToString(page);
            });
        }

        private void Browser_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            var coreWebView = Browser.CoreWebView2;
            // coreWebView.NewWindowRequested += CoreWebView2_NewWindowRequested;
            // coreWebView.DOMContentLoaded += CoreWebView_DOMContentLoaded;
        }

        private void HeaderTb_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            ViewModel.HeaderItems.Add(new Models.DataItem());
        }

        private void QueriesTb_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            ViewModel.Queries.Add(new Models.DataItem());
        }

        private void FormDataTb_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            ViewModel.FormDataItems.Add(new Models.FormItem());
        }

        private void FormTb_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            ViewModel.FormItems.Add(new Models.DataItem());
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
            _ =ViewModel.SaveOptionAsync();
            ViewModel.Dispose();
        }

        private void IconButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveHistory((sender as Button).DataContext as string);
        }
    }
}
