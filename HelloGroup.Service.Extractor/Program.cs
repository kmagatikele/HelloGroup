using HelloGroup.Repository;
using HelloGroup.Service.Extractor;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton(typeof(ITransactionRepository), typeof(TransactionRepository));


var host = builder.Build();
host.Run();
