# Grid Mapping Analysis

Dựa trên hình ảnh `grid_tiles.jpg`, lưới (grid) thực tế có kích thước là **5x6** (5 hàng ngang, 6 cột dọc). Xin lỗi vì đếm nhầm chiều dọc/ngang trước đó. Dưới đây là kết quả Mapping lại mảng 5x6 chuẩn xác nhất vào ma trận số nguyên (int) tương ứng với enum `RoadCellType`:

```csharp
int[,] mapGrid = new int[5, 6] {
    // Cột 0   Cột 1   Cột 2   Cột 3   Cột 4   Cột 5
    {   0,     31,     32,     31,     32,      0   }, // Hàng 0 (Khu Gara ngang: Xe Tím C1-C2, Xe Xanh C3-C4, dùng BusStop_1_N/2_N)
    {   5,      2,      2,      2,      2,      6   }, // Hàng 1 (Đường ngang trên, và góc cua trên)
    {   6,      0,      0,      0,     33,      1   }, // Hàng 2 (C0: Nửa trên của góc cua trái (6), C4: Nhà chờ dọc 1 (BusStop_1_E))
    {   4,      0,      0,      0,     34,      1   }, // Hàng 3 (C0: Nửa dưới của góc cua trái (4), C4: Nhà chờ dọc 2 (BusStop_2_E))
    {   3,      2,      2,      2,      2,      4   }  // Hàng 4 (Đường ngang dưới, xe Vàng đỗ ở góc C5)
};
```

---

## Chi tiết các Cell Type được dùng trong Grid

### `0` - Empty
- **Tên:** Empty
- **Là gì:** Ô đất trống, không có đường.
- **Đặc điểm:** Trong hình, đây là vùng nền trắng ở giữa bản đồ.
- **Làm gì:** Giới hạn không gian di chuyển, có thể được dùng để đặt các mô hình trang trí (Prop) hoặc bãi cỏ trong tương lai. Xe cộ không thể đi vào.

### `1` - Straight_NS (North - South)
- **Tên:** Đường thẳng dọc
- **Là gì:** Đoạn đường nhựa thẳng đứng.
- **Đặc điểm:** Kết nối 2 ô liền kề ở phía Bắc (trên) và Nam (dưới).
- **Làm gì:** Cho phép xe buýt di chuyển thông qua theo trục dọc. Vị trí `(3, 4)` trong ảnh đang chứa một đầu xe màu vàng đỗ trên đoạn đường này.

### `2` - Straight_EW (East - West)
- **Tên:** Đường thẳng ngang
- **Là gì:** Đoạn đường nhựa nằm ngang.
- **Đặc điểm:** Kết nối 2 ô liền kề ở phía Tây (trái) và Đông (phải). 
- **Làm gì:** Cho phép xe buýt di chuyển theo trục ngang. Ở hàng trên cùng `(0, 1)` và `(0, 2)`, vùng đường này đóng vai trò kết nối trực tiếp với các Gara xuất phát của xe buýt tím và xanh lá.

### `3` - Corner_NE (North - East)
- **Tên:** Góc cua B-Đ (Dưới-Trái)
- **Là gì:** Khúc cua phần dưới cùng bên trái của map.
- **Đặc điểm:** Có hình cung, rẽ từ trục Dọc (Bắc) sang ngang (Đông).
- **Làm gì:** Điều hướng xe buýt chuyển góc vuông 90 độ mượt mà từ đường dọc sang đường ngang.

### `4` - Corner_NW (North - West)
- **Tên:** Góc cua B-T (Dưới-Phải)
- **Là gì:** Khúc cua phần dưới cùng bên phải của map.
- **Đặc điểm:** Rẽ từ hướng Đông/Tây sang Bắc.
- **Làm gì:** Khép kín chu trình đường bên góc dưới phải.

### `5` - Corner_SE (South - East)
- **Tên:** Góc cua N-Đ (Trên-Trái)
- **Là gì:** Khúc cua trên cùng bên trái.
- **Đặc điểm:** Cua giữa hướng Nam (xuống) và Đông (phải).
- **Làm gì:** Nối mạch đường từ vành đai ngang bên trên rẽ xuống vành đai dọc bên trái.

### `4` và `6` - Góc cua chữ C bên trái (Corner_NW & Corner_SW)
- **Là gì:** Khúc rẽ trái dài ôm lấy bản đồ dọc theo cột 0.
- **Đặc điểm:** Thay vì dùng 1 ô đường thẳng đứng `1` làm điểm giữa như trước (bị sai do mép ngoài bị uốn cong lồi ra khỏi lưới thẳng), phần "phình to" ở cột 0 kết nối 2 ô Corner để tạo thành biên dạng U-turn (như trong ảnh đánh dấu X). `6` (Corner_SW) nằm trên `4` (Corner_NW) chập lại thành đường cong chữ C cho phần viền ngoài. 

### `31->38` - Cụm Nhóm BusStop và Gara (Nguyên khối theo hướng xoay)
- **Là gì:** Phần cấu hình 2 cell của trạm xe buýt hoặc khu vực gara đỗ chờ. Các mã số được đặt xen kẽ liền kề nhau để dễ dàng ghi nhớ và lập trình:
  - **Hướng Bắc (N):** `31` (Phần 1) và `32` (Phần 2)
  - **Hướng Đông (E):** `33` (Phần 1) và `34` (Phần 2)
  - **Hướng Nam (S):** `35` (Phần 1) và `36` (Phần 2)
  - **Hướng Tây (W):** `37` (Phần 1) và `38` (Phần 2)
- **Làm gì:** Việc gán index đi đôi này giúp map render ra cụm trạm xe liền khối hoàn hảo mà không cần phải gọi hàm truy xuất hay lặp logic xử lý mảng Rotation. Hệ thống prefab tự động mapping theo đúng enum thẳng tiến.
- **Làm gì:** Định vị phần còn lại để hoàn thiện khối Trạm Xe Buýt / Gara xuất phát. (Ở hàng 0 nằm trên cột 2 và 4, còn khi trạm dựng dọc thì nó nằm kế tiếp ở hàng 3 cột 4).
