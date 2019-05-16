#r "Newtonsoft.Json"

using System;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Rest;
using System.Threading;
using System.Threading.Tasks;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    string textAnalyticsAPIKey = Environment.GetEnvironmentVariable("text_analytics_api_key");
    string textAnalyticsEndpoint = Environment.GetEnvironmentVariable("text_analytics_endpoint");

    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    dynamic data = JsonConvert.DeserializeObject(requestBody);
    string inputText = data.text;

    var credentials = new ApiKeyServiceClientCredentials(textAnalyticsAPIKey);
    var client = new TextAnalyticsClient(credentials)
    {
        Endpoint = textAnalyticsEndpoint
    };

    //Detecting language first
    var inputDocuments = new LanguageBatchInput(
            new List<LanguageInput>
                {
                    new LanguageInput(id: "1", text: inputText)
                });

    var langResults = await client.DetectLanguageAsync(false, inputDocuments);
    string inputLanguage = null;
    foreach (var document in langResults.Documents)
    {
        inputLanguage = document.DetectedLanguages[0].Iso6391Name;
        log.LogInformation($"Document ID: {document.Id} , Language: {inputLanguage}");
    }

    //Detecting sentiment of the input text
    var inputDocuments2 = new MultiLanguageBatchInput(
    new List<MultiLanguageInput>
    {
            new MultiLanguageInput(inputLanguage, "1", inputText)
    });

    var sentimentResult = await client.SentimentAsync(false, inputDocuments2);
    double? sentimentScore = 0;
    foreach (var document in sentimentResult.Documents)
    {
        sentimentScore = document.Score;
        log.LogInformation($"Document ID: {document.Id} , Sentiment Score: {sentimentScore:0.00}");
    }


    return inputText != null
        ? (ActionResult)new OkObjectResult($"Hello, {inputText}")
        : new BadRequestObjectResult("Please pass the text input for the text analytics operations");
}

class ApiKeyServiceClientCredentials : ServiceClientCredentials
{
    private readonly string subscriptionKey;

    /// <summary>
    /// Creates a new instance of the ApiKeyServiceClientCredentails class
    /// </summary>
    /// <param name="subscriptionKey">The subscription key to authenticate and authorize as</param>
    public ApiKeyServiceClientCredentials(string subscriptionKey)
    {
        this.subscriptionKey = subscriptionKey;
    }

    /// <summary>
    /// Add the Basic Authentication Header to each outgoing request
    /// </summary>
    /// <param name="request">The outgoing request</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException("request");
        }

        request.Headers.Add("Ocp-Apim-Subscription-Key", this.subscriptionKey);
        return base.ProcessHttpRequestAsync(request, cancellationToken);
    }
}
