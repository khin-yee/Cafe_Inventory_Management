using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.Domain.IRepository;
using Cafe_Inventory_Management.Domain.Model;
using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Repository.Repositories;
public class OrderRepository: IOrderRepo
{
    protected readonly ApplicationDbContext _context;
    public OrderRepository(ApplicationDbContext context)
    {
        _context =context;
    }

    public async Task<int> CreateProduct(Orders order)
    {
        await _context.Orders.AddAsync(order);
        var result = _context.SaveChanges();
        return result;
    }

    public async Task<ApiResponse> SaveOrder(OrderRequestDto request)
    {
        var response = new ApiResponse();
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            
            Random random = new Random();
            string code = "";
            for (int i = 0; i < 5; i++)
            {
                code = random.Next(10000, 99999).ToString();
            }
            // 2. Create the Order Header
            var newOrder = new Orders
            {
                OrderId = code,
                CreatedAt = DateTime.Now,
                TotalPrice = request.Items.Sum(x => x.Price * x.Quantity),
                Status = Status.Success,
                CreatedBy = request.UserName,
                UpdatedBy = request.UserName,
            };

            List<OrderItems> orderlist = new List<OrderItems>();

            foreach (var item in request.Items)
            {
                var res = await _context.ProductIngredients.Where(x => x.ProductCode== item.ProductCode).ToListAsync();
                foreach (var ingre in res)
                {
                    var ingredient = await _context.Ingredients.Where(x => x.Code==ingre.IngredientCode).FirstOrDefaultAsync();

                    if (ingredient == null)
                    {
                        response.ErrorCode = "01";
                        response.ErrorMessage = $"Ingredient with ID {ingre.IngredientCode} not found.";
                        return response;
                    }
                    decimal totalDeduction = item.Quantity  * (ingre.RequiredAmount);

                    if (ingredient.Quatity < totalDeduction)
                    {
                        response.ErrorCode = "01";
                        response.ErrorMessage = $"Stock too low for {ingredient.Name}. Needed: {totalDeduction}";
                        return response;
                    }

                    ingredient.Quatity -= totalDeduction;
                    _context.Ingredients.Update(ingredient);
                }
                var orderitems = new OrderItems
                {
                    OrderId = newOrder.OrderId,
                    ProductCode = item.ProductName,
                    Quatity = item.Quantity,
                    Amount = item.Price,
                    CreatedBy = request.UserName,
                    UpdatedBy = request.UserName,
                };
                orderlist.Add(orderitems);
            };

            _context.OrderItems.AddRange(orderlist);

            // 4. Save to Database
            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            // 5. Commit Transaction
            await transaction.CommitAsync();

            return response;
        }

        catch (Exception ex)
        {
            throw ex;
        }
    }
}

