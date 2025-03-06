import { createRouter, createWebHistory } from 'vue-router';
import Home from '../views/Home.vue'; // 首頁組件
import ChartPage from '../views/ChartPage.vue'; // 圖表頁面組件

const routes = [
  { path: '/', name: 'Home', component: Home }, // 路由：根路徑映射到 Home.vue
  { path: '/chart', name: 'ChartPage', component: ChartPage }, // 路由：/chart 映射到 ChartPage.vue
];

const router = createRouter({
  history: createWebHistory(), // 使用 HTML5 History 模式，提供乾淨的 URL（如 /chart）
  routes, // 傳入路由規則
});

export default router; // 匯出路由實例供應用程式使用