﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlayGround.Models;
using QuickMapper.Core;

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

// Simple mapping
var userDto = new UserDto { Name = "John Doe", Age = 30 };
var userEntity = mapper.Map<UserDto, UserEntity>(userDto);
Console.WriteLine($"Mapped User: {userEntity.Name}, Age: {userEntity.Age}");

// Mapping of nested objects
var userWithAddressDto = new UserWithAddressDto
{
    Name = "Jane Doe",
    Address = new AddressDto { Street = "123 Main St", City = "Anytown" }
};
var userWithAddressEntity = mapper.Map<UserWithAddressDto, UserEntity>(userWithAddressDto);
Console.WriteLine($"Mapped User with Address: {userWithAddressEntity.Name}, Address: {userWithAddressEntity.Address.Street}, {userWithAddressEntity.Address.City}");

// Mapping of collections
var orderDto = new OrderDto
{
    Items =
        [
            new ItemDto { ProductName = "Item1", Quantity = 2 },
            new ItemDto { ProductName = "Item2", Quantity = 3 }
        ]
};
var orderEntity = mapper.Map<OrderDto, Order>(orderDto);
Console.WriteLine($"Mapped Order with {orderEntity.Items.Count} items:");
foreach (var item in orderEntity.Items)
    Console.WriteLine($"  - {item.ProductName}: {item.Quantity}");

// Conversor personalizado: sufixar o nome
mapper.ForMember<UserDto, UserEntity, string>(
    dest => dest.Name,
    src => $"{src.Name} (mapped)"
);

var customUser = new UserDto { Name = "Alice", Age = 25 };
var customMappedUser = mapper.Map<UserDto, UserEntity>(customUser);
Console.WriteLine($"User with custom mapped name: {customMappedUser.Name}");

// Mapeamento bidirecional
mapper.CreateReverseMap<UserDto, UserEntity>();
var reverseEntity = new UserEntity { Name = "Carlos", Age = 40 };
var reverseDto = mapper.Map<UserEntity, UserDto>(reverseEntity);
Console.WriteLine($"Reversed UserDto: {reverseDto.Name}, {reverseDto.Age}");

// Validator example
mapper.AddValidator<UserDto>(user =>
{
    if (string.IsNullOrWhiteSpace(user.Name))
        throw new InvalidOperationException("User name cannot be empty.");
    if (user.Age < 0)
        throw new InvalidOperationException("Age must be a positive value.");
    return true; // valid
});

try
{
    var invalidUser = new UserDto { Name = "s", Age = -5 };
    mapper.Map<UserDto, UserEntity>(invalidUser);
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Validation failed: {ex.Message}");
}

void ConfigureMappings(IMapper mapper)
{
    mapper.CreateMap<UserDto, UserEntity>();
    mapper.CreateMap<UserWithAddressDto, UserEntity>();
    mapper.CreateMap<AddressDto, AddressDto>(); // Caso queira fazer deep copy de Address
    mapper.CreateMap<OrderDto, Order>();
    mapper.CreateMap<ItemDto, Item>();

    // Bidirecional
    mapper.CreateReverseMap<UserDto, UserEntity>();
}
