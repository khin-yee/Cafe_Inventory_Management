using Cafe_Inventory_Management.Domain.IRepository;
using Cafe_Inventory_Management.Domain.IServices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Service;
public static class DependencyInjection
{
    public static IServiceCollection AddService(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        return services;
    }
}
