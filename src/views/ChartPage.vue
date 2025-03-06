<template>
    <!-- 根容器，定義圖表頁面的整體結構 -->
    <div class="chart-page">
      <!-- 頁面標題，顯示 "公告統計圖" -->
      <h1>公告統計圖</h1>
      <!-- 畫布元素，用於渲染 Chart.js 圖表，ref 屬性綁定到 chartCanvas 以供 JavaScript 操作 -->
      <canvas ref="chartCanvas"></canvas>
      <!-- 返回按鈕，點擊時觸發 $router.push('/')，導航回首頁 -->
      <button @click="$router.push('/')" class="back-btn">返回首頁</button>
    </div>
  </template>
  
  <script>
  // 引入 axios，用於發送 HTTP 請求獲取後端資料
  import axios from 'axios';
  // 從 chart.js 引入 Chart 主類和所有可註冊模組（例如柱狀圖、線圖等）
  import { Chart, registerables } from 'chart.js';
  // 引入 nextTick，確保 DOM 更新後執行圖表初始化
  import { nextTick } from 'vue';
  
  // 註冊 Chart.js 的所有模組，使圖表功能可用
  Chart.register(...registerables);
  
  // 定義並匯出 Vue 組件
  export default {
    name: 'ChartPage', // 組件名稱，用於識別和除錯
  
    // 定義組件的資料屬性，返回初始狀態
    data() {
      return {
        bulletins: [], // 儲存從後端獲取的公告資料陣列
        chartInstance: null, // 儲存 Chart.js 圖表實例，用於後續更新或銷毀
      };
    },
  
    // 組件掛載時執行的生命週期鉤子
    mounted() {
      // 組件渲染完成後立即獲取公告資料並初始化圖表
      this.fetchBulletins();
    },
  
    // 定義組件的方法
    methods: {
      // 異步方法，從後端 API 獲取公告資料
      async fetchBulletins() {
        try {
          // 使用 axios 發送 GET 請求到後端 API
          const response = await axios.get('http://localhost:5158/api/welcome/bulletins');
          // 將回應資料存入 bulletins 屬性
          this.bulletins = response.data;
          // 在控制台輸出公告資料，方便除錯
          console.log('公告資料:', this.bulletins);
          // 等待 DOM 更新完成，確保圖表畫布可用
          await nextTick();
          // 呼叫 updateChart 方法更新圖表
          this.updateChart();
        } catch (error) {
          // 捕獲請求失敗的錯誤，輸出到控制台
          console.error('無法載入公告:', error);
          // 如果失敗，清空 bulletins 陣列
          this.bulletins = [];
        }
      },
  
      // 更新或初始化 Chart.js 圖表
      updateChart() {
        // 檢查畫布元素是否存在，若不存在則輸出錯誤並返回
        if (!this.$refs.chartCanvas) {
          console.error("圖表 Canvas 元素未找到！");
          return;
        }
  
        // 如果已有圖表實例，先銷毀以避免重複渲染
        if (this.chartInstance) {
          this.chartInstance.destroy();
        }
  
        // 獲取畫布的 2D 渲染上下文
        const ctx = this.$refs.chartCanvas.getContext('2d');
        // 創建新的 Chart.js 圖表實例
        this.chartInstance = new Chart(ctx, {
          type: 'bar', // 圖表類型：柱狀圖
          data: {
            // X 軸標籤：公告標題
            labels: this.bulletins.map(b => b.title),
            datasets: [{
              label: '公告字數', // 資料集標籤
              // Y 軸數據：公告內容的字數
              data: this.bulletins.map(b => b.content.length),
              backgroundColor: 'rgba(54, 162, 235, 0.5)', // 柱狀圖填充顏色
              borderColor: 'rgba(54, 162, 235, 1)', // 柱狀圖邊框顏色
              borderWidth: 1 // 邊框寬度
            }]
          },
          options: {
            responsive: true, // 圖表隨容器大小自適應
            scales: {
              y: {
                beginAtZero: true // Y 軸從 0 開始
              }
            }
          }
        });
      },
    },
  };
  </script>
  
  <style scoped>
  .chart-page {
    /*text-align: center;  頁面內容置中 */
    margin-top: 50px; /* 與頂部留 50px 間距 */
  }
  .back-btn {
    margin-top: 20px; /* 與圖表留 20px 間距 */
    padding: 10px 20px; /* 內邊距：上下 10px，左右 20px */
    font-size: 16px; /* 字體大小 */
    background-color: #28a745; /* 背景顏色：綠色 */
    color: white; /* 文字顏色：白色 */
    border: none; /* 無邊框 */
    border-radius: 5px; /* 圓角邊框 */
    cursor: pointer; /* 滑鼠懸停時顯示手形 */
  }
  .back-btn:hover {
    background-color: #218838; /* 滑鼠懸停時變為深綠色 */
  }
  </style>