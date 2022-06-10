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
    public class MainViewModel: BindableBase, IDisposable
    {

        public MainViewModel()
        {
            ClearTemp();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            RawBodyType = RawTypeItems[1];
            Method = MethodItems[0];
            _ = LoadOptionAsync();
        }

        public AppOption Option = new();

        private ObservableCollection<string> urlHistories = new();

        public ObservableCollection<string> UrlHistories
        {
            get => urlHistories;
            set => Set(ref urlHistories, value);
        }

        private string[] methodItems = new[] { "Get", "Post", "Put", "Delete", "Head", "Options", "Patch", "Trace" };

        public string[] MethodItems
        {
            get => methodItems;
            set => Set(ref methodItems, value);
        }

        private string proxyAddress = string.Empty;

        public string ProxyAddress
        {
            get => proxyAddress;
            set => Set(ref proxyAddress, value);
        }

        private string proxyUserName = string.Empty;

        public string ProxyUserName
        {
            get => proxyUserName;
            set => Set(ref proxyUserName, value);
        }

        private string proxyPassword = string.Empty;

        public string ProxyPassword
        {
            get => proxyPassword;
            set => Set(ref proxyPassword, value);
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

        private string[] formTypeItems = new[] {"Text", "File", "Json", "Xml"};

        public string[] FormTypeItems
        {
            get => formTypeItems;
            set => Set(ref formTypeItems, value);
        }

        private string method = string.Empty;

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

        private ObservableCollection<DataItem> responseCookies = new();

        public ObservableCollection<DataItem> ResponseCookies
        {
            get => responseCookies;
            set => Set(ref responseCookies, value);
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
            ResponseInfo.Clear();
            ResponseHeaders.Clear();
            ContentInfo.Clear();
            ResponseCookies.Clear();
            TokenSource = new();
            AddHistory(Url);
            var token = TokenSource.Token;
            var requstUrl = GetUri();
            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true
            };
            var cookie = HeaderItems.Where(i => i.Name.ToLower() == "cookie").FirstOrDefault();
            if (cookie != null)
            {
                foreach (var item in cookie.Value.Split(';'))
                {
                    handler.CookieContainer.SetCookies(requstUrl, item);
                    AddCookie(item);
                }
            }
            if (!string.IsNullOrWhiteSpace(ProxyAddress))
            {
                handler.Proxy = new WebProxy()
                {
                    Address = new Uri(ProxyAddress),
                    UseDefaultCredentials = string.IsNullOrWhiteSpace(ProxyUserName),
                    Credentials = string.IsNullOrWhiteSpace(ProxyUserName) ? null : new NetworkCredential(
                   userName: ProxyUserName,
                   password: ProxyPassword)
                };
            }
            using var client = new HttpClient(handler);
            using var req = new HttpRequestMessage();
            req.RequestUri = requstUrl;
            var url = requstUrl.ToString();
            ResponseInfo.Add(new DataItem("URL", url));
            ContentInfo.Add("URL", url);
            foreach (var item in HeaderItems)
            {
                if (item.Name.ToLower() == "cookie")
                {
                    continue;
                }
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
            var length = res.Content.Headers.ContentLength;
            ResponseInfo.Add(new DataItem("Status Code", $"{res.StatusCode}"));
            ResponseInfo.Add(new DataItem("Version", $"{res.Version}"));
            ResponseInfo.Add(new DataItem("Length", Disk.FormatSize(length)));
            var items = res.Content.Headers.GetValues("Content-Type");
            foreach (var item in items)
            {
                if (item.Contains(';'))
                {
                    ContentInfo.Add("Type", item[..item.IndexOf(';')]);
                }
                var i = item.IndexOf("charset=");
                if (i < 0)
                {
                    ContentInfo.Add("Type", item);
                    continue;
                }
                ContentInfo.Add("Encoding", item[(i + 8)..]);
            }
            foreach (var item in res.Headers)
            {
                foreach (var val in item.Value)
                {
                    if (item.Key.ToLower() == "set-cookie")
                    {
                        AddCookie(val);
                    }
                    responseHeaders.Add(new DataItem(item.Key, val));
                }
            }
            if (length != null &&  length.ToString()!.Length > 10)
            {
                IsLoading = false;
                return;
            }
            Stream input;
            if (res.Content.Headers.ContentEncoding.Contains("gzip"))
            {
                input = new GZipStream(await res.Content.ReadAsStreamAsync(), mode: CompressionMode.Decompress);
            } else
            {
                input = await res.Content.ReadAsStreamAsync();
            }
            ClearResponseStream();
            using var output = new FileStream(HttpTempFileName, FileMode.Create);
            var preBuffer = 2048;
            var bArr = new byte[preBuffer];
            while (true)
            {
                var size = input.Read(bArr, 0, bArr.Length);
                output.Write(bArr, 0, size);
                if (token.IsCancellationRequested)
                {
                    input.Close();
                    IsLoading = false;
                    return;
                }
                if (output.Length.ToString().Length > 10)
                {
                    input.Close();
                    IsLoading = false;
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

        private void AddCookie(string line)
        {
            var args = line.Split(';');
            var expires = string.Empty;
            for (var i = args.Length - 1; i > 0; i--)
            {
                var items = args[i].Split(new char[] { '=' }, 2);
                if (items[0].Trim().ToLower() == "expires")
                {
                    expires = items[1].Trim();
                }
            }
            args = args[0].Split(new char[] { '=' }, 2);
            if (args.Length < 2)
            {
                return;
            }
            var key = args[0].Trim();
            if (string.IsNullOrWhiteSpace(expires) || DateTime.Parse(expires) > DateTime.Now)
            {
                ResponseCookies.Add(new DataItem(key, args[1]));
                return;
            }
            for (var i = ResponseCookies.Count - 1; i >= 0; i--)
            {
                if (ResponseCookies[i].Name == key)
                {
                    ResponseCookies.RemoveAt(i);
                }
            }

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
            var page = await LocationStorage.ReadAsync(HttpTempFileName);
            var url = ContentInfo["URL"];
            var i = url.IndexOf('?');
            if (i > 0)
            {
                url = url[..i];
            }
            return $"<base href=\"{url}\">{page}";
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

        public FileStream? ResponseStream { get; private set; }
        public void ClearResponseStream()
        {
            if (ResponseStream is null)
            {
                return;
            }
            ResponseStream.Close();
            ResponseStream = null;
        }

        public void CreateResponseStream()
        {
            ClearResponseStream();
            ResponseStream = new FileStream(HttpTempFileName, FileMode.Open);
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

        public async Task<AppOption> LoadOptionAsync()
        {
            var option = await AppData.LoadAsync<AppOption>();
            if (option != null)
            {
                Option = option;
                Method = option.Method;
                Url = option.Url;
                foreach (var item in option.Histories)
                {
                    UrlHistories.Add(item);
                }
            }
            return Option;
        }

        public async Task SaveOptionAsync()
        {
            Option.Url = Url;
            Option.Method = Method;
            Option.Histories = UrlHistories.ToList();
            await AppData.SaveAsync(Option);
        }

        public void AddHistory(string url)
        {
            if (UrlHistories.Contains(url))
            {
                UrlHistories.Move(UrlHistories.IndexOf(url), 0);
                _ = SaveOptionAsync();
                return;
            }
            UrlHistories.Insert(0, url);
            for (var i = UrlHistories.Count - 1; i > 10; i--)
            {
                UrlHistories.RemoveAt(i);
            }
            _ = SaveOptionAsync();
        }

        public void RemoveHistory(string url)
        {
            UrlHistories.Remove(url);
            _ = SaveOptionAsync();
        }

        private void ClearTemp()
        {
            if (File.Exists(HttpTempFileName))
            {
                File.Delete(HttpTempFileName);
            }
        }

        public void Dispose()
        {
            Cancel();
            ClearResponseStream();
            ClearTemp();
        }
    }


}
