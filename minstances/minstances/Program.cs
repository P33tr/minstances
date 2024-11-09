using minstances.Hubs;
using minstances.Services;

namespace minstances
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddHttpClient();

            builder.Services.AddScoped<IInstancesService, InstancesService>();
            builder.Services.AddScoped<IMastodonService, MastodonService>();

            // adding signal r 
            builder.Services.AddSignalR();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // map the route for the signalr endpoint
            app.UseEndpoints(endpoints => { endpoints.MapHub<StatusHub>("/statusHub"); }
            );
            app.Run();
        }
    }
}