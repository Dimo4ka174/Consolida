namespace DB.Abstract
{
    public interface IEntity
    {
        int? Id { get; }
        public bool IsDeleted { get; set; }
    }
}
