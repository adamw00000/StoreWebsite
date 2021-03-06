﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StoreWebsite.Data;
using StoreWebsite.Models;
using StoreWebsite.Models.Orders;

namespace StoreWebsite.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddOrderAsync(Order order, List<OrderDetails> orderDetails)
        {
            await _context.AddAsync(order);

            foreach (OrderDetails itemDetails in orderDetails)
            {
                await _context.AddAsync(itemDetails);
            }

            var saveResult = await _context.SaveChangesAsync();
            return saveResult == 1 + orderDetails.Count;
        }

        public async Task<IEnumerable<Order>> GetOrderListAsync(Guid userId, OrderFilters filters)
        {
            if (filters.EndDate == DateTime.MinValue)
                filters.EndDate = DateTime.MaxValue;

            Func<OrderStatus, bool> match;

            if (filters.StatusCode == OrderStatus.None)
            {
                match = (code) => code != OrderStatus.Cancelled && code != OrderStatus.Completed && code != OrderStatus.None
                 && code != OrderStatus.All;
            }
            else if (filters.StatusCode == OrderStatus.All)
            {
                match = (_) => true;
            }
            else
            {
                match = (code) => code == filters.StatusCode;
            }

            return await _context.Orders
                .Where(order => order.UserId == userId && match(order.StatusCode)
                    && order.OrderDate >= filters.BeginDate && order.OrderDate <= filters.EndDate)
                .Include(order => order.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(order => order.OrderDate)
                .ToArrayAsync();
        }

        public Task<Order> GetOrderAsync(Guid orderId)
        {
            return _context.Orders
                .Where(order => order.Id == orderId)
                .Include(order => order.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Image)
                .FirstAsync();
        }

        public async Task<bool> CancelOrderAsync(Order order)
        {
            order.StatusCode = OrderStatus.Cancelled;

            int saveResult = await _context.SaveChangesAsync();
            return saveResult == 1;
        }
    }
}
