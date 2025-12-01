using System.Net;
using Microsoft.Extensions.Configuration;

namespace Advent2025;

internal class InputDownloader {
    public static Stream DownloadInput(int day) {
        HttpClient client = new() {
            BaseAddress = new($"https://adventofcode.com/{AdventUtils.Year}/day/{day}/input"),
            DefaultRequestHeaders = {
                { HttpRequestHeader.Accept.ToString(), "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8" },
                { HttpRequestHeader.AcceptEncoding.ToString(), "gzip, deflate, br, zstd" },
                { HttpRequestHeader.Cookie.ToString(), $"session={GetSessionId()}" },
            },
        };

        var task = client.GetStreamAsync(client.BaseAddress);
        task.Wait();
        var response = task.Result;
        return response;
    }

    private static string? GetSessionId() {
        var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddUserSecrets<InputDownloader>();

        var config = builder.Build();
        return config.GetSection("AoC:SessionId").Value;
    }
}
