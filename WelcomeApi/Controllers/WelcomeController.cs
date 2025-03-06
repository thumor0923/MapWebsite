using Microsoft.AspNetCore.Mvc; // 引入 ASP.NET Core MVC 命名空間，提供控制器和路由功能
using System.IO; // 引入檔案操作命名空間，用於讀取 TXT 檔案
using MongoDB.Driver; // 引入 MongoDB .NET 驅動程式，用於與 MongoDB 互動
using System.Collections.Generic; // 引入集合命名空間，用於處理資料列表
using System.Threading.Tasks; // 引入非同步操作命名空間，支持 async/await
using MongoDB.Bson; // 引入 MongoDB BSON 命名空間，處理 MongoDB 資料格式
using MongoDB.Bson.Serialization.Attributes; // 引入 BSON 序列化屬性，用於映射 MongoDB 欄位

// 定義 API 的路由前綴為 "api/welcome"，[controller] 會被替換為類別名稱（去掉 "Controller"）
[Route("api/[controller]")]
[ApiController] // 標記為 API 控制器，啟用 API 特定功能（如自動模型驗證）
public class WelcomeController : ControllerBase // 繼承 ControllerBase，提供基本的 API 控制器功能
{
    // 定義唯讀欄位，用於存儲 MongoDB 的 bulletins 集合
    private readonly IMongoCollection<Bulletin> _bulletinsCollection;

    // 建構函數，初始化 MongoDB 連線
    public WelcomeController()
    {
        try
        {
            // 建立 MongoDB 客戶端，使用 Atlas 提供的連接字串連接到 announcement_db 資料庫
            var client = new MongoClient("mongodb+srv://thumor:11111@cluster0.ztipc.mongodb.net/announcement_db?retryWrites=true&w=majority");
            // 獲取 announcement_db 資料庫
            var database = client.GetDatabase("announcement_db");
            // 獲取 bulletins 集合，並映射到 Bulletin 類別
            _bulletinsCollection = database.GetCollection<Bulletin>("bulletins");
            // 測試連線是否成功，發送 ping 命令並等待回應
            database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait();
            // 如果連線成功，輸出訊息到控制台
            Console.WriteLine("MongoDB 連線成功");
        }
        catch (Exception ex) // 捕獲連線過程中的任何異常
        {
            // 輸出連線失敗的詳細錯誤訊息到控制台
            Console.WriteLine($"MongoDB 連線失敗: {ex.ToString()}");
            // 拋出異常，終止程式執行
            throw;
        }
    }

    // 定義 GET 請求端點：/api/welcome，獲取歡迎訊息
    [HttpGet]
    public IActionResult GetWelcomeMessage()
    {
        try
        {
            // 組合 welcome.txt 的檔案路徑，使用當前執行目錄
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "welcome.txt");
            // 檢查檔案是否存在
            if (!System.IO.File.Exists(filePath))
            {
                // 如果檔案不存在，返回 404 Not Found 狀態碼和錯誤訊息
                return NotFound(new { message = "歡迎訊息檔案不存在" });
            }
            // 讀取檔案內容作為字串
            string message = System.IO.File.ReadAllText(filePath);
            // 返回 200 OK 狀態碼，並將訊息封裝為 JSON 物件
            return Ok(new { message });
        }
        catch (Exception ex) // 捕獲讀取檔案過程中的任何異常
        {
            // 返回 500 Internal Server Error 狀態碼，並包含錯誤訊息
            return StatusCode(500, new { message = "讀取歡迎訊息時發生錯誤", error = ex.Message });
        }
    }

    // 定義 GET 請求端點：/api/welcome/bulletins，獲取公告列表
    // 使用 BsonDocument 查詢原始資料，避免直接反序列化到 Bulletin 類別
    [HttpGet("bulletins")]
    public async Task<IActionResult> GetBulletins()
    {
        try
        {
            // 查詢 bulletins 集合中的所有文件，並排除 _id 欄位
            // 使用 Project 方法投影為 BsonDocument，避免反序列化問題
            var rawBulletins = await _bulletinsCollection.Find(_ => true)
                .Project<BsonDocument>(Builders<Bulletin>.Projection.Exclude("_id"))
                .ToListAsync();
            // 輸出查詢到的公告數量到控制台
            Console.WriteLine($"查詢到的公告數量: {rawBulletins.Count}");
            // 遍歷每筆原始資料，輸出 JSON 格式到控制台以供除錯
            foreach (var doc in rawBulletins)
            {
                Console.WriteLine(doc.ToJson());
            }
            // 將原始資料轉換為匿名物件列表，明確指定欄位類型
            var bulletins = rawBulletins.Select(doc => new
            {
                id = doc["id"].AsInt32, // 將 id 轉為 Int32
                title = doc["title"].AsString, // 將 title 轉為 string
                content = doc["content"].AsString // 將 content 轉為 string
            }).ToList();
            // 返回 200 OK 狀態碼和公告列表
            return Ok(bulletins);
        }
        catch (Exception ex) // 捕獲查詢過程中的任何異常
        {
            // 輸出查詢失敗的詳細錯誤訊息到控制台
            Console.WriteLine($"查詢失敗: {ex.ToString()}");
            // 返回 500 Internal Server Error 狀態碼，並包含錯誤訊息
            return StatusCode(500, new { message = "讀取公告時發生錯誤", error = ex.ToString() });
        }
    }
}

// 定義公告資料模型，用於映射 MongoDB 資料
public class Bulletin
{
    // 映射 MongoDB 的 "id" 欄位到 Id 屬性，類型為 int
    [BsonElement("id")]
    public int Id { get; set; }
    // 映射 MongoDB 的 "title" 欄位到 Title 屬性，類型為 string，可為 null
    [BsonElement("title")]
    public string? Title { get; set; }
    // 映射 MongoDB 的 "content" 欄位到 Content 屬性，類型為 string，可為 null
    [BsonElement("content")]
    public string? Content { get; set; }
    // 忽略 MongoDB 的 "_id" 欄位，避免與 Id 混淆
    [BsonIgnore]
    public ObjectId _id { get; set; }
}