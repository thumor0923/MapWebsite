<template>
  <div class="map-page">
    <h2>台北市道路標記示例</h2>
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
      parkingSpaces: [],
      polygons: [],
    };
  },
  mounted() {
    this.loadLeaflet(() => {
      console.log('Leaflet 載入完成，初始化地圖');
      this.initializeMap();
      this.loadParkingSpacesFromDB();
    });
  },
  methods: {
    loadLeaflet(callback) {
      if (typeof L !== 'undefined') {
        console.log('Leaflet 已存在，直接執行 callback');
        callback();
        return;
      }
      const script = document.createElement('script');
      script.src = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.js';
      script.onload = () => {
        console.log('Leaflet JS 已動態載入');
        callback();
      };
      script.onerror = () => {
        console.error('Leaflet JS 載入失敗');
      };
      document.head.appendChild(script);

      const link = document.createElement('link');
      link.rel = 'stylesheet';
      link.href = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css';
      link.onload = () => console.log('Leaflet CSS 已載入');
      link.onerror = () => console.error('Leaflet CSS 載入失敗');
      document.head.appendChild(link);
    },
    initializeMap() {
      if (!this.$refs.mapContainer) {
        console.error('地圖容器未找到！');
        return;
      }
      console.log('初始化地圖，容器存在');
      this.map = L.map(this.$refs.mapContainer).setView([25.0814, 121.7006], 18);
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap',
        maxZoom: 18,
      }).addTo(this.map);
      console.log('地圖底圖已添加');
    },
    async loadParkingSpacesFromDB() {
      try {
        const response = await axios.get('http://localhost:5158/api/welcome/parkingspaces');
        this.parkingSpaces = response.data;
        console.log('載入的停車格資料:', this.parkingSpaces);
        this.updatePolygons();
      } catch (error) {
        console.error('從資料庫載入停車格失敗:', error);
      }
    },
    updatePolygons() {
      console.log('開始更新多邊形');
      this.polygons.forEach(polygon => polygon.remove());
      this.polygons = [];

      this.parkingSpaces.forEach(parking => {
        const latLngs = parking.coordinates.map(coord => [coord[1], coord[0]]);
        console.log(`繪製 Parking ID: ${parking.parkingId}, 座標點數: ${latLngs.length}`);

        const polygon = L.polygon(latLngs, {
          color: 'blue',
          fillColor: 'blue',
          fillOpacity: 0.2,
          weight: 2,
        }).addTo(this.map);

        polygon.bindPopup(`<b>Parking ID: ${parking.parkingId}</b><br>Road: ${parking.road}`);
        this.polygons.push(polygon);
      });
      console.log('多邊形繪製完成，總數:', this.polygons.length);
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