using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.Domain.IRepository;
using Cafe_Inventory_Management.Domain.Model;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
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
                    ProductName = item.ProductName,
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
                    ProductName = item.ProductName,
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
        if (updatedOrder.Status == Status.Preparing)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            {
                foreach (var item in updatedOrder.Items)
                {
                    var product = await _context.Product.Where(x => x.Code == item.ProductCode).FirstOrDefaultAsync();

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
            CreatedBy = o.CreatedBy,
            Items = allItems.Where(i => i.OrderId == o.OrderId).ToList()
        }).OrderByDescending(x => x.CreatedAt).ToList();

        return viewModelList!;
    }

    public async Task<PagedResult<OrderViewModel>> GetAllSuccessOrders(int page, int pageSize, string? search, DateTime? start, DateTime? end)
    {
        var query = _context.Orders.Where(o => o.Status == "Success" || o.Status == "Completed");

        if (!string.IsNullOrEmpty(search))
            query = query.Where(o => o.OrderId.Contains(search));

        if (start.HasValue) query = query.Where(o => o.CreatedAt >= start.Value);
        if (end.HasValue) query = query.Where(o => o.CreatedAt < end.Value.AddDays(1));

        int totalCount = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PagedResult<OrderViewModel>
        {
            TotalCount = totalCount,
            Items = orders.Select(o => new OrderViewModel
            {
                OrderId = o.OrderId,
                TotalPrice = o.TotalPrice,
                Status = o.Status,
                CreatedAt = o.CreatedAt
            }).ToList()
        };
        return result;
    }

    public async Task<List<OrderViewModel>> ExportHistory(string? search, DateTime? start, DateTime? end)
    {
        var query = _context.Orders
            .Where(o => o.Status == "Success" || o.Status == "Completed");

        if (!string.IsNullOrEmpty(search)) query = query.Where(o => o.OrderId.Contains(search));
        if (start.HasValue) query = query.Where(o => o.CreatedAt >= start.Value);
        if (end.HasValue) query = query.Where(o => o.CreatedAt < end.Value.AddDays(1));

        var data = await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderViewModel
            {
                OrderId = o.OrderId,
                TotalPrice = o.TotalPrice,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                Items = _context.OrderItems.Where(i => i.OrderId == o.OrderId).ToList()
            })
            .ToListAsync();

        return data;

    }

    public async Task<DashboardData> GetStats(string range)
    {
        var today = DateTime.Now.Date;
        DateTime startDate = range switch
        {
            "month" => new DateTime(today.Year, today.Month, 1),
            "year" => new DateTime(today.Year, 1, 1),
            _ => today.AddDays(-6)
        };

        var orders = await _context.Orders
            .Where(o => (o.Status == "Success" || o.Status == "Completed") && o.CreatedAt >= startDate)
            .ToListAsync();

        var itemDetails = await (from item in _context.OrderItems
                                 join prod in _context.Product on item.ProductCode equals prod.Code
                                 join ord in _context.Orders on item.OrderId equals ord.OrderId
                                 where (ord.Status == "Success" || ord.Status == "Completed") && ord.CreatedAt >= startDate
                                 select new
                                 {
                                     item.ProductCode,
                                     prod.Name,
                                     prod.Category,
                                     item.Quatity,
                                     item.Amount,
                                     ord.CreatedAt
                                 }).ToListAsync();

        // 3. Category Distribution (Pie Chart)
        var categoryData = itemDetails
            .GroupBy(x => x.Category ?? "Other")
            .Select(g => new CategoryStat
            {
                CategoryName = g.Key,
                TotalRevenue = (double)g.Sum(x => x.Quatity * x.Amount)
            }).ToList();

        // 4. Top 5 Products
        var topProducts = itemDetails
            .GroupBy(x => new { x.ProductCode, x.Name })
            .Select(g => new ProductStat
            {
                ProductName = g.Key.Name,
                TotalQty = g.Sum(x => x.Quatity)
            })
            .OrderByDescending(x => x.TotalQty)
            .Take(5)
            .ToList();

        // 5. Chart Data (Bar/Line Chart)
        var labels = new List<string>();
        var dataPoints = new List<double>();

        if (range == "year")
        {
            for (int i = 1; i <= 12; i++)
            {
                labels.Add(new DateTime(today.Year, i, 1).ToString("MMM"));
                dataPoints.Add((double)orders.Where(o => o.CreatedAt.Month == i).Sum(o => o.TotalPrice));
            }
        }
        if (range == "month")
        {
            var daysInMonth = (today - startDate).Days;
            for (int i = 0; i <= daysInMonth; i++)
            {
                var date = startDate.AddDays(i);
                if (i == 0 || i % 5 == 0 || date == today)
                {
                    labels.Add(date.ToString("dd MMM"));
                }
                else
                {
                    labels.Add("");
                }

                dataPoints.Add((double)orders.Where(o => o.CreatedAt.Date == date).Sum(o => o.TotalPrice));
            }
        }
        else
        {
            for (var date = startDate; date <= today; date = date.AddDays(1))
            {
                labels.Add(date.ToString("dd MMM"));
                dataPoints.Add((double)orders.Where(o => o.CreatedAt.Date == date).Sum(o => o.TotalPrice));
            }
        }

        var stats = new DashboardData
        {
            TotalSales = orders.Sum(o => o.TotalPrice),
            TotalOrders = orders.Count,
            TodaySales = orders.Where(o => o.CreatedAt >= today).Sum(o => o.TotalPrice),
            AvgOrderValue = orders.Count > 0 ? orders.Average(o => o.TotalPrice) : 0,
            ChartLabels = labels.ToArray(),
            ChartData = dataPoints.ToArray(),
            CategoryDistribution = categoryData,
            TopProducts = topProducts
        };
        return stats;
    }

    public async Task<List<StaffResponseDto>> GetStaffPerformance(string range)
    {
        var today = DateTime.Now.Date;
        DateTime startDate = range switch
        {
            "today" => today,
            "month" => new DateTime(today.Year, today.Month, 1),
            _ => today.AddDays(-6)
        };

        var staffStats = await _context.Orders.Where(o => (o.Status == "Success" || o.Status == "Completed") && o.CreatedAt >= startDate)
            .GroupBy(o => o.CreatedBy)
            .Select(g => new StaffResponseDto
            {
                StaffName = g.Key ?? "Unknown Staff",
                TotalOrdersHandled = g.Count(),
                TotalRevenueGenerated = g.Sum(o => o.TotalPrice)
            })
            .OrderByDescending(x => x.TotalOrdersHandled)
            .ToListAsync();

        return staffStats;
    }

    public async Task<AdminDashboardData> GetAdminStatus()
    {     
        var lowStock = await _context.Product.Where(p => p.Quatity <= 1)
            .Select(p => new LowStockProduct
            {
                Name = p.Name,
                CurrentStock = (double)p.Quatity,
                Category = p.Category
            })
            .ToListAsync();

        var activity = await _context.Orders.OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new UserActivity
            {
                User = o.CreatedBy,
                Action = $"Placed order {o.OrderId}",
                Time = o.CreatedAt
            })
            .ToListAsync();

        var result = new AdminDashboardData
        {
            LowStockItems = lowStock,
            RecentActivities = activity
        };

        return result;
    }

    public async Task<OrderViewModel> GetOrderDetails(string orderId)
    {
        var order = await _context.Orders
           .Where(o => o.OrderId == orderId)
           .Select(o => new OrderViewModel
           {
               OrderId = o.OrderId,
               TotalPrice = o.TotalPrice,
               Status = o.Status,
               CreatedAt = o.CreatedAt,
               CreatedBy = o.CreatedBy,
           }).FirstOrDefaultAsync();

        if (order == null) return order!;

        try
        {
            order.Items = await _context.OrderItems
                .Where(i => i.OrderId == orderId)
                .ToListAsync();
        }
        catch(Exception ex)
        {

        }
        return order;
    }
}
