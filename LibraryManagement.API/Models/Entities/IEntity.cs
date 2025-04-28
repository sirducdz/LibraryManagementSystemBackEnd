namespace LibraryManagement.API.Models.Entities
{
    public interface IEntity<TKey> where TKey : IEquatable<TKey>
    {
        TKey Id { get; set; }
    }
}
