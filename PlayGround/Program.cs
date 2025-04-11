﻿﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlayGround.Models;
using QuickMapper.Core;
using System.Linq;

var serviceProvider = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole())
    .AddSingleton<IMapper>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<Mapper>>();
        var mapper = new Mapper(logger);
        ConfigureMappings(mapper);
        return mapper;
    })
    .BuildServiceProvider();

var mapper = serviceProvider.GetRequiredService<IMapper>();

// Example of basic mapping
var userDto = new UserDto { Name = "John Doe", Age = 30 };
var userEntity = mapper.Map<UserDto, UserEntity>(userDto);
Console.WriteLine($"Mapped User: {userEntity.Name}, Age: {userEntity.Age}");

// Example of nested objects
var userWithAddressDto = new UserWithAddressDto
{
    Name = "Jane Doe",
    Address = new AddressDto { Street = "123 Main St", City = "Anytown" }
};
mapper.CreateMap<UserWithAddressDto, UserEntity>();
var userWithAddressEntity = mapper.Map<UserWithAddressDto, UserEntity>(userWithAddressDto);
Console.WriteLine($"Mapped User with Address: {userWithAddressEntity.Name}, Address: {userWithAddressEntity.Address.Street}, {userWithAddressEntity.Address.City}");

// Example of collection mapping
var orderDto = new OrderDto
{
    Items =
        [
            new ItemDto { ProductName = "Item1", Quantity = 2 },
            new ItemDto { ProductName = "Item2", Quantity = 3 }
        ]
};
mapper.CreateMap<OrderDto, Order>();
mapper.CreateMap<ItemDto, Item>();
var orderEntity = mapper.Map<OrderDto, Order>(orderDto);
Console.WriteLine($"Mapped Order with {orderEntity.Items.Count} items.");
orderEntity.Items.ToList().ForEach(item => 
    Console.WriteLine($"Item: {item.ProductName}, Quantity: {item.Quantity}")
);

void ConfigureMappings(IMapper mapper)
{
    mapper.CreateMap<UserDto, UserEntity>();
    mapper.CreateMap<UserWithAddressDto, UserEntity>();
    mapper.CreateMap<OrderDto, Order>();
    mapper.CreateMap<ItemDto, Item>();
}

