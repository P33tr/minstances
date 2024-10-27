﻿using ErrorOr;
using System.Net.Http.Headers;
using System.Text.Json;
using minstances.Models;

namespace minstances.Services
{
    public interface IMastodonService
    {
        Task<ErrorOr<Models.StatusX>> GetStatusesAsync(string instance);
    }
//    Get an API token
//Application name: minstances
//Application ID: 141087153
//Secret token: WBKOudZsI7lYH36cpYOQRTHjeCwtcv1SPjQBW6eKmjxbdaJIfT3ns6yuLhaQzrJzHzj6qP3WC4ctWv3iFa8RWPFLtR4mbhNTbNqAJdUaOvtiFJb5kHQpfVZuhRXCZIWm
    public class MastodonService: IMastodonService
    {
        public MastodonService() { }

        public async Task<ErrorOr<Models.StatusX>> GetStatusesAsync(string instance)
        {
            HttpClient client = new();

            // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "WBKOudZsI7lYH36cpYOQRTHjeCwtcv1SPjQBW6eKmjxbdaJIfT3ns6yuLhaQzrJzHzj6qP3WC4ctWv3iFa8RWPFLtR4mbhNTbNqAJdUaOvtiFJb5kHQpfVZuhRXCZIWm");

            using HttpResponseMessage response = await client.GetAsync($"https://{instance}/api/v1/timelines/public");
            if (response.IsSuccessStatusCode)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                Models.StatusX results = JsonSerializer.Deserialize<Models.StatusX>(responseData);
                return results;
            }
            else
            {
                return Error.Failure(response.StatusCode.ToString());
            }
        }
    }
}