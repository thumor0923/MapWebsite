<template>
  <div class="chart-page">
    <h1>公告統計圖</h1>
    <canvas ref="chartCanvas"></canvas>
    <h2>台北市道路標記示例</h2>
    <div id="map" ref="mapContainer"></div>
    <button @click="$router.push('/')" class="back-btn">返回首頁</button>
  </div>
</template>

<script>
import axios from "axios";
import { Chart, registerables } from "chart.js";
import { nextTick } from "vue";
import Papa from "papaparse";

Chart.register(...registerables);

export default {
  name: "ChartPage",
  data() {
    return {
      bulletins: [],
      chartInstance: null,
      map: null,
      roadLines: { 忠孝東路: [] },
    };
  },
  mounted() {
    this.loadLeaflet(() => {
      this.initializeMap();
      this.fetchBulletins();
      setTimeout(() => {
        this.loadLocationsFromDB(); // 使用新方法
      }, 200);
    });
  },
  methods: {
    loadLeaflet(callback) {
      if (typeof L !== "undefined") {
        callback();
        return;
      }
      const script = document.createElement("script");
      script.src = "https://unpkg.com/leaflet/dist/leaflet.js";
      script.onload = () => {
        console.log("Leaflet 已動態載入");
        callback();
      };
      script.onerror = () => {
        console.error("Leaflet CDN 載入失敗");
      };
      document.head.appendChild(script);

      const link = document.createElement("link");
      link.rel = "stylesheet";
      link.href = "https://unpkg.com/leaflet/dist/leaflet.css";
      document.head.appendChild(link);
    },
    async fetchBulletins() {
      try {
        const response = await axios.get(
          "http://localhost:5158/api/welcome/bulletins"
        );
        this.bulletins = response.data;
        console.log("公告資料:", this.bulletins);
        await nextTick();
        this.updateChart();
      } catch (error) {
        console.error("無法載入公告:", error);
        this.bulletins = [];
      }
    },
    updateChart() {
      if (!this.$refs.chartCanvas) {
        console.error("圖表 Canvas 元素未找到！");
        return;
      }
      if (this.chartInstance) {
        this.chartInstance.destroy();
      }
      const ctx = this.$refs.chartCanvas.getContext("2d");
      this.chartInstance = new Chart(ctx, {
        type: "bar",
        data: {
          labels: this.bulletins.map((b) => b.title),
          datasets: [
            {
              label: "公告字數",
              data: this.bulletins.map((b) => b.content.length),
              backgroundColor: "rgba(54, 162, 235, 0.5)",
              borderColor: "rgba(54, 162, 235, 1)",
              borderWidth: 1,
            },
          ],
        },
        options: {
          responsive: true,
          scales: { y: { beginAtZero: true } },
        },
      });
    },
    initializeMap() {
      if (!this.$refs.mapContainer) {
        console.error("地圖容器未找到！");
        return;
      }
      this.map = L.map(this.$refs.mapContainer).setView([25.0416, 121.541], 15);
      L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        attribution: "© OpenStreetMap",
        maxZoom: 18,
      }).addTo(this.map);
    },
    async loadLocationsFromDB() {
      try {
        const response = await axios.get(
          "http://localhost:5158/api/welcome/locations"
        );
        const locations = response.data;

        locations.forEach((row) => {
          const lat = row.latitude; // 直接使用數字格式
          const lon = row.longitude; // 直接使用數字格式
          const name = row.name;
          const road = row.road.trim();

          if (!lat || !lon || !road) return;

          L.circle([lat, lon], {
            color: "red",
            fillColor: "#f03",
            fillOpacity: 0.7,
            radius: 3,
          })
            .addTo(this.map)
            .bindPopup(`<b>${name}</b><br>${road}`);

          if (this.roadLines[road]) {
            this.roadLines[road].push([lat, lon]);
          } else {
            this.roadLines[road] = [[lat, lon]];
          }
        });

        Object.keys(this.roadLines).forEach((road, index) => {
          if (this.roadLines[road].length > 1) {
            L.polyline(this.roadLines[road], {
              color: index === 0 ? "blue" : index === 1 ? "green" : "purple",
              weight: 5,
              opacity: 0.7,
            })
              .addTo(this.map)
              .bindPopup(`<b>${road}</b>`);
          }
        });
      } catch (error) {
        console.error("從資料庫載入位置失敗:", error);
      }
    },
  },
};
</script>

<style scoped>
.chart-page {
  margin-top: 50px;
  height: auto;
  overflow: visible;
  height: 600px;
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
