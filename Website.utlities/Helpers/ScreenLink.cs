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

        private async Task _AddToQ(IPage page, BigInteger level)
        {
            var _links = await page.QuerySelectorAllAsync("a[href]");
            foreach (var _link in _links)
            {
                string? href = await _link.GetAttributeAsync("href");
                if (href != null && !links.TryGetValue(href, out BigInteger _))
                {
                    // Resolve to a fully qualified URL
                    var fullUrl = new Uri(new Uri(page.Url), href).AbsoluteUri;
                    links.TryAdd(fullUrl, level + 1);
                }
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
            string _file = screenLinkConfig.NestedFolder? _NestedFile(screenLinkConfig.SnapshotFolder, url): _FlatFile(screenLinkConfig.SnapshotFolder, url);
            if (links.TryGetValue(url, out BigInteger level))
            {
                if (level == _KeyIndex)
                {
                    if (Path.HasExtension(_url) == false && _url.StartsWith(screenLinkConfig.Domain) == !screenLinkConfig.AllowExternal)
                    {
                        try
                        {
                            await page.GotoAsync(_url);
                            await page.ScreenshotAsync(new PageScreenshotOptions
                            {
                                FullPage = true,
                                Path = $"{_file}.png"
                            });
                            await _AddToQ(page, level);
                        }
                        catch (Exception ex)
                        {
                            logEx(ex);
                            //throw;
                        }
                    }
                }
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
            return fullPath;
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
