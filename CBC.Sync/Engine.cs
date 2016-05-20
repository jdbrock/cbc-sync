using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TickerTools.Common;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Net.Code.Csv;
using System.Configuration;

namespace CBC.Sync
{
    public static class Engine
    {
        // ===========================================================================
        // = Public Properties
        // ===========================================================================
        
        public static HashAlgorithm Hasher { get; } = new SHA256Managed();

        // ===========================================================================
        // = Public Methods
        // ===========================================================================
        
        public static void Initialize()
        {
            // Initialize Facebook credentials.
            FacebookHelper.Initialize(
                ConfigurationManager.AppSettings["FACEBOOK_ACCESS_TOKEN"],
                ConfigurationManager.AppSettings["FACEBOOK_APP_ID"],
                ConfigurationManager.AppSettings["FACEBOOK_APP_SECRET"]);
        }

        public static void SyncFromCsv()
        {
            var beers = LoadCsv(@"C:\temp\cbc-2016.csv");

            //MutateForScreenshots(beers);

            var cbc = new Cbc();
            cbc.Beers.AddRange(beers.OrderBy(X => X.BreweryName).ThenBy(X => X.BeerName));
            //cbc.Note = "This is a beta beer list. Pull to update.";

            Publish(cbc);
        }

        public static void SyncFromFacebook()
        {
            var beers = LoadFromFacebook();

            //MutateForScreenshots(beers);

            var cbc = new Cbc();
            cbc.Beers.AddRange(beers.OrderBy(X => X.BreweryName).ThenBy(X => X.BeerName));
            //cbc.Note = "This is a beta beer list. Pull to update.";

            Publish(cbc);
        }

        // ===========================================================================
        // = Private Methods
        // ===========================================================================
        
        private static void MutateForScreenshots(List<CbcBeer> beers)
        {
            var i = 0;

            foreach (var beer in beers)
            {
                i++;

                //beer.BreweryName = "My Brewery " + i;
                beer.BeerName = "My Beer " + i;
            }
        }

        private static List<CbcBeer> LoadFromFacebook()
        {
            var posts = FacebookHelper.FetchEventPosts(162211374161547).ConfigureAwait(false).GetAwaiter().GetResult();
            var beers = new List<CbcBeer>();

            foreach (var post in posts)
            {
                if (post.Text != null && post.Text.Contains("RED") && post.Text.Contains("GREEN") && post.Text.Contains("BLUE") && post.Text.Contains("YELLOW"))
                {
                    var reader = new CbcFacebookReader(post.Text);

                    while (true)
                    {
                        var beer = reader.ReadBeer();

                        if (beer == null)
                            break;

                        beers.Add(beer);
                    }
                }
            }

            return beers;
        }

        private static void Publish(Cbc cbc)
        {
            var account = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AZURE_BLOB_STORAGE_CONNECTION_STRING"));
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference("data-prod");

            CloudBlockBlob blob = container.GetBlockBlobReference("beer-5.0.0-dev.json");

            using (var ms = new MemoryStream())
            {
                var writer = new JsonTextWriter(new StreamWriter(ms));
                JsonSerializer.Create().Serialize(writer, cbc);
                writer.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                blob.UploadFromStream(ms);
            }
        }

        private static List<CbcBeer> LoadCsv(String path)
        {
            var beers = new List<CbcBeer>();

            using (var reader = CsvExtensions.ReadFileAsCsv(path, Encoding.GetEncoding("windows-1252")))
            {
                while (reader.Read())
                {
                    beers.Add(new CbcBeer
                    {
                        SessionName = reader.GetString(0),
                        BreweryName = reader.GetString(1),
                        BeerName = reader.GetString(2),
                        StyleName = reader.GetString(3),
                        ABV = reader.GetDecimal(4),
                        Id = reader.GetString(5),

                        SessionNumber = CbcFacebookReader.GetSessionNumber(reader.GetString(0))
                    });
                }
            }

            return beers;
        }

        private static void SaveCsv(Cbc data)
        {
            var beers = data.Beers
                .OrderBy(X => X.SessionNumber)
                .ThenBy(X => X.BreweryName)
                .ThenBy(X => X.BeerName);

            using (var output = File.CreateText(@"c:\temp\cbc-2016-fb.csv"))
            {
                foreach (var beer in beers)
                    output.WriteLine(String.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\", \"{5}\"", beer.SessionName, beer.BreweryName, beer.BeerName, beer.StyleName, beer.ABV, Guid.NewGuid().ToString()));

                output.Flush();
            }
        }

        private static void SaveJson(Cbc data)
        {
            var outputPath = @"c:\temp\cbc-2016.js";
            File.WriteAllText(outputPath, JsonConvert.SerializeObject(data));
        }
    }
}
