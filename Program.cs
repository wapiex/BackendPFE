var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHttpClient();
//ligneee hedhy maj3oulaa bch te9bell ay requette ahyka allow any origins w allow any methods ww allow any header .
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        // Vous devez remplacer "/swagger/v1/swagger.json" par le chemin approprié 
        // si votre endpoint Swagger est différent.
    });
}

app.UseAuthorization();

app.MapControllers();
//zednnaaaa hedhy bch nkhadmou el CORS w lezmhaa tji binet ligne 41 w 46 .
app.UseCors();
app.Run();
