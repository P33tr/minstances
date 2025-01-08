using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using minstances.Data;
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
            builder.Services.AddScoped<IBlueskyService, BlueskyService>();

            //Configure the ConnectionString and DbContext class
            var connectionString = @"Data Source=C:\minstances_data\minstances.db";
            builder.Services.AddDbContext<MinstancesContext>(options =>
            {
                options.UseSqlite(connectionString);
            });
            builder.Services.AddScoped<IMinstancesRepository, MinstancesRepository>();

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
            app.Run();
        }
    }
}