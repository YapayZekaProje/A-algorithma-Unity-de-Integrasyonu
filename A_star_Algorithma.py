import json
import heapq
import socket


# Node sınıfı: Grid üzerindeki her bir hücreyi temsil eder
class Node:
    def __init__(self, walkable, world_position, grid_x, grid_y, cost=1):
        self.walkable = walkable  # Hücrenin geçilebilir olup olmadığını belirler
        self.world_position = world_position  # Hücrenin dünya üzerindeki koordinatları
        self.grid_x = grid_x  # Grid üzerindeki X koordinatı
        self.grid_y = grid_y  # Grid üzerindeki Y koordinatı
        self.cost = cost  # Hücre için hareket maliyeti
        self.g_cost = float('inf')  # Başlangıçtan buraya kadar olan maliyet
        self.h_cost = 0  # Tahmini hedef maliyeti (heuristik)
        self.parent = None  # Yol izleme için bir önceki hücre referansı

    @property
    def f_cost(self):
        # Toplam maliyet: g_cost + h_cost
        return self.g_cost + self.h_cost

    def __repr__(self):
        # Node bilgisini okunabilir bir biçimde döndürür
        return (f"Node(walkable={self.walkable}, "
                f"world_position={self.world_position}, "
                f"grid_x={self.grid_x}, grid_y={self.grid_y})")

    def __lt__(self, other):
        # Node'lar arasında kıyaslama yapılırken f_cost kullanılır
        return self.f_cost < other.f_cost


# Grid sınıfı: Tüm harita verisini ve düğümleri tutar
class Grid:
    def __init__(self, grid_data):
        # Grid boyutu ve düğüm yarıçapı gibi bilgileri alır
        self.node_radius = grid_data['nodeRadius']
        self.grid_size_x = grid_data['gridSizeX']
        self.grid_size_y = grid_data['gridSizeY']
        self.grid_world_size = (
            self.grid_size_x * self.node_radius * 2,
            self.grid_size_y * self.node_radius * 2
        )
        # Grid'i düğümlerle doldurur
        self.grid = [[None for _ in range(self.grid_size_y)] for _ in range(self.grid_size_x)]
        for node_data in grid_data['nodes']:
            x, y = node_data['gridX'], node_data['gridY']
            world_pos = (node_data['worldX'], node_data['worldZ'])
            self.grid[x][y] = Node(node_data['walkable'], world_pos, x, y)

    def get_neighbors(self, node):
        # Bir düğümün komşularını döndürür
        neighbors = []
        for x in range(-1, 2):
            for y in range(-1, 2):
                if x == 0 and y == 0:
                    continue  # Kendisi komşu değildir
                if (x == -1 and y == -1) or (x == 1 and y == 1) or (x == 1 and y == -1) or (x == -1 and y == 1):
                    continue  # Çapraz komşular hariç tutulur
                check_x = node.grid_x + x
                check_y = node.grid_y + y

                if 0 <= check_x < self.grid_size_x and 0 <= check_y < self.grid_size_y:
                    neighbors.append(self.grid[check_x][check_y])

        return neighbors

    def node_from_world_point(self, world_position):
        # Dünya koordinatlarından grid üzerindeki düğümü bulur
        percent_x = (world_position[0] + self.grid_world_size[0] / 2) / self.grid_world_size[0]
        percent_y = (world_position[1] + self.grid_world_size[1] / 2) / self.grid_world_size[1]
        percent_x = min(max(percent_x, 0), 1)
        percent_y = min(max(percent_y, 0), 1)
        x = round((self.grid_size_x - 1) * percent_x)
        y = round((self.grid_size_y - 1) * percent_y)
        return self.grid[x][y]

    def print_grid(self):
        # Grid'in ilk birkaç düğümünü yazdırır (örnekleme amaçlı)
        for i in range(min(self.grid_size_x, 2)):
            for j in range(min(self.grid_size_y, 2)):
                node = self.grid[i][j]
                print(f"Node at ({node.grid_x}, {node.grid_y}) -> Position: {node.world_position}, Walkable: {node.walkable}")


# JSON dosyasından grid verilerini okur
def read_grid_from_file(file_path):
    with open(file_path, 'r') as file:
        grid_data = json.load(file)
    return grid_data


# İki düğüm arasındaki Manhattan mesafesini hesaplar
def get_distance(node_a, node_b):
    return abs(node_a.grid_x - node_b.grid_x) + abs(node_a.grid_y - node_b.grid_y)


# Bir düğümden diğerine yolu bulmak için izleme yapar
def retrace_path(start_node, end_node):
    path = []
    current_node = end_node
    while current_node != start_node:
        path.append((current_node.grid_x, current_node.grid_y, 0))
        current_node = current_node.parent
    path.append((start_node.grid_x, start_node.grid_y, 0))
    path.reverse()
    return path


# A* algoritması ile yol bulma işlemi
def a_star_pathfinding(grid, start_pos, target_pos):
    start_node = grid.node_from_world_point((start_pos[0], start_pos[2]))
    target_node = grid.node_from_world_point((target_pos[0], target_pos[2]))

    print("Start Position:", start_pos)
    print("Target Position:", target_pos)
    print("Start Node =", start_node)
    print("Target Node =", target_node)

    if start_node is None or target_node is None:
        print("Hata: Düğüm bulunamadı.")
        return None

    if not start_node.walkable or not target_node.walkable:
        print(f"Düğüm geçersiz! Başlangıç: {start_node.walkable}, Hedef: {target_node.walkable}")
        return None

    open_set = []
    closed_set = set()

    start_node.g_cost = 0
    start_node.h_cost = get_distance(start_node, target_node)

    heapq.heappush(open_set, (start_node.f_cost, start_node))
    while open_set:
        current_node = heapq.heappop(open_set)[1]
        closed_set.add(current_node)

        if current_node == target_node:
            return retrace_path(start_node, target_node), current_node.g_cost

        for neighbor in grid.get_neighbors(current_node):
            if not neighbor.walkable or neighbor in closed_set:
                continue

            tentative_g_cost = current_node.g_cost + neighbor.cost

            if tentative_g_cost < neighbor.g_cost:
                neighbor.parent = current_node
                neighbor.g_cost = tentative_g_cost
                neighbor.h_cost = get_distance(neighbor, target_node)

                if (neighbor.f_cost, neighbor) not in open_set:
                    heapq.heappush(open_set, (neighbor.f_cost, neighbor))

    return None, None


# Gelen pozisyon verilerini işler
def parse_positions(position_data):
    start_pos = (position_data['start_pos']['x'], position_data['start_pos']['y'], position_data['start_pos']['z'])
    target_pos = (position_data['target_pos']['x'], position_data['target_pos']['y'], position_data['target_pos']['z'])
    return start_pos, target_pos


# İstemciden tüm veriyi alır
def receive_all_data(client_socket):
    data = client_socket.recv(4096).decode('utf-8')
    return data


# Gelen veriyi işler ve cevap döndürür
def handle_received_data(data):
    try:
        start_data, target_data = data.split(';')
        start_pos = tuple(map(float, start_data.split(',')))
        target_pos = tuple(map(float, target_data.split(',')))

        file_path = 'GridInfo.json'
        grid_data = read_grid_from_file(file_path)
        grid = Grid(grid_data)

        path, path_cost = a_star_pathfinding(grid, start_pos, target_pos)
        if path:
            print("Path found:", path)
            print("Total Path Cost:", path_cost)
            response = ";".join([f"{x},{y},{z}" for x, y, z in path])
        else:
            response = "Yol bulunamadı."
    except Exception as e:
        response = f"Hata: {str(e)}"
    return response


# Sunucu başlatma
def start_server():
    print("Sunucu başlatılıyor...")
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(("127.0.0.1", 8089))
    print("Sunucu 8089 portunda başlatıldı.")
    server_socket.listen(5)

    while True:
        client_socket, address = server_socket.accept()
        print(f"Bağlantı sağlandı: {address}")

        data = receive_all_data(client_socket)
        print(f"Gelen veri: {data}")
        if not data:
            client_socket.close()
            continue

        response = handle_received_data(data)
        print(f"Cevap: {response}")
        client_socket.sendall(response.encode('utf-8'))
        client_socket.close()


# Main fonksiyonu
if __name__ == "__main__":
    start_server()
