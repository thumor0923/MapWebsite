import { createRouter, createWebHistory } from 'vue-router';
import Home from '../views/Home.vue';     //引入一個名為 Home 的 Vue 組件。

const routes = [
  {
    path: '/',         // 當用戶訪問根路徑（例如 http://localhost:3000/）
    name: 'Home',      // 路由的名稱，方便在程式碼中引用
    component: Home,   // 當匹配到這個路徑時，要渲染的組件。
  }
];

const router = createRouter({   //使用 createRouter 函數創建一個路由器實例，並傳入配置物件。
  history: createWebHistory(),
  routes,
});

export default router;