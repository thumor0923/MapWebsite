
<template>
  <div class="home"> <!-- 頁面的根容器 -->
    <h1>{{ welcomeMessage }}</h1> <!-- 歡迎訊息 -->
    
    <div class="bulletin-board"> <!-- 公告欄的容器 -->
      <h2>公告欄</h2>
      <div class="bulletin-list">
        <div class="bulletin-item" v-for="bulletin in bulletins" :key="bulletin.id"> 
          <h3>{{ bulletin.title }}</h3>
          <p>{{ bulletin.content }}</p>
        </div>
      </div>
    </div>

    <!-- 📊 Chart.js 圖表區塊 -->
    <div class="chart-container">
      <h2>公告統計圖</h2>
      <canvas ref="chartCanvas"></canvas>
    </div>

  </div>
</template>

<script>
import axios from 'axios';
import { Chart, registerables } from 'chart.js';
import { nextTick } from 'vue';

Chart.register(...registerables); // ✅ 註冊 Chart.js 模組



export default {
  name: 'HomePage',
  data() {
    return {
      welcomeMessage: '載入中...',
      bulletins: [], // 儲存所有公告
      pollingIntervalWelcome: null,
      pollingIntervalBulletins: null,
      chartInstance: null, // ✅ 存放 Chart.js 實例
    };
  },
  mounted() {
    // 初次載入時立即獲取數據
    this.fetchWelcomeMessage();
    this.fetchBulletins();
    // 每 5 秒檢查一次資料庫變化
    this.pollingIntervalWelcome = setInterval(() => {
      this.fetchWelcomeMessage();
    }, 5000); // 5000 毫秒 = 5 秒
    this.pollingIntervalBulletins = setInterval(() => {
      this.fetchBulletins();
    }, 5000); // 5000 毫秒 = 5 秒
  },
  beforeUnmount() {
    // 組件銷毀時清除定時器，避免內存洩漏
    clearInterval(this.pollingIntervalWelcome);
    clearInterval(this.pollingIntervalBulletins);
  },
  methods: {
    async fetchWelcomeMessage() {
      try {
        const response = await axios.get('http://localhost:5158/api/welcome');
        this.welcomeMessage = response.data.message;
      } catch (error) {
        this.welcomeMessage = '無法載入歡迎訊息，請稍後再試。';
        console.error(error);
      }
    },
    
  // 從後端 API 獲取公告
  async fetchBulletins() {
      try {
        const response = await axios.get('http://localhost:5158/api/welcome/bulletins');
        this.bulletins = response.data;
        console.log('公告資料:', this.bulletins); // 檢查資料
        await nextTick(); // ✅ 確保 DOM 更新完成後再初始化圖表
        this.updateChart(); // 每次更新公告時更新圖表
      } catch (error) {
        console.error('無法載入公告:', error);
        this.bulletins = [];
      }
    },
    updateChart() {
  if (!this.$refs.chartCanvas) {
    console.error("圖表 Canvas 元素未找到！");
    return;
  }

  if (this.chartInstance) {
    this.chartInstance.destroy(); // ✅ 銷毀舊圖表，避免重複建立
  }

  const ctx = this.$refs.chartCanvas.getContext('2d');
  this.chartInstance = new Chart(ctx, {
    type: 'bar', // 使用柱狀圖
    data: {
      labels: this.bulletins.map(b => b.title), // 公告標題作為 X 軸
      datasets: [{
        label: '公告字數',
        data: this.bulletins.map(b => b.content.length), // 內容長度作為數據
        backgroundColor: 'rgba(54, 162, 235, 0.5)',
        borderColor: 'rgba(54, 162, 235, 1)',
        borderWidth: 1
      }]
    },
    options: {
      responsive: true,
      scales: {
        y: {
          beginAtZero: true
        }
      }
    }
  });
}

  }
};
</script>



<style scoped>
.home {                   /*針對 <div class="home"> 的樣式。*/
  /*text-align: center;*/
  margin-top: 9px;

}
.chart-container{
  margin-top:200px;
}
.bulletin-board {
  margin-top: 200px; /*在歡迎訊息和公告欄之間留的間距。*/
}
.bulletin-list {
  max-height: 125px; /* 限制公告欄高度 */
  overflow-y: auto; /* 啟用垂直滾動條 */
  border: 1px solid #ccc;
  padding: 10px;    /*內部留 10px 空白，讓內容不貼邊。*/
  width: 80%; /* 控制寬度 */
  margin: 0 auto; /* 置中 */
}
.bulletin-item {  /*針對每則公告的樣式。*/
  border-bottom: 1px solid #eee;
  padding: 15px;
  text-align: left;
}
.bulletin-item:last-child {
  border-bottom: none; /* 最後一項無底線 */
}
.bulletin-item h3 {  /*針對每則公告的標題*/
  margin: 0 0 10px 0;
  font-size: 18px;
}
.bulletin-item p {
  margin: 0;
  font-size: 14px;
}
</style>

