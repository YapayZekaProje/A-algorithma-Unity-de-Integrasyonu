using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;
using Newtonsoft.Json; // Newtonsoft.Json kütüphanesini eklediğinizden emin olun

public class Pathfinding : MonoBehaviour
{
    Grid grid;
    public Transform seeker, target;
    Player player;
    public bool driveable = true;
    private int currentNodeIndex = 0; // Şu anki hedef düğümün indeksi

    private void Awake()
    {
        grid = GetComponent<Grid>();
        grid.InitializeGrid(); // Grid'in oluşturulduğundan emin olun
        player = FindObjectOfType<Player>(); // Oyuncuyu bulun
        PrintGridInfoToFile(); // Grid detaylarını bir dosyaya yazdırın
    }

    private void Update()
    {
        Debug.Log("Update metodu çağrıldı.");
        RequestPathFromServer(seeker.position, target.position);
    }

    // Grid bilgisini bir dosyaya yazdıran metod
    void PrintGridInfoToFile()
    {
        if (grid.grid == null)
        {
            Debug.LogError("Grid henüz başlatılmadı.");
            return;
        }

        string filePath = @"C:\Users\kelvi\Desktop\New folder\YZ\proje\GridInfo.json";
        List<Dictionary<string, object>> gridInfoList = new List<Dictionary<string, object>>();

        float nodeRadius = grid.NodeRadius; // Node yarıçapını alın

        // Grid üzerindeki her bir düğümü dolaşarak bilgilerini kaydedin
        for (int x = 0; x < grid.grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.grid.GetLength(1); y++)
            {
                Node node = grid.grid[x, y];
                var nodeInfo = new Dictionary<string, object>
                {
                    { "gridX", node.gridX },
                    { "gridY", node.gridY },
                    { "worldX", node.WorldPosition.x },
                    { "worldZ", node.WorldPosition.z },
                    { "walkable", node.Walkable }
                };
                gridInfoList.Add(nodeInfo);
            }
        }

        // JSON yapısını oluşturun ve dosyaya yazdırın
        var finalJsonStructure = new
        {
            nodeRadius = nodeRadius,
            gridSizeX = grid.grid.GetLength(0),
            gridSizeY = grid.grid.GetLength(1),
            nodes = gridInfoList
        };

        string json = JsonConvert.SerializeObject(finalJsonStructure, Formatting.Indented);
        File.WriteAllText(filePath, json);
        Debug.Log($"Grid bilgisi şu dosyaya kaydedildi: {filePath}");
    }

    private bool requestSent = false;

    // Server'dan yol isteği gönderen metod
    void RequestPathFromServer(Vector3 startPoz, Vector3 targetPoz)
    {
        if (requestSent)
        {
            Debug.Log("İstek zaten gönderildi, yanıt bekleniyor...");
            return;
        }

        string serverIP = "127.0.0.1"; // Sunucunun IP adresi
        int port = 8089; // Python sunucusuyla eşleşen port numarası

        try
        {
            using (TcpClient client = new TcpClient(serverIP, port))
            using (NetworkStream stream = client.GetStream())
            {
                // Başlangıç ve hedef pozisyonlarını gönderin
                string request = $"{startPoz.x},{startPoz.y},{startPoz.z};{targetPoz.x},{targetPoz.y},{targetPoz.z}";
                Debug.Log($"Sunucuya istek gönderiliyor: {request}");
                byte[] data = Encoding.UTF8.GetBytes(request);
                stream.Write(data, 0, data.Length);

                // Yanıt alın
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Debug.Log($"Sunucudan gelen yanıt: {response}");

                // Yanıtı çözümle ve yolu işle
                if (response != "No path found")
                {
                    ParsePathResponse(response);
                }
            }
        }
        catch (SocketException ex)
        {
            Debug.LogError($"SocketException: {ex.Message}");
        }

        requestSent = true; // Yeni istek gönderilmesini engelle
    }

    // Sunucudan gelen yolu çözümleyen metod
    void ParsePathResponse(string response)
    {
        string[] pathNodes = response.Split(';');
        List<Node> path = new List<Node>();

        foreach (string nodeData in pathNodes)
        {
            if (string.IsNullOrEmpty(nodeData)) continue;

            string[] nodeCoordinates = nodeData.Split(',');
            if (nodeCoordinates.Length == 3)
            {
                // Grid koordinatlarını kullanarak düğümü bulun
                int gridX = int.Parse(nodeCoordinates[0]);
                int gridY = int.Parse(nodeCoordinates[1]);

                Node node = grid.grid[gridX, gridY]; // Grid üzerinden düğümü alın
                if (node != null)
                {
                    path.Add(node);
                }
            }
        }

        // Oyuncuya yolu ayarla
        player.SetPath(path);

        // Yol sırasını yazdır
        PrintPathOrder(path);
    }

    // Yoldaki düğümlerin sırasını yazdıran metod
    void PrintPathOrder(List<Node> path)
    {
        Debug.Log("Yol Sırası:");
        foreach (Node node in path)
        {
            Debug.Log($"Düğüm Grid Pozisyonu: X={node.gridX}, Y={node.gridY}, Yürünebilir={node.Walkable}");
        }
    }
}
