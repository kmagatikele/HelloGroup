using HelloGroup.Model;
using HelloGroup.Repository;
using HelloGroup.Shared;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace HelloGroup.Service.Extractor
{
    public class Worker : BackgroundService
    {
        const string _filePath = "R_SampleDataIU.csv";
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly ITransactionRepository _repository;
        private readonly int _batchSize;
        private readonly string _apiUrl;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, ITransactionRepository repository)
        {
            if(configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
            _configuration = configuration;
            _repository = repository;
            _batchSize = string.IsNullOrEmpty(_configuration["AppSettings:BatchSize"]) ? 1000 : int.Parse(_configuration["AppSettings:BatchSize"]);
            _apiUrl = string.IsNullOrEmpty(_configuration["AppSettings:ApiUrl"]) ? throw new Exception("Please sent api url to sent data to") : _configuration["AppSettings:ApiUrl"];
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    try
                    {
                        var transactions = await ExtracFileAsync(stoppingToken);
                        _logger.LogInformation($"{transactions.Count} transactions to be saved...");
                        var addToDatabaseTask = Task.Run(async () =>
                        {
                            await AddToDatabaseAsync(transactions, stoppingToken);
                        });

                        var addToApiTask = Task.Run(async () =>
                        {
                            await AddToApiAsync(transactions, stoppingToken);
                        });

                        await Task.WhenAll(addToDatabaseTask, addToApiTask);
                        _logger.LogInformation($"Finished sending {transactions.Count} transactions");
                        await StopAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error occured, message: {ex.ToString()}");
                        await StopAsync(stoppingToken);
                    }
                }
            }
        }

        async ValueTask<List<Transaction>> ExtracFileAsync(CancellationToken cancellationToken)
        {
            var transactions = new List<Transaction>();
            if (!File.Exists(_filePath))
            {
                Console.WriteLine($"File {_filePath} does not exist");
                await StopAsync(cancellationToken);
                return transactions;
            }

            try
            {
                using (var reader = new StreamReader(File.OpenRead(_filePath)))
                {
                    long lineNumber = 0;
                    long rowCount = 0;
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var lineArray = line.Split(",");
                        if (lineArray.Length > 0)
                        {
                            rowCount++;
                            if (rowCount > 1)
                            {
                                lineNumber++;
                                var transaction = new Transaction()
                                {
                                    TransactionId = long.TryParse(lineArray[6], out long id) ? id : throw OnException("TransactionId", $"{lineArray[6]}"),
                                    LineNumber = lineNumber,
                                };

                                double? totalAmount = default(double);  
                                double? daytonaUSDBase = default(double);

                                if (short.TryParse(lineArray[45], out short status) && status == 5000)
                                {
                                    totalAmount = double.TryParse(lineArray[13], NumberStyles.Float, CultureInfo.InvariantCulture, out double total) ? total : null;
                                    daytonaUSDBase = double.TryParse(lineArray[36], NumberStyles.Float, CultureInfo.InvariantCulture, out double dayUsd) ? dayUsd : null;
                                    if(totalAmount != null && daytonaUSDBase != null)
                                    {
                                        transaction.FCDebit = totalAmount * daytonaUSDBase;
                                        transaction.Debit = totalAmount;
                                    }
                                }

                                if ( status == 5005)
                                {
                                    totalAmount = double.TryParse(lineArray[13], NumberStyles.Float, CultureInfo.InvariantCulture, out double total) ? total : null;
                                    daytonaUSDBase = double.TryParse(lineArray[36], NumberStyles.Float, CultureInfo.InvariantCulture, out double dayUsd) ? dayUsd : null;
                                    if (totalAmount != null && daytonaUSDBase != null)
                                    {
                                        transaction.FCCredit = totalAmount * daytonaUSDBase;
                                        transaction.Credit = totalAmount;
                                    }
                                }

                                if (!string.IsNullOrEmpty(lineArray[4]))
                                    transaction.PostDate = DateOnly.FromDateTime(DateTime.Parse((lineArray[4])));

                                transaction.Currency = string.IsNullOrEmpty(lineArray[9]) ? null : lineArray[9];
                                transactions.Add(transaction);
                            }
                            else
                            {
                                lineNumber = 0;
                            }
                        }
                    }
                }

                return transactions;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured error : {ex.ToString()}");
                await StopAsync(cancellationToken).ConfigureAwait(false);
                return transactions;
            }
        }

        async ValueTask AddToDatabaseAsync(List<Transaction> transactions, CancellationToken token)
        {
            var batches = transactions.Batch(_batchSize);
            await Parallel.ForEachAsync(batches, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2, CancellationToken = token }, async (batch, token) =>
            {
                await _repository.AddRangeAsync(batch.ToList());
                _logger.LogInformation($"Saved batch of {batch.Count()} successfully to the database");
            });
        }

        async ValueTask AddToApiAsync(List<Transaction> transactions, CancellationToken token)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string jsonData = JsonSerializer.Serialize(transactions);
                    HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(_apiUrl, content, token);
                    _logger.LogInformation($"Saved batch of {transactions.Count()} successfully to the api");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending data to {_apiUrl}");
            }
        }

        Exception OnException(string key, string value)
        {
            return new Exception($"Invalid {key} {value}");
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }
    }
}
