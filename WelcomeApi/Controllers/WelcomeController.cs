using Microsoft.AspNetCore.Mvc;
using System.IO;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

// 定義 API 路由前綴為 "api/welcome"
[Route("api/[controller]")]
[ApiController]
public class WelcomeController : ControllerBase
{
    // 私有唯讀欄位，儲存 MongoDB 的三個集合
    private readonly IMongoCollection<Bulletin> _bulletinsCollection; // 公告集合
    private readonly IMongoCollection<Location> _locationsCollection; // 位置集合
    private readonly IMongoCollection<ParkingSpace> _parkLocationsCollection; // 停車格集合

    // 控制器建構函數，初始化 MongoDB 連線與集合
    public WelcomeController()
    {
        try
        {
            // 建立 MongoDB 客戶端，連接到本機的 MongoDB 服務
            var client = new MongoClient("mongodb://localhost:27017");
            // 獲取名為 "announcement_db" 的資料庫
            var database = client.GetDatabase("announcement_db");
            // 初始化集合，從資料庫中獲取對應的集合
            _bulletinsCollection = database.GetCollection<Bulletin>("bulletins");
            _locationsCollection = database.GetCollection<Location>("locations");
            _parkLocationsCollection = database.GetCollection<ParkingSpace>("parklocations");
            // 執行 ping 命令，測試與 MongoDB 的連線是否成功
            database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait();
            Console.WriteLine("MongoDB 連線成功");
        }
        catch (Exception ex)
        {
            // 連線失敗時記錄錯誤並拋出異常
            Console.WriteLine($"MongoDB 連線失敗: {ex.ToString()}");
            throw;
        }
    }

    // HTTP GET 請求，路由: /api/welcome
    // 從檔案中讀取歡迎訊息並返回
    [HttpGet]
    public IActionResult GetWelcomeMessage()
    {
        try
        {
            // 組合歡迎訊息檔案的完整路徑
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "welcome.txt");
            // 檢查檔案是否存在
            if (!System.IO.File.Exists(filePath))
            {
                // 檔案不存在時返回 404 狀態碼與錯誤訊息
                return NotFound(new { message = "歡迎訊息檔案不存在" });
            }
            // 讀取檔案內容
            string message = System.IO.File.ReadAllText(filePath);
            // 返回 200 狀態碼與訊息
            return Ok(new { message });
        }
        catch (Exception ex)
        {
            // 發生錯誤時返回 500 狀態碼與錯誤詳情
            return StatusCode(500, new { message = "讀取歡迎訊息時發生錯誤", error = ex.Message });
        }
    }

    // HTTP GET 請求，路由: /api/welcome/bulletins
    // 從 MongoDB 查詢所有公告並返回
    [HttpGet("bulletins")]
    public async Task<IActionResult> GetBulletins()
    {
        try
        {
            // 查詢所有公告資料，排除 "_id" 欄位
            var rawBulletins = await _bulletinsCollection.Find(_ => true)
                .Project<BsonDocument>(Builders<Bulletin>.Projection.Exclude("_id"))
                .ToListAsync();
            // 記錄查詢到的公告數量
            Console.WriteLine($"查詢到的公告數量: {rawBulletins.Count}");
            // 遍歷並輸出每筆公告的 JSON 格式（除錯用）
            foreach (var doc in rawBulletins)
            {
                Console.WriteLine(doc.ToJson());
            }
            // 將原始資料轉換為自訂格式，去除 MongoDB 特定的欄位
            var bulletins = rawBulletins.Select(doc => new
            {
                id = doc["id"].AsInt32, // 公告編號
                title = doc["title"].AsString, // 公告標題
                content = doc["content"].AsString // 公告內容
            }).ToList();
            // 返回 200 狀態碼與公告列表
            return Ok(bulletins);
        }
        catch (Exception ex)
        {
            // 查詢失敗時記錄錯誤並返回 500 狀態碼
            Console.WriteLine($"查詢失敗: {ex.ToString()}");
            return StatusCode(500, new { message = "讀取公告時發生錯誤", error = ex.ToString() });
        }
    }

    // HTTP GET 請求，路由: /api/welcome/locations
    // 從 MongoDB 查詢所有位置並返回
    [HttpGet("locations")]
    public async Task<IActionResult> GetLocations()
    {
        try
        {
            // 查詢所有位置資料，排除 "_id" 欄位
            var rawLocations = await _locationsCollection.Find(_ => true)
                .Project<BsonDocument>(Builders<Location>.Projection.Exclude("_id"))
                .ToListAsync();
            // 記錄查詢到的位置數量
            Console.WriteLine($"查詢到的位置數量: {rawLocations.Count}");
            // 遍歷並輸出每筆位置的 JSON 格式（除錯用）
            foreach (var doc in rawLocations)
            {
                Console.WriteLine(doc.ToJson());
            }
            // 將原始資料轉換為自訂格式
            var locations = rawLocations.Select(doc => new
            {
                name = doc["name"].AsString, // 位置名稱
                latitude = doc["latitude"].AsDouble, // 緯度
                longitude = doc["longitude"].AsDouble, // 經度
                road = doc["road"].AsString, // 道路名稱
                isValid = doc["isValid"].AsBoolean // 是否有效
            }).ToList();
            // 返回 200 狀態碼與位置列表
            return Ok(locations);
        }
        catch (Exception ex)
        {
            // 查詢失敗時記錄錯誤並返回 500 狀態碼
            Console.WriteLine($"查詢位置失敗: {ex.ToString()}");
            return StatusCode(500, new { message = "讀取位置時發生錯誤", error = ex.ToString() });
        }
    }

    // HTTP GET 請求，路由: /api/welcome/parkingspaces
    // 從 MongoDB 查詢所有停車格並返回
    [HttpGet("parkingspaces")]
    public async Task<IActionResult> GetParkingSpaces()
    {
        try
        {
            // 查詢所有停車格資料
            var rawParkingSpaces = await _parkLocationsCollection.Find(_ => true).ToListAsync();
            // 記錄查詢到的停車格數量
            Console.WriteLine($"查詢到的停車格數量: {rawParkingSpaces.Count}");
            // 遍歷並輸出每筆停車格的資訊（除錯用）
            foreach (var ps in rawParkingSpaces)
            {
                Console.WriteLine($"Parking ID: {ps.ParkingId}, Road: {ps.Road}, ParkType: {ps.ParkType}, Valid: {ps.Valid}");
            }

            // 將原始資料轉換為自訂格式，包含停車格的多邊形坐標
            var parkingSpaces = rawParkingSpaces.Select(ps => new
            {
                parkingId = ps.ParkingId, // 停車格編號
                road = ps.Road, // 道路名稱
                parkType = ps.ParkType, // 停車格類型（新增）
                valid = ps.Valid, // 是否有效（新增）
                coordinates = ps.LocationPolygon.Coordinates[0] // 多邊形坐標的第一個環（外環）
            }).ToList();

            // 返回 200 狀態碼與停車格列表
            return Ok(parkingSpaces);
        }
        catch (Exception ex)
        {
            // 查詢失敗時記錄錯誤並返回 500 狀態碼
            Console.WriteLine($"查詢停車格失敗: {ex.ToString()}");
            return StatusCode(500, new { message = "讀取停車格時發生錯誤", error = ex.ToString() });
        }
    }
}

// 公告資料模型，對應 MongoDB 中的 "bulletins" 集合
public class Bulletin
{
    [BsonElement("id")] // 映射 MongoDB 文檔中的 "id" 欄位
    public int Id { get; set; } // 公告編號

    [BsonElement("title")] // 映射 "title" 欄位
    public string? Title { get; set; } // 公告標題，可為 null

    [BsonElement("content")] // 映射 "content" 欄位
    public string? Content { get; set; } // 公告內容，可為 null

    [BsonIgnore] // 忽略 "_id" 欄位，不序列化到 MongoDB
    public ObjectId _id { get; set; } // MongoDB 自動生成的 ID
}

// 位置資料模型，對應 MongoDB 中的 "locations" 集合
public class Location
{
    [BsonId] // 指定為 MongoDB 的主鍵
    [BsonRepresentation(BsonType.ObjectId)] // 以 ObjectId 類型儲存
    public string Id { get; set; } // 位置 ID

    [BsonElement("name")] // 映射 "name" 欄位
    public string Name { get; set; } // 位置名稱

    [BsonElement("latitude")] // 映射 "latitude" 欄位
    public double Latitude { get; set; } // 緯度

    [BsonElement("longitude")] // 映射 "longitude" 欄位
    public double Longitude { get; set; } // 經度

    [BsonElement("road")] // 映射 "road" 欄位
    public string Road { get; set; } // 道路名稱

    [BsonElement("isValid")] // 映射 "isValid" 欄位
    public bool IsValid { get; set; } // 是否有效
}

// 停車格資料模型，對應 MongoDB 中的 "parklocations" 集合
public class ParkingSpace
{
    [BsonId] // 指定為 MongoDB 的主鍵
    [BsonRepresentation(BsonType.ObjectId)] // 以 ObjectId 類型儲存
    public string Id { get; set; } // 停車格 ID

    [BsonElement("parking_id")] // 映射 "parking_id" 欄位
    public string ParkingId { get; set; } // 停車格編號

    [BsonElement("road")] // 映射 "road" 欄位
    public string Road { get; set; } // 道路名稱

    [BsonElement("parktype")] // 映射 "parktype" 欄位（新增）
    public string ParkType { get; set; } // 停車格類型

    [BsonElement("valid")] // 映射 "valid" 欄位（新增）
    public bool Valid { get; set; } // 是否有效

    [BsonElement("location")] // 映射 "location" 欄位
    public LocationPolygon LocationPolygon { get; set; } // 停車格的多邊形位置資訊
}

// 停車格位置的多邊形結構
public class LocationPolygon
{
    [BsonElement("type")] // 映射 "type" 欄位
    public string Type { get; set; } // GeoJSON 類型（通常為 "Polygon"）

    [BsonElement("coordinates")] // 映射 "coordinates" 欄位
    public double[][][] Coordinates { get; set; } // 多邊形坐標，三維陣列表示 [環][點][經緯度]
}