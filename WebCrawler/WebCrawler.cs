using System;
using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WebCrawler.UI;

namespace WebCrawler
{
	internal class WebCrawler
	{
		private readonly Regex _urlTagPattern = new Regex(@"<a.*?href=[""'](?<url>.*?)[""'].*?>(?<name>.*?)</a>", RegexOptions.IgnoreCase);
		private readonly Regex _hrefPattern = new Regex("href\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", RegexOptions.IgnoreCase);
		private readonly Queue<Uri> _frontier = new Queue<Uri>();
		private Dictionary<string, bool> _visitedUrls = new Dictionary<string, bool>();


		public void Start(string userUrl, int maximumLinkAmount)
		{
			UriBuilder uriBuilder = new UriBuilder(userUrl);
			Uri hostUrl = new UriBuilder(uriBuilder.Host).Uri;

			Crawl(uriBuilder.Uri, hostUrl);
		}

		private void Crawl(Uri userUrl, Uri hostUrl) 
		{
			UserInput userInput = new UserInput();
			try
			{
				WebClient webClient = new WebClient();
				string webPage = webClient.DownloadString(userUrl.ToString());
				_visitedUrls.Add(userUrl.ToString(), true);
				userInput.Write($"---Found web page: {hostUrl}---");

				MatchCollection urls = _urlTagPattern.Matches(webPage);
				foreach (Match url in urls)
				{
					string newUrl = _hrefPattern.Match(url.Value).Groups[1].Value;
					Uri absoluteUrl = Normalize(hostUrl, newUrl);
					if (absoluteUrl != null)
					{
						_frontier.Enqueue(absoluteUrl);
						userInput.Write($"Found: {absoluteUrl}");
					}
				}
				userInput.Write($"*Total links: {_frontier.Count}");
			}
			catch
			{
				userInput.Write($"---Did not find: {hostUrl}---");
				_visitedUrls.Add(userUrl.ToString(), false);
			}
		}

		private Uri Normalize(Uri hostUrl, string stringUrl)
		{
			Uri normalizedUrl = Uri.TryCreate(hostUrl, stringUrl, out Uri absoluteUrl) ? absoluteUrl : null;
			if (normalizedUrl.Scheme == Uri.UriSchemeHttps)
				return normalizedUrl;
			else
				return null;
		}
	}
}