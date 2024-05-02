using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace HelloGroup.Repository
{
    public class TransactionRepository : ITransactionRepository
    {
        const string _tableName = "Transactions";
        private readonly IConfiguration _configuration;
        private readonly int _batchSize;

        public TransactionRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _batchSize = string.IsNullOrEmpty(_configuration["AppSettings:BatchSize"]) ? 1000 : int.Parse(_configuration["AppSettings:BatchSize"]);
        }

        public async Task AddRangeAsync(List<Model.Transaction> transactions, CancellationToken token)
        {
            DataTable transactionDt = new DataTable();
            transactionDt.Columns.Add("TransactionId", typeof(long));
            transactionDt.Columns.Add("LineNumber", typeof(long));
            transactionDt.Columns.Add("FCDebit", typeof(double));
            transactionDt.Columns.Add("FCCredit", typeof(double));
            transactionDt.Columns.Add("Debit", typeof(double));
            transactionDt.Columns.Add("Credit", typeof(double));
            transactionDt.Columns.Add("PostDate", typeof(DateTime));
            transactionDt.Columns.Add("Currency", typeof(string));

            foreach (var transaction in transactions)
            {
                DataRow transactionRow = transactionDt.NewRow();
                transactionRow["TransactionId"] = transaction.TransactionId;
                transactionRow["LineNumber"] = transaction.LineNumber != null ? transaction.LineNumber : DBNull.Value;
                transactionRow["FCDebit"] = transaction.FCDebit != null ? transaction.FCDebit : DBNull.Value;
                transactionRow["FCCredit"] = transaction.FCCredit != null ? transaction.FCCredit : DBNull.Value;
                transactionRow["Debit"] = transaction.Debit != null ? transaction.Debit : DBNull.Value;
                transactionRow["Credit"] = transaction.Credit != null ? transaction.Credit : DBNull.Value;
                transactionRow["PostDate"] = transaction.PostDate != null ? new DateTime(transaction.PostDate.Value.Year, transaction.PostDate.Value.Month, transaction.PostDate.Value.Day) : DBNull.Value;
                transactionRow["Currency"] = transaction.Currency != null ? transaction.Currency : DBNull.Value;
                transactionDt.Rows.Add(transactionRow);    
            }

            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync(token);
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, trans))
                            {
                                bulkCopy.DestinationTableName = _tableName;
                                bulkCopy.BatchSize = _batchSize;
                                bulkCopy.BulkCopyTimeout = 60;
                                await bulkCopy.WriteToServerAsync(transactionDt, token);
                            }

                            await trans.CommitAsync(token);
                        }
                        catch (Exception ex)
                        {
                            await trans.RollbackAsync();
                            throw new Exception($"{ex.ToString()}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occured saving transactions, Error : {ex.ToString()}");
            }
        }

        public Task<List<Model.Transaction>> GetAllAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
