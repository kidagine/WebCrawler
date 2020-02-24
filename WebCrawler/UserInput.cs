using System;
using System.Collections.Generic;

namespace WebCrawler
{
	internal class UserInput
	{
		private readonly string _userUrl = "https://www.elgiganten.dk";


		public void Initialize()
		{
			while (true)
			{
				Console.WriteLine("--------------------------------");
				Console.WriteLine("1. Find google search result");
				Console.WriteLine("2. Find url");
				Console.WriteLine("--------------------------------");
				Console.Write("Enter option number: ");
				bool hasChosenOption = default;
				do
				{
					bool isNumber = int.TryParse(Console.ReadLine(), out int outOption);
					if (isNumber && outOption >= 1 && outOption <= 2)
					{
						Console.WriteLine();
						hasChosenOption = true;
						if (outOption == 1)
						{
							FindSearch();
						}
						else
						{
							FindUrl();
						}
					}
					else if (!isNumber)
					{
						Console.WriteLine("The value you inserted was not a number.");
					}
					else
					{
						Console.WriteLine("The value you inserted was not an option.");
					}
				} while (!hasChosenOption);
			}
		}

		private void FindSearch()
		{
			Console.Write("Enter a google search: ");
			string searchWord = Console.ReadLine().Trim();
			WebCrawlerManager webCrawlerManager = new WebCrawlerManager();
			Queue<Uri> initialQueue = webCrawlerManager.InitialSearch(searchWord);
			Console.WriteLine($"Initial search size: {initialQueue.Count}");

			List<WebCrawler> crawlers = webCrawlerManager.CreateCrawlers(searchWord, initialQueue, 10);
			Console.WriteLine();
			Console.WriteLine("Crawling...");

			bool isDoneCrawling = default;
			while (!isDoneCrawling)
			{
				Console.WriteLine();
				Console.WriteLine("Press any key to continue crawling. Press ESC to stop");
				if (Console.ReadKey().Key == ConsoleKey.Escape)
				{
					isDoneCrawling = true;
				}
				Console.WriteLine();
				Console.WriteLine($"Search size: {crawlers[0].GetFrontierSize()}");
			}
			webCrawlerManager.StopCrawlers(crawlers);

			Dictionary<Uri, bool> visitedUrls = crawlers[0].GetVisitedUrls();
			Queue<Uri> results = crawlers[0].GetResultUrls();
			Console.WriteLine($"Total found links: {visitedUrls.Count}");
			foreach (Uri url in results)
			{
				Console.WriteLine(url);
			}
			Console.WriteLine();
			Console.WriteLine($"Total found results: {results.Count}");
		}

		private void FindUrl()
		{
			Console.Write("Enter a URL: ");
			bool hasSetMaximumLinkAmount = default;
			int maximumLinkAmount = default;
			do
			{
				Console.Write("Enter the amount of links to visit: ");
				bool isNumber = int.TryParse(Console.ReadLine().Trim(), out int outMaximumLinkAmount);
				if (isNumber)
				{
					hasSetMaximumLinkAmount = true;
					maximumLinkAmount = outMaximumLinkAmount;
				}
				else
				{
					Console.WriteLine("The value you inserted was not a number.");
				}
			} while (!hasSetMaximumLinkAmount);

			Console.WriteLine("Crawling...");
			Console.WriteLine();
			WebCrawler webCrawler = new WebCrawler();
			webCrawler.Start(_userUrl, maximumLinkAmount);

			Console.Write($"---Found web page: {_userUrl}---");
			Queue<Uri> results = webCrawler.GetResultUrls();
			foreach (Uri url in results)
			{
				Console.WriteLine(url);
			}
			Console.WriteLine($"*Total found links: {results.Count}");
		}
	}
}
