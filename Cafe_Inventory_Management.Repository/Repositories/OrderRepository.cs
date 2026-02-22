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
public class OrderRepository : IOrderRepo
{
    protected readonly ApplicationDbContext _context;
    public OrderRepository(ApplicationDbContext context)
    {
        _context =context;
    }

    public async Task<int> CreateProduct(OrdersModel order)
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
            var newOrder = new OrdersModel
            {
                OrderId = code,
                CreatedAt = DateTime.Now,
                TotalPrice = request.Items.Sum(x => x.Price * x.Quantity),
                Status = Status.Pending,
                CreatedBy = request.UserName,
                UpdatedBy = request.UserName,
            };

            List<OrderItems> orderlist = new List<OrderItems>();

            foreach (var item in request.Items)
            {
                
                var orderitems = new OrderItems
                {
                    OrderId = newOrder.OrderId,
                    ProductCode = item.ProductCode,
                    Quatity = item.Quantity,
                    Amount = item.Price,
                    CreatedBy = request.UserName,
                    UpdatedBy = request.UserName,
                };
                orderlist.Add(orderitems);
            }
            ;

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

    public async Task<ApiResponse> UpdateOrder(OrderViewModel updatedOrder)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var response = new ApiResponse();
            // 1. Find the Order Header
            var orderHeader = await _context.Orders
                .FirstOrDefaultAsync(x => x.OrderId == updatedOrder.OrderId);

            if (orderHeader == null)
            {
                response.ErrorCode = "01";
                response.ErrorMessage = "Order Not Found";
                return response;
            }
            if (orderHeader.Status != "Pending")
            {
                response.ErrorCode = "01";
                response.ErrorMessage = "Only pending orders can be edited";
                return response;
            }
            var existingItems = _context.OrderItems.Where(x => x.OrderId == updatedOrder.OrderId);
            _context.OrderItems.RemoveRange(existingItems);

            decimal newTotal = 0;
            foreach (var item in updatedOrder.Items)
            {
                // Map UI items back to Database Model
                var dbItem = new OrderItems
                {
                    OrderId = updatedOrder.OrderId,
                    ProductCode = item.ProductCode,
                    Quatity = item.Quatity, // Matches your DB typo
                    Amount = item.Amount,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                newTotal += (item.Amount * item.Quatity); // Recalculate based on quantity
                _context.OrderItems.Add(dbItem);
            }

            // 3. Update the Header Total
            orderHeader.TotalPrice = newTotal;
            orderHeader.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new ApiResponse()
            {
                ErrorCode = "01",
                ErrorMessage = ex.Message,
            };
        }
    }

    public async Task<ApiResponse> UpdateOrderStatus(OrderViewModel updatedOrder)
    {
        var order = await _context.Orders.Where(x => x.OrderId == updatedOrder.OrderId).FirstOrDefaultAsync();
        order.Status = updatedOrder.Status;
        if(updatedOrder.Status == Status.Preparing)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            {
                foreach (var item in updatedOrder.Items)
                {
                    var product =await _context.Product.Where(x => x.Code == item.ProductCode).FirstOrDefaultAsync();
             
                    var response = new ApiResponse();
                    if (product!.IsRecipe == true)
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
                            decimal totalDeduction = item.Quatity  * (ingre.RequiredAmount);

                            if (ingredient.Quatity < totalDeduction)
                            {
                                response.ErrorCode = "01";
                                response.ErrorMessage = $"Stock too low for {ingredient.Name}. Needed: {totalDeduction}";
                                return response;
                            }

                            ingredient.Quatity -= totalDeduction;
                            _context.Ingredients.Update(ingredient);
                        }
                    }
                }
                 _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

        }
        var result = _context.Orders.Update(order);
        await _context.SaveChangesAsync();
        return new ApiResponse();
    }

    public async Task<List<OrderViewModel>> GetOrders()
    {
        var orders = await _context.Orders
         .Where(o => o.Status != "Completed")
         .OrderByDescending(o => o.CreatedAt)
         .ToListAsync();

        var orderIds = orders.Select(o => o.OrderId).ToList();
        var allItems = await _context.OrderItems
            .Where(i => orderIds.Contains(i.OrderId))
            .ToListAsync();

        var viewModelList = orders.Select(o => new OrderViewModel
        {
            OrderId = o.OrderId,
            TotalPrice = o.TotalPrice,
            Status = o.Status,
            CreatedAt = o.CreatedAt,
            Items = allItems.Where(i => i.OrderId == o.OrderId).ToList()
        }).OrderByDescending(x=>x.CreatedAt).ToList();

        return viewModelList!;
    }

}
