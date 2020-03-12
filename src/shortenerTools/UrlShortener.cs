using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using cloud5mins.models;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;

namespace cloud5mins.Function
{
    public static class UrlShortener
    {
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
        private static readonly int Base = Alphabet.Length;

        [FunctionName("UrlShortener")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestMessage req,
            [Table("UrlsDetails", "1", "KEY", Take = 1)]NextId keyTable,
            [Table("UrlsDetails")]CloudTable tableOut,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed this request: {req}");

            // Validation of the inputs
            if (req == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            ShortRequest input = await req.Content.ReadAsAsync<ShortRequest>();

            if (input == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            if (keyTable == null)
            {
                keyTable = new NextId
                {
                    PartitionKey = "1",
                    RowKey = "KEY",
                    Id = 1024
                };
                var keyAdd = TableOperation.Insert(keyTable);
                await tableOut.ExecuteAsync(keyAdd); 
            }
            
            try
            {
                var result = new ShortResponse();
                string longUrl = input.Url.Trim();
                string vanity = input.Vanity.Trim();

                log.LogInformation($"longUrl={longUrl}, vanity={vanity}");
                log.LogInformation($"Current key: {keyTable.Id}");
                
                var host = req.RequestUri.GetLeftPart(UriPartial.Authority);
                string endUrl = GetValidEndUrl(vanity, keyTable.Id++);

                log.LogInformation($"host={host} endUrl={endUrl}");

                result = BuildResponse(host, longUrl, endUrl);
                var newRow = BuildRow(host, longUrl, endUrl);

                async Task saveKeyAsync()
                {
                    var updOp = TableOperation.Replace(keyTable);
                    await tableOut.ExecuteAsync(updOp);
                }

                async Task<bool> CheckIfExistRowAsync(){
                    var selOp = TableOperation.Retrieve<ShortUrl>(newRow.PartitionKey,newRow.RowKey);
                    var rowCheck = await tableOut.ExecuteAsync(selOp);  

                    return(rowCheck.HttpStatusCode == (int)HttpStatusCode.OK);
                }

                async Task saveRowAsync()
                {
                    var insOp = TableOperation.Insert(newRow);
                    var operationResult = await tableOut.ExecuteAsync(insOp);  
                }

                await saveKeyAsync();

                if(await CheckIfExistRowAsync())
                    return req.CreateResponse(HttpStatusCode.Conflict, "This Short URL already exist.");
                else
                    await saveRowAsync();

                log.LogInformation("Short Url created.");
                return req.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An unexpected error was encountered.");
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }
        }
    
        private static string GetValidEndUrl(string vanity, int i)
        {
            if(string.IsNullOrEmpty(vanity))
            {
                string getCode() => Encode(i); 
                return string.Join(string.Empty, getCode());
            }
            else
            {
                return string.Join(string.Empty, vanity);
            }
        }

        private static string Encode(int i)
        {
            if (i == 0)
                return Alphabet[0].ToString();
            var s = string.Empty;
            while (i > 0)
            {
                s += Alphabet[i % Base];
                i = i / Base;
            }

            return string.Join(string.Empty, s.Reverse());
        }

        private static ShortResponse BuildResponse(string host, string longUrl, string endUrl)
        {
            return new ShortResponse{
                LongUrl = longUrl,
                ShortUrl = string.Join(host,endUrl)
            };
        }

        private static ShortUrl BuildRow(string host, string longUrl, string endUrl){

            var newUrl = new ShortUrl
                {
                    PartitionKey = endUrl.First().ToString(),
                    RowKey = endUrl,
                    Url = longUrl
                };
            return newUrl;
        }

    }
}
