using System;

namespace WebCrawler.UI
{
	internal class UserInput
	{
		private readonly string userUrl = "https://www.easv.dk/da/";


		public void Initialize()
		{
			Console.Write("Enter a URL: ");
			Console.ReadLine();

			bool hasSetMaximumLinkAmount = default;
			int maximumLinkAmount = default;
			do
			{
				Console.Write("Enter the amount of links to visit: ");
				bool isNumber = int.TryParse(Console.ReadLine(), out int outMaximumLinkAmount);
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
			webCrawler.Start(userUrl, maximumLinkAmount);
		}

		public void Write(string value, bool isNewLine = true)
		{
			if (isNewLine)
				Console.WriteLine(value);
			else
				Console.Write(value);
		}
	}
}
