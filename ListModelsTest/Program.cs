using System;
using System.Threading.Tasks;
using Mscc.GenerativeAI;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder().AddUserSecrets<Program>();
        var config = builder.Build();
        var apiKey = config["Gemini:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("API Key not found in User Secrets!");
            return;
        }

        var googleAi = new GoogleAI(apiKey);
        var models = await googleAi.ListModels();
        foreach (var m in models)
        {
            Console.WriteLine(m.Name);
        }
    }
}
