using Cafe_Inventory_Management.Domain.IRepository;
using Cafe_Inventory_Management.Domain.IServices;
using Cafe_Inventory_Management.Repository.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Repository
{
    public static class DependencyInjection
    {

        public static IServiceCollection AddRepo(this IServiceCollection services)
        {
            services.AddScoped<IProductRepo, ProductRepository>();
            services.AddScoped<IIngredientRepo, IngredientRepository>();
            services.AddScoped<IOrderRepo, OrderRepository>();
            return services;
        }
    }
}
