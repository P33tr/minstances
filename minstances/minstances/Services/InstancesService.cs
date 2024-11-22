using ErrorOr;
using System.Net.Http.Headers;
using System.Text.Json;

namespace minstances.Services
{
    public interface IInstancesService
    {
        Task<ErrorOr<InstX>> GetAsync();
        Task<ErrorOr<InstX>> ListAsync(string sort_by, string sort_order);
    }
    //    Get an API token
    //Application name: minstances
    //Application ID: 141087153
    //Secret token: WBKOudZsI7lYH36cpYOQRTHjeCwtcv1SPjQBW6eKmjxbdaJIfT3ns6yuLhaQzrJzHzj6qP3WC4ctWv3iFa8RWPFLtR4mbhNTbNqAJdUaOvtiFJb5kHQpfVZuhRXCZIWm
    public class InstancesService: IInstancesService
    {
        public InstancesService() { }

        public async Task<ErrorOr<InstX>> GetAsync()
        {
            HttpClient client = new();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "WBKOudZsI7lYH36cpYOQRTHjeCwtcv1SPjQBW6eKmjxbdaJIfT3ns6yuLhaQzrJzHzj6qP3WC4ctWv3iFa8RWPFLtR4mbhNTbNqAJdUaOvtiFJb5kHQpfVZuhRXCZIWm");

            using HttpResponseMessage response = await client.GetAsync("https://instances.social/api/1.0/instances/sample");
            if (response.IsSuccessStatusCode)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                InstX results = JsonSerializer.Deserialize<InstX>(responseData);
                return results;
            }
            else
            {
                return Error.Failure(response.StatusCode.ToString());
            }
        }
        
        public async Task<ErrorOr<InstX>> ListAsync(string sort_by, string sort_order)
        {
            HttpClient client = new();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "WBKOudZsI7lYH36cpYOQRTHjeCwtcv1SPjQBW6eKmjxbdaJIfT3ns6yuLhaQzrJzHzj6qP3WC4ctWv3iFa8RWPFLtR4mbhNTbNqAJdUaOvtiFJb5kHQpfVZuhRXCZIWm");

            
            using HttpResponseMessage response = await client.GetAsync($"https://instances.social/api/1.0/instances/list?sort_by={sort_by}&sort_order={sort_order}&count=500");
            if (response.IsSuccessStatusCode)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                InstX results = JsonSerializer.Deserialize<InstX>(responseData);
                return results;
            }
            else
            {
                return Error.Failure(response.StatusCode.ToString());
            }
        }
        
    }
}
