using System.Linq;

namespace Cloud5mins.domain
{
    public static class Utility
    {
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
        private static readonly int Base = Alphabet.Length;

        public static string GetValidEndUrl(string vanity, int i)
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

        public static string Encode(int i)
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

        public static ShortResponse BuildResponse(string host, string longUrl, string endUrl)
        {
            return new ShortResponse{
                LongUrl = longUrl,
                ShortUrl = string.Join(host,endUrl)
            };
        }

        public static ShortUrl BuildRow(string host, string longUrl, string endUrl){

            var newUrl = new ShortUrl
                {
                    PartitionKey = endUrl.First().ToString(),
                    RowKey = endUrl,
                    Url = longUrl
                };
            return newUrl;
        }

        public static async Task<bool> CheckIfExistRowAsync(){
                var selOp = TableOperation.Retrieve<ShortUrl>(newRow.PartitionKey,newRow.RowKey);
                var rowCheck = await tableOut.ExecuteAsync(selOp);  

                return(rowCheck.HttpStatusCode == (int)HttpStatusCode.OK);
            }
    }
}