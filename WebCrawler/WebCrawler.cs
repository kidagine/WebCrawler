using System;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.IO;

namespace WebCrawler
{
	class WebCrawler
	{
		private readonly Regex _urlTagPattern = new Regex(@"<a.*?href=[""'](?<url>.*?)[""'].*?>(?<name>.*?)</a>", RegexOptions.IgnoreCase);
		private readonly Regex _hrefPattern = new Regex("href\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", RegexOptions.IgnoreCase);
		private static readonly ConcurrentDictionary<Uri, bool> _visitedUrls = new ConcurrentDictionary<Uri, bool>();
		private  readonly Dictionary<Uri, string[]> _robotsTxtDisallowed = new Dictionary<Uri, string[]>();
		private readonly Queue<Uri> _test = new Queue<Uri>();
		private static readonly BlockingCollection<Uri> _resultedUrls = new BlockingCollection<Uri>(new ConcurrentQueue<Uri>());
		private static readonly BlockingCollection<KeyValuePair<Uri, int>> _frontier = new BlockingCollection<KeyValuePair<Uri, int>>(new ConcurrentQueue<KeyValuePair<Uri, int>>());
		private static Thread thread;
		private static string _searchWord;
		private static int _maximumLevel;
		private int _currentLinkAmount;
		private bool _hasFinishedCrawling;

		public Queue<Uri> Urls { get; private set; }
		public string SearchWord { get; set; }
		public int MaximumLevel { get; set; }


		public WebCrawler()
		{
			thread = new Thread(new ThreadStart(Crawl));
			thread.Start();
		}

		public WebCrawler(string searchWord, Queue<Uri> urls, int maximumLevel): this()
		{
			_searchWord = searchWord;
			_maximumLevel = maximumLevel;
			foreach (Uri url in urls)
			{
				_frontier.Add(new KeyValuePair<Uri, int>(url, 0));
			}
		}

		private void Crawl()
		{
			_hasFinishedCrawling = false;
			while (!_hasFinishedCrawling)
			{
				try
				{
					KeyValuePair<Uri, int> keyValue = _frontier.Take();
					Uri page = keyValue.Key;
					int level = keyValue.Value;
					ResolvePage(page, level);
				}
				catch (ThreadInterruptedException){ }
			}
		}

		private void ResolvePage(Uri page, int level)
		{
			if (!_visitedUrls.ContainsKey(page))
			{
				_visitedUrls[page] = true;
				try
				{
					WebClient webClient = new WebClient();
					webClient.Headers.Add(HttpRequestHeader.UserAgent, "User-agent");
					string webPage = webClient.DownloadString(page.ToString());
					if (webPage.ToLower().Contains(_searchWord.ToLower()))
					{
						_resultedUrls.Add(page);
						if (level < _maximumLevel)
						{
							MatchCollection urls = _urlTagPattern.Matches(webPage);
							Uri uriBuilder = new UriBuilder(page.Host).Uri;
							foreach (Match url in urls)
							{
								try
								{
									string newUrl = url.Groups["url"].Value;
									Uri.TryCreate(uriBuilder, newUrl, out Uri result);

									if (result.HostNameType == UriHostNameType.Dns && !_visitedUrls.ContainsKey(result))
									{
										_frontier.Add(new KeyValuePair<Uri, int>(result, level + 1));
									}
								}
								catch { }
							}
						}
					}
					webClient.Dispose();
				}
				catch (Exception)
				{
					_visitedUrls[page] = false;
				}
			}
		}

		public void Stop()
		{
			_hasFinishedCrawling = true;
			if (thread.ThreadState == ThreadState.WaitSleepJoin)
			{
				thread.Interrupt();
			}
		}

		public void Start(string userUrl, int maximumLinkAmount)
		{
			UriBuilder uriBuilder = new UriBuilder(userUrl);
			Uri hostUrl = new UriBuilder(uriBuilder.Host).Uri;

			Crawl(uriBuilder.Uri, hostUrl, maximumLinkAmount);
		}

		private void Crawl(Uri userUrl, Uri hostUrl, int maximumLinkAmount)
		{
			try
			{
				WebClient webClient = new WebClient();
				string webPage = webClient.DownloadString(userUrl.ToString());
				_visitedUrls[userUrl] = true;

				MatchCollection urls = _urlTagPattern.Matches(webPage);
				foreach (Match url in urls)
				{
					_currentLinkAmount++;
					if (maximumLinkAmount >= _currentLinkAmount)
					{
						string newUrl = _hrefPattern.Match(url.Value).Groups[1].Value;
						Uri absoluteUrl = Normalize(hostUrl, newUrl);
						if (absoluteUrl != null && IsUrlDisallowed(hostUrl, absoluteUrl.AbsolutePath))
						{
							_frontier.Add(new KeyValuePair<Uri, int>(absoluteUrl, _currentLinkAmount));
							_resultedUrls.Add(absoluteUrl);
						}
						_currentLinkAmount++;
					}
				}
				webClient.Dispose();
			}
			catch
			{
				_visitedUrls[userUrl] = false;
			}
		}

		private bool IsUrlDisallowed(Uri hostUrl, string absolutePath)
		{
			bool isUrlAllowed;
			if (!_robotsTxtDisallowed.ContainsKey(hostUrl))
			{
				string hostRobotTxt = new WebClient().DownloadString(hostUrl.ToString() + "robots.txt");
				if (hostRobotTxt.Contains("Disallow"))
				{
					string[] disallowedLines = GetHostRobotTxtDisallowed(hostRobotTxt);
					_robotsTxtDisallowed.Add(hostUrl, disallowedLines);
					isUrlAllowed = CheckIfUrlAllowed(hostUrl, absolutePath);
				}
				else
				{
					isUrlAllowed = true;
				}
			}
			else
			{
				isUrlAllowed = CheckIfUrlAllowed(hostUrl, absolutePath);
			}
			return isUrlAllowed;
		}

		private string[] GetHostRobotTxtDisallowed(string hostRobotTxt)
		{
			string[] disallowedLines = new string[500];
			string[] allLines = hostRobotTxt.Split(new[] { "\r\n", "\r", "\n" },StringSplitOptions.None);
			int currentDisallowedIndex = 0;
			for (int i = 0; i < allLines.Length; i++)
			{
				if (allLines[i].StartsWith("Disallow"))
				{
					disallowedLines[currentDisallowedIndex] = allLines[i].Substring(9).Trim();
					currentDisallowedIndex++;
				}
			}
			return disallowedLines;
		}

		private bool CheckIfUrlAllowed(Uri hostUrl, string absolutePath)
		{
			if (_robotsTxtDisallowed.ContainsKey(hostUrl))
			{
				string[] disallowedLines = _robotsTxtDisallowed[hostUrl];
				for (int i = 0; i < disallowedLines.Length; i++)
				{
					if (disallowedLines[i] != null)
					{
						Console.WriteLine(disallowedLines[i]);
						if (disallowedLines[i].Equals(absolutePath))
						{
							Console.WriteLine("NOT ALLOWED");
							return false;
						}
					}
				}
			}
			return true;
		}

		private Uri Normalize(Uri hostUrl, string stringUrl)
		{
			Uri normalizedUrl = Uri.TryCreate(hostUrl, stringUrl, out Uri absoluteUrl) ? absoluteUrl : null;
			if (normalizedUrl.Scheme == Uri.UriSchemeHttps)
			{
				return normalizedUrl;
			}
			else
			{
				return null;
			}
		}

		public int GetFrontierSize()
		{
			return _frontier.Count;
		}

		public int GetTest()
		{
			return _test.Count;
		}

		public Dictionary<Uri, bool> GetVisitedUrls()
		{
			return new Dictionary<Uri, bool>(_visitedUrls);
		}

		public Queue<Uri> GetResultUrls()
		{
			return new Queue<Uri>(_resultedUrls);
		}
	}
}
