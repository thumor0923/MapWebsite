<template>
  <div class="home">
    <h1>{{ welcomeMessage }}</h1>

    <div class="bulletin-board">
      <h2>公告欄</h2>
      <div class="bulletin-list">
        <div class="bulletin-item" v-for="bulletin in bulletins" :key="bulletin.id">
          <h3>{{ bulletin.title }}</h3>
          <p>{{ bulletin.content }}</p>
        </div>
      </div>
    </div>

    <!-- 新增「下一頁」按鈕，導航到圖表頁面 -->
    <button @click="$router.push('/chart')" class="next-page-btn">下一頁</button>
  </div>
</template>

<script>
import axios from 'axios';

export default {
  name: 'HomePage',
  data() {
    return {
      welcomeMessage: '載入中...',
      bulletins: [],
      pollingIntervalWelcome: null,
      pollingIntervalBulletins: null,
    };
  },
  mounted() {
    this.fetchWelcomeMessage();
    this.fetchBulletins();
    this.pollingIntervalWelcome = setInterval(() => {
      this.fetchWelcomeMessage();
    }, 5000);
    this.pollingIntervalBulletins = setInterval(() => {
      this.fetchBulletins();
    }, 5000);
  },
  beforeUnmount() {
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
    async fetchBulletins() {
      try {
        const response = await axios.get('http://localhost:5158/api/welcome/bulletins');
        this.bulletins = response.data;
        console.log('公告資料:', this.bulletins);
      } catch (error) {
        console.error('無法載入公告:', error);
        this.bulletins = [];
      }
    },
  },
};
</script>

<style scoped>
.home {
  margin-top: 9px;
}
.bulletin-board {
  margin-top: 200px;
}
.bulletin-list {
  max-height: 125px;
  overflow-y: auto;
  border: 1px solid #ccc;
  padding: 10px;
  width: 80%;
  margin: 0 auto;
}
.bulletin-item {
  border-bottom: 1px solid #eee;
  padding: 15px;
  text-align: left;
}
.bulletin-item:last-child {
  border-bottom: none;
}
.bulletin-item h3 {
  margin: 0 0 10px 0;
  font-size: 18px;
}
.bulletin-item p {
  margin: 0;
  font-size: 14px;
}
.next-page-btn {
  margin-top: 20px;
  padding: 10px 20px;
  font-size: 16px;
  background-color: #007bff;
  color: white;
  border: none;
  border-radius: 5px;
  cursor: pointer;
}
.next-page-btn:hover {
  background-color: #0056b3;
}
</style>