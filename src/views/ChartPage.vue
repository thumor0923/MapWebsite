<template>
  <div class="map-page">
    <h2>台北市道路標記示例</h2>
    <div class="button-group">
      <button :class="{ active: showValid }" @click="toggleValid">
        {{ showValid ? '隱藏 Valid' : '顯示 Valid' }}
      </button>
      <button :class="{ active: showInvalid }" @click="toggleInvalid">
        {{ showInvalid ? '隱藏 Invalid' : '顯示 Invalid' }}
      </button>
    </div>
    <div id="map" ref="mapContainer"></div>
    <button @click="$router.push('/')" class="back-btn">返回首頁</button>
  </div>
</template>

<script>
import axios from 'axios';

export default {
  name: 'MapPage',
  data() {
    return {
      map: null,
      locations: [], // 儲存所有地點資料
      markers: [], // 儲存地圖上的標記
      showValid: false, // 控制 Valid 按鈕狀態
      showInvalid: false, // 控制 Invalid 按鈕狀態
    };
  },
  mounted() {
    this.loadLeaflet(() => {
      this.initializeMap();
      this.loadLocationsFromDB();
    });
  },
  methods: {
    loadLeaflet(callback) {
      if (typeof L !== 'undefined') {
        callback();
        return;
      }
      const script = document.createElement('script');
      script.src = 'https://unpkg.com/leaflet/dist/leaflet.js';
      script.onload = () => {
        console.log('Leaflet 已動態載入');
        callback();
      };
      script.onerror = () => {
        console.error('Leaflet CDN 載入失敗');
      };
      document.head.appendChild(script);

      const link = document.createElement('link');
      link.rel = 'stylesheet';
      link.href = 'https://unpkg.com/leaflet/dist/leaflet.css';
      document.head.appendChild(link);
    },
    initializeMap() {
      if (!this.$refs.mapContainer) {
        console.error('地圖容器未找到！');
        return;
      }
      this.map = L.map(this.$refs.mapContainer).setView([25.0416, 121.5410], 15);
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap',
        maxZoom: 18,
      }).addTo(this.map);
    },
    async loadLocationsFromDB() {
      try {
        const response = await axios.get('http://localhost:5158/api/welcome/locations');
        this.locations = response.data;
        this.updateMarkers();
      } catch (error) {
        console.error('從資料庫載入位置失敗:', error);
      }
    },
    updateMarkers() {
      // 移除現有標記
      this.markers.forEach(marker => marker.remove());
      this.markers = [];

      // 根據按鈕狀態過濾並顯示標記
      this.locations.forEach(location => {
        const lat = location.latitude;
        const lon = location.longitude;
        const name = location.name;
        const road = location.road;
        const isValid = location.isValid;

        if (this.showValid && !this.showInvalid && !isValid) return; // 只顯示 valid
        if (this.showInvalid && !this.showValid && isValid) return; // 只顯示 invalid
        if (!this.showValid && !this.showInvalid) {
          // 兩個按鈕都關閉，顯示所有點
        } else if (this.showValid && this.showInvalid) {
          // 兩個按鈕都開啟，顯示所有點
        }

        const marker = L.circle([lat, lon], {
          color: isValid ? 'blue' : 'red',
          fillColor: isValid ? '#00f' : '#f03',
          fillOpacity: 0.7,
          radius: 50,
        })
          .addTo(this.map)
          .bindPopup(`<b>${name}</b><br>${road}<br>${isValid ? 'Valid' : 'Invalid'}`);

        this.markers.push(marker);
      });
    },
    toggleValid() {
      this.showValid = !this.showValid;
      this.updateMarkers();
    },
    toggleInvalid() {
      this.showInvalid = !this.showInvalid;
      this.updateMarkers();
    },
  },
};
</script>

<style scoped>
.map-page {
  margin-top: 50px;
  height: auto;
  overflow: visible;
}
.button-group {
  margin-bottom: 20px;
}
.button-group button {
  padding: 10px 20px;
  margin-right: 10px;
  font-size: 16px;
  background-color: #007bff;
  color: white;
  border: none;
  border-radius: 5px;
  cursor: pointer;
}
.button-group button.active {
  background-color: #0056b3;
}
.button-group button:hover {
  background-color: #0056b3;
}
#map {
  height: 600px;
  width: 100%;
  margin-top: 20px;
  position: relative;
}
.back-btn {
  margin-top: 20px;
  padding: 10px 20px;
  font-size: 16px;
  background-color: #28a745;
  color: white;
  border: none;
  border-radius: 5px;
  cursor: pointer;
}
.back-btn:hover {
  background-color: #218838;
}
</style>