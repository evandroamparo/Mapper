namespace PlayGround.Models;

public class UserDto
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class UserEntity
{
    public string Name { get; set; }
    public int Age { get; set; }
    public AddressDto Address { get; set; } // Add Address property
}
public class AddressDto
{
    public string Street { get; set; }
    public string City { get; set; }
}

public class UserWithAddressDto
{
    public string Name { get; set; }
    public AddressDto Address { get; set; }
}

public class ItemDto
{
    public string ProductName { get; set; }
    public int Quantity { get; set; }
}

public class Item
{
    public string ProductName { get; set; }
    public int Quantity { get; set; }
}

public class OrderDto
{
    public List<ItemDto> Items { get; set; }
}

public class Order
{
    public ICollection<Item> Items { get; set; }
}
