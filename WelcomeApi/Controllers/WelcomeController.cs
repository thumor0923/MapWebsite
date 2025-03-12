using Microsoft.AspNetCore.Mvc;
using System.IO;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

[Route("api/[controller]")]
[ApiController]
public class WelcomeController : ControllerBase
{
    private readonly IMongoCollection<Bulletin> _bulletinsCollection;
    private readonly IMongoCollection<Location> _locationsCollection;
    private readonly IMongoCollection<ParkingSpace> _parkLocationsCollection;

    public WelcomeController()
    {
        try
        {
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("announcement_db");
            _bulletinsCollection = database.GetCollection<Bulletin>("bulletins");
            _locationsCollection = database.GetCollection<Location>("locations");
            _parkLocationsCollection = database.GetCollection<ParkingSpace>("parklocations");
            database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait();
            Console.WriteLine("MongoDB 連線成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MongoDB 連線失敗: {ex.ToString()}");
            throw;
        }
    }

    [HttpGet]
    public IActionResult GetWelcomeMessage()
    {
        try
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "welcome.txt");
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { message = "歡迎訊息檔案不存在" });
            }
            string message = System.IO.File.ReadAllText(filePath);
            return Ok(new { message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "讀取歡迎訊息時發生錯誤", error = ex.Message });
        }
    }

    [HttpGet("bulletins")]
    public async Task<IActionResult> GetBulletins()
    {
        try
        {
            var rawBulletins = await _bulletinsCollection.Find(_ => true)
                .Project<BsonDocument>(Builders<Bulletin>.Projection.Exclude("_id"))
                .ToListAsync();
            Console.WriteLine($"查詢到的公告數量: {rawBulletins.Count}");
            foreach (var doc in rawBulletins)
            {
                Console.WriteLine(doc.ToJson());
            }
            var bulletins = rawBulletins.Select(doc => new
            {
                id = doc["id"].AsInt32,
                title = doc["title"].AsString,
                content = doc["content"].AsString
            }).ToList();
            return Ok(bulletins);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"查詢失敗: {ex.ToString()}");
            return StatusCode(500, new { message = "讀取公告時發生錯誤", error = ex.ToString() });
        }
    }

    [HttpGet("locations")]
    public async Task<IActionResult> GetLocations()
    {
        try
        {
            var rawLocations = await _locationsCollection.Find(_ => true)
                .Project<BsonDocument>(Builders<Location>.Projection.Exclude("_id"))
                .ToListAsync();
            Console.WriteLine($"查詢到的位置數量: {rawLocations.Count}");
            foreach (var doc in rawLocations)
            {
                Console.WriteLine(doc.ToJson());
            }
            var locations = rawLocations.Select(doc => new
            {
                name = doc["name"].AsString,
                latitude = doc["latitude"].AsDouble,
                longitude = doc["longitude"].AsDouble,
                road = doc["road"].AsString,
                isValid = doc["isValid"].AsBoolean
            }).ToList();
            return Ok(locations);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"查詢位置失敗: {ex.ToString()}");
            return StatusCode(500, new { message = "讀取位置時發生錯誤", error = ex.ToString() });
        }
    }

    [HttpGet("parkingspaces")]
    public async Task<IActionResult> GetParkingSpaces()
    {
        try
        {
            var database = _parkLocationsCollection.Database;
            var collectionNames = await database.ListCollectionNames().ToListAsync();
            Console.WriteLine("可用集合: " + string.Join(", ", collectionNames));

            var rawParkingSpaces = await _parkLocationsCollection.Find(_ => true).ToListAsync();
            Console.WriteLine($"查詢到的停車格數量: {rawParkingSpaces.Count}");
            foreach (var ps in rawParkingSpaces)
            {
                Console.WriteLine($"Parking ID: {ps.ParkingId}, Road: {ps.Road}, Coordinates: {ps.LocationPolygon.Coordinates[0].Length} points");
            }

            var parkingSpaces = rawParkingSpaces.Select(ps => new
            {
                parkingId = ps.ParkingId,
                road = ps.Road,
                coordinates = ps.LocationPolygon.Coordinates[0]
            }).ToList();

            return Ok(parkingSpaces);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"查詢停車格失敗: {ex.ToString()}");
            return StatusCode(500, new { message = "讀取停車格時發生錯誤", error = ex.ToString() });
        }
    }
}

public class Bulletin
{
    [BsonElement("id")]
    public int Id { get; set; }

    [BsonElement("title")]
    public string? Title { get; set; }

    [BsonElement("content")]
    public string? Content { get; set; }

    [BsonIgnore]
    public ObjectId _id { get; set; }
}

public class Location
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("latitude")]
    public double Latitude { get; set; }

    [BsonElement("longitude")]
    public double Longitude { get; set; }

    [BsonElement("road")]
    public string Road { get; set; }

    [BsonElement("isValid")]
    public bool IsValid { get; set; }
}

public class ParkingSpace
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("parking_id")]
    public string ParkingId { get; set; }

    [BsonElement("road")]
    public string Road { get; set; }

    [BsonElement("location")]
    public LocationPolygon LocationPolygon { get; set; }
}

public class LocationPolygon
{
    [BsonElement("type")]
    public string Type { get; set; }

    [BsonElement("coordinates")]
    public double[][][] Coordinates { get; set; }
}