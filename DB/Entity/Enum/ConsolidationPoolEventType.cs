namespace DB.Entity.Enum
{
    public enum ConsolidationPoolEventType
    {
        Created = 1,
        OrderAdded = 2,
        OrderRemoved = 3,
        OrderMovedIn = 4,
        OrderMovedOut = 5,
        StatusChanged = 6,
        ExpectedDeliveryDateChanged = 7,
        Dissolved = 8,
        ColorChanged = 9,
        WeightLimitExceeded = 10
    }
}
