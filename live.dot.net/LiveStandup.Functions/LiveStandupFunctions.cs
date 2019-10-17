using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using LiveStandup.Web.Services;
using System.Net;

namespace LiveStandup.Functions
{
    public static class LiveStandupFunctions
    {
        [FunctionName(nameof(GetShows))]
        public static async Task<HttpResponseMessage> GetShows(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequest req,
            [Blob("livestandup/shows.json", FileAccess.Read, Connection = "AzureWebJobsStorage")]Stream inBlob,
            ILogger log)
        {
            return await BlobHelpers.BlobToHttpResponseMessageAsync(inBlob, log, "shows");
        }

        [FunctionName(nameof(UpdateShows))]
        public static async Task<HttpResponseMessage> UpdateShows(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req,
            [Blob("livestandup/shows.json", FileAccess.Write, Connection = "AzureWebJobsStorage")]Stream outBlob,
            ILogger log)
        {
            return await UpdateShowsAsync(outBlob, log);
        }

        [FunctionName(nameof(UpdateShowsTimer))]
        public static async Task UpdateShowsTimer(
           [TimerTrigger("0 */5 * * * *")]TimerInfo myTimer,
           [Blob("livestandup/shows.json", FileAccess.Write, Connection = "AzureWebJobsStorage")]Stream outBlob,
           ILogger log)
        {
            await UpdateShowsAsync(outBlob, log);
        }

        private static async Task<HttpResponseMessage> UpdateShowsAsync(Stream outBlob, ILogger log)
        {
            try
            {
                log.LogInformation("Getting show feed.");

                var service = new YouTubeShowsService();
                var showsRaw = await service.GetShows();

                var json = JsonConvert.SerializeObject(showsRaw, Formatting.None);

                log.LogInformation("Writting feed to blob.");
                using (var writer = new StreamWriter(outBlob))
                {
                    writer.Write(json);
                }

                log.LogInformation("Shows function finished.");

            }
            catch (Exception ex)
            {
                log.LogError(ex, "Unable to get show feed");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
