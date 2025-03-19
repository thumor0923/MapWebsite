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
    };
  },
  mounted() {
    // 當組件掛載時執行
    this.loadLeaflet(() => {
      console.log('Leaflet 載入完成，初始化地圖');
      this.initializeMap(); // 初始化地圖
      this.loadParkingSpacesFromDB(); // 從後端載入停車格資料
      this.addLegend(); // 添加圖例
    });
  },
  methods: {
    // 動態載入 Leaflet 庫
    loadLeaflet(callback) {
      // 檢查 Leaflet 是否已經載入
      if (typeof L !== 'undefined') {
        console.log('Leaflet 已存在，直接執行 callback');
        callback(); // 如果已載入，直接執行回調
        return;
      }
      // 動態創建並載入 Leaflet JS 檔案
      const script = document.createElement('script');
      script.src = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.js';
      script.onload = () => {
        console.log('Leaflet JS 已動態載入');
        callback(); // 載入完成後執行回調
      };
      script.onerror = () => console.error('Leaflet JS 載入失敗');
      document.head.appendChild(script); // 將 script 標籤添加到頁面

      // 動態載入 Leaflet CSS 樣式
      const link = document.createElement('link');
      link.rel = 'stylesheet';
      link.href = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css';
      link.onload = () => console.log('Leaflet CSS 已載入');
      link.onerror = () => console.error('Leaflet CSS 載入失敗');
      document.head.appendChild(link); // 將 link 標籤添加到頁面
    },

    // 初始化 Leaflet 地圖
    initializeMap() {
      // 檢查地圖容器是否存在
      if (!this.$refs.mapContainer) {
        console.error('地圖容器未找到！');
        return;
      }
      console.log('初始化地圖，容器存在');
      // 創建地圖實例，設置初始視圖（中心點與縮放級別）
      this.map = L.map(this.$refs.mapContainer).setView([25.0814, 121.7006], 20);
      // 添加 Mapbox 底圖圖層
      L.tileLayer('https://api.mapbox.com/styles/v1/mapbox/streets-v11/tiles/{z}/{x}/{y}?access_token={accessToken}', {
        attribution: '© Mapbox © OpenStreetMap', // 版權聲明
        maxZoom: 22, // 最大縮放級別
        tileSize: 512, // 瓦片大小
        zoomOffset: -1, // 縮放偏移
        accessToken: 'pk.eyJ1IjoidGh1bW9yIiwiYSI6ImNtODZvNjNuazA2cHYya29tenF0bnl1ZW0ifQ.t3NlPCJ8F1TgXpeh8sV9Ow' // Mapbox API 密鑰
      }).addTo(this.map);
      console.log('地圖底圖已添加');

      // 監聽地圖縮放事件，動態調整標記圖示大小
      this.map.on('zoomend', () => {
        this.updateMarkerSizes(); // 每次縮放結束時更新圖示大小
      });
    },

    // 從後端 API 載入停車格資料
    async loadParkingSpacesFromDB() {
      try {
        // 使用 axios 發送 GET 請求
        const response = await axios.get('http://localhost:5158/api/welcome/parkingspaces');
        this.parkingSpaces = response.data; // 將返回的資料存入 parkingSpaces
        console.log('載入的停車格資料:', this.parkingSpaces);
        this.updatePolygons(); // 更新地圖上的多邊形與標記
      } catch (error) {
        console.error('從資料庫載入停車格失敗:', error); // 處理載入失敗的情況
      }
    },

    // 更新地圖上的停車格多邊形與標記
    updatePolygons() {
      console.log('開始更新多邊形與標記');
      // 移除舊的多邊形與標記
      this.polygons.forEach(polygon => polygon.remove());
      this.markers.forEach(marker => marker.remove());
      this.polygons = []; // 清空多邊形陣列
      this.markers = []; // 清空標記陣列

      // 遍歷每筆停車格資料
      this.parkingSpaces.forEach(parking => {
        // 將後端坐標格式 [lng, lat] 轉為 Leaflet 的 [lat, lng]
        const latLngs = parking.coordinates.map(coord => [coord[1], coord[0]]);
        console.log(`繪製 Parking ID: ${parking.parkingId}, Park Type: ${parking.parkType}`);

        // 根據 valid 屬性動態設置多邊形顏色
        let polygonColor = 'blue';
        if (parking.valid === true) {
          polygonColor = 'blue'; // 有效停車格設為藍色
        } else {
          polygonColor = 'red'; // 無效停車格設為紅色
        }

        // 創建並添加停車格多邊形
        const polygon = L.polygon(latLngs, {
          color: polygonColor, // 邊框顏色
          fillColor: polygonColor, // 填充顏色
          fillOpacity: 0.2, // 填充透明度
          weight: 2, // 邊框粗細
        }).addTo(this.map);

        // 綁定彈出視窗，顯示停車格資訊
        polygon.bindPopup(`
          <b>停車格ID: ${parking.parkingId}</b><br>
          道路名稱: ${parking.road}<br>
          停車格類型: ${parking.parkType}<br>
          車位狀況: ${parking.valid}
        `);
        this.polygons.push(polygon); // 將多邊形存入陣列

        // 僅為 disable 和 offload 類型添加圖示標記
        if (parking.parkType === 'disable' || parking.parkType === 'offload') {
          // 計算多邊形中心點作為標記位置
          const centerLat = latLngs.reduce((sum, coord) => sum + coord[0], 0) / latLngs.length;
          const centerLng = latLngs.reduce((sum, coord) => sum + coord[1], 0) / latLngs.length;

          // 根據 parkType 選擇圖示
          let iconUrl;
          if (parking.parkType === 'disable') {
            iconUrl = '/disable.png'; // 身障停車格圖示
          } else if (parking.parkType === 'offload') {
            iconUrl = '/offload.png'; // 充電停車格圖示
          }

          // 創建自訂圖示，動態調整大小與錨點
          const customIcon = L.icon({
            iconUrl: iconUrl,
            iconSize: this.getIconSize(), // 根據縮放級別設置圖示大小
            iconAnchor: this.getIconAnchor(), // 設置圖示錨點
          });

          // 添加標記到地圖
          const marker = L.marker([centerLat, centerLng], { icon: customIcon }).addTo(this.map);
          marker.bindPopup(`
            <b>Parking ID: ${parking.parkingId}</b><br>
            Road: ${parking.road}<br>
            Park Type: ${parking.parkType}
          `);
          this.markers.push(marker); // 將標記存入陣列
        }
      });
      console.log('多邊形與標記繪製完成，總數:', this.polygons.length);
      this.updateMarkerSizes(); // 初次載入時調整圖示大小
    },

    // 根據地圖縮放級別計算圖示大小
    getIconSize() {
      const zoom = this.map.getZoom(); // 獲取當前縮放級別
      const baseSize = 25; // 在 zoom 20 時的基準大小
      const scaleFactor = Math.pow(1.2, zoom - 20); // 計算縮放比例因子
      const size = Math.round(baseSize * scaleFactor); // 計算調整後的大小
      return [size, size]; // 返回寬高陣列
    },

    // 根據圖示大小計算錨點位置
    getIconAnchor() {
      const [width, height] = this.getIconSize(); // 獲取圖示寬高
      return [width / 2, height / 2]; // 將錨點設為圖示中心
    },

    // 更新所有標記的圖示大小
    updateMarkerSizes() {
      this.markers.forEach(marker => {
        const icon = marker.options.icon; // 獲取標記的圖示
        const newSize = this.getIconSize(); // 計算新大小
        const newAnchor = this.getIconAnchor(); // 計算新錨點
        icon.options.iconSize = newSize; // 更新圖示大小
        icon.options.iconAnchor = newAnchor; // 更新錨點
        marker.setIcon(icon); // 應用新圖示
      });
      console.log(`縮放級別: ${this.map.getZoom()}, 圖示大小: ${this.getIconSize()}`); // 除錯訊息
    },

    // 添加地圖右下角的圖例
    addLegend() {
      const legend = L.control({ position: 'bottomright' }); // 創建右下角控制項

      // 定義圖例的內容
      legend.onAdd = () => {
        const div = L.DomUtil.create('div', 'legend'); // 創建圖例容器
        div.innerHTML = `
          <h4>停車格圖例</h4>
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
        `;
        return div; // 返回圖例元素
      };

      legend.addTo(this.map); // 將圖例添加到地圖
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