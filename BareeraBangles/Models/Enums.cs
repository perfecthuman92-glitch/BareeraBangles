namespace BareeraBangles.Models;

public enum SizeCategory
{
    Adult = 0,
    BabyKids = 1,
    Unisex = 2
}

public enum ProductAgeGroup
{
    Adult = 0,
    BabyKids = 1,
    Unisex = 2
}

public enum OrderStatus
{
    Pending = 0,
    PaymentPending = 1,
    Paid = 2,
    Processing = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6
}

public enum PaymentMethod
{
    JazzCash = 0,
    BankCard = 1,
    CashOnDelivery = 2
}

public enum PaymentStatus
{
    Pending = 0,
    AwaitingProvider = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
