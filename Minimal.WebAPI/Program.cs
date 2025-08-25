using Elastic.Clients.Elasticsearch;

namespace Minimal.WebAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSwaggerGen();
        builder.Services.AddEndpointsApiExplorer();

        ElasticsearchClientSettings settings = new(new Uri("http://localhost:9200"));
        settings.DefaultIndex("products");

        ElasticsearchClient client = new ElasticsearchClient(settings);

        client.IndexAsync("products").GetAwaiter().GetResult();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapPost("/products/create", async (CreateProductDTO request,CancellationToken cancellationToken) =>
        {
            Product product = new()
            {
                Name = request.Name,
                Price = request.Price,
                Stock = request.Stock,
                Description = request.Description
            };


            var createRequest = new CreateRequest<Product>(product)
            {
                Index = "products",
                Id = product.Id.ToString() 
            };


            CreateResponse response = await client.CreateAsync(createRequest, cancellationToken);

            return Results.Created($"/products/{product.Id}", new ProductDTO(product.Id, product.Name, product.Price, product.Stock, product.Description));



        });


        app.MapGet("/products/getall", async (CancellationToken cancellationToken) =>
        {

            var response = await client.SearchAsync<Product>(s => s
    .Index("products")
    .Query(q => q.MatchAll()), cancellationToken);

            var products = response.Hits.Select(h => h.Source).ToList(); // _source verisini alýyoruz
            return Results.Ok(products);
        });



        app.Run();
    }


}




public class Product
{
    public Product()
    {
        Id = Guid.NewGuid();
    }
    public Guid Id { get; set; }

    public string Name { get; set; }
    public double Price { get; set; }
    public int Stock { get; set; }
    public string Description { get; set; }
}


public record CreateProductDTO(string Name, double Price, int Stock, string Description);

public record UpdateProductDTO(Guid Id, string Name, double Price, int Stock, string Description);

public record ProductDTO(Guid Id, string Name, double Price, int Stock, string Description);
