<template>
  <div class="map-page">
    <h2>台北市道路標記示例</h2>
    <div id="map" ref="mapContainer"></div>
    <button @click="$router.push('/')" class="back-btn">返回首頁</button>
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
      markers: [],
    };
  },
  mounted() {
    this.loadLeaflet(() => {
      console.log('Leaflet 載入完成，初始化地圖');
      this.initializeMap();
      this.loadParkingSpacesFromDB();
      this.addLegend();
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
      script.onerror = () => console.error('Leaflet JS 載入失敗');
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
      this.map = L.map(this.$refs.mapContainer).setView([25.0814, 121.7006], 20);
      L.tileLayer('https://api.mapbox.com/styles/v1/mapbox/streets-v11/tiles/{z}/{x}/{y}?access_token={accessToken}', {
        attribution: '© Mapbox © OpenStreetMap',
        maxZoom: 22,
        tileSize: 512,
        zoomOffset: -1,
        accessToken: 'pk.eyJ1IjoidGh1bW9yIiwiYSI6ImNtODZvNjNuazA2cHYya29tenF0bnl1ZW0ifQ.t3NlPCJ8F1TgXpeh8sV9Ow'
      }).addTo(this.map);
      console.log('地圖底圖已添加');
      // 監聽縮放事件，動態調整圖示大小
      this.map.on('zoomend', () => {
        this.updateMarkerSizes();
      });
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
      console.log('開始更新多邊形與標記');
      this.polygons.forEach(polygon => polygon.remove());
      this.markers.forEach(marker => marker.remove());
      this.polygons = [];
      this.markers = [];

      this.parkingSpaces.forEach(parking => {
        const latLngs = parking.coordinates.map(coord => [coord[1], coord[0]]);
        console.log(`繪製 Parking ID: ${parking.parkingId}, Park Type: ${parking.parkType}`);

        // 動態設置顏色
        let polygonColor = 'blue';
        if (parking.valid === true) {
          polygonColor = 'blue';
        } else {
          polygonColor = 'red';
        }

        const polygon = L.polygon(latLngs, {
          color: polygonColor,
          fillColor: polygonColor,
          fillOpacity: 0.2,
          weight: 2,
        }).addTo(this.map);

        polygon.bindPopup(`
          <b>Parking ID: ${parking.parkingId}</b><br>
          Road: ${parking.road}<br>
          Park Type: ${parking.parkType}
        `);
        this.polygons.push(polygon);

        // 只有 disable 和 charging 添加圖示
        if (parking.parkType === 'disable' || parking.parkType === 'charging') {
          const centerLat = latLngs.reduce((sum, coord) => sum + coord[0], 0) / latLngs.length;
          const centerLng = latLngs.reduce((sum, coord) => sum + coord[1], 0) / latLngs.length;

          let iconUrl;
          if (parking.parkType === 'disable') {
            iconUrl = '/disable.png';
          } else if (parking.parkType === 'charging') {
            iconUrl = '/charging.png';
          }

          const customIcon = L.icon({
            iconUrl: iconUrl,
            iconSize: this.getIconSize(), // 初始大小根據當前縮放級別
            iconAnchor: this.getIconAnchor(), // 錨點隨大小調整
          });

          const marker = L.marker([centerLat, centerLng], { icon: customIcon }).addTo(this.map);
          marker.bindPopup(`
            <b>Parking ID: ${parking.parkingId}</b><br>
            Road: ${parking.road}<br>
            Park Type: ${parking.parkType}
          `);
          this.markers.push(marker);
        }
      });
      console.log('多邊形與標記繪製完成，總數:', this.polygons.length);
      this.updateMarkerSizes(); // 初次載入時調整大小
    },
    getIconSize() {
      const zoom = this.map.getZoom();
      const baseSize = 25; // 基準大小（zoom 20 時）
      const scaleFactor = Math.pow(1.2, zoom - 20); // 隨縮放級別調整比例
      const size = Math.round(baseSize * scaleFactor);
      return [size, size];
    },
    getIconAnchor() {
      const [width, height] = this.getIconSize();
      return [width / 2, height / 2]; // 錨點設為圖示中心
    },
    updateMarkerSizes() {
      this.markers.forEach(marker => {
        const icon = marker.options.icon;
        const newSize = this.getIconSize();
        const newAnchor = this.getIconAnchor();
        icon.options.iconSize = newSize;
        icon.options.iconAnchor = newAnchor;
        marker.setIcon(icon);
      });
      console.log(`縮放級別: ${this.map.getZoom()}, 圖示大小: ${this.getIconSize()}`);
    },
    addLegend() {
      const legend = L.control({ position: 'bottomright' });

      legend.onAdd = () => {
        const div = L.DomUtil.create('div', 'legend');
        div.innerHTML = `
          <h4>停車格圖例</h4>
          <div>
            <img src="/disable.png" width="20" height="20" style="vertical-align:middle">
            <span>身障停車格 (Disable)</span>
          </div>
          <div>
            <img src="/charging.png" width="20" height="20" style="vertical-align:middle">
            <span>充電停車格 (Charging)</span>
          </div>
          <div>
            <span style="display:inline-block;width:20px;height:20px;background:blue;opacity:0.2;vertical-align:middle"></span>
            <span>普通停車格 (Normal)</span>
          </div>
        `;
        return div;
      };

      legend.addTo(this.map);
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
.legend {
  background: white;
  padding: 10px;
  border-radius: 5px;
  box-shadow: 0 0 15px rgba(0,0,0,0.2);
}
.legend h4 {
  margin: 0 0 5px;
}
.legend div {
  margin: 5px 0;
}
</style>