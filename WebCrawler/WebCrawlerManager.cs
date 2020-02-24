using System;
using System.Collections.Generic;
using Google.Apis.Services;
using Google.Apis.Customsearch.v1;
using Google.Apis.Customsearch.v1.Data;

namespace WebCrawler
{
	internal class WebCrawlerManager
	{
		private readonly string _apiKey = "AIzaSyCDtRRPYI9S3Sin_cttV8uT88YjOoCJyxw";
		private readonly string _searchEngineId = "001735471954775331193:ph3ry182tz1";

		public Queue<Uri> InitialSearch(string searchWord)
		{
			string apiKey = _apiKey;
			string searchEngineId = _searchEngineId;

			CustomsearchService customsearchService = new CustomsearchService(new BaseClientService.Initializer { ApiKey = apiKey });
			CseResource.ListRequest listRequest = customsearchService.Cse.List(searchWord);
			listRequest.Cx = searchEngineId;
			Search search = listRequest.Execute();

			Queue<Uri> links = new Queue<Uri>();
			foreach (Result result in search.Items)
			{
				links.Enqueue(new UriBuilder(result.Link).Uri);
			}
			customsearchService.Dispose();
			return links;
		}

		public List<WebCrawler> CreateCrawlers(string searchWord, Queue<Uri> initialQueue, int amount)
		{
			List<WebCrawler> crawlers = new List<WebCrawler>{ new WebCrawler(searchWord, initialQueue, 5)};
			for (int i = 1; i < amount; i++)
			{
				crawlers.Add(new WebCrawler());
			}
			return crawlers;
		}

		public void StopCrawlers(List<WebCrawler> crawlers)
		{
			foreach (WebCrawler webCrawler in crawlers)
			{
				webCrawler.Stop();
			}
		}
	}
}
