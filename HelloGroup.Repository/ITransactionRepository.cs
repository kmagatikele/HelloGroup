using HelloGroup.Model;

namespace HelloGroup.Repository
{
    public interface ITransactionRepository
    {
        Task AddRangeAsync(List<Transaction> transactions, CancellationToken token = default);
    }
}
