using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Website.utlities.Helpers
{
    public class ScreenLink
    {
        private readonly TaskPageDI _taskPageDI;
        private BigInteger _KeyIndex = 0;
        private readonly Dictionary<string, BigInteger> links = [];
        public ScreenLink(TaskPageDI taskPageDI)
        {
            _taskPageDI = taskPageDI;
        }

        private async Task _AddToQ(IPage page, BigInteger level, ScreenLinkConfig screenLinkConfig)
        {
            var _links = await page.QuerySelectorAllAsync("a[href]");
            foreach (var _link in _links)
            {
                string? href = await _link.GetAttributeAsync("href");
                if (href != null && !links.TryGetValue(href, out BigInteger _))
                {
                    // Resolve to a fully qualified URL
                    var fullUrl = new Uri(new Uri(page.Url), href).AbsoluteUri;

                    if (Path.HasExtension(fullUrl) == false)
                    {
                        if (screenLinkConfig.AllowExternal == false)
                        {
                            if (fullUrl.StartsWith(screenLinkConfig.Domain))
                            {
                                _AddUrl(fullUrl, level + 1);

                            }
                        }
                        else
                        {
                            _AddUrl(fullUrl, level + 1);
                        }
                    }
                    //links.TryAdd(fullUrl, level + 1);

                }
            }
        }

        private void _AddUrl(string fullUrl, BigInteger level)
        {
            if (!links.TryGetValue(fullUrl, out BigInteger _))
            {
                links.TryAdd(fullUrl, level);
            }
        }

        private string _MergeUrl(string domain, string url)
        {
            string host = "https://";
            if (url.StartsWith(host))
            {
                return url;
            }
            if (domain.StartsWith(host))
            {
                return host + (domain + url)[host.Length..].Replace("//", "/");
            }
            return domain + url;
        }

        private async Task _Screen(IPage page, ScreenLinkConfig screenLinkConfig, string url, Action<Exception> logEx)
        {
            string _url = _MergeUrl(screenLinkConfig.Domain, url);
            string _file = screenLinkConfig.NestedFolder ? _NestedFile(screenLinkConfig.SnapshotFolder, _url) : _FlatFile(screenLinkConfig.SnapshotFolder, _url);
            if (links.TryGetValue(url, out BigInteger level))
            {
                if (level == _KeyIndex)
                {
                    if (Path.HasExtension(_url) == false)
                    {
                        if (screenLinkConfig.AllowExternal == false)
                        {
                            if (_url.StartsWith(screenLinkConfig.Domain))
                            {
                                await _ScreenUrl(page, screenLinkConfig, logEx, _url, _file, level);
                            }
                        }
                        else
                        {
                            await _ScreenUrl(page, screenLinkConfig, logEx, _url, _file, level);
                        }
                    }
                }
            }
        }

        private async Task _ScreenUrl(IPage page, ScreenLinkConfig screenLinkConfig, Action<Exception> logEx, string _url, string _file, BigInteger level)
        {
            try
            {
                Console.WriteLine($"Url: {_url}");
                await page.GotoAsync(_url);
                bool attempt = false;
                try
                {
                    while (!attempt)
                    {
                        await page.WaitForLoadStateAsync(LoadState.Load);
                        attempt = true;
                    }
                }
                catch(Exception ex)
                {
                    await Task.Delay(500);
                    //try
                    //{
                    //    await page.WaitForLoadStateAsync(
                    //    LoadState.NetworkIdle,
                    //    new PageWaitForLoadStateOptions { Timeout = screenLinkConfig.Timeout * 1000 });
                    //}
                    //catch (Exception)
                    //{
                    //    await page.WaitForLoadStateAsync(LoadState.Load);
                    //}
                }
                //await page.PdfAsync(new PagePdfOptions
                //{
                //    Path = $"{_file}.pdf",
                //    Format = "A4"
                //});

                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    FullPage = _url.Contains('#') == false,
                    Quality = 70,
                    //OmitBackground = true,
                    Type = ScreenshotType.Jpeg,
                    Path = $"{_file}.jpg"
                });
                await _AddToQ(page, level, screenLinkConfig);
            }
            catch (Exception ex)
            {
                logEx(ex);
                //throw;
            }
        }

        private string _FlatFile(string folder, string url)
        {
            string safePath = string.Join("_", url.Split(Path.GetInvalidFileNameChars()));

            // Optionally, replace URL-specific characters
            safePath = safePath.Replace("://", "_")
                               .Replace("/", "_")
                               .Replace("?", "_")
                               .Replace("&", "_");
            string _file = Path.Combine(folder, safePath);
            return _file;
        }

        private string _NestedFile(string folder, string url)
        {
            var uri = new Uri(url);

            // Root folder = domain
            string root = uri.Host; // "playwright.dev"

            // Path segments
            string[] segments = uri.AbsolutePath
                                   .Trim('/')
                                   .Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Sanitize each segment
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i] = Sanitize(segments[i]);
            }

            // Handle query string
            string? queryPart = string.IsNullOrEmpty(uri.Query) ? null : Sanitize(uri.Query.TrimStart('?'));

            // Handle fragment
            string? fragmentPart = string.IsNullOrEmpty(uri.Fragment) ? null : Sanitize(uri.Fragment.TrimStart('#'));

            // Build nested path
            string fullPath = Path.Combine(root, Path.Combine(segments));

            if (!string.IsNullOrEmpty(queryPart))
            {
                fullPath = Path.Combine(fullPath, $"query_{queryPart}");
            }

            if (!string.IsNullOrEmpty(fragmentPart))
            {
                fullPath = Path.Combine(fullPath, $"fragment_{fragmentPart}");
            }
            //return fullPath;
            string _file = Path.Combine(folder, fullPath);
            return _file;
            
        }

        private string Sanitize(string input)
        {
            return string.Join("_", input.Split(Path.GetInvalidFileNameChars()));
        }

        public async Task ExecuteAsync(ScreenLinkConfig screenLinkConfig, Action<Exception> logEx)
        {
            async Task load(IPage page)
            {
                links.Add("/", 0);

                while (screenLinkConfig.NestedLevel.HasValue == false || (screenLinkConfig.NestedLevel.HasValue && screenLinkConfig.NestedLevel.Value >= _KeyIndex))
                {
                    var urls = links.Where(li => li.Value == _KeyIndex).Select(ki => ki.Key).ToList();

                    Console.WriteLine($"Screens {urls.Count} at Level {_KeyIndex}");

                    if (urls.Count == 0)
                    {
                        break;
                    }
                    else
                    {
                        for (int indx = 0; indx < urls.Count; indx++)
                        {
                            string? url = urls[indx];
                            await _Screen(page, screenLinkConfig, url, logEx);
                        }
                    }
                    _KeyIndex++;
                }
            }

            await _taskPageDI.Execute(load);
        }
    }
}
