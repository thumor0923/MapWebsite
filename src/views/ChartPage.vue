<template>
  <div class="map-page">
    <h2>台北市道路標記示例</h2>
    <div id="map" ref="mapContainer"></div>
    <button @click="$router.push('/')" class="back-btn">返回首頁</button>
  </div>
</template>

<script>
import axios from 'axios'; // 引入 axios 用於發送 HTTP 請求

export default {
  name: 'MapPage', // 組件名稱
  data() {
    return {
      map: null, // 儲存 Leaflet 地圖實例
      parkingSpaces: [], // 儲存從後端載入的停車格資料
      polygons: [], // 儲存地圖上的停車格多邊形
      markers: [], // 儲存地圖上的圖示標記
      roadLines: [], // 儲存地圖上的道路線條
    };
  },
  async mounted() {
    this.loadLeaflet(async () => {
      console.log('Leaflet 載入完成，初始化地圖');
      this.initializeMap();
      await this.loadParkingSpacesFromDB(); // 等待停車格資料加載完成
      // 載入多條道路並指定顏色
      this.loadRoadPath(
        ['百一街', '福六街'], // 道路名稱列表
        {
          '百一街': 'red', // 百一街用紅色
          '福六街': 'yellow', // 福六街用黃色
        }
      );
      this.addLegend();
    });
  },
  methods: {
    // 動態載入 Leaflet 庫
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

    // 初始化 Leaflet 地圖
    initializeMap() {
      if (!this.$refs.mapContainer) {
        console.error('地圖容器未找到！');
        return;
      }
      console.log('初始化地圖，容器存在');
      // 初始視圖設為一個較廣泛的範圍，後續會動態調整
      this.map = L.map(this.$refs.mapContainer).setView([25.05, 121.5], 12);
      L.tileLayer('https://api.mapbox.com/styles/v1/mapbox/streets-v11/tiles/{z}/{x}/{y}?access_token={accessToken}', {
        attribution: '© Mapbox © OpenStreetMap',
        maxZoom: 22,
        tileSize: 512,
        zoomOffset: -1,
        accessToken: 'pk.eyJ1IjoidGh1bW9yIiwiYSI6ImNtODZvNjNuazA2cHYya29tenF0bnl1ZW0ifQ.t3NlPCJ8F1TgXpeh8sV9Ow'
      }).addTo(this.map);
      console.log('地圖底圖已添加');

      this.map.on('zoomend', () => {
        this.updateMarkerSizes();
      });
    },

    // 從後端 API 載入停車格資料
    async loadParkingSpacesFromDB() {
  try {
    const response = await axios.get('http://localhost:5158/api/welcome/parkingspaces');
    this.parkingSpaces = response.data;
    console.log('載入的停車格資料:', JSON.stringify(this.parkingSpaces, null, 2));
    // 檢查資料格式
    this.parkingSpaces.forEach((parking, index) => {
      console.log(`停車格 ${index}:`, {
        parking_id: parking.parking_id,
        road: parking.road,
        valid: parking.valid,
        parktype: parking.parktype,
        location: parking.location ? JSON.stringify(parking.location) : 'undefined'
      });
    });
    this.updatePolygons();
  } catch (error) {
    console.error('從資料庫載入停車格失敗:', error);
  }
},

    async loadRoadPath(roadNames, colorMap = {}) {
  console.log(`開始載入道路: ${roadNames}`);
  const maxRetries = 3; // 最大重試次數
  let retryCount = 0;

  // 計算每條道路上 valid = true 的停車格數量
  const roadParkingCounts = {};
  roadNames.forEach(name => {
    const roadId = name === '百一街' ? '50' : '60'; // 百一街: 50, 福六街: 60
    const validCount = this.parkingSpaces.filter(
      parking => String(parking.road) === roadId && parking.valid === true
    ).length;
    roadParkingCounts[name] = validCount;
  });
  console.log('各道路可用停車格數量:', roadParkingCounts);

  // 收集所有坐標，用於調整地圖視圖
  const allLatLngs = [];

  while (retryCount < maxRetries) {
    try {
      // 支援多條道路查詢，使用正則表達式匹配名稱
      const roadQuery = roadNames
        .map(name => `way["name"~"${name}"]["highway"](25.0,121.4,25.1,121.7);`)
        .join('');
      const query = `
        [out:json];
        (${roadQuery});
        out geom;
      `;
      const url = retryCount % 2 === 0 
        ? 'https://overpass-api.de/api/interpreter' 
        : 'https://z.overpass-api.de/api/interpreter';
      console.log(`使用端點: ${url}, 第 ${retryCount + 1} 次嘗試`);
      const response = await axios.post(url, query);
      const osmData = response.data.elements;

      console.log('OSM 道路數據:', osmData);

      // 移除舊的道路線條
      this.roadLines.forEach(line => line.remove());
      this.roadLines = [];

      // 如果沒有資料，返回
      if (!osmData || osmData.length === 0) {
        console.warn('未找到任何道路數據');
        return;
      }

      // 遍歷返回的道路數據
      osmData.forEach(way => {
        if (way.type === 'way' && way.geometry) {
          const latLngs = way.geometry.map(point => [point.lat, point.lon]);
          const roadName = way.tags.name;

          // 檢查坐標數據是否有效
          if (latLngs.length === 0 || !latLngs.every(coord => coord[0] && coord[1])) {
            console.warn(`路段 ${way.id} 的坐標數據無效:`, latLngs);
            return;
          }

          // 收集道路坐標
          latLngs.forEach(coord => allLatLngs.push(coord));

          // 根據道路名稱匹配 colorMap
          let lineColor = 'purple'; // 預設顏色
          let matchedRoadName = null;
          for (const name of roadNames) {
            if (roadName.includes(name)) {
              lineColor = colorMap[name] || 'purple';
              matchedRoadName = name;
              break;
            }
          }

          const roadLine = L.polyline(latLngs, {
            color: lineColor,
            weight: 8,
            opacity: 0.8,
          }).addTo(this.map);

          // 綁定點擊事件，顯示可用停車格數量
          if (matchedRoadName) {
            const validCount = roadParkingCounts[matchedRoadName] || 0;
            roadLine.bindPopup(`
              <b>道路名稱: ${roadName}</b><br>
              可用停車格數量: ${validCount}
            `);
          } else {
            roadLine.bindPopup(`<b>道路名稱: ${roadName}</b>`);
          }

          this.roadLines.push(roadLine);
          console.log(`繪製路段: ${way.id}, 名稱: ${roadName}, 顏色: ${lineColor}`);
        }
      });
      console.log('道路繪製完成，總路段數:', this.roadLines.length);

      // 收集停車格的坐標
      this.parkingSpaces.forEach(parking => {
        if (parking.location && parking.location.coordinates && parking.location.coordinates[0]) {
          const latLngs = parking.location.coordinates[0].map(coord => [coord[1], coord[0]]);
          latLngs.forEach(coord => allLatLngs.push(coord));
        }
      });

      // 如果沒有收集到任何坐標，使用預設範圍
      if (allLatLngs.length === 0) {
        console.warn('未收集到任何有效坐標，使用預設視圖範圍');
        this.map.setView([25.08137652, 121.7006359], 18); // 預設聚焦到停車格坐標
      } else {
        const bounds = L.latLngBounds(allLatLngs);
        this.map.fitBounds(bounds, { padding: [50, 50] });
        console.log('地圖視圖已調整到包含所有道路和停車格:', bounds);
      }

      return; // 成功後退出
    } catch (error) {
      console.error(`第 ${retryCount + 1} 次嘗試失敗:`, error);
      retryCount++;
      if (retryCount === maxRetries) {
        console.error('載入道路數據失敗，已達到最大重試次數:', error);
        console.error('查詢語法:', query);
        return;
      }
      // 等待一段時間後重試
      await new Promise(resolve => setTimeout(resolve, 2000 * retryCount)); // 每次重試等待時間增加
    }
  }
},

updatePolygons() {
  console.log('開始更新多邊形與標記');
  this.polygons.forEach(polygon => polygon.remove());
  this.markers.forEach(marker => marker.remove());
  this.polygons = [];
  this.markers = [];

  this.parkingSpaces.forEach((parking, index) => {
    const latLngs = parking.coordinates.map(coord => [coord[1], coord[0]]);
    console.log(`繪製 Parking ID: ${parking.parkingId}, Park Type: ${parking.parkType}`);

    let polygonColor = parking.valid === true ? 'blue' : 'red';
    const polygon = L.polygon(latLngs, {
      color: polygonColor,
      fillColor: polygonColor,
      fillOpacity: 0.2,
      weight: 2,
    }).addTo(this.map);

    polygon.bindPopup(`
      <b>停車格ID: ${parking.parkingId}</b><br>
      道路名稱: ${parking.road}<br>
      停車格類型: ${parking.parkType}<br>
      車位狀況: ${parking.valid}
    `);
    this.polygons.push(polygon);

    // 計算多邊形的中心點
    const centerLat = latLngs.reduce((sum, coord) => sum + coord[0], 0) / latLngs.length;
    const centerLng = latLngs.reduce((sum, coord) => sum + coord[1], 0) / latLngs.length;

    // 如果是身障或卸貨停車格，添加圖示標記
    if (parking.parkType === 'disable' || parking.parkType === 'offload') {
      let iconUrl = parking.parkType === 'disable' ? '/disable.png' : '/offload.png';
      const customIcon = L.icon({
        iconUrl: iconUrl,
        iconSize: this.getIconSize(),
        iconAnchor: this.getIconAnchor(),
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
  this.updateMarkerSizes();
},

    // 根據地圖縮放級別計算圖示大小
    getIconSize() {
      const zoom = this.map.getZoom();
      const baseSize = 25;
      const scaleFactor = Math.pow(1.2, zoom - 20);
      const size = Math.round(baseSize * scaleFactor);
      return [size, size];
    },

    // 根據圖示大小計算錨點位置
    getIconAnchor() {
      const [width, height] = this.getIconSize();
      return [width / 2, height / 2];
    },

    // 更新所有標記的圖示大小
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

    // 添加地圖右下角的圖例
    addLegend() {
      const legend = L.control({ position: 'bottomright' });

      legend.onAdd = () => {
        const div = L.DomUtil.create('div', 'legend');
        div.innerHTML = `
          <h4>地圖圖例</h4>
          <div>
            <img src="/disable.png" width="20" height="20" style="vertical-align:middle">
            <span>身障停車格 (Disable)</span>
          </div>
          <div>
            <img src="/offload.png" width="20" height="20" style="vertical-align:middle">
            <span>卸貨停車格 (Offload)</span>
          </div>
          <div>
            <span style="display:inline-block;width:20px;height:20px;background:blue;opacity:0.2;vertical-align:middle"></span>
            <span>普通停車格 (Normal)</span>
          </div>
          <div>
            <span style="display:inline-block;width:20px;height:20px;background:red;opacity:0.8;vertical-align:middle"></span>
            <span>百一街</span>
          </div>
          <div>
            <span style="display:inline-block;width:20px;height:20px;background:yellow;opacity:0.8;vertical-align:middle"></span>
            <span>福六街</span>
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