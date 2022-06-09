using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZoDream.HttpRequester.Models;
using ZoDream.HttpRequester.Utils;
using ZoDream.Shared.Storage;
using ZoDream.Shared.ViewModels;

namespace ZoDream.HttpRequester.ViewModels
{
    public class MainViewModel: BindableBase
    {

        public MainViewModel()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private string[] methodItems = new[] { "Get", "Post", "Put", "Delete", "Head", "Options", "Patch", "Trace" };

        public string[] MethodItems
        {
            get => methodItems;
            set => Set(ref methodItems, value);
        }

        private string[] headerNameItems = Enum.GetNames(typeof(HttpRequestHeader));

        public string[] HeaderNameItems
        {
            get => headerNameItems;
            set => Set(ref headerNameItems, value);
        }

        private string[] rawTypeItems = new[] { "text/plain", "application/json", "text/xml" };

        public string[] RawTypeItems
        {
            get => rawTypeItems;
            set => Set(ref rawTypeItems, value);
        }

        private string method = "Get";

        public string Method
        {
            get => method;
            set => Set(ref method, value);
        }

        private string url = string.Empty;

        public string Url
        {
            get => url;
            set => Set(ref url, value);
        }

        private ObservableCollection<DataItem> queries = new();

        public ObservableCollection<DataItem> Queries
        {
            get => queries;
            set => Set(ref queries, value);
        }

        private ObservableCollection<DataItem> headerItems = new();

        public ObservableCollection<DataItem> HeaderItems
        {
            get => headerItems;
            set => Set(ref headerItems, value);
        }

        private int bodyTab;

        public int BodyTab
        {
            get => bodyTab;
            set => Set(ref bodyTab, value);
        }

        private string rawBodyType = string.Empty;

        public string RawBodyType
        {
            get => rawBodyType;
            set => Set(ref rawBodyType, value);
        }

        private string rawBody = string.Empty;

        public string RawBody
        {
            get => rawBody;
            set => Set(ref rawBody, value);
        }

        private string hexBody = string.Empty;
        public string HexBody
        {
            get => hexBody;
            set => Set(ref hexBody, value);
        }

        private string fileBody = string.Empty;

        public string FileBody
        {
            get => fileBody;
            set => Set(ref fileBody, value);
        }



        private ObservableCollection<DataItem> formItems = new();

        public ObservableCollection<DataItem> FormItems
        {
            get => formItems;
            set => Set(ref formItems, value);
        }

        private ObservableCollection<FormItem> formDataItems = new();

        public ObservableCollection<FormItem> FormDataItems
        {
            get => formDataItems;
            set => Set(ref formDataItems, value);
        }

        private bool isLoading = false;

        public bool IsLoading
        {
            get => isLoading;
            set => Set(ref isLoading, value);
        }


        private ObservableCollection<DataItem> responseHeaders = new();

        public ObservableCollection<DataItem> ResponseHeaders
        {
            get => responseHeaders;
            set => Set(ref responseHeaders, value);
        }

        private ObservableCollection<DataItem> responseInfo = new();

        public ObservableCollection<DataItem> ResponseInfo
        {
            get => responseInfo;
            set => Set(ref responseInfo, value);
        }

        private CancellationTokenSource? TokenSource;
        private readonly Stopwatch Timer = new();
        private readonly Dictionary<string, string> ContentInfo = new();
        private readonly string HttpTempFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_save_temp");

        public void Cancel()
        {
            if (IsLoading)
            {
                TokenSource?.Cancel();
            }
            IsLoading = false;
        }

        public async Task ExecuteAsync()
        {
            if (IsLoading)
            {
                TokenSource?.Cancel();
            }
            IsLoading = true;
            responseInfo.Clear();
            responseHeaders.Clear();
            ContentInfo.Clear();
            TokenSource = new();
            var token = TokenSource.Token;
            using var client = new HttpClient();
            using var req = new HttpRequestMessage();
            req.RequestUri = GetUri();
            foreach (var item in HeaderItems)
            {
                req.Headers.TryAddWithoutValidation(item.Name, item.Value);
            }
            req.Method = Method.ToLower() switch
            {
                "post" => HttpMethod.Post,
                "put" => HttpMethod.Put,
                "delete" => HttpMethod.Delete,
                "head" => HttpMethod.Head,
                "options" => HttpMethod.Options,
                "patch" => HttpMethod.Patch,
                "trace" => HttpMethod.Trace,
                _ => HttpMethod.Get,
            };
            if (!IsNotBody(Method) && BodyTab > 0)
            {
                if (BodyTab == 1)
                {
                    req.Content = new StringContent(RawBody, Encoding.UTF8, RawBodyType);
                } else if (BodyTab == 2)
                {
                    req.Content = new ByteArrayContent(Hex.ToByte(HexBody));
                }
                else if (BodyTab == 3)
                {
                    req.Content = new FormUrlEncodedContent(FormItems.Where(i => !string.IsNullOrWhiteSpace(i.Name)).ToDictionary(item => item.Name, item => item.Value));
                }
                else if (BodyTab == 4)
                {
                    if (!string.IsNullOrEmpty(FileBody) && File.Exists(FileBody))
                    {
                        req.Content = new StreamContent(new FileStream(FileBody, FileMode.Open));
                    }
                }
                else if (BodyTab == 5)
                {
                    var form = new MultipartFormDataContent();
                    foreach (var item in FormDataItems)
                    {
                        if (string.IsNullOrWhiteSpace(item.Name))
                        {
                            continue;
                        }
                        if (item.DataType < 1)
                        {
                            form.Add(new StringContent(item.Value), item.Name);
                            continue;
                        }
                        if (!string.IsNullOrEmpty(item.Value) && File.Exists(item.Value))
                        {
                            req.Content = new StreamContent(new FileStream(item.Value, FileMode.Open));
                        }
                    }
                    req.Content = form;
                }
            }
            if (token.IsCancellationRequested)
            {
                IsLoading = false;
                return;
            }
            Timer.Restart();
            using var res = await client.SendAsync(req, token);
            Timer.Stop();
            if (token.IsCancellationRequested)
            {
                IsLoading = false;
                return;
            }
            ResponseInfo.Add(new DataItem("Execute Time", $"{Timer.ElapsedMilliseconds} ms"));
            if (res is null)
            {
                IsLoading = false;
                return;
            }
            ResponseInfo.Add(new DataItem("Status Code", $"{res.StatusCode}"));
            ResponseInfo.Add(new DataItem("Version", $"{res.Version}"));
            ResponseInfo.Add(new DataItem("Length", Disk.FormatSize(res.Content.Headers.ContentLength)));
            var items = res.Content.Headers.GetValues("Content-Type");
            foreach (var item in items)
            {
                if (item.Contains(';'))
                {
                    ContentInfo.Add("Type", item.Substring(0, item.IndexOf(';')));
                }
                var i = item.IndexOf("charset=");
                if (i < 0)
                {
                    ContentInfo.Add("Type", item);
                    continue;
                }
                ContentInfo.Add("Encoding", item[(i + 7)..]);
            }
            foreach (var item in res.Content.Headers)
            {
                responseHeaders.Add(new DataItem(item.Key, string.Join(';', item.Value)));
            }
            Stream input;
            if (res.Content.Headers.ContentEncoding.Contains("gzip"))
            {
                input = new GZipStream(await res.Content.ReadAsStreamAsync(), mode: CompressionMode.Decompress);
            } else
            {
                input = await res.Content.ReadAsStreamAsync();
            }
            using var output = new FileStream(HttpTempFileName, FileMode.Create);
            var bArr = new byte[2048];
            while (true)
            {
                var size = input.Read(bArr, 0, bArr.Length);
                output.Write(bArr, 0, size);
                if (token.IsCancellationRequested)
                {
                    input.Close();
                    return;
                }
                if (size <= 0)
                {
                    break;
                }
            }
            input.Close();
            ResponseInfo.Add(new DataItem("File Length", Disk.FormatSize(output.Length)));
            IsLoading = false;
        }


        public async Task<string> GetFormatHtmlAsync()
        {
            var contentType = ContentInfo.ContainsKey("Type") ? ContentInfo["Type"] : string.Empty;
            if (contentType.Contains("html"))
            {
                return await GetHtmlAsync();
            }
            if (contentType.Contains("json"))
            {
                return await GetJsonAsync();
            }
            if (contentType.Contains("image"))
            {
                return await GetImageAsync();
            }
            var page = await LocationStorage.ReadAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Assets/file.html"));
            return page.Replace("{{.}}", HttpTempFileName);
        }

        public async Task<string> GetRawAsync()
        {
            var contentType = ContentInfo.ContainsKey("Type") ? ContentInfo["Type"] : string.Empty;
            if (contentType.Contains("text") || contentType.Contains("html") || 
                contentType.Contains("json") || 
                contentType.Contains("javascript") ||
                contentType.Contains("css") ||
                contentType.Contains("style")
                )
            {
                return await LocationStorage.ReadAsync(HttpTempFileName);
            }
            return "";
        }

        private async Task<string> GetImageAsync()
        {
            var image = Convert.ToBase64String(File.ReadAllBytes(HttpTempFileName));
            var page = await LocationStorage.ReadAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                "Assets/image.html"));
            return page.Replace("{{.}}", image);
        }

        private async Task<string> GetHtmlAsync()
        {
            return await LocationStorage.ReadAsync(HttpTempFileName);
        }

        private async Task<string> GetJsonAsync()
        {
            var encoding = ContentInfo.ContainsKey("Encoding") ? 
                Encoding.GetEncoding(ContentInfo["Encoding"]) : Encoding.UTF8;
            var json = await GetTextAsync(encoding);
            var page = await LocationStorage.ReadAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/json.html"));
            return page.Replace("{{.}}", json);
        }

        private async Task<string> GetTextAsync(Encoding encoding)
        {
            if (!File.Exists(HttpTempFileName))
            {
                return string.Empty;
            }
            var fs = new FileStream(HttpTempFileName, FileMode.Open);
            using var reader = new StreamReader(fs, encoding);
            var content = await reader.ReadToEndAsync();
            return content;
        }

        private Uri GetUri()
        {
            var path = Url;
            if (Queries.Count == 0)
            {
                return new Uri(path);
            }
            var sb = new StringBuilder();
            foreach (var item in Queries)
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    continue;
                }
                if (sb.Length > 0)
                {
                    sb.Append('&');
                }
                sb.Append($"{WebUtility.UrlEncode(item.Name)}={WebUtility.UrlEncode(item.Value)}");
            }
            var query = sb.ToString();
            if (path.Contains('?'))
            {
                return new Uri(path + "&" + query);
            }
            return new Uri(path + "?" + query);
        }

        public static bool IsNotBody(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                return true;
            }
            method = method.ToLower();
            return method == "get" || method == "head" || method == "delete";
        }
    }


}
